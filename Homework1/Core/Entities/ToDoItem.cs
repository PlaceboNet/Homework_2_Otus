using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace Homework1.Core.Entities
{
    public class ToDoItem
    {
        public Guid Id { get; }
        public Guid UserId { get; }  // Добавьте это свойство
        public string Name { get; }
        public DateTime CreatedAt { get; }
        public ToDoItemState State { get; private set; }

        public ToDoItem(ToDoUser user, string name)
        {
            Id = Guid.NewGuid();
            UserId = user.Id;  // Сохраняем ID пользователя
            Name = name;
            CreatedAt = DateTime.Now;
            State = ToDoItemState.Active;
        }

        public void Complete()
        {
            State = ToDoItemState.Completed;
        }
    }

    public enum ToDoItemState
    {
        Active,
        Completed
    }
}
