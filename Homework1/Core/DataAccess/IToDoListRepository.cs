using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Homework1.Core.DataAccess
{
    public interface IToDoListRepository
    {
        Task<ToDoList?> Get(Guid id, CancellationToken ct);
        Task<IReadOnlyList<ToDoList>> GetByUserId(Guid userId, CancellationToken ct);
        Task Add(ToDoList list, CancellationToken ct);
        Task Delete(Guid id, CancellationToken ct);
        Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct);
    }

    // ./Infrastructure/DataAccess/FileToDoListRepository.cs
    public class FileToDoListRepository : IToDoListRepository
    {
        private readonly string _basePath;

        public FileToDoListRepository(string basePath)
        {
            _basePath = basePath;
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
        {
            var filePath = GetListFilePath(id);
            if (!File.Exists(filePath))
                return null;

            return await ReadListFromFileAsync(filePath, ct);
        }

        public async Task<IReadOnlyList<ToDoList>> GetByUserId(Guid userId, CancellationToken ct)
        {
            if (!Directory.Exists(_basePath))
                return new List<ToDoList>();

            var lists = new List<ToDoList>();
            var files = Directory.GetFiles(_basePath, "*.json");

            foreach (var file in files)
            {
                var list = await ReadListFromFileAsync(file, ct);
                if (list != null && list.UserId == userId)
                {
                    lists.Add(list);
                }
            }

            return lists;
        }

        public async Task Add(ToDoList list, CancellationToken ct)
        {
            var filePath = GetListFilePath(list.Id);
            await WriteListToFileAsync(filePath, list, ct);
        }

        public Task Delete(Guid id, CancellationToken ct)
        {
            var filePath = GetListFilePath(id);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
        {
            var lists = await GetByUserId(userId, ct);
            return lists.Any(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private string GetListFilePath(Guid id)
        {
            return Path.Combine(_basePath, $"{id}.json");
        }

        private async Task<ToDoList?> ReadListFromFileAsync(string filePath, CancellationToken ct)
        {
            try
            {
                await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return await JsonSerializer.DeserializeAsync<ToDoList>(fileStream, cancellationToken: ct);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task WriteListToFileAsync(string filePath, ToDoList list, CancellationToken ct)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(fileStream, list, options, ct);
        }
    }
}
