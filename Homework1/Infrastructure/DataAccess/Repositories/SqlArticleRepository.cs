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
    public class SqlArticleRepository : IArticleRepository
    {
        private readonly IDataContextFactory<AbioticDataContext> _factory;

        public SqlArticleRepository(IDataContextFactory<AbioticDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = await db.Articles.FirstOrDefaultAsync(a => a.Id == id, ct);
            return model != null ? ModelMapper.MapFromModel(model) : null;
        }

        public async Task<IReadOnlyList<Article>> SearchAsync(string query, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            // Basic case-insensitive search if TS Vector is not fully configured yet
            var models = await db.Articles
                .Where(a => a.IsApproved && (a.Title.ToLower().Contains(query.ToLower()) || a.Content.ToLower().Contains(query.ToLower())))
                .ToListAsync(ct);
            return models.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<Article>> GetByCategoryAsync(Category category, bool onlyApproved = true, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var query = db.Articles.Where(a => a.Category == (int)category);
            if (onlyApproved) query = query.Where(a => a.IsApproved);
            
            var models = await query.ToListAsync(ct);
            return models.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<Article>> GetAllApprovedByTitleAsync(CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var models = await db.Articles
                .Where(a => a.IsApproved)
                .OrderBy(a => a.Title)
                .ToListAsync(ct);
            return models.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<Article>> GetUnapprovedAsync(CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var models = await db.Articles.Where(a => !a.IsApproved).ToListAsync(ct);
            return models.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }

        public async Task AddAsync(Article article, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = ModelMapper.MapToModel(article);
            await db.InsertAsync(model, token: ct);
        }

        public async Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            return await db.Articles.AnyAsync(a => a.Title.ToLower() == title.ToLower(), ct);
        }

        public async Task UpdateAsync(Article article, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            var model = ModelMapper.MapToModel(article);
            await db.UpdateAsync(model, token: ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            using var db = _factory.CreateDataContext();
            await db.Articles.Where(a => a.Id == id).DeleteAsync(ct);
        }
    }
}
