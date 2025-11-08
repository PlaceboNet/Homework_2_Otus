using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Homework1.ToDoItem;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace Homework1
{
    public class ToDoService : IToDoService
    {
        private readonly List<ToDoItem> _tasks = new List<ToDoItem>();
        private readonly int _maxTaskCount;
        private readonly int _maxTaskLength;

        public ToDoService(int maxTaskCount, int maxTaskLength)
        {
            _maxTaskCount = maxTaskCount;
            _maxTaskLength = maxTaskLength;
        }

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
        {
            return _tasks.Where(t => t.UserId == userId).ToList();
        }

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
        {
            return _tasks.Where(t => t.UserId == userId && t.State == ToDoItemState.Active).ToList();
        }

        public ToDoItem Add(ToDoUser user, string name)
        {
            // Проверка максимального количества задач
            var userTasksCount = _tasks.Count(t => t.UserId == user.Id);
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
            if (_tasks.Any(task => task.UserId == user.Id &&
                task.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateTaskException(name);
            }

            var newTask = new ToDoItem(user, name);
            _tasks.Add(newTask);
            return newTask;
        }

        public void MarkCompleted(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                throw new ArgumentException($"Задача с ID {id} не найдена.");
            }

            if (task.State == ToDoItemState.Completed)
            {
                throw new InvalidOperationException("Задача уже завершена.");
            }

            task.Complete();
        }

        public void Delete(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                throw new ArgumentException($"Задача с ID {id} не найдена.");
            }

            _tasks.Remove(task);
        }
    }
}
