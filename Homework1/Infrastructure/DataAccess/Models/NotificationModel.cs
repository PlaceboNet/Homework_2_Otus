using Homework1.Core.DataAccess.Models;
using LinqToDB.Mapping;
using System;

namespace Homework1.Infrastructure.DataAccess.Models
{
    [Table("Notification")]
    public class NotificationModel
    {
        [PrimaryKey]
        [Column("Id")]
        public Guid Id { get; set; }

        [Column("UserId")]
        public Guid UserId { get; set; }

        [Column("Type")]
        public string Type { get; set; } = string.Empty;

        [Column("Text")]
        public string Text { get; set; } = string.Empty;

        [Column("ScheduledAt")]
        public DateTime ScheduledAt { get; set; }

        [Column("IsNotified")]
        public bool IsNotified { get; set; }

        [Column("NotifiedAt")]
        public DateTime? NotifiedAt { get; set; }

        [Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoUserModel.Id))]
        public ToDoUserModel? User { get; set; }
    }
}
