using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace Homework1
{
    public class UserService : IUserService
    {
        private readonly Dictionary<long, ToDoUser> _users = new Dictionary<long, ToDoUser>();

        public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
        {
            var user = new ToDoUser(telegramUserId, telegramUserName);
            _users[telegramUserId] = user;
            return user;
        }

        public ToDoUser? GetUser(long telegramUserId)
        {
            return _users.ContainsKey(telegramUserId) ? _users[telegramUserId] : null;
        }
    }
}
