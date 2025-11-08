using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace Homework1
{
    public class TaskLengthLimitException : System.Exception
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
