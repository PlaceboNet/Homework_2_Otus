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

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
        {
            return _toDoRepository.GetAllByUserId(userId);
        }

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
        {
            return _toDoRepository.GetActiveByUserId(userId);
        }

        public ToDoItem Add(ToDoUser user, string name)
        {
            // Проверка максимального количества задач
            var userTasksCount = _toDoRepository.CountActive(user.Id);
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
            if (_toDoRepository.ExistsByName(user.Id, name))
            {
                throw new DuplicateTaskException(name);
            }

            var newTask = new ToDoItem(user, name);
            _toDoRepository.Add(newTask);
            return newTask;
        }

        public void MarkCompleted(Guid id)
        {
            var task = _toDoRepository.Get(id);
            if (task == null)
            {
                throw new ArgumentException($"Задача с ID {id} не найдена.");
            }

            if (task.State == ToDoItemState.Completed)
            {
                throw new InvalidOperationException("Задача уже завершена.");
            }

            task.Complete();
            _toDoRepository.Update(task);
        }

        public void Delete(Guid id)
        {
            _toDoRepository.Delete(id);
        }

        public IReadOnlyList<ToDoItem> Find(ToDoUser user, string namePrefix)
        {
            return _toDoRepository.Find(user.Id, task =>
                task.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
