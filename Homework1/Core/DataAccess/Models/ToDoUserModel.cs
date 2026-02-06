using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Core.DataAccess.Models
{
    [Table("ToDoUser")]
    public class ToDoUserModel
    {
        [PrimaryKey]
        [Column("Id")]
        public Guid Id { get; set; }

        [Column("TelegramUserId")]
        public long TelegramUserId { get; set; }

        [Column("TelegramUserName")]
        public string TelegramUserName { get; set; } = string.Empty;

        [Association(ThisKey = nameof(Id), OtherKey = nameof(ToDoListModel.UserId))]
        public List<ToDoListModel>? Lists { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(ToDoItemModel.UserId))]
        public List<ToDoItemModel>? Tasks { get; set; }
    }
}
