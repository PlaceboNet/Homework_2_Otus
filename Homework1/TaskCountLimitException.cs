using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace Homework1
{
    public class TaskCountLimitException : System.Exception
    {
        public int TaskCountLimit { get; }

        public TaskCountLimitException(int taskCountLimit)
            : base($"Превышено максимальное количество задач равное {taskCountLimit}")
        {
            TaskCountLimit = taskCountLimit;
        }
    }
}
