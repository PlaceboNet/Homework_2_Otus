using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Homework1.Core.Services;
using Homework1.Core.Entities;

namespace Homework1.TelegramBot.Scenario
{
    public class SearchScenario : IScenario
    {
        private readonly IArticleService _articleService;

        public SearchScenario(IArticleService articleService)
        {
            _articleService = articleService;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.Search;

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(context.CurrentStep))
            {
                await bot.SendMessage(message.Chat.Id, "Введите название или часть текста для поиска:", cancellationToken: ct);
                context.CurrentStep = "waiting_query";
                return ScenarioResult.InProgress;
            }

            if (context.CurrentStep == "waiting_query")
            {
                var query = message.Text;
                if (string.IsNullOrEmpty(query)) return ScenarioResult.InProgress;

                var results = await _articleService.SearchArticlesAsync(query, ct);
                if (!results.Any())
                {
                    await bot.SendMessage(message.Chat.Id, "Ничего не найдено. Попробуйте другой запрос.", cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                foreach (var article in results.Take(5))
                {
                    var escapedTitle = System.Net.WebUtility.HtmlEncode(article.Title);
                    var excerpt = article.Content.Substring(0, Math.Min(article.Content.Length, 1000));
                    var escapedExcerpt = System.Net.WebUtility.HtmlEncode(excerpt);
                    var escapedSource = System.Net.WebUtility.HtmlEncode(article.SourceUrl ?? "не указан");
                    var text = $"<b>{escapedTitle}</b>\n\n{escapedExcerpt}...\n\nИсточник: {escapedSource}";

                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Прочитать полностью", $"read_{article.Id}"),
                        InlineKeyboardButton.WithCallbackData("В избранное", $"fav_{article.Id}")
                    });

                    await bot.SendMessage(message.Chat.Id, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: ct);
                }

                return ScenarioResult.Completed;
            }

            return ScenarioResult.Completed;
        }
    }
}
