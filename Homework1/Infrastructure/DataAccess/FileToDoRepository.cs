using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess
{
    public class FileToDoRepository : IToDoRepository
    {
        private readonly string _basePath;
        private readonly string _indexFilePath;
        private readonly SemaphoreSlim _indexSemaphore = new SemaphoreSlim(1, 1);
        private readonly Dictionary<Guid, Guid> _itemIndex = new Dictionary<Guid, Guid>();
        private bool _isIndexLoaded = false;

        public FileToDoRepository(string basePath)
        {
            _basePath = basePath;
            _indexFilePath = Path.Combine(_basePath, "index.json");

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }

            // Загружаем индекс при инициализации
            LoadIndexAsync().GetAwaiter().GetResult();
        }

        private async Task LoadIndexIfNeededAsync(CancellationToken cancellationToken = default)
        {
            if (!_isIndexLoaded)
            {
                await LoadIndexAsync();
                _isIndexLoaded = true;
            }
        }

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await LoadIndexIfNeededAsync(cancellationToken);

            var userFolder = GetUserFolderPath(userId);
            if (!Directory.Exists(userFolder))
                return new List<ToDoItem>();

            var tasks = new List<ToDoItem>();
            var files = Directory.GetFiles(userFolder, "*.json");

            foreach (var file in files)
            {
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var taskId))
                {
                    var task = await ReadTaskFromFileAsync(file, cancellationToken);
                    if (task != null)
                    {
                        tasks.Add(task);
                    }
                }
            }

            return tasks;
        }

        public async Task<IReadOnlyList<ToDoItem>> GetByUserIdAndListAsync(Guid userId, Guid? listId, CancellationToken cancellationToken = default)
        {
            await LoadIndexIfNeededAsync(cancellationToken);

            var userFolder = GetUserFolderPath(userId);
            if (!Directory.Exists(userFolder))
                return new List<ToDoItem>();

            var tasks = new List<ToDoItem>();
            var files = Directory.GetFiles(userFolder, "*.json");

            foreach (var file in files)
            {
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var taskId))
                {
                    var task = await ReadTaskFromFileAsync(file, cancellationToken);
                    if (task != null && task.UserId == userId && task.ListId == listId)
                    {
                        tasks.Add(task);
                    }
                }
            }

            return tasks;
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var allTasks = await GetAllByUserIdAsync(userId, cancellationToken);
            return allTasks.Where(t => t.State == ToDoItemState.Active).ToList();
        }

        public async Task<ToDoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await LoadIndexIfNeededAsync(cancellationToken);
            await _indexSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_itemIndex.TryGetValue(id, out var userId))
                {
                    var filePath = GetTaskFilePath(userId, id);
                    if (File.Exists(filePath))
                    {
                        return await ReadTaskFromFileAsync(filePath, cancellationToken);
                    }
                }
                return null;
            }
            finally
            {
                _indexSemaphore.Release();
            }
        }

        public async Task AddAsync(ToDoItem item, CancellationToken cancellationToken = default)
        {
            await LoadIndexIfNeededAsync(cancellationToken);
            await _indexSemaphore.WaitAsync(cancellationToken);
            try
            {
                var userFolder = GetUserFolderPath(item.UserId);
                if (!Directory.Exists(userFolder))
                {
                    Directory.CreateDirectory(userFolder);
                }

                var filePath = GetTaskFilePath(item.UserId, item.Id);
                await WriteTaskToFileAsync(filePath, item, cancellationToken);

                _itemIndex[item.Id] = item.UserId;
                await SaveIndexAsync(cancellationToken);
            }
            finally
            {
                _indexSemaphore.Release();
            }
        }

        public async Task UpdateAsync(ToDoItem item, CancellationToken cancellationToken = default)
        {
            await LoadIndexIfNeededAsync(cancellationToken);
            await _indexSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_itemIndex.TryGetValue(item.Id, out var userId))
                {
                    var filePath = GetTaskFilePath(userId, item.Id);
                    if (File.Exists(filePath))
                    {
                        await WriteTaskToFileAsync(filePath, item, cancellationToken);
                    }
                }
            }
            finally
            {
                _indexSemaphore.Release();
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await LoadIndexIfNeededAsync(cancellationToken);
            await _indexSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_itemIndex.TryGetValue(id, out var userId))
                {
                    var filePath = GetTaskFilePath(userId, id);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    _itemIndex.Remove(id);
                    await SaveIndexAsync(cancellationToken);
                }
            }
            finally
            {
                _indexSemaphore.Release();
            }
        }

        public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            var tasks = await GetAllByUserIdAsync(userId, cancellationToken);
            return tasks.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var tasks = await GetAllByUserIdAsync(userId, cancellationToken);
            return tasks.Count(t => t.State == ToDoItemState.Active);
        }

        public async Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken cancellationToken = default)
        {
            var tasks = await GetAllByUserIdAsync(userId, cancellationToken);
            return tasks.Where(predicate).ToList();
        }

        private string GetUserFolderPath(Guid userId)
        {
            return Path.Combine(_basePath, userId.ToString());
        }

        private string GetTaskFilePath(Guid userId, Guid taskId)
        {
            var userFolder = GetUserFolderPath(userId);
            return Path.Combine(userFolder, $"{taskId}.json");
        }

        private async Task<ToDoItem?> ReadTaskFromFileAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return await JsonSerializer.DeserializeAsync<ToDoItem>(fileStream, options, cancellationToken);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task WriteTaskToFileAsync(string filePath, ToDoItem item, CancellationToken cancellationToken)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(fileStream, item, options, cancellationToken);
        }

        private async Task LoadIndexAsync()
        {
            if (!File.Exists(_indexFilePath))
            {
                await RebuildIndexAsync();
                return;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                await using var fileStream = new FileStream(_indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var indexData = await JsonSerializer.DeserializeAsync<Dictionary<Guid, Guid>>(fileStream, options);
                _itemIndex.Clear();
                if (indexData != null)
                {
                    foreach (var kvp in indexData)
                    {
                        _itemIndex[kvp.Key] = kvp.Value;
                    }
                }
                _isIndexLoaded = true;
            }
            catch (Exception)
            {
                await RebuildIndexAsync();
            }
        }


        private async Task SaveIndexAsync(CancellationToken cancellationToken)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            await using var fileStream = new FileStream(_indexFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(fileStream, _itemIndex, options, cancellationToken);
        }

        private async Task RebuildIndexAsync()
        {
            _itemIndex.Clear();

            if (!Directory.Exists(_basePath))
                return;

            var userFolders = Directory.GetDirectories(_basePath);
            foreach (var userFolder in userFolders)
            {
                if (Guid.TryParse(Path.GetFileName(userFolder), out var userId))
                {
                    var taskFiles = Directory.GetFiles(userFolder, "*.json");
                    foreach (var taskFile in taskFiles)
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(taskFile), out var taskId))
                        {
                            _itemIndex[taskId] = userId;
                        }
                    }
                }
            }

            await SaveIndexAsync(CancellationToken.None);
            _isIndexLoaded = true;
        }
    }
}
