using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess
{
    public class InMemoryToDoRepository : IToDoRepository
    {
        private readonly Dictionary<Guid, List<ToDoItem>> _userTasks = new();
        private readonly Dictionary<Guid, ToDoItem> _tasks = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return _userTasks.TryGetValue(userId, out var tasks)
                    ? tasks.ToList().AsReadOnly()
                    : new List<ToDoItem>().AsReadOnly();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_userTasks.TryGetValue(userId, out var tasks))
                {
                    return tasks
                        .Where(t => t.State == ToDoItemState.Active)
                        .ToList()
                        .AsReadOnly();
                }
                return new List<ToDoItem>().AsReadOnly();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IReadOnlyList<ToDoItem>> GetByUserIdAndListAsync(Guid userId, Guid? listId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_userTasks.TryGetValue(userId, out var tasks))
                {
                    return tasks
                        .Where(t => t.ListId == listId)
                        .ToList()
                        .AsReadOnly();
                }
                return new List<ToDoItem>().AsReadOnly();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<ToDoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return _tasks.TryGetValue(id, out var task) ? task : null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task AddAsync(ToDoItem item, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_userTasks.ContainsKey(item.UserId))
                {
                    _userTasks[item.UserId] = new List<ToDoItem>();
                }

                _userTasks[item.UserId].Add(item);
                _tasks[item.Id] = item;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task UpdateAsync(ToDoItem item, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_tasks.ContainsKey(item.Id))
                {
                    _tasks[item.Id] = item;

                    // Обновляем также в списке пользователя
                    if (_userTasks.TryGetValue(item.UserId, out var userTasks))
                    {
                        var index = userTasks.FindIndex(t => t.Id == item.Id);
                        if (index >= 0)
                        {
                            userTasks[index] = item;
                        }
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_tasks.TryGetValue(id, out var task))
                {
                    _tasks.Remove(id);

                    if (_userTasks.TryGetValue(task.UserId, out var userTasks))
                    {
                        userTasks.RemoveAll(t => t.Id == id);

                        // Удаляем пустой список пользователя
                        if (userTasks.Count == 0)
                        {
                            _userTasks.Remove(task.UserId);
                        }
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_userTasks.TryGetValue(userId, out var tasks))
                {
                    return tasks.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                }
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_userTasks.TryGetValue(userId, out var tasks))
                {
                    return tasks.Count(t => t.State == ToDoItemState.Active);
                }
                return 0;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_userTasks.TryGetValue(userId, out var tasks))
                {
                    return tasks.Where(predicate).ToList().AsReadOnly();
                }
                return new List<ToDoItem>().AsReadOnly();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(Guid userId, DateTime from, DateTime to, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                if (_userTasks.TryGetValue(userId, out var tasks))
                {
                    return tasks
                        .Where(t => t.State == ToDoItemState.Active && 
                                    t.Deadline >= from && 
                                    t.Deadline < to)
                        .ToList()
                        .AsReadOnly();
                }
                return new List<ToDoItem>().AsReadOnly();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // Дополнительный метод для очистки (не из интерфейса, но полезен)
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _userTasks.Clear();
                _tasks.Clear();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}