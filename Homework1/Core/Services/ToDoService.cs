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

        public async Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime? deadline, CancellationToken cancellationToken)
        {
            var activeTasks = await _repository.GetActiveByUserIdAsync(user.Id, cancellationToken);

            if (activeTasks.Count >= _maxTasks)
            {
                throw new InvalidOperationException($"Превышено максимальное количество задач: {_maxTasks}");
            }

            if (name.Length > _maxTaskLength)
            {
                throw new InvalidOperationException($"Превышена максимальная длина задачи: {_maxTaskLength}");
            }

            var exists = await _repository.ExistsByNameAsync(user.Id, name, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"Задача с именем '{name}' уже существует");
            }

            var item = new ToDoItem(user, name, deadline);
            await _repository.AddAsync(item, cancellationToken);
            return item;
        }

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _repository.GetAllByUserIdAsync(userId, cancellationToken);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _repository.GetActiveByUserIdAsync(userId, cancellationToken);
        }

        public async Task<ToDoItem?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken)
        {
            return await _repository.GetAsync(taskId, cancellationToken);
        }

        public async Task MarkCompletedAsync(Guid taskId, CancellationToken cancellationToken)
        {
            var task = await _repository.GetAsync(taskId, cancellationToken);
            if (task == null)
            {
                throw new InvalidOperationException($"Задача с ID {taskId} не найдена");
            }

            task.Complete();
            await _repository.UpdateAsync(task, cancellationToken);
        }

        public async Task DeleteAsync(Guid taskId, CancellationToken cancellationToken)
        {
            await _repository.DeleteAsync(taskId, cancellationToken);
        }

        public async Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken cancellationToken)
        {
            return await _repository.FindAsync(user.Id, t =>
                t.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase),
                cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken)
        {
            return await _repository.ExistsByNameAsync(userId, name, cancellationToken);
        }

        public async Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _repository.CountActiveAsync(userId, cancellationToken);
        }
    }
}
