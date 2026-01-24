using Homework1.Core.Entities;
using Homework1.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Homework1.TelegramBot.Scenario
{
    public class DeleteListScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoListService _listService;
        private readonly IToDoService _toDoService;

        public DeleteListScenario(IUserService userService, IToDoListService listService, IToDoService toDoService)
        {
            _userService = userService;
            _listService = listService;
            _toDoService = toDoService;
        }

        public bool CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.DeleteList;
        }

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            switch (context.CurrentStep)
            {
                case null:
                    return await HandleInitialStep(bot, context, message, ct);
                case "Approve":
                    return await HandleApproveStep(bot, context, message, ct);
                case "Delete":
                    return await HandleDeleteStep(bot, context, message, ct);
                default:
                    return ScenarioResult.Completed;
            }
        }

        private async Task<ScenarioResult> HandleInitialStep(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var user = await _userService.GetUserByTelegramUserIdAsync(message.From.Id, ct);
            if (user == null)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "Пользователь не найден. Используйте /start для регистрации.",
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            context.SetData("User", user);
            context.CurrentStep = "Approve";

            var lists = await _listService.GetUserLists(user.Id, ct);
            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var list in lists)
            {
                buttons.Add(new[]
                {
                InlineKeyboardButton.WithCallbackData(
                    list.Name,
                    new Dto.ToDoListCallbackDto
                    {
                        Action = "deletelist",
                        ToDoListId = list.Id
                    }.ToString())
            });
            }

            if (buttons.Count == 0)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "У вас нет списков для удаления.",
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            var keyboard = new InlineKeyboardMarkup(buttons);

            await bot.SendMessage(
                message.Chat.Id,
                "Выберите список для удаления:",
                replyMarkup: keyboard,
                cancellationToken: ct);

            return ScenarioResult.Transition;
        }

        private async Task<ScenarioResult> HandleApproveStep(ITelegramBotClient bot, ScenarioContext context, Message callbackQuery, CancellationToken ct)
        {
            // Парсим callback данные
            var callbackData = Dto.ToDoListCallbackDto.FromString(callbackQuery.Text);

            if (!callbackData.ToDoListId.HasValue)
            {
                await bot.SendMessage(
                    callbackQuery.Chat.Id,
                    "Неверный список.",
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            var list = await _listService.Get(callbackData.ToDoListId.Value, ct);
            if (list == null)
            {
                await bot.SendMessage(
                    callbackQuery.Chat.Id,
                    "Список не найден.",
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            context.SetData("List", list);
            context.CurrentStep = "Delete";

            // Создаем кнопки подтверждения
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
                $"Подтверждаете удаление списка \"{list.Name}\" и всех его задач?",
                replyMarkup: keyboard,
                cancellationToken: ct);

            return ScenarioResult.Transition;
        }

        private async Task<ScenarioResult> HandleDeleteStep(ITelegramBotClient bot, ScenarioContext context, Message callbackQuery, CancellationToken ct)
        {
            var data = callbackQuery.Text; // Это будет "yes" или "no"
            var list = context.GetData<ToDoList>("List");
            var user = context.GetData<ToDoUser>("User");

            if (data == "yes")
            {
                try
                {
                    // Удаляем все задачи списка
                    var tasks = await _toDoService.GetByUserIdAndListAsync(user.Id, list.Id, ct);

                    foreach (var task in tasks)
                    {
                        await _toDoService.DeleteAsync(task.Id, ct);
                    }

                    // Удаляем сам список
                    await _listService.Delete(list.Id, ct);

                    await bot.SendMessage(
                        callbackQuery.Chat.Id,
                        $"✅ Список \"{list.Name}\" и все его задачи удалены.",
                        cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    await bot.SendMessage(
                        callbackQuery.Chat.Id,
                        $"❌ Ошибка при удалении списка: {ex.Message}",
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
