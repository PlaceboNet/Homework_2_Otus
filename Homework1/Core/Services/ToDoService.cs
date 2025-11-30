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
            Console.WriteLine($"=== ToDoService.AddAsync ===");
            Console.WriteLine($"User: {user?.TelegramUserName}, Id: {user?.Id}");
            Console.WriteLine($"Task name: {name}");

            if (user == null)
            {
                Console.WriteLine("ОШИБКА: пользователь null!");
                throw new ArgumentNullException(nameof(user));
            }

            // Проверка максимального количества задач
            var userTasksCount = await _toDoRepository.CountActiveAsync(user.Id, cancellationToken);
            Console.WriteLine($"Текущее количество задач пользователя: {userTasksCount}");
            Console.WriteLine($"Максимальное количество: {_maxTaskCount}");

            if (userTasksCount >= _maxTaskCount)
            {
                throw new TaskCountLimitException(_maxTaskCount);
            }

            // Проверка длины задачи
            Console.WriteLine($"Длина задачи: {name.Length}, максимальная: {_maxTaskLength}");
            if (name.Length > _maxTaskLength)
            {
                throw new TaskLengthLimitException(name.Length, _maxTaskLength);
            }

            // Проверка на дубликаты
            bool exists = await _toDoRepository.ExistsByNameAsync(user.Id, name, cancellationToken);
            Console.WriteLine($"Задача с таким именем уже существует: {exists}");

            if (exists)
            {
                throw new DuplicateTaskException(name);
            }

            Console.WriteLine("Создаем новую задачу...");
            var newTask = new ToDoItem(user, name);

            Console.WriteLine("Вызываем репозиторий для сохранения...");
            await _toDoRepository.AddAsync(newTask, cancellationToken);
            Console.WriteLine("Задача сохранена в репозитории");

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
