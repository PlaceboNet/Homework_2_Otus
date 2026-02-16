using LinqToDB.Mapping;
using System;

namespace Homework1.Infrastructure.DataAccess.Models
{
    [Table("Article")]
    public class ArticleModel
    {
        [PrimaryKey]
        [Column("Id")]
        public Guid Id { get; set; }

        [Column("Title")]
        public string Title { get; set; } = string.Empty;

        [Column("Content")]
        public string Content { get; set; } = string.Empty;

        [Column("Category")]
        public int Category { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("IsApproved")]
        public bool IsApproved { get; set; }

        [Column("SourceUrl")]
        public string? SourceUrl { get; set; }
    }
}
