using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1
{
    public class ToDoList
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public ToDoList() { }

        public ToDoList(ToDoUser user, string name)
        {
            Id = Guid.NewGuid();
            UserId = user.Id;
            Name = name;
            CreatedAt = DateTime.Now;
        }
    }
}
