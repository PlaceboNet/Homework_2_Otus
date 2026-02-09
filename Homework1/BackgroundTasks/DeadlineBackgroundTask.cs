using Homework1.Core.DataAccess;
using Homework1.Core.Services;
using Homework1.Core.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.BackgroundTasks
{
    public class DeadlineBackgroundTask : BackgroundTask
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly IToDoRepository _toDoRepository;

        public DeadlineBackgroundTask(
            INotificationService notificationService,
            IUserRepository userRepository,
            IToDoRepository toDoRepository)
            : base(TimeSpan.FromHours(1), nameof(DeadlineBackgroundTask))
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
            _toDoRepository = toDoRepository;
        }

        protected override async Task Execute(CancellationToken ct)
        {
            var users = await _userRepository.GetUsers(ct);
            var now = DateTime.UtcNow;
            var from = now.AddDays(-1).Date;
            var to = now.Date;

            foreach (var user in users)
            {
                var missedTasks = await _toDoRepository.GetActiveWithDeadline(user.Id, from, to, ct);

                foreach (var task in missedTasks)
                {
                    var type = $"Dealine_{task.Id}";
                    var text = $"Ой\\! Вы пропустили дедлайн по задаче {task.Name}";
                    await _notificationService.ScheduleNotification(user.Id, type, text, now, ct);
                }
            }
        }
    }
}
