using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Homework1.Core.Entities;
using Telegram.Bot;

namespace Homework1.Core.Services
{
    public interface IUserService
    {
        Task<ToDoUser> RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken cancellationToken = default);
        Task<ToDoUser?> GetUserAsync(long telegramUserId, CancellationToken cancellationToken = default);
    }
}
