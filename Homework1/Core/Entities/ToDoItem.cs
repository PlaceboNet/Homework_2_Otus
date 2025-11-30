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
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ToDoItemState State { get; set; }

        public ToDoItem()
        {
            
        }

        public ToDoItem(ToDoUser user, string name)
        {
            Id = Guid.NewGuid();
            UserId = user?.Id ?? Guid.Empty;
            Name = name ?? string.Empty;
            CreatedAt = DateTime.Now;
            State = ToDoItemState.Active;
        }

        public void Complete()
        {
            State = ToDoItemState.Completed;
        }
    }
}
