using LinqToDB;
using LinqToDB.Data;
using Homework1.Core.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess
{
    public class DataContextFactory : IDataContextFactory<ToDoDataContext>
    {
        private readonly string _connectionString;

        public DataContextFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ToDoDataContext CreateDataContext()
        {
            return new ToDoDataContext(_connectionString);
        }

        public void EnsureCreated()
        {
            // 1. Создание самой БД (нужно подключиться к postgres)
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

            // 2. Создание таблиц
            using var db = CreateDataContext();
            try { db.CreateTable<ToDoUserModel>(); } catch { }
            try { db.CreateTable<ToDoListModel>(); } catch { }
            try { db.CreateTable<ToDoItemModel>(); } catch { }
        }
    }
}
