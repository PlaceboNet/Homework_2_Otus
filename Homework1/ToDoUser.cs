using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1
{
    internal class ToDoUser
    {
        public Guid UserId { get; } // Заполняется в конструкторе
        public string TelegramUserName { get; } // Имя пользователя
        public DateTime RegisteredAt { get; } // Заполняется в конструкторе

        public ToDoUser(string telegramUserName)
        {
            UserId = Guid.NewGuid();
            TelegramUserName = telegramUserName;
            RegisteredAt = DateTime.UtcNow;
        }
    }
}
