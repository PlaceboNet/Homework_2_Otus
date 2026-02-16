using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Homework1.Core.DataAccess;
using Homework1.Core.Entities;

namespace Homework1.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<AbioticUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
        {
            return await _userRepository.GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
        }

        public async Task<AbioticUser> RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken cancellationToken = default)
        {
            var existingUser = await GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (existingUser != null)
            {
                return existingUser;
            }

            var user = new AbioticUser
            {
                Id = Guid.NewGuid(),
                TelegramUserId = telegramUserId,
                TelegramUserName = telegramUserName,
                Role = UserRole.User // Default role
            };
            
            // First user could be admin for testing purposes, but let's stick to manual promotion or config
            await _userRepository.AddAsync(user, cancellationToken);
            return user;
        }

        public async Task PromoteToAdminAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _userRepository.GetUserAsync(userId, ct);
            if (user != null)
            {
                user.Role = UserRole.Admin;
                await _userRepository.UpdateAsync(user, ct);
            }
        }
    }
}
