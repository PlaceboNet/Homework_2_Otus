using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Homework1.Core.Services;
using Homework1.Core.Entities;

namespace Homework1.TelegramBot.Scenario
{
    public class FavoritesScenario : IScenario
    {
        private readonly IArticleService _articleService;
        private readonly IUserService _userService;

        public FavoritesScenario(IArticleService articleService, IUserService userService)
        {
            _articleService = articleService;
            _userService = userService;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.Favorites;

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var user = await _userService.GetUserByTelegramUserIdAsync(message.From!.Id, ct);
            if (user == null) return ScenarioResult.Completed;

            var favorites = await _articleService.GetFavoritesAsync(user.Id, ct);
            if (!favorites.Any())
            {
                await bot.SendMessage(message.Chat.Id, "У вас пока нет избранных статей.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            await bot.SendMessage(message.Chat.Id, "Ваши избранные статьи:", cancellationToken: ct);
            foreach (var article in favorites)
            {
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Открыть", $"read_{article.Id}"),
                    InlineKeyboardButton.WithCallbackData("Удалить", $"unfav_{article.Id}")
                });

                await bot.SendMessage(message.Chat.Id, $"📌 {article.Title}", replyMarkup: inlineKeyboard, cancellationToken: ct);
            }

            return ScenarioResult.Completed;
        }
    }
}
