using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Core.DataAccess
{
    public interface IToDoRepository
    {
        Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<ToDoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(ToDoItem item, CancellationToken cancellationToken = default);
        Task UpdateAsync(ToDoItem item, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);
        Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ToDoItem>> GetByUserIdAndListAsync(Guid userId, Guid? listId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(Guid userId, DateTime from, DateTime to, CancellationToken ct);
    }
}
