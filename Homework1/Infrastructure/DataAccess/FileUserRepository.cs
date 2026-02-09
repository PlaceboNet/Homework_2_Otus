using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess
{
    public class FileUserRepository : IUserRepository
    {
        private readonly string _basePath;

        public FileUserRepository(string basePath)
        {
            _basePath = basePath;

            // Создаем базовую папку если её нет
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var filePath = GetUserFilePath(userId);
            if (!File.Exists(filePath))
                return null;

            return await ReadUserFromFileAsync(filePath, cancellationToken);
        }

        public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_basePath))
                return null;

            var files = Directory.GetFiles(_basePath, "*.json");
            foreach (var file in files)
            {
                var user = await ReadUserFromFileAsync(file, cancellationToken);
                if (user != null && user.TelegramUserId == telegramUserId)
                {
                    return user;
                }
            }

            return null;
        }

        public async Task AddAsync(ToDoUser user, CancellationToken cancellationToken = default)
        {
            var filePath = GetUserFilePath(user.Id);
            await WriteUserToFileAsync(filePath, user, cancellationToken);
        }

        public async Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct)
        {
            if (!Directory.Exists(_basePath))
                return new List<ToDoUser>();

            var files = Directory.GetFiles(_basePath, "*.json");
            var users = new List<ToDoUser>();

            foreach (var file in files)
            {
                var user = await ReadUserFromFileAsync(file, ct);
                if (user != null)
                {
                    users.Add(user);
                }
            }

            return users.AsReadOnly();
        }

        private string GetUserFilePath(Guid userId)
        {
            return Path.Combine(_basePath, $"{userId}.json");
        }

        private async Task<ToDoUser?> ReadUserFromFileAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return await JsonSerializer.DeserializeAsync<ToDoUser>(fileStream, cancellationToken: cancellationToken);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task WriteUserToFileAsync(string filePath, ToDoUser user, CancellationToken cancellationToken)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(fileStream, user, options, cancellationToken);
        }
    }
}
