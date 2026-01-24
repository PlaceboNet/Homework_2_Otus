using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Homework1.Core.Entities
{
    public class ToDoItem
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? ListId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public ToDoItemState State { get; set; }

        public ToDoItem() { }

        public ToDoItem(ToDoUser user, string name, DateTime? deadline, ToDoList? list)
        {
            Id = Guid.NewGuid();
            UserId = user?.Id ?? Guid.Empty;
            ListId = list?.Id;  // Вот это важно!
            Name = name ?? string.Empty;
            CreatedAt = DateTime.Now;
            Deadline = deadline;
            State = ToDoItemState.Active;

            Console.WriteLine($"ToDoItem создан: ListId={ListId}");
        }

        public void Complete()
        {
            State = ToDoItemState.Completed;
        }
    }
}
