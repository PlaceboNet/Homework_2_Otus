using Homework1.Core.DataAccess.Models;
using LinqToDB;
using LinqToDB.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess
{
    public class ToDoDataContext : DataConnection
    {
        public ToDoDataContext(string connectionString)
            : base(ProviderName.PostgreSQL, connectionString)
        {
        }

        public ITable<ToDoUserModel> ToDoUsers => this.GetTable<ToDoUserModel>();
        public ITable<ToDoListModel> ToDoLists => this.GetTable<ToDoListModel>();
        public ITable<ToDoItemModel> ToDoItems => this.GetTable<ToDoItemModel>();
        public ITable<Homework1.Infrastructure.DataAccess.Models.NotificationModel> Notifications => this.GetTable<Homework1.Infrastructure.DataAccess.Models.NotificationModel>();
    }
}
