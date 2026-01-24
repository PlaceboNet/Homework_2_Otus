using Homework1.TelegramBot.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.TelegramBot.Dto
{
    public class CallbackDto
    {
        public string Action { get; set; } = string.Empty;

        public static CallbackDto FromString(string input)
        {
            var parts = input.Split('|', 2);
            return new CallbackDto
            {
                Action = parts[0]
            };
        }

        public override string ToString()
        {
            return Action;
        }
    }

}