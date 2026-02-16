using Homework1.Infrastructure.DataAccess.Models;
using LinqToDB;
using LinqToDB.Data;
using System;

namespace Homework1.Infrastructure.DataAccess
{
    public class AbioticDataContext : DataConnection
    {
        public AbioticDataContext(string connectionString)
            : base(ProviderName.PostgreSQL, connectionString)
        {
        }

        public ITable<AbioticUserModel> Users => this.GetTable<AbioticUserModel>();
        public ITable<ArticleModel> Articles => this.GetTable<ArticleModel>();
        public ITable<FavoriteModel> Favorites => this.GetTable<FavoriteModel>();
        public ITable<NotificationModel> Notifications => this.GetTable<NotificationModel>();
    }
}
