using LinqToDB;
using LinqToDB.Data;
using Homework1.Infrastructure.DataAccess.Models;
using System;
using System.Linq;

namespace Homework1.Infrastructure.DataAccess
{
    public class DataContextFactory : IDataContextFactory<AbioticDataContext>
    {
        private readonly string _connectionString;

        public DataContextFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public AbioticDataContext CreateDataContext()
        {
            return new AbioticDataContext(_connectionString);
        }

        public void EnsureCreated()
        {
            // 1. Database Creation
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(_connectionString);
            var targetDb = builder.Database;
            builder.Database = "postgres";
            var masterConnectionString = builder.ToString();

            using (var masterDb = new DataConnection(ProviderName.PostgreSQL, masterConnectionString))
            {
                var exists = masterDb.Query<int>($"SELECT 1 FROM pg_database WHERE datname = '{targetDb}'").Any();
                if (!exists)
                {
                    masterDb.Execute($"CREATE DATABASE \"{targetDb}\"");
                }
            }

            // 2. Table Creation
            using var db = CreateDataContext();
            try { db.CreateTable<AbioticUserModel>(); } catch { }
            try { db.CreateTable<ArticleModel>(); } catch { }
            try { db.CreateTable<FavoriteModel>(); } catch { }
            try { db.CreateTable<NotificationModel>(); } catch { }
            
            // Indexes
            try { db.Execute("CREATE INDEX IF NOT EXISTS \"IX_Favorite_UserId\" ON \"Favorite\" (\"UserId\")"); } catch { }
            try { db.Execute("CREATE INDEX IF NOT EXISTS \"IX_Article_Title\" ON \"Article\" USING gin (to_tsvector('russian', \"Title\"))"); } catch { }
        }
    }
}
