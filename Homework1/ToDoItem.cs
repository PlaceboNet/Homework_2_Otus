using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1
{
    internal class ToDoItem
    {
        public enum ToDoItemState
        {
            Active,
            Completed
        }
        public Guid Id { get; }
        public ToDoUser User { get; }
        public string Name { get; }
        public DateTime CreatedAt { get; }
        public ToDoItemState State { get; private set; }
        public DateTime? StateChangedAt { get; private set; }

        public ToDoItem(ToDoUser user, string name)
        {
            Id = Guid.NewGuid();
            User = user;
            Name = name;
            CreatedAt = DateTime.UtcNow;
            State = ToDoItemState.Active;
            StateChangedAt = null;
        }

        // Метод для завершения задачи
        public void Complete()
        {
            State = ToDoItemState.Completed;
            StateChangedAt = DateTime.UtcNow;
        }
    }
}
