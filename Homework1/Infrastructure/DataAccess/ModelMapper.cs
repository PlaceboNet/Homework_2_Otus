using Homework1.Infrastructure.DataAccess.Models;
using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Homework1.Infrastructure.DataAccess
{
    internal static class ModelMapper
    {
        public static AbioticUser MapFromModel(AbioticUserModel model)
        {
            if (model == null) return null!;

            return new AbioticUser
            {
                Id = model.Id,
                TelegramUserId = model.TelegramUserId,
                TelegramUserName = model.TelegramUserName,
                Role = (UserRole)model.Role,
                Favorites = model.Favorites?.Select(MapFromModel).ToList()
            };
        }

        public static AbioticUserModel MapToModel(AbioticUser entity)
        {
            return new AbioticUserModel
            {
                Id = entity.Id,
                TelegramUserId = entity.TelegramUserId,
                TelegramUserName = entity.TelegramUserName,
                Role = (int)entity.Role
            };
        }

        public static Article MapFromModel(ArticleModel model)
        {
            if (model == null) return null!;

            return new Article
            {
                Id = model.Id,
                Title = model.Title,
                Content = model.Content,
                Category = (Category)model.Category,
                CreatedAt = model.CreatedAt,
                IsApproved = model.IsApproved,
                SourceUrl = model.SourceUrl
            };
        }

        public static ArticleModel MapToModel(Article entity)
        {
            return new ArticleModel
            {
                Id = entity.Id,
                Title = entity.Title,
                Content = entity.Content,
                Category = (int)entity.Category,
                CreatedAt = entity.CreatedAt,
                IsApproved = entity.IsApproved,
                SourceUrl = entity.SourceUrl
            };
        }

        public static Favorite MapFromModel(FavoriteModel model)
        {
            if (model == null) return null!;

            return new Favorite
            {
                Id = model.Id,
                UserId = model.UserId,
                ArticleId = model.ArticleId,
                AddedAt = model.AddedAt
            };
        }

        public static FavoriteModel MapToModel(Favorite entity)
        {
            return new FavoriteModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                ArticleId = entity.ArticleId,
                AddedAt = entity.AddedAt
            };
        }
    }
}
