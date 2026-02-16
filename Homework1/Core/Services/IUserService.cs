using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Homework1.Core.Entities;

namespace Homework1.Core.Services
{
    public interface IUserService
    {
        Task<AbioticUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default);
        Task<AbioticUser> RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken cancellationToken = default);
        Task PromoteToAdminAsync(Guid userId, CancellationToken ct = default);
    }
}
