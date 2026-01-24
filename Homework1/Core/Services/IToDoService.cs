using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Homework1.Core.Entities;
using Telegram.Bot;

namespace Homework1.Core.Services
{
    public interface IToDoService
    {
        Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime? deadline, ToDoList? list, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> GetByUserIdAndListAsync(Guid userId, Guid? listId, CancellationToken cancellationToken);
        Task<ToDoItem?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken);
        Task MarkCompletedAsync(Guid taskId, CancellationToken cancellationToken);
        Task DeleteAsync(Guid taskId, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken cancellationToken);
        Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken);
        Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken);
        Task<ToDoItem?> GetAsync(Guid toDoItemId, CancellationToken ct);
    }
}
