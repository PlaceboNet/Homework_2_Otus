using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace Homework1
{
    public class DuplicateTaskException : System.Exception
    {
        public string Task { get; }

        public DuplicateTaskException(string task)
            : base($"Задача '{task}' уже существует")
        {
            Task = task;
        }
    }
}
