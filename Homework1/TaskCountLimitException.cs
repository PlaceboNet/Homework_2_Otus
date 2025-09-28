using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1
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
