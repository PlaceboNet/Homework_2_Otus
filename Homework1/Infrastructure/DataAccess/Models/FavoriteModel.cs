using LinqToDB.Mapping;
using System;

namespace Homework1.Infrastructure.DataAccess.Models
{
    [Table("Favorite")]
    public class FavoriteModel
    {
        [PrimaryKey]
        [Column("Id")]
        public Guid Id { get; set; }

        [Column("UserId")]
        public Guid UserId { get; set; }

        [Column("ArticleId")]
        public Guid ArticleId { get; set; }

        [Column("AddedAt")]
        public DateTime AddedAt { get; set; }

        [Association(ThisKey = nameof(UserId), OtherKey = nameof(AbioticUserModel.Id))]
        public AbioticUserModel? User { get; set; }

        [Association(ThisKey = nameof(ArticleId), OtherKey = nameof(ArticleModel.Id))]
        public ArticleModel? Article { get; set; }
    }
}
