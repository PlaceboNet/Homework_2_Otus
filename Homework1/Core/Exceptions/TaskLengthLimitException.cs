using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Homework1.Core.Exceptions
{
    public class TaskLengthLimitException : Exception
    {
        public int TaskLength { get; }
        public int TaskLengthLimit { get; }

        public TaskLengthLimitException(int taskLength, int taskLengthLimit)
            : base($"Длина задачи '{taskLength}' превышает максимально допустимое значение {taskLengthLimit}")
        {
            TaskLength = taskLength;
            TaskLengthLimit = taskLengthLimit;
        }
    }

}
