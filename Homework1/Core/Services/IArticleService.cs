using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Homework1.Core.Entities;

namespace Homework1.Core.Services
{
    public interface IArticleService
    {
        Task<Article?> GetArticleAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Article>> SearchArticlesAsync(string query, CancellationToken ct = default);
        Task<IReadOnlyList<Article>> GetArticlesByCategoryAsync(Category category, CancellationToken ct = default);
        Task<IReadOnlyList<Article>> GetAllApprovedArticlesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Article>> GetUnapprovedArticlesAsync(CancellationToken ct = default);
        
        Task AddArticleAsync(string title, string content, Category category, string? sourceUrl = null, CancellationToken ct = default);
        Task<bool> ArticleExistsAsync(string title, CancellationToken ct = default);
        Task UpdateArticleAsync(Guid articleId, string content, CancellationToken ct = default);
        Task ApproveArticleAsync(Guid articleId, CancellationToken ct = default);
        Task DeleteArticleAsync(Guid articleId, CancellationToken ct = default);
        
        Task AddToFavoritesAsync(Guid userId, Guid articleId, CancellationToken ct = default);
        Task RemoveFromFavoritesAsync(Guid userId, Guid articleId, CancellationToken ct = default);
        Task<IReadOnlyList<Article>> GetFavoritesAsync(Guid userId, CancellationToken ct = default);
        Task<bool> IsFavoriteAsync(Guid userId, Guid articleId, CancellationToken ct = default);
    }
}
