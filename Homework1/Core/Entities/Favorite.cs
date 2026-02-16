using System;

namespace Homework1.Core.Entities
{
    public class Favorite
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ArticleId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
