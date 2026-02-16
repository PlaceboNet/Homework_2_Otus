using System;

namespace Homework1.Core.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public AbioticUser User { get; set; } = null!;
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public bool IsNotified { get; set; }
        public DateTime? NotifiedAt { get; set; }
    }
}
