using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<ToDoUser> _users = new List<ToDoUser>();

        public Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == userId));
        }

        public Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.TelegramUserId == telegramUserId));
        }

        public Task AddAsync(ToDoUser user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ToDoUser>>(_users.ToList().AsReadOnly());
        }
    }
}
