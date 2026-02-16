using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.Core.Services
{
    public class WikiParserService : IWikiParserService
    {
        private readonly IArticleService _articleService;
        private readonly HttpClient _httpClient;
        private readonly ITranslationService _translationService;
        private const string WikiApiUrl = "https://abioticfactor.wiki.gg/api.php";

        public bool IsEnabled { get; private set; } = true;

        public void ToggleEnabled()
        {
            IsEnabled = !IsEnabled;
            Console.WriteLine($"[WikiParser] Parsing status changed: {(IsEnabled ? "ENABLED" : "DISABLED")}");
        }

        public WikiParserService(IArticleService articleService, ITranslationService translationService)
        {
            _articleService = articleService;
            _translationService = translationService;
            _httpClient = new HttpClient();
            // wiki.gg often requires a proper User-Agent
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AbioticFactorEncyclopediaBot/1.0 (contact@example.com)");
        }

        public async Task ParseNewArticlesAsync(CancellationToken ct)
        {
            if (!IsEnabled) return;

            var allTitles = new List<string>();

            // 1. Get some random pages
            var randomPages = await FetchRandomPagesAsync(10, ct);
            allTitles.AddRange(randomPages);

            // 2. Get pages from "Enemies" category
            var enemies = await FetchCategoryPagesAsync("Enemies", 5, ct);
            allTitles.AddRange(enemies);

            // 3. Get pages from "Locations" category
            var locations = await FetchCategoryPagesAsync("Locations", 5, ct);
            allTitles.AddRange(locations);

            // 4. Fallback to allpages with a random starting point
            if (allTitles.Count < 5)
            {
                var fallback = await FetchAllPagesAsync(ct);
                allTitles.AddRange(fallback);
            }

            // Shuffle results
            var random = new Random();
            var shuffled = allTitles.Distinct().OrderBy(x => random.Next()).ToList();

            int importedCount = 0;
            foreach (var title in shuffled)
            {
                if (importedCount >= 5) break;

                // CHECK IF EXISTS
                if (await _articleService.ArticleExistsAsync(title, ct))
                {
                    Console.WriteLine($"[WikiParser] Article '{title}' already exists. Skipping.");
                    continue;
                }

                if (await ImportArticleAsync(title, ct))
                {
                    importedCount++;
                }
            }
        }

        public async Task<bool> ImportArticleAsync(string title, CancellationToken ct)
        {
            Console.WriteLine($"[WikiParser] Attempting to import from wiki.gg: {title}");
            
            // 1. Try direct import
            if (await TryImportTitleAsync(title, ct)) return true;

            // 2. Try English fallback if title has pattern "Russian (English)"
            var englishMatch = System.Text.RegularExpressions.Regex.Match(title, @".*\((.*)\)");
            if (englishMatch.Success)
            {
                var englishTitle = englishMatch.Groups[1].Value.Trim();
                Console.WriteLine($"[WikiParser] Trying English fallback: {englishTitle}");
                if (await TryImportTitleAsync(englishTitle, ct)) return true;
            }

            // 3. Try search fallback
            Console.WriteLine($"[WikiParser] Direct match for '{title}' failed on wiki.gg. Trying search...");
            var searchResults = await SearchWikiAsync(title, ct);
            foreach (var searchTitle in searchResults)
            {
                if (await TryImportTitleAsync(searchTitle, ct)) return true;
            }

            return false;
        }

        private async Task<bool> TryImportTitleAsync(string title, CancellationToken ct)
        {
            try
            {
                // wiki.gg uses MediaWiki, but action=parse is safer for content when extracts is missing
                var url = $"{WikiApiUrl}?action=parse&page={Uri.EscapeDataString(title)}&format=json&prop=text|displaytitle&redirects=1";
                var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode) return false;

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                if (!json.TryGetProperty("parse", out var parse))
                    return false;

                var actualTitle = parse.GetProperty("title").GetString() ?? title;
                var htmlContent = parse.GetProperty("text").GetProperty("*").GetString() ?? string.Empty;
                
                var cleanContent = StripHtml(htmlContent);
                var sourceUrl = $"https://abioticfactor.wiki.gg/wiki/{Uri.EscapeDataString(actualTitle)}";

                // Basic validation
                if (string.IsNullOrWhiteSpace(cleanContent) || cleanContent.Length < 50) return false;

                // Translate title and content if they look English
                string translatedTitle = actualTitle;
                string translatedContent = cleanContent;

                if (IsEnglish(actualTitle) || IsEnglish(cleanContent.Substring(0, Math.Min(50, cleanContent.Length))))
                {
                    Console.WriteLine($"[WikiParser] Translating article: {actualTitle}");
                    translatedTitle = await _translationService.TranslateAsync(actualTitle, "en", "ru", ct);
                    translatedContent = await _translationService.TranslateAsync(cleanContent, "en", "ru", ct);
                }

                await _articleService.AddArticleAsync(translatedTitle, translatedContent, Category.Lore, sourceUrl, ct);
                Console.WriteLine($"[WikiParser] Successfully imported and translated: {translatedTitle}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WikiParser] Error during TryImportTitle ('{title}'): {ex.Message}");
                return false;
            }
        }

        private bool IsEnglish(string text)
        {
            // Simple check: if it contains Cyrillic, it's probably Russian
            return !System.Text.RegularExpressions.Regex.IsMatch(text, @"\p{IsCyrillic}");
        }

        private string StripHtml(string html)
        {
            // Remove Navboxes, sidebars and other junk common on wiki.gg
            string step1 = Regex.Replace(html, "<style.*?>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string step2 = Regex.Replace(step1, "<script.*?>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string step3 = Regex.Replace(step2, "<div class=\"navbox\".*?>.*?</div>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string step4 = Regex.Replace(step3, "<aside.*?>.*?</aside>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string step5 = Regex.Replace(step4, "<table.*?>.*?</table>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string step6 = Regex.Replace(step5, "<[^>]*>", "");
            
            // Clean up multiple newlines and spaces
            string step7 = Regex.Replace(step6, @"\s+", " ");
            return System.Net.WebUtility.HtmlDecode(step7).Trim();
        }

        private async Task<List<string>> SearchWikiAsync(string query, CancellationToken ct)
        {
            try
            {
                var url = $"{WikiApiUrl}?action=query&list=search&srsearch={Uri.EscapeDataString(query)}&srlimit=3&format=json";
                var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode) return new List<string>();

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                if (!json.TryGetProperty("query", out var queryProp) || !queryProp.TryGetProperty("search", out var search))
                    return new List<string>();

                return search.EnumerateArray()
                    .Select(s => s.GetProperty("title").GetString()!)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<List<string>> FetchRandomPagesAsync(int limit, CancellationToken ct)
        {
            try
            {
                var url = $"{WikiApiUrl}?action=query&list=random&rnlimit={limit}&rnnamespace=0&format=json";
                var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode) return new List<string>();

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                if (!json.TryGetProperty("query", out var queryProp) || !queryProp.TryGetProperty("random", out var random))
                    return new List<string>();

                return random.EnumerateArray()
                    .Select(p => p.GetProperty("title").GetString()!)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<List<string>> FetchCategoryPagesAsync(string category, int limit, CancellationToken ct)
        {
            try
            {
                var url = $"{WikiApiUrl}?action=query&list=categorymembers&cmtitle=Category:{Uri.EscapeDataString(category)}&cmlimit={limit}&format=json";
                var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode) return new List<string>();

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                if (!json.TryGetProperty("query", out var queryProp) || !queryProp.TryGetProperty("categorymembers", out var members))
                    return new List<string>();

                return members.EnumerateArray()
                    .Select(m => m.GetProperty("title").GetString()!)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<List<string>> FetchAllPagesAsync(CancellationToken ct)
        {
            try
            {
                // Add a random prefix character to get different results from allpages
                var alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var randomChar = alpha[new Random().Next(alpha.Length)];
                
                var url = $"{WikiApiUrl}?action=query&list=allpages&apfrom={randomChar}&aplimit=10&format=json";
                var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode) return new List<string>();

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                if (!json.TryGetProperty("query", out var queryProp) || !queryProp.TryGetProperty("allpages", out var allpages))
                    return new List<string>();

                return allpages.EnumerateArray()
                    .Select(p => p.GetProperty("title").GetString()!)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
