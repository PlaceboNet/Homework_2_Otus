using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.TelegramBot.Dto
{
    public class PagedListCallbackDto : ToDoListCallbackDto
    {
        public int Page { get; set; }

        public static new PagedListCallbackDto FromString(string input)
        {
            var parts = input.Split('|', 4);
            var dto = new PagedListCallbackDto
            {
                Action = parts[0]
            };

            if (parts.Length > 1)
            {
                if (string.IsNullOrEmpty(parts[1]) || parts[1].ToLower() == "null")
                {
                    dto.ToDoListId = null;
                }
                else if (Guid.TryParse(parts[1], out var listId))
                {
                    dto.ToDoListId = listId;
                }
            }

            if (parts.Length > 2 && int.TryParse(parts[2], out var page))
            {
                dto.Page = page;
            }

            return dto;
        }

        public override string ToString()
        {
            return $"{base.ToString()}|{Page}";
        }
    }
}
