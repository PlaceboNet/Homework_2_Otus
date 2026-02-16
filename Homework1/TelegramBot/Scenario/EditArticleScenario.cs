using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Homework1.Core.Services;
using Homework1.Core.Entities;

namespace Homework1.TelegramBot.Scenario
{
    public class EditArticleScenario : IScenario
    {
        private readonly IArticleService _articleService;

        public EditArticleScenario(IArticleService articleService)
        {
            _articleService = articleService;
        }

        public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.EditArticle;

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var articleId = context.GetData<Guid>("ArticleId");
            if (articleId == Guid.Empty)
            {
                await bot.SendMessage(message.Chat.Id, "Ошибка: ID статьи не найден.", cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            if (string.IsNullOrEmpty(message.Text))
            {
                // This is the initial entry into the scenario (called from StartScenario)
                var article = await _articleService.GetArticleAsync(articleId, ct);
                if (article == null)
                {
                    await bot.SendMessage(message.Chat.Id, "Ошибка: Статья не найдена.", cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                await bot.SendMessage(message.Chat.Id, 
                    $"Редактирование статьи: <b>{System.Net.WebUtility.HtmlEncode(article.Title)}</b>\n\n" +
                    $"Текущий текст:\n<pre>{System.Net.WebUtility.HtmlEncode(article.Content)}</pre>\n\n" +
                    $"Пожалуйста, пришлите <b>новый текст</b> для этой статьи или введите /cancel для отмены.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    cancellationToken: ct);
                
                return ScenarioResult.InProgress;
            }

            // User sent the new content
            await _articleService.UpdateArticleAsync(articleId, message.Text, ct);
            await bot.SendMessage(message.Chat.Id, "✅ Текст статьи успешно обновлен! Теперь вы можете одобрить её в /admin.", cancellationToken: ct);
            
            return ScenarioResult.Completed;
        }
    }
}
