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
        private readonly List<ToDoItem> _tasks = new List<ToDoItem>();

        public Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<ToDoItem>)_tasks.Where(t => t.UserId == userId).ToList());
        }

        public Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<ToDoItem>)_tasks.Where(t => t.UserId == userId && t.State == ToDoItemState.Active).ToList());
        }

        public Task<ToDoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id));
        }

        public Task AddAsync(ToDoItem item, CancellationToken cancellationToken = default)
        {
            _tasks.Add(item);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ToDoItem item, CancellationToken cancellationToken = default)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == item.Id);
            if (existing != null)
            {
                _tasks.Remove(existing);
                _tasks.Add(item);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                _tasks.Remove(task);
            }
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tasks.Any(t => t.UserId == userId &&
                                  t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tasks.Count(t => t.UserId == userId && t.State == ToDoItemState.Active));
        }

        public Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<ToDoItem>)_tasks.Where(t => t.UserId == userId).Where(predicate).ToList());
        }
    }
}
