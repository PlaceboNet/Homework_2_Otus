using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using Homework1.Core.Services;
using Homework1.Infrastructure.DataAccess;
using Homework1.Infrastructure.DataAccess.Models;
using LinqToDB;
using LinqToDB.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.Infrastructure
{
    public class NotificationService : INotificationService
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public NotificationService(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<bool> ScheduleNotification(
            Guid userId,
            string type,
            string text,
            DateTime scheduledAt,
            CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();

            var exists = await db.Notifications.AnyAsync(n => n.UserId == userId && n.Type == type, ct);
            if (exists)
                return false;

            var model = new NotificationModel
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Text = text,
                ScheduledAt = scheduledAt,
                IsNotified = false,
                NotifiedAt = null
            };

            await db.InsertAsync(model, token: ct);
            return true;
        }

        public async Task<IReadOnlyList<Notification>> GetScheduledNotification(DateTime scheduledBefore, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();

            var models = await db.Notifications
                .LoadWith(n => n.User)
                .Where(n => !n.IsNotified && n.ScheduledAt <= scheduledBefore)
                .ToListAsync(ct);

            return models.Select(m => new Notification
            {
                Id = m.Id,
                Type = m.Type,
                Text = m.Text,
                ScheduledAt = m.ScheduledAt,
                IsNotified = m.IsNotified,
                NotifiedAt = m.NotifiedAt,
                User = ModelMapper.MapFromModel(m.User!) // Assuming we need user data to send notifications
            }).ToList().AsReadOnly();
        }

        public async Task MarkNotified(Guid notificationId, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();

            await db.Notifications
                .Where(n => n.Id == notificationId)
                .Set(n => n.IsNotified, true)
                .Set(n => n.NotifiedAt, DateTime.UtcNow)
                .UpdateAsync(ct);
        }
    }
}
