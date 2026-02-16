using System.Threading;
using System.Threading.Tasks;

namespace Homework1.Core.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct);
    }
}
