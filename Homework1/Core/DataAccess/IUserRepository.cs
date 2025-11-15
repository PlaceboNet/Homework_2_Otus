using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Core.DataAccess
{
    public interface IUserRepository
    {
        Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default);
        Task AddAsync(ToDoUser user, CancellationToken cancellationToken = default);
    }
}
