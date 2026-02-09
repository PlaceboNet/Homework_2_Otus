using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using Homework1.Core.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.BackgroundTasks
{
    public class TodayBackgroundTask : BackgroundTask
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly IToDoRepository _toDoRepository;

        public TodayBackgroundTask(
            INotificationService notificationService,
            IUserRepository userRepository,
            IToDoRepository toDoRepository)
            : base(TimeSpan.FromDays(1), nameof(TodayBackgroundTask))
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
            _toDoRepository = toDoRepository;
        }

        protected override async Task Execute(CancellationToken ct)
        {
            var users = await _userRepository.GetUsers(ct);
            var today = DateTime.UtcNow.Date;

            foreach (var user in users)
            {
                var tasks = await _toDoRepository.GetActiveByUserIdAsync(user.Id, ct);
                var todayTasks = tasks.Where(t => t.Deadline.HasValue && t.Deadline.Value.Date == today).ToList();

                if (todayTasks.Any())
                {
                    var type = $"Today_{DateOnly.FromDateTime(DateTime.UtcNow)}";
                    var text = string.Join("\n", todayTasks.Select(t => t.Name));

                    await _notificationService.ScheduleNotification(user.Id, type, text, DateTime.UtcNow, ct);
                }
            }
        }
    }
}
