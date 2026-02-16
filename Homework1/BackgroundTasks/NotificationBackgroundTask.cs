using Homework1.Core.Services;
using Telegram.Bot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.BackgroundTasks
{
    public class NotificationBackgroundTask : BackgroundTask
    {
        private readonly INotificationService _notificationService;
        private readonly ITelegramBotClient _botClient;

        public NotificationBackgroundTask(INotificationService notificationService, ITelegramBotClient botClient)
            : base(TimeSpan.FromMinutes(1), "NotificationTask")
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
                        notification.User.TelegramUserId,
                        $"📢 *УВЕДОМЛЕНИЕ:*\n\n{notification.Text}",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        cancellationToken: ct);

                    await _notificationService.MarkNotified(notification.Id, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отправке уведомления пользователю {notification.User.TelegramUserId}: {ex.Message}");
                }
            }
        }
    }
}
