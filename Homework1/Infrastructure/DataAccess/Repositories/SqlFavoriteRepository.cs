using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using Homework1.Infrastructure.DataAccess.Models;
using LinqToDB;
using LinqToDB.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess.Repositories
{
    public class SqlFavoriteRepository : IFavoriteRepository
    {
        private readonly IDataContextFactory<AbioticDataContext> _factory;

        public SqlFavoriteRepository(IDataContextFactory<AbioticDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<Article>> GetUserFavoritesAsync(Guid userId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var models = await db.Favorites
                .LoadWith(f => f.Article)
                .Where(f => f.UserId == userId)
                .Select(f => f.Article)
                .ToListAsync(ct);
            
            return models.Where(m => m != null).Select(m => ModelMapper.MapFromModel(m!)).ToList().AsReadOnly();
        }

        public async Task AddAsync(Favorite favorite, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = ModelMapper.MapToModel(favorite);
            await db.InsertAsync(model, token: ct);
        }

        public async Task RemoveAsync(Guid userId, Guid articleId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            await db.Favorites
                .Where(f => f.UserId == userId && f.ArticleId == articleId)
                .DeleteAsync(ct);
        }

        public async Task<bool> IsFavoriteAsync(Guid userId, Guid articleId, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            return await db.Favorites
                .AnyAsync(f => f.UserId == userId && f.ArticleId == articleId, ct);
        }
    }
}
