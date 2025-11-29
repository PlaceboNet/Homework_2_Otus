using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Homework1.Core.Exceptions
{
    public class TaskCountLimitException : Exception
    {
        public int TaskCountLimit { get; }

        public TaskCountLimitException(int taskCountLimit)
            : base($"Превышено максимальное количество задач равное {taskCountLimit}")
        {
            TaskCountLimit = taskCountLimit;
        }
    }
}
