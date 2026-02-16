using System;
using System.Collections.Generic;

namespace Homework1.Core.Entities
{
    public enum UserRole
    {
        User,
        Admin
    }

    public class AbioticUser
    {
        public Guid Id { get; set; }
        public long TelegramUserId { get; set; }
        public string TelegramUserName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
        
        // Navigation properties (optional, depending on ORM needs)
        public List<Favorite>? Favorites { get; set; }
    }
}
