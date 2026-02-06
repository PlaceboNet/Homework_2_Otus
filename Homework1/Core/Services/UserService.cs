using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using Telegram.Bot;

namespace Homework1.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ToDoUser?> GetUserAsync(long telegramUserId, CancellationToken cancellationToken = default)
        {
            return await _userRepository.GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
        }

        public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
        {
            return await _userRepository.GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
        }

        public async Task<ToDoUser> RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken cancellationToken = default)
        {
            var existingUser = await GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (existingUser != null)
            {
                return existingUser;
            }

            var user = new ToDoUser
            {
                Id = Guid.NewGuid(),
                TelegramUserId = telegramUserId,
                TelegramUserName = telegramUserName
            };
            await _userRepository.AddAsync(user, cancellationToken);
            return user;
        }
    }
}
