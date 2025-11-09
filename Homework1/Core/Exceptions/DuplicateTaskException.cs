using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace Homework1.Core.Exceptions
{
    public class DuplicateTaskException : Exception
    {
        public string Task { get; }

        public DuplicateTaskException(string task)
            : base($"Задача '{task}' уже существует")
        {
            Task = task;
        }
    }
}
