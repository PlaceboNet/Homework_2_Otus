using Homework1.Core.Entities;
using Homework1.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Core.DataAccess
{
    public class ToDoReportService : IToDoReportService
    {
        private readonly IToDoRepository _toDoRepository;

        public ToDoReportService(IToDoRepository toDoRepository)
        {
            _toDoRepository = toDoRepository;
        }

        public async Task<(int total, int completed, int active, DateTime generatedAt)> GetUserStatsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var allTasks = await _toDoRepository.GetAllByUserIdAsync(userId, cancellationToken);
            var total = allTasks.Count;
            var completed = allTasks.Count(t => t.State == ToDoItemState.Completed);
            var active = allTasks.Count(t => t.State == ToDoItemState.Active);
            var generatedAt = DateTime.Now;

            return (total, completed, active, generatedAt);
        }
    }
}
