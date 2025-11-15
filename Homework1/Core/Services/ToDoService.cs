using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using Homework1.Core.Exceptions;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;
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
        private readonly IToDoRepository _toDoRepository;
        private readonly int _maxTaskCount;
        private readonly int _maxTaskLength;

        public ToDoService(IToDoRepository toDoRepository, int maxTaskCount, int maxTaskLength)
        {
            _toDoRepository = toDoRepository;
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
        }

        public Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _toDoRepository.GetAllByUserIdAsync(userId, cancellationToken);
        }

        public Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _toDoRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        }

        public async Task<ToDoItem> AddAsync(ToDoUser user, string name, CancellationToken cancellationToken = default)
        {
            // Проверка максимального количества задач
            var userTasksCount = await _toDoRepository.CountActiveAsync(user.Id, cancellationToken);
            if (userTasksCount >= _maxTaskCount)
            {
                throw new TaskCountLimitException(_maxTaskCount);
            }

            // Проверка длины задачи
            if (name.Length > _maxTaskLength)
            {
                throw new TaskLengthLimitException(name.Length, _maxTaskLength);
            }

            // Проверка на дубликаты
            if (await _toDoRepository.ExistsByNameAsync(user.Id, name, cancellationToken))
            {
                throw new DuplicateTaskException(name);
            }

            var newTask = new ToDoItem(user, name);
            await _toDoRepository.AddAsync(newTask, cancellationToken);
            return newTask;
        }

        public async Task MarkCompletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var task = await _toDoRepository.GetAsync(id, cancellationToken);
            if (task == null)
            {
                throw new ArgumentException($"Задача с ID {id} не найдена.");
            }

            if (task.State == ToDoItemState.Completed)
            {
                throw new InvalidOperationException("Задача уже завершена.");
            }

            task.Complete();
            await _toDoRepository.UpdateAsync(task, cancellationToken);
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _toDoRepository.DeleteAsync(id, cancellationToken);
        }

        public Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken cancellationToken = default)
        {
            return _toDoRepository.FindAsync(user.Id, task =>
                task.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase), cancellationToken);
        }
    }
}
