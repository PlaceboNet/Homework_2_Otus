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
    public class AddListScenario : IScenario
    {
        private readonly IUserService _userService;
        private readonly IToDoListService _listService;

        public AddListScenario(IUserService userService, IToDoListService listService)
        {
            _userService = userService;
            _listService = listService;
        }

        public bool CanHandle(ScenarioType scenario)
        {
            return scenario == ScenarioType.AddList;
        }

        public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            switch (context.CurrentStep)
            {
                case null:
                    return await HandleInitialStep(bot, context, message, ct);
                case "Name":
                    return await HandleNameStep(bot, context, message, ct);
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
                "Введите название списка (максимум 10 символов):",
                cancellationToken: ct);

            return ScenarioResult.Transition;
        }

        private async Task<ScenarioResult> HandleNameStep(ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
        {
            var listName = message.Text?.Trim();
            if (string.IsNullOrWhiteSpace(listName))
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "Название списка не может быть пустым. Введите название списка:",
                    cancellationToken: ct);
                return ScenarioResult.Transition;
            }

            var user = context.GetData<ToDoUser>("User");

            try
            {
                var list = await _listService.Add(user, listName, ct);
                await bot.SendMessage(
                    message.Chat.Id,
                    $"✅ Список \"{listName}\" создан!",
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    $"❌ Ошибка при создании списка: {ex.Message}",
                    cancellationToken: ct);
            }

            return ScenarioResult.Completed;
        }
    }
}
