using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Homework1.Core.Services;
using Homework1.Core.Entities;

namespace Homework1.TelegramBot.Scenario
{
    public class AdminScenario : IScenario
    {
        private readonly IArticleService _articleService;
        private readonly IUserService _userService;

        public AdminScenario(IArticleService articleService, IUserService userService)
        {
            _articleService = articleService;
            _userService = userService;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.Admin;

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var user = await _userService.GetUserByTelegramUserIdAsync(message.From!.Id, ct);
            if (user?.Role != UserRole.Admin)
            {
                await bot.SendMessage(message.Chat.Id, "У вас нет прав администратора.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            var unapproved = await _articleService.GetUnapprovedArticlesAsync(ct);
            if (!unapproved.Any())
            {
                await bot.SendMessage(message.Chat.Id, "Нет новых статей для проверки.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            await bot.SendMessage(message.Chat.Id, $"Найдено статей на проверку: {unapproved.Count}", cancellationToken: ct);
            foreach (var article in unapproved.Take(3))
            {
                var escapedTitle = System.Net.WebUtility.HtmlEncode(article.Title);
                var excerpt = article.Content.Substring(0, Math.Min(article.Content.Length, 3000));
                var escapedExcerpt = System.Net.WebUtility.HtmlEncode(excerpt);
                var escapedSource = System.Net.WebUtility.HtmlEncode(article.SourceUrl ?? "не указан");

                var text = $"<b>ПРОВЕРКА: {escapedTitle}</b>\n\n{escapedExcerpt}\n\nИсточник: {escapedSource}";
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new [] { InlineKeyboardButton.WithCallbackData("✅ Одобрить", $"approve_{article.Id}") },
                    new [] { InlineKeyboardButton.WithCallbackData("📝 Редактировать", $"edit_{article.Id}") },
                    new [] { InlineKeyboardButton.WithCallbackData("❌ Удалить", $"del_{article.Id}") }
                });

                await bot.SendMessage(message.Chat.Id, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: ct);
            }

            return ScenarioResult.Completed;
        }
    }
}
