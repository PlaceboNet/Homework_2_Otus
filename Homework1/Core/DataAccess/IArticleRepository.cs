using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.Core.DataAccess
{
    public interface IArticleRepository
    {
        Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Article>> SearchAsync(string query, CancellationToken ct = default);
        Task<IReadOnlyList<Article>> GetByCategoryAsync(Category category, bool onlyApproved = true, CancellationToken ct = default);
        Task<IReadOnlyList<Article>> GetAllApprovedByTitleAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Article>> GetUnapprovedAsync(CancellationToken ct = default);
        Task AddAsync(Article article, CancellationToken ct = default);
        Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default);
        Task UpdateAsync(Article article, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
