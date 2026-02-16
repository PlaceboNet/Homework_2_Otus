using LinqToDB.Mapping;
using System;
using System.Collections.Generic;

namespace Homework1.Infrastructure.DataAccess.Models
{
    [Table("AbioticUser")]
    public class AbioticUserModel
    {
        [PrimaryKey]
        [Column("Id")]
        public Guid Id { get; set; }

        [Column("TelegramUserId")]
        public long TelegramUserId { get; set; }

        [Column("TelegramUserName")]
        public string TelegramUserName { get; set; } = string.Empty;

        [Column("Role")]
        public int Role { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(FavoriteModel.UserId))]
        public List<FavoriteModel>? Favorites { get; set; }
    }
}
