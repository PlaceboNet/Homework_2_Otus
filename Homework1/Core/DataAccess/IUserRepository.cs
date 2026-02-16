using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.Core.DataAccess
{
    public interface IUserRepository
    {
        Task<AbioticUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<AbioticUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default);
        Task AddAsync(AbioticUser user, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AbioticUser>> GetUsers(CancellationToken ct);
        Task UpdateAsync(AbioticUser user, CancellationToken cancellationToken = default);
    }
}
