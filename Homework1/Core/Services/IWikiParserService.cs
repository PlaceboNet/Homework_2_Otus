using System.Threading;
using System.Threading.Tasks;

namespace Homework1.Core.Services
{
    public interface IWikiParserService
    {
        /// <summary>
        /// Parses the wiki for new entries and adds them as unapproved articles.
        /// </summary>
        bool IsEnabled { get; }
        void ToggleEnabled();
        Task ParseNewArticlesAsync(CancellationToken ct);
        Task<bool> ImportArticleAsync(string title, CancellationToken ct);
    }
}
