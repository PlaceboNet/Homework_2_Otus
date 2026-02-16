using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using Homework1.Infrastructure.DataAccess.Models;
using LinqToDB;
using LinqToDB.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess.Repositories
{
    public class SqlUserRepository : IUserRepository
    {
        private readonly IDataContextFactory<AbioticDataContext> _factory;

        public SqlUserRepository(IDataContextFactory<AbioticDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<AbioticUser?> GetUserAsync(Guid userId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = await db.Users
                .LoadWith(u => u.Favorites!)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
            return model != null ? ModelMapper.MapFromModel(model) : null;
        }

        public async Task<AbioticUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = await db.Users
                .LoadWith(u => u.Favorites!)
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);
            return model != null ? ModelMapper.MapFromModel(model) : null;
        }

        public async Task AddAsync(AbioticUser user, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = ModelMapper.MapToModel(user);
            await db.InsertAsync(model, token: ct);
        }

        public async Task<IReadOnlyList<AbioticUser>> GetUsers(CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var models = await db.Users.ToListAsync(ct);
            return models.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }

        public async Task UpdateAsync(AbioticUser user, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = ModelMapper.MapToModel(user);
            await db.UpdateAsync(model, token: ct);
        }
    }
}
