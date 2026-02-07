using System;
using System.Threading;
using System.Threading.Tasks;
using Homework1.TelegramBot.Scenario;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Homework1.BackgroundTasks
{
    public class ResetScenarioBackgroundTask : BackgroundTask
    {
        private readonly TimeSpan _resetScenarioTimeout;
        private readonly IScenarioContextRepository _scenarioRepository;
        private readonly ITelegramBotClient _botClient;

        public ResetScenarioBackgroundTask(
            TimeSpan resetScenarioTimeout,
            IScenarioContextRepository scenarioRepository,
            ITelegramBotClient botClient) 
            : base(TimeSpan.FromHours(1), nameof(ResetScenarioBackgroundTask))
        {
            _resetScenarioTimeout = resetScenarioTimeout;
            _scenarioRepository = scenarioRepository;
            _botClient = botClient;
        }

        protected override async Task Execute(CancellationToken ct)
        {
            var contexts = await _scenarioRepository.GetContexts(ct);
            var now = DateTime.UtcNow;

            foreach (var context in contexts)
            {
                if (now - context.CreatedAt > _resetScenarioTimeout)
                {
                    await _scenarioRepository.ResetContext(context.UserId, ct);

                    var keyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[] { new KeyboardButton("/addtask"), new KeyboardButton("/show"), new KeyboardButton("/report") }
                    })
                    {
                        ResizeKeyboard = true
                    };

                    try
                    {
                        await _botClient.SendMessage(
                            chatId: context.UserId,
                            text: $"Сценарий отменен, так как не поступил ответ в течение {_resetScenarioTimeout}",
                            replyMarkup: keyboard,
                            cancellationToken: ct);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error notifying user {context.UserId}: {ex.Message}");
                    }
                }
            }
        }
    }
}
