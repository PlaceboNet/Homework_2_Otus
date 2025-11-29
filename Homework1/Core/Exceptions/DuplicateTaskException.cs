using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

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
