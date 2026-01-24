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

namespace Homework1.Scenario
{
    public class AddTaskScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly IToDoListService _listService;

        public AddTaskScenario(IUserService userService, IToDoService toDoService, IToDoListService listService)
        {
            _userService = userService;
            _toDoService = toDoService;
            _listService = listService;
        }

        public bool CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.AddTask;
        }

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var messageText = message.Text ?? string.Empty;

            if (messageText.StartsWith("selectlist"))
            {
                return await HandleCallbackQuery(bot, context, message, ct);
            }

            switch (context.CurrentStep)
            {
                case null:
                    return await HandleInitialStep(bot, context, message, ct);
                case "Name":
                    return await HandleNameStep(bot, context, message, ct);
                case "List":
                    return await HandleListStep(bot, context, message, ct);
                case "Deadline":
                    return await HandleDeadlineStep(bot, context, message, ct);
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
            context.CurrentStep = "Name";

            await bot.SendMessage(
                message.Chat.Id,
                "Введите название задачи:",
                cancellationToken: ct);

            return ScenarioResult.Transition;
        }

        private async Task<ScenarioResult> HandleNameStep(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var taskName = message.Text?.Trim();
            if (string.IsNullOrWhiteSpace(taskName))
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "Название задачи не может быть пустым. Введите название задачи:",
                    cancellationToken: ct);
                return ScenarioResult.Transition;
            }

            context.SetData("TaskName", taskName);
            context.CurrentStep = "List";

            // Показываем выбор списка
            var user = context.GetData<ToDoUser>("User");
            var lists = await _listService.GetUserLists(user.Id, ct);
            var buttons = new List<InlineKeyboardButton[]>();

            // Кнопка "Без списка"
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData(
            "📌 Без списка",
            new TelegramBot.Dto.ToDoListCallbackDto
            {
                Action = "selectlist",
                ToDoListId = null
            }.ToString())
    });

            // Кнопки для каждого списка
            foreach (var list in lists)
            {
                buttons.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData(
                list.Name,
                new TelegramBot.Dto.ToDoListCallbackDto
                {
                    Action = "selectlist",
                    ToDoListId = list.Id
                }.ToString())
        });
            }

            var keyboard = new InlineKeyboardMarkup(buttons);

            await bot.SendMessage(
                message.Chat.Id,
                "Выберите список для задачи:",
                replyMarkup: keyboard,
                cancellationToken: ct);

            return ScenarioResult.Transition;
        }

        private async Task<ScenarioResult> HandleCallbackQuery(ITelegramBotClient bot, ScenarioContext context, Message callbackQuery, CancellationToken ct)
        {
            try
            {
                // Парсим callback данные
                var callbackDto = TelegramBot.Dto.CallbackDto.FromString(callbackQuery.Text);
                if (callbackDto.Action == "selectlist")
                {
                    // Парсим как ToDoListCallbackDto для получения ListId
                    var listCallback = TelegramBot.Dto.ToDoListCallbackDto.FromString(callbackQuery.Text);
                    context.SetData("ListId", listCallback.ToDoListId);
                    context.CurrentStep = "Deadline";

                    await bot.SendMessage(
                        callbackQuery.Chat.Id,
                        "Введите дату дедлайна в формате dd.MM.yyyy (например, 31.12.2024) или введите /skip чтобы пропустить:",
                        cancellationToken: ct);

                    return ScenarioResult.Transition;
                }

            }
            catch (Exception ex)
            {
                await bot.SendMessage(
                    callbackQuery.Chat.Id,
                    "Ошибка при выборе списка. Попробуйте снова.",
                    cancellationToken: ct);
            }

            return ScenarioResult.Transition;
        }

        private async Task<ScenarioResult> HandleListStep(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            // Если это callback с выбором списка
            if (message.Text?.StartsWith("selectlist") == true)
            {
                return await HandleCallbackQuery(bot, context, message, ct);
            }

            // Если пользователь отправил текстовое сообщение (не callback)
            // Можем либо игнорировать, либо просить выбрать список через кнопки
            await bot.SendMessage(
                message.Chat.Id,
                "Пожалуйста, выберите список из предложенных кнопок выше.",
                cancellationToken: ct);

            return ScenarioResult.Transition;
        }

        private async Task<ScenarioResult> HandleDeadlineStep(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var taskName = context.GetData<string>("TaskName");
            var user = context.GetData<ToDoUser>("User");
            var listId = context.GetData<Guid?>("ListId");
            ToDoList? list = null;

            if (listId.HasValue)
            {
                list = await _listService.Get(listId.Value, ct);
            }

            DateTime? deadline = null;
            var input = message.Text?.Trim();

            // Обрабатываем /skip
            if (input?.ToLower() == "/skip")
            {
                deadline = null;
            }
            else if (!string.IsNullOrWhiteSpace(input))
            {
                if (DateTime.TryParseExact(input, "dd.MM.yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    deadline = parsedDate;
                }
                else
                {
                    await bot.SendMessage(
                        message.Chat.Id,
                        "Неверный формат даты. Введите дату в формате dd.MM.yyyy (например, 31.12.2024) или введите /skip чтобы пропустить:",
                        cancellationToken: ct);
                    return ScenarioResult.Transition;
                }
            }
            else
            {
                // Если пустое сообщение, просим ввести дату или /skip
                await bot.SendMessage(
                    message.Chat.Id,
                    "Введите дату дедлайна в формате dd.MM.yyyy (например, 31.12.2024) или введите /skip чтобы пропустить:",
                    cancellationToken: ct);
                return ScenarioResult.Transition;
            }

            try
            {
                var task = await _toDoService.AddAsync(user, taskName, deadline, list, ct);
                var listText = list != null ? $" в списке \"{list.Name}\"" : "";
                var messageText = $"✅ Задача \"{taskName}\"{listText} добавлена!";

                if (deadline.HasValue)
                {
                    messageText += $"\n📅 Дедлайн: {deadline.Value:dd.MM.yyyy}";
                }

                await bot.SendMessage(
                    message.Chat.Id,
                    messageText,
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    $"❌ Ошибка при добавлении задачи: {ex.Message}",
                    cancellationToken: ct);
            }

            return ScenarioResult.Completed;
        }
    }
}