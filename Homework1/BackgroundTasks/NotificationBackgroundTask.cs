using Homework1.Core.Services;
using Homework1.Core.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Homework1.BackgroundTasks
{
    public class NotificationBackgroundTask : BackgroundTask
    {
        private readonly INotificationService _notificationService;
        private readonly ITelegramBotClient _botClient;

        public NotificationBackgroundTask(INotificationService notificationService, ITelegramBotClient botClient)
            : base(TimeSpan.FromMinutes(1), nameof(NotificationBackgroundTask))
        {
            _notificationService = notificationService;
            _botClient = botClient;
        }

        protected override async Task Execute(CancellationToken ct)
        {
            var notifications = await _notificationService.GetScheduledNotification(DateTime.UtcNow, ct);

            foreach (var notification in notifications)
            {
                try
                {
                    await _botClient.SendMessage(
                        chatId: notification.User.TelegramUserId,
                        text: notification.Text,
                        parseMode: ParseMode.MarkdownV2,
                        cancellationToken: ct);

                    await _notificationService.MarkNotified(notification.Id, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending notification {notification.Id} to {notification.User.TelegramUserId}: {ex.Message}");
                }
            }
        }
    }
}
