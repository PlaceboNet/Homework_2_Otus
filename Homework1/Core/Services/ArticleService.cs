using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Homework1.Core.DataAccess;
using Homework1.Core.Entities;

namespace Homework1.Core.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IArticleRepository _articleRepository;
        private readonly IFavoriteRepository _favoriteRepository;

        public ArticleService(IArticleRepository articleRepository, IFavoriteRepository favoriteRepository)
        {
            _articleRepository = articleRepository;
            _favoriteRepository = favoriteRepository;
        }

        public async Task<Article?> GetArticleAsync(Guid id, CancellationToken ct = default)
        {
            return await _articleRepository.GetByIdAsync(id, ct);
        }

        public async Task<IReadOnlyList<Article>> SearchArticlesAsync(string query, CancellationToken ct = default)
        {
            return await _articleRepository.SearchAsync(query, ct);
        }

        public async Task<IReadOnlyList<Article>> GetArticlesByCategoryAsync(Category category, CancellationToken ct = default)
        {
            return await _articleRepository.GetByCategoryAsync(category, true, ct);
        }

        public async Task<IReadOnlyList<Article>> GetAllApprovedArticlesAsync(CancellationToken ct = default)
        {
            return await _articleRepository.GetAllApprovedByTitleAsync(ct);
        }

        public async Task<IReadOnlyList<Article>> GetUnapprovedArticlesAsync(CancellationToken ct = default)
        {
            return await _articleRepository.GetUnapprovedAsync(ct);
        }

        public async Task AddArticleAsync(string title, string content, Category category, string? sourceUrl = null, CancellationToken ct = default)
        {
            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                Category = category,
                SourceUrl = sourceUrl,
                CreatedAt = DateTime.UtcNow,
                IsApproved = false // Manual approval required
            };
            await _articleRepository.AddAsync(article, ct);
        }

        public async Task<bool> ArticleExistsAsync(string title, CancellationToken ct = default)
        {
            return await _articleRepository.ExistsByTitleAsync(title, ct);
        }

        public async Task UpdateArticleAsync(Guid articleId, string content, CancellationToken ct = default)
        {
            var article = await _articleRepository.GetByIdAsync(articleId, ct);
            if (article != null)
            {
                article.Content = content;
                await _articleRepository.UpdateAsync(article, ct);
            }
        }

        public async Task ApproveArticleAsync(Guid articleId, CancellationToken ct = default)
        {
            var article = await _articleRepository.GetByIdAsync(articleId, ct);
            if (article != null)
            {
                article.IsApproved = true;
                await _articleRepository.UpdateAsync(article, ct);
            }
        }

        public async Task DeleteArticleAsync(Guid articleId, CancellationToken ct = default)
        {
            await _articleRepository.DeleteAsync(articleId, ct);
        }

        public async Task AddToFavoritesAsync(Guid userId, Guid articleId, CancellationToken ct = default)
        {
            if (!await _favoriteRepository.IsFavoriteAsync(userId, articleId, ct))
            {
                var favorite = new Favorite
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ArticleId = articleId,
                    AddedAt = DateTime.UtcNow
                };
                await _favoriteRepository.AddAsync(favorite, ct);
            }
        }

        public async Task RemoveFromFavoritesAsync(Guid userId, Guid articleId, CancellationToken ct = default)
        {
            await _favoriteRepository.RemoveAsync(userId, articleId, ct);
        }

        public async Task<IReadOnlyList<Article>> GetFavoritesAsync(Guid userId, CancellationToken ct = default)
        {
            return await _favoriteRepository.GetUserFavoritesAsync(userId, ct);
        }

        public async Task<bool> IsFavoriteAsync(Guid userId, Guid articleId, CancellationToken ct = default)
        {
            return await _favoriteRepository.IsFavoriteAsync(userId, articleId, ct);
        }
    }
}
