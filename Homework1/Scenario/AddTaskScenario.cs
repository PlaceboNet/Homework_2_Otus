using Homework1.Core.Entities;
using Homework1.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Homework1.Scenario
{
    public class AddTaskScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;

        public AddTaskScenario(IUserService userService, IToDoService toDoService)
        {
            _userService = userService;
            _toDoService = toDoService;
        }

        public bool CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.AddTask;
        }

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            switch (context.CurrentStep)
            {
                case null:
                    return await HandleInitialStep(bot, context, message, ct);
                case "Name":
                    return await HandleNameStep(bot, context, message, ct);
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
            context.CurrentStep = "Deadline";

            await bot.SendMessage(
                message.Chat.Id,
                "Введите дату дедлайна в формате dd.MM.yyyy (например, 31.12.2024) или введите /skip чтобы пропустить:",
                cancellationToken: ct);

            return ScenarioResult.Transition;
        }

        private async Task<ScenarioResult> HandleDeadlineStep(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var taskName = context.GetData<string>("TaskName");
            var user = context.GetData<ToDoUser>("User");
            DateTime? deadline = null;

            var input = message.Text?.Trim();
            if (input?.ToLower() != "/skip" && !string.IsNullOrWhiteSpace(input))
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

            try
            {
                var task = await _toDoService.AddAsync(user, taskName, deadline, ct);
                await bot.SendMessage(
                    message.Chat.Id,
                    $"✅ Задача \"{taskName}\" добавлена!{(deadline.HasValue ? $"\n📅 Дедлайн: {deadline.Value:dd.MM.yyyy}" : "")}",
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
