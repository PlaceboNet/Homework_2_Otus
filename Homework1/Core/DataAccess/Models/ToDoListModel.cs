using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Core.DataAccess.Models
{
    [Table("ToDoList")]
    public class ToDoListModel
    {
        [PrimaryKey]
        [Column("Id")]
        public Guid Id { get; set; }

        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Column("UserId")]
        public Guid UserId { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoUserModel.Id))]
        public ToDoUserModel? User { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(ToDoItemModel.ListId))]
        public List<ToDoItemModel>? Tasks { get; set; }
    }
}
