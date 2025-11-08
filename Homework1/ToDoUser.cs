using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace Homework1
{
    public class ToDoUser
    {
        public Guid Id { get; }
        public long TelegramUserId { get; }
        public string TelegramUserName { get; }

        public ToDoUser(long telegramUserId, string telegramUserName)
        {
            Id = Guid.NewGuid();
            TelegramUserId = telegramUserId;
            TelegramUserName = telegramUserName;
        }
    }
}
