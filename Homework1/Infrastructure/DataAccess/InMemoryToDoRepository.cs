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

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
        {
            return _tasks.Where(t => t.UserId == userId).ToList();
        }

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
        {
            return _tasks.Where(t => t.UserId == userId && t.State == ToDoItemState.Active).ToList();
        }

        public ToDoItem? Get(Guid id)
        {
            return _tasks.FirstOrDefault(t => t.Id == id);
        }

        public void Add(ToDoItem item)
        {
            _tasks.Add(item);
        }

        public void Update(ToDoItem item)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == item.Id);
            if (existing != null)
            {
                _tasks.Remove(existing);
                _tasks.Add(item);
            }
        }

        public void Delete(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                _tasks.Remove(task);
            }
        }

        public bool ExistsByName(Guid userId, string name)
        {
            return _tasks.Any(t => t.UserId == userId &&
                                  t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public int CountActive(Guid userId)
        {
            return _tasks.Count(t => t.UserId == userId && t.State == ToDoItemState.Active);
        }

        public IReadOnlyList<ToDoItem> Find(Guid userId, Func<ToDoItem, bool> predicate)
        {
            return _tasks.Where(t => t.UserId == userId).Where(predicate).ToList();
        }
    }
}
