using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Homework1.Core.Entities
{
    public class ToDoUser
    {
        public Guid Id { get; set; }
        public long TelegramUserId { get; set; }
        public string TelegramUserName { get; set; } = string.Empty;
        public List<ToDoList>? Lists { get; set; }
        public List<ToDoItem>? Tasks { get; set; }
    }
}
