using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using LinqToDB;
using LinqToDB.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess.Repositories
{
    public class SqlUserRepository : IUserRepository
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public SqlUserRepository(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            var query = dbContext.ToDoUsers
                .LoadWith(u => u.Lists!)
                .LoadWith(u => u.Tasks!)
                .Where(u => u.Id == userId);

            var model = await query.FirstOrDefaultAsync(cancellationToken);

            return model != null ? ModelMapper.MapFromModel(model) : null;
        }

        public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            var query = dbContext.ToDoUsers
                .LoadWith(u => u.Lists!)
                .LoadWith(u => u.Tasks!)
                .Where(u => u.TelegramUserId == telegramUserId);

            var model = await query.FirstOrDefaultAsync(cancellationToken);

            return model != null ? ModelMapper.MapFromModel(model) : null;
        }

        public async Task AddAsync(ToDoUser user, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            var model = ModelMapper.MapToModel(user);
            await dbContext.InsertAsync(model, token: cancellationToken);
        }

        public async Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct)
        {
            using var dbContext = _factory.CreateDataContext();

            var users = await dbContext.ToDoUsers.ToListAsync(ct);

            return users.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }
    }
}
