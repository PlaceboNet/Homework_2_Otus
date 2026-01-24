using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.TelegramBot.Dto
{
    public class ToDoItemCallbackDto : CallbackDto
    {
        public Guid ToDoItemId { get; set; }

        public static new ToDoItemCallbackDto FromString(string input)
        {
            var parts = input.Split('|', 3);
            var dto = new ToDoItemCallbackDto
            {
                Action = parts[0]
            };

            if (parts.Length > 1 && Guid.TryParse(parts[1], out var itemId))
            {
                dto.ToDoItemId = itemId;
            }

            return dto;
        }

        public override string ToString()
        {
            return $"{Action}|{ToDoItemId}";
        }
    }
}
