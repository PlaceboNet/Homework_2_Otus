using Homework1.Core.Entities;
using Homework1.Core.Services;
using Homework1.TelegramBot.Scenario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Homework1
{
    public class DeleteTaskScenario : IScenario
    {
        private readonly IToDoService _toDoService;

        public DeleteTaskScenario(IToDoService toDoService)
        {
            _toDoService = toDoService;
        }

        public bool CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.DeleteTask;
        }

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            switch (context.CurrentStep)
            {
                case null:
                    return await HandleInitialStep(bot, context, message, ct);
                case "Approve":
                    return await HandleApproveStep(bot, context, message, ct);
                default:
                    return ScenarioResult.Completed;
            }
        }

        private async Task<ScenarioResult> HandleInitialStep(ITelegramBotClient bot, ScenarioContext context, Message callbackQuery, CancellationToken ct)
        {
            var callbackData = TelegramBot.Dto.ToDoItemCallbackDto.FromString(callbackQuery.Text);
            var task = await _toDoService.GetAsync(callbackData.ToDoItemId, ct);

            if (task == null)
            {
                await bot.SendMessage(
                    callbackQuery.Chat.Id,
                    "Задача не найдена.",
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            context.SetData("Task", task);
            context.CurrentStep = "Approve";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Да", "yes"),
                InlineKeyboardButton.WithCallbackData("❌ Нет", "no")
            }
        });

            await bot.SendMessage(
                callbackQuery.Chat.Id,
                $"Подтверждаете удаление задачи \"{task.Name}\"?",
                replyMarkup: keyboard,
                cancellationToken: ct);

            return ScenarioResult.Transition;
        }

        private async Task<ScenarioResult> HandleApproveStep(ITelegramBotClient bot, ScenarioContext context, Message callbackQuery, CancellationToken ct)
        {
            var data = callbackQuery.Text;
            var task = context.GetData<ToDoItem>("Task");

            if (data == "yes")
            {
                try
                {
                    await _toDoService.DeleteAsync(task.Id, ct);
                    await bot.SendMessage(
                        callbackQuery.Chat.Id,
                        $"✅ Задача \"{task.Name}\" удалена.",
                        cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    await bot.SendMessage(
                        callbackQuery.Chat.Id,
                        $"❌ Ошибка при удалении задачи: {ex.Message}",
                        cancellationToken: ct);
                }
            }
            else
            {
                await bot.SendMessage(
                    callbackQuery.Chat.Id,
                    "Удаление отменено.",
                    cancellationToken: ct);
            }

            return ScenarioResult.Completed;
        }
    }
}
