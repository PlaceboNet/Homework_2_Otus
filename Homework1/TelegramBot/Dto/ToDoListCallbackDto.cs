using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.TelegramBot.Dto
{
    public class ToDoListCallbackDto : CallbackDto
    {
        public Guid? ToDoListId { get; set; }

        public static new ToDoListCallbackDto FromString(string input)
        {
            var parts = input.Split('|', 3);
            var dto = new ToDoListCallbackDto
            {
                Action = parts[0]
            };

            if (parts.Length > 1)
            {
                if (Guid.TryParse(parts[1], out var listId))
                {
                    dto.ToDoListId = listId;
                }
                else if (string.IsNullOrEmpty(parts[1]) || parts[1].ToLower() == "null")
                {
                    dto.ToDoListId = null;
                }
            }

            return dto;
        }

        public override string ToString()
        {
            var result = $"{Action}|{ToDoListId}";
            return result;
        }
    }
}
