using Homework1.Core.Entities;
using Homework1.Core.Services;
using Homework1.TelegramBot.Scenario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Homework1.TelegramBot
{
    public delegate void MessageEventHandler(string message);

    public class UpdateHandler : IUpdateHandler
    {
        private readonly IUserService _userService;
        private readonly IArticleService _articleService;
        private readonly IWikiParserService _wikiParserService;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly List<IScenario> _scenarios;

        public event MessageEventHandler? OnHandleUpdateStarted;
        public event MessageEventHandler? OnHandleUpdateCompleted;

        private readonly ReplyKeyboardMarkup _mainKeyboard = new(new[]
        {
            new KeyboardButton[] { "🔍 Поиск", "📚 Статьи", "⭐ Избранное" },
            new KeyboardButton[] { "❓ Помощь", "❌ Отмена", "ℹ️ Инфо" },
            new KeyboardButton[] { "⚙️ Админ" }
        })
        {
            ResizeKeyboard = true
        };

        public UpdateHandler(
            IUserService userService,
            IArticleService articleService,
            IWikiParserService wikiParserService,
            IScenarioContextRepository contextRepository,
            IEnumerable<IScenario> scenarios)
        {
            _userService = userService;
            _articleService = articleService;
            _wikiParserService = wikiParserService;
            _contextRepository = contextRepository;
            _scenarios = scenarios.ToList();
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                await (update switch
                {
                    { Message: { } message } => OnMessage(botClient, message, cancellationToken),
                    { CallbackQuery: { } callbackQuery } => OnCallbackQuery(botClient, callbackQuery, cancellationToken),
                    _ => Task.CompletedTask
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке обновления: {ex.Message}");
            }
        }

        private async Task OnMessage(ITelegramBotClient botClient, Message message, CancellationToken ct)
        {
            var messageText = message.Text ?? string.Empty;
            OnHandleUpdateStarted?.Invoke(messageText);

            var telegramUserId = message.From!.Id;
            var telegramUserName = message.From.Username ?? message.From.FirstName;

            // Map buttons to commands
            if (messageText == "🔍 Поиск") messageText = "/search";
            else if (messageText == "📚 Статьи") messageText = "/all";
            else if (messageText == "⭐ Избранное") messageText = "/favorites";
            else if (messageText == "❓ Помощь") messageText = "/help";
            else if (messageText == "ℹ️ Инфо") messageText = "/info";
            else if (messageText == "❌ Отмена") messageText = "/cancel";
            else if (messageText == "⚙️ Админ") messageText = "/admin";

            if (messageText == "/cancel")
            {
                await _contextRepository.ResetContext(telegramUserId, ct);
                await botClient.SendMessage(message.Chat.Id, "Сценарий отменен.", replyMarkup: _mainKeyboard, cancellationToken: ct);
                return;
            }

            var context = await _contextRepository.GetContext(telegramUserId, ct);
            if (context != null && context.CurrentScenario != ScenarioType.None)
            {
                await ProcessScenario(botClient, context, message, ct);
                return;
            }

            if (messageText.StartsWith("/"))
            {
                var user = await _userService.GetUserByTelegramUserIdAsync(telegramUserId, ct);
                if (user == null && messageText != "/start")
                {
                    await botClient.SendMessage(message.Chat.Id, "Пожалуйста, введите /start для регистрации.");
                    return;
                }

                switch (messageText.Split(' ')[0])
                {
                    case "/start":
                        await _userService.RegisterUserAsync(telegramUserId, telegramUserName, ct);
                        await botClient.SendMessage(message.Chat.Id, $"Привет, {telegramUserName}! Я бот-энциклопедия по Abiotic Factor.", replyMarkup: _mainKeyboard, cancellationToken: ct);
                        break;
                    case "/help":
                        await botClient.SendMessage(message.Chat.Id, Program.iCanDo, replyMarkup: _mainKeyboard, cancellationToken: ct);
                        break;
                    case "/info":
                        await botClient.SendMessage(message.Chat.Id, Program.info, replyMarkup: _mainKeyboard, cancellationToken: ct);
                        break;
                    case "/search":
                        await StartScenario(botClient, telegramUserId, ScenarioType.Search, message.Chat.Id, ct);
                        break;
                    case "/all":
                        await StartScenario(botClient, telegramUserId, ScenarioType.ListArticles, message.Chat.Id, ct);
                        break;
                    case "/favorites":
                        await StartScenario(botClient, telegramUserId, ScenarioType.Favorites, message.Chat.Id, ct);
                        break;
                    case "/admin":
                        await StartScenario(botClient, telegramUserId, ScenarioType.Admin, message.Chat.Id, ct);
                        break;
                    case "/promote":
                        if (user != null)
                        {
                            await _userService.PromoteToAdminAsync(user.Id, ct);
                            await botClient.SendMessage(message.Chat.Id, "Вы успешно стали администратором! Теперь вам доступна команда /admin и /import.", replyMarkup: _mainKeyboard, cancellationToken: ct);
                        }
                        break;
                    case "/import":
                        var userForImport = await _userService.GetUserByTelegramUserIdAsync(telegramUserId, ct);
                        if (userForImport?.Role == UserRole.Admin)
                        {
                            var commandPrefix = messageText.Split(' ')[0];
                            var titleToImport = messageText.Substring(commandPrefix.Length).Trim();
                            if (string.IsNullOrEmpty(titleToImport))
                            {
                                await botClient.SendMessage(message.Chat.Id, "Использование: `/import [Название статьи]`\nНапример: `/import GATE`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                            }
                            else
                            {
                                await botClient.SendMessage(message.Chat.Id, $"Начинаю импорт статьи: {titleToImport}...", cancellationToken: ct);
                                var success = await _wikiParserService.ImportArticleAsync(titleToImport, ct);
                                if (success)
                                    await botClient.SendMessage(message.Chat.Id, "✅ Статья успешно загружена и ожидает проверки в /admin.", cancellationToken: ct);
                                else
                                    await botClient.SendMessage(message.Chat.Id, "❌ Не удалось найти или загрузить статью с таким названием.", cancellationToken: ct);
                            }
                        }
                        else
                        {
                            await botClient.SendMessage(message.Chat.Id, "У вас нет прав для этой команды.", cancellationToken: ct);
                        }
                        break;
                    default:
                        await botClient.SendMessage(message.Chat.Id, "Неизвестная команда.", replyMarkup: _mainKeyboard, cancellationToken: ct);
                        break;
                }
            }
        }

        private async Task OnCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
        {
            var data = callbackQuery.Data;
            if (string.IsNullOrEmpty(data)) return;

            var user = await _userService.GetUserByTelegramUserIdAsync(callbackQuery.From.Id, ct);
            if (user == null) return;

            if (data.StartsWith("read_"))
            {
                var articleId = Guid.Parse(data.Substring(5));
                var article = await _articleService.GetArticleAsync(articleId, ct);
                if (article != null)
                {
                    var isFavorite = await _articleService.IsFavoriteAsync(user.Id, articleId, ct);
                    
                    var escapedTitle = System.Net.WebUtility.HtmlEncode(article.Title);
                    var escapedContent = System.Net.WebUtility.HtmlEncode(article.Content);
                    var escapedSource = System.Net.WebUtility.HtmlEncode(article.SourceUrl ?? "не указан");
                    
                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        isFavorite 
                            ? InlineKeyboardButton.WithCallbackData("⭐ Удалить из избранного", $"unfav_{article.Id}")
                            : InlineKeyboardButton.WithCallbackData("⭐ В избранное", $"fav_{article.Id}")
                    });

                    await botClient.SendMessage(callbackQuery.Message!.Chat.Id, $"<b>{escapedTitle}</b>\n\n{escapedContent}\n\nИсточник: {escapedSource}", parseMode: ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: ct);
                }
            }
            else if (data.StartsWith("fav_"))
            {
                var articleId = Guid.Parse(data.Substring(4));
                await _articleService.AddToFavoritesAsync(user.Id, articleId, ct);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "Добавлено в избранное!", cancellationToken: ct);
                
                // Toggle button to 'unfav'
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("⭐ Удалить из избранного", $"unfav_{articleId}")
                });
                await botClient.EditMessageReplyMarkup(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, inlineKeyboard, cancellationToken: ct);
            }
            else if (data.StartsWith("unfav_"))
            {
                var articleId = Guid.Parse(data.Substring(6));
                await _articleService.RemoveFromFavoritesAsync(user.Id, articleId, ct);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "Удалено из избранного!", cancellationToken: ct);
                
                // Toggle button back to 'fav'
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("⭐ В избранное", $"fav_{articleId}")
                });
                await botClient.EditMessageReplyMarkup(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, inlineKeyboard, cancellationToken: ct);
            }
            else if (data.StartsWith("approve_"))
            {
                var articleId = Guid.Parse(data.Substring(8));
                await _articleService.ApproveArticleAsync(articleId, ct);
                var article = await _articleService.GetArticleAsync(articleId, ct);
                
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "Статья одобрена!", cancellationToken: ct);
                var statusText = article != null 
                    ? $"✅ Статья \"{article.Title}\" одобрена и добавлена в энциклопедию.\n\nИсточник: {article.SourceUrl ?? "не указан"}"
                    : "✅ Статья одобрена и добавлена в энциклопедию.";
                
                await botClient.EditMessageText(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, statusText, cancellationToken: ct);
            }
            else if (data.StartsWith("del_"))
            {
                var articleId = Guid.Parse(data.Substring(4));
                await _articleService.DeleteArticleAsync(articleId, ct);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "Статья удалена!", cancellationToken: ct);
                await botClient.EditMessageText(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, "❌ Статья удалена.", cancellationToken: ct);
            }

            else if (data.StartsWith("edit_"))
            {
                var articleId = Guid.Parse(data.Substring(5));
                var context = new ScenarioContext(callbackQuery.From.Id, ScenarioType.EditArticle);
                context.SetData("ArticleId", articleId);
                await _contextRepository.SetContext(callbackQuery.From.Id, context, ct);
                
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                await ProcessScenario(botClient, context, new Message { Chat = callbackQuery.Message!.Chat, From = callbackQuery.From, Text = "" }, ct);
                return;
            }

            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
        }

        private async Task StartScenario(ITelegramBotClient bot, long tgId, ScenarioType type, long chatId, CancellationToken ct)
        {
            var context = new ScenarioContext(tgId, type);
            await _contextRepository.SetContext(tgId, context, ct);
            await ProcessScenario(bot, context, new Message { Chat = new Chat { Id = chatId }, From = new User { Id = tgId }, Text = "" }, ct);
        }

        private async Task ProcessScenario(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var scenario = _scenarios.FirstOrDefault(s => s.CanHandle(context.CurrentScenario));
            if (scenario != null)
            {
                var result = await scenario.HandleMessageAsync(bot, context, message, ct);
                if (result == ScenarioResult.Completed)
                {
                    await _contextRepository.ResetContext(context.UserId, ct);
                }
            }
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка в Telegram Bot: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
