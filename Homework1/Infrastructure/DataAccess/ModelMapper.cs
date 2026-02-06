using Homework1.Core.DataAccess.Models;
using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess
{
    internal static class ModelMapper
    {
        public static ToDoUser MapFromModel(ToDoUserModel model)
        {
            if (model == null) return null!;

            return new ToDoUser
            {
                Id = model.Id,
                TelegramUserId = model.TelegramUserId,
                TelegramUserName = model.TelegramUserName,
                Lists = model.Lists?.Select(MapFromModel).ToList(),
                Tasks = model.Tasks?.Select(MapFromModel).ToList()
            };
        }

        public static ToDoUserModel MapToModel(ToDoUser entity)
        {
            return new ToDoUserModel
            {
                Id = entity.Id,
                TelegramUserId = entity.TelegramUserId,
                TelegramUserName = entity.TelegramUserName
            };
        }

        public static ToDoItem MapFromModel(ToDoItemModel model)
        {
            if (model == null) return null!;

            return new ToDoItem
            {
                Id = model.Id,
                UserId = model.UserId,
                ListId = model.ListId,
                Name = model.Name,
                CreatedAt = model.CreatedAt,
                Deadline = model.Deadline,
                State = (ToDoItemState)model.State,
                User = model.User != null ? MapFromModel(model.User) : null,
                List = model.List != null ? MapFromModel(model.List) : null
            };
        }

        public static ToDoItemModel MapToModel(ToDoItem entity)
        {
            return new ToDoItemModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                ListId = entity.ListId,
                Name = entity.Name,
                CreatedAt = entity.CreatedAt,
                Deadline = entity.Deadline,
                State = (int)entity.State
            };
        }

        public static ToDoList MapFromModel(ToDoListModel model)
        {
            if (model == null) return null!;

            return new ToDoList
            {
                Id = model.Id,
                Name = model.Name,
                UserId = model.UserId,
                CreatedAt = model.CreatedAt,
                User = model.User != null ? MapFromModel(model.User) : null,
                Tasks = model.Tasks?.Select(MapFromModel).ToList()
            };
        }

        public static ToDoListModel MapToModel(ToDoList entity)
        {
            return new ToDoListModel
            {
                Id = entity.Id,
                Name = entity.Name,
                UserId = entity.UserId,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
