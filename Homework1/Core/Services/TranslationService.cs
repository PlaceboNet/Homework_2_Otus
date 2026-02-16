using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.Core.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://api.mymemory.translated.net/get";

        public TranslationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "AbioticBot/1.0 (contact@example.com)");
            }
        }

        public async Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            try
            {
                // MyMemory has limits on text length (usually 500 characters per request for better compatibility)
                const int maxChunkSize = 500;
                var result = new System.Text.StringBuilder();
                
                for (int i = 0; i < text.Length; i += maxChunkSize)
                {
                    var chunkSize = Math.Min(maxChunkSize, text.Length - i);
                    var chunk = text.Substring(i, chunkSize);
                    
                    var url = $"{ApiUrl}?q={Uri.EscapeDataString(chunk)}&langpair={fromLanguage}|{toLanguage}";
                    var response = await _httpClient.GetStringAsync(url, ct);
                    var json = JsonDocument.Parse(response);

                    if (json.RootElement.TryGetProperty("responseData", out var responseData))
                    {
                        var translatedChunk = responseData.GetProperty("translatedText").GetString();
                        if (!string.IsNullOrEmpty(translatedChunk))
                        {
                            result.Append(WebUtility.HtmlDecode(translatedChunk));
                        }
                        else
                        {
                            result.Append(chunk); // Fallback to original chunk if translation fails
                        }
                    }
                    
                    // Small delay to respect API rate limits
                    await Task.Delay(200, ct);
                }

                return result.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TranslationService] Error: {ex.Message}");
            }

            return text; // Return original if translation fails
        }
    }
}
