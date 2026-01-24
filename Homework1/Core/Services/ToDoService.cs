using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using Homework1.Core.Exceptions;
using Telegram.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Homework1.Core.Entities.ToDoItem;

namespace Homework1.Core.Services
{
    public class ToDoService : IToDoService
    {
        private readonly IToDoRepository _repository;
        private readonly int _maxTasks;
        private readonly int _maxTaskLength;

        public ToDoService(IToDoRepository repository, int maxTasks, int maxTaskLength)
        {
            _repository = repository;
            _maxTasks = maxTasks;
            _maxTaskLength = maxTaskLength;
        }

        // ВАЖНО: Этот метод должен возвращать Task<ToDoItem>
        public async Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime? deadline, ToDoList? list, CancellationToken cancellationToken)
        {
            var activeTasks = await _repository.GetActiveByUserIdAsync(user.Id, cancellationToken);

            if (activeTasks.Count >= _maxTasks)
                throw new TaskCountLimitException(_maxTasks);

            if (name.Length > _maxTaskLength)
                throw new TaskLengthLimitException(name.Length, _maxTaskLength);

            var exists = await _repository.ExistsByNameAsync(user.Id, name, cancellationToken);
            if (exists)
                throw new DuplicateTaskException(name);

            var item = new ToDoItem(user, name, deadline, list);

            await _repository.AddAsync(item, cancellationToken);
            return item;
        }
        public async Task<ToDoItem?> GetAsync(Guid toDoItemId, CancellationToken ct)
        {
            return await _repository.GetAsync(toDoItemId, ct);
        }

        public Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => _repository.GetAllByUserIdAsync(userId, cancellationToken);

        public Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => _repository.GetActiveByUserIdAsync(userId, cancellationToken);

        public Task<IReadOnlyList<ToDoItem>> GetByUserIdAndListAsync(Guid userId, Guid? listId, CancellationToken cancellationToken)
            => _repository.GetByUserIdAndListAsync(userId, listId, cancellationToken);

        public Task<ToDoItem?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken)
            => _repository.GetAsync(taskId, cancellationToken);

        public async Task MarkCompletedAsync(Guid taskId, CancellationToken cancellationToken)
        {
            var task = await GetByIdAsync(taskId, cancellationToken);
            if (task == null) throw new InvalidOperationException($"Задача с ID {taskId} не найдена");
            task.Complete();
            await _repository.UpdateAsync(task, cancellationToken);
        }

        public async Task DeleteAsync(Guid taskId, CancellationToken cancellationToken)
        {
            var task = await GetByIdAsync(taskId, cancellationToken);
            if (task == null) throw new InvalidOperationException($"Задача с ID {taskId} не найдена");
            await _repository.DeleteAsync(taskId, cancellationToken);
        }

        public async Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken cancellationToken)
            => await _repository.FindAsync(user.Id, t => t.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase), cancellationToken);

        public Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken)
            => _repository.ExistsByNameAsync(userId, name, cancellationToken);

        public Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken)
            => _repository.CountActiveAsync(userId, cancellationToken);
    }
}
