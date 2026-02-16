using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Homework1.Core.Services;
using Homework1.Core.Entities;

namespace Homework1.TelegramBot.Scenario
{
    public class ListArticlesScenario : IScenario
    {
        private readonly IArticleService _articleService;

        public ListArticlesScenario(IArticleService articleService)
        {
            _articleService = articleService;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.ListArticles;

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var articles = await _articleService.GetAllApprovedArticlesAsync(ct);
            if (!articles.Any())
            {
                await bot.SendMessage(message.Chat.Id, "В энциклопедии пока нет статей.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            var text = "<b>Список всех статей:</b>\n\nВыберите статью из списка ниже:";
            
            // Generate buttons for articles. 
            // Note: If there are many articles, we might want pagination, 
            // but for now let's just show them in a list or chunks.
            var buttons = articles.Select(a => new[] { InlineKeyboardButton.WithCallbackData(a.Title, $"read_{a.Id}") }).ToList();
            var inlineKeyboard = new InlineKeyboardMarkup(buttons);

            await bot.SendMessage(message.Chat.Id, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: ct);

            return ScenarioResult.Completed;
        }
    }
}
