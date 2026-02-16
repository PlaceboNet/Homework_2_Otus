using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.Core.DataAccess
{
    public interface IFavoriteRepository
    {
        Task<IReadOnlyList<Article>> GetUserFavoritesAsync(Guid userId, CancellationToken ct = default);
        Task AddAsync(Favorite favorite, CancellationToken ct = default);
        Task RemoveAsync(Guid userId, Guid articleId, CancellationToken ct = default);
        Task<bool> IsFavoriteAsync(Guid userId, Guid articleId, CancellationToken ct = default);
    }
}
