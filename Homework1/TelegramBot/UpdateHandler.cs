using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using Homework1.Core.Services;
using Homework1.Scenario;
using Homework1.TelegramBot;
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
using static Homework1.Core.Entities.ToDoItem;

namespace Homework1.TelegramBot
{
    // Делегат для событий
    public delegate void MessageEventHandler(string message);

    public class UpdateHandler : IUpdateHandler
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly IToDoReportService _reportService;
        private readonly IToDoListService _listService;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly List<IScenario> _scenarios;

        // События
        public event MessageEventHandler? OnHandleUpdateStarted;
        public event MessageEventHandler? OnHandleUpdateCompleted;

        // Клавиатуры
        private readonly ReplyKeyboardMarkup _startKeyboard = new(new[]
        {
        new KeyboardButton[] { "/start" }
    })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        private readonly ReplyKeyboardMarkup _mainKeyboard = new(new[]
        {
            new KeyboardButton[] { "/addtask", "/show" },
            new KeyboardButton[] { "/report", "/help" },
            new KeyboardButton[] { "/info" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        private readonly ReplyKeyboardMarkup _scenarioKeyboard = new(new[]
        {
        new KeyboardButton[] { "/cancel" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        public UpdateHandler(
            IUserService userService,
            IToDoService toDoService,
            IToDoReportService reportService,
            IToDoListService listService,
            IScenarioContextRepository contextRepository,
            IEnumerable<IScenario> scenarios)
        {
            _userService = userService;
            _toDoService = toDoService;
            _reportService = reportService;
            _listService = listService;
            _contextRepository = contextRepository;
            _scenarios = scenarios.ToList();
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                await (update switch
                {
                    { Message: { } message } => OnMessage(botClient, update, message, cancellationToken),
                    { CallbackQuery: { } callbackQuery } => OnCallbackQuery(botClient, update, callbackQuery, cancellationToken),
                    _ => Task.CompletedTask
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке обновления: {ex.Message}");
            }
        }

        private async Task OnMessage(ITelegramBotClient botClient, Update update, Message message, CancellationToken ct)
        {
            var messageText = message.Text ?? string.Empty;

            OnHandleUpdateStarted?.Invoke(messageText);

            if (string.IsNullOrEmpty(messageText))
                return;

            var chat = message.Chat;
            var telegramUserId = message.From.Id;
            var telegramUserName = message.From.Username ?? $"{message.From.FirstName} {message.From.LastName}".Trim();

            // Проверяем команду /cancel
            if (messageText.StartsWith("/cancel"))
            {
                await _contextRepository.ResetContext(telegramUserId, ct);
                await botClient.SendMessage(
                    chat.Id,
                    "Сценарий отменен.",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: ct);
                OnHandleUpdateCompleted?.Invoke(messageText);
                return;
            }

            // Проверяем активный сценарий
            var context = await _contextRepository.GetContext(telegramUserId, ct);
            if (context != null && context.CurrentScenario != ScenarioType.None)
            {
                await ProcessScenario(botClient, context, message, ct);
                OnHandleUpdateCompleted?.Invoke(messageText);
                return;
            }

            // Обработка обычных команд
            if (messageText.StartsWith("/"))
            {
                var commandParts = messageText.Split(' ', 2);
                var command = commandParts[0];
                var argument = commandParts.Length > 1 ? commandParts[1] : string.Empty;

                var user = await _userService.GetUserAsync(telegramUserId, ct);
                var keyboard = user == null ? _startKeyboard : _mainKeyboard;

                if (user == null && !messageText.StartsWith("/start") &&
                    !messageText.StartsWith("/help") && !messageText.StartsWith("/info"))
                {
                    await botClient.SendMessage(
                        chat.Id,
                        "Пожалуйста, сначала зарегистрируйтесь с помощью команды /start",
                        replyMarkup: _startKeyboard,
                        cancellationToken: ct);
                    OnHandleUpdateCompleted?.Invoke(messageText);
                    return;
                }

                switch (command)
                {
                    case "/start":
                        await HandleStartCommand(botClient, chat, telegramUserId, telegramUserName, ct);
                        break;
                    case "/help":
                        await HandleHelpCommand(botClient, chat, user, ct);
                        break;
                    case "/info":
                        await HandleInfoCommand(botClient, chat, user, ct);
                        break;
                    case "/addtask":
                        await HandleAddTaskCommand(botClient, chat, telegramUserId, ct);
                        break;
                    case "/show":
                        await HandleShowCommand(botClient, chat, user, ct);
                        break;
                    case "/report":
                        await HandleReportCommand(botClient, chat, user, ct);
                        break;
                    case "/find":
                        await HandleFindCommand(botClient, chat, user, argument, ct);
                        break;
                    case "/completetask":
                        await HandleCompleteTaskCommand(botClient, chat, argument, ct);
                        break;
                    case "/removetask":
                        await HandleRemoveTaskCommand(botClient, chat, user, argument, ct);
                        break;
                    case "/exit":
                        await botClient.SendMessage(
                            chat.Id,
                            "До свидания!",
                            replyMarkup: keyboard,
                            cancellationToken: ct);
                        break;
                    default:
                        await botClient.SendMessage(
                            chat.Id,
                            "Такой команды не знаю",
                            replyMarkup: keyboard,
                            cancellationToken: ct);
                        break;
                }
            }
            else
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Пожалуйста, используйте команды из меню или введите /help для справки",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: ct);
            }

            OnHandleUpdateCompleted?.Invoke(messageText);
        }

        private async Task OnCallbackQuery(ITelegramBotClient botClient, Update update, CallbackQuery callbackQuery, CancellationToken ct)
        {
            try
            {
                var user = await _userService.GetUserAsync(callbackQuery.From.Id, ct);
                if (user == null)
                {
                    await botClient.AnswerCallbackQuery(
                        callbackQueryId: callbackQuery.Id,
                        text: "Сначала зарегистрируйтесь с помощью /start",
                        showAlert: true,
                        cancellationToken: ct);
                    return;
                }

                var callbackDataString = callbackQuery.Data;

                // Проверяем активный сценарий ВСЕГДА, даже для selectlist
                var context = await _contextRepository.GetContext(user.TelegramUserId, ct);

                // Если есть активный сценарий, передаем callback ему
                if (context != null && context.CurrentScenario != ScenarioType.None)
                {
                    // Передаем callback в сценарий
                    var message = new Message
                    {
                        Chat = callbackQuery.Message?.Chat,
                        From = callbackQuery.From,
                        Text = callbackDataString
                    };

                    await ProcessScenario(botClient, context, message, ct);
                    await botClient.AnswerCallbackQuery(callbackQueryId: callbackQuery.Id, cancellationToken: ct);
                    return;
                }

                // Только если НЕТ активного сценария, обрабатываем команды
                if (callbackDataString == "addlist")
                {
                    await HandleAddListCommand(botClient, callbackQuery.Message.Chat, user.TelegramUserId, ct);
                }
                else if (callbackDataString == "deletelist")
                {
                    await HandleDeleteListCommand(botClient, callbackQuery.Message.Chat, user.TelegramUserId, ct);
                }
                else if (callbackDataString.StartsWith("show"))
                {
                    await HandleShowCallback(botClient, callbackQuery, ct);
                }
                else if (callbackDataString.StartsWith("selectlist"))
                {
                    // Если пришел selectlist, но нет активного сценария - это ошибка
                    await botClient.AnswerCallbackQuery(
                        callbackQueryId: callbackQuery.Id,
                        text: "Сначала запустите команду /addtask",
                        showAlert: true,
                        cancellationToken: ct);
                }

                await botClient.AnswerCallbackQuery(callbackQueryId: callbackQuery.Id, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в OnCallbackQuery: {ex.Message}");
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Произошла ошибка",
                    showAlert: true,
                    cancellationToken: ct);
            }
        }

        private async Task HandleStartCommand(ITelegramBotClient botClient, Chat chat, long telegramUserId, string telegramUserName, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                user = await _userService.RegisterUserAsync(telegramUserId, telegramUserName, cancellationToken);
                await botClient.SendMessage(
                    chat.Id,
                    $"Привет, {user.TelegramUserName}! Добро пожаловать!\n\n" +
                    "Теперь вам доступны все команды бота. Используйте кнопки ниже или введите /help для справки.",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(
                    chat.Id,
                    $"С возвращением, {user.TelegramUserName}!",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandleHelpCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var helpText = Program.iCanDo + "\n/cancel - отменить текущий сценарий";
            var message = user != null
                ? $"{user.TelegramUserName}, вот что я могу сделать:{helpText}"
                : $"Вот что я могу сделать:{helpText}";

            var keyboard = user == null ? _startKeyboard : _mainKeyboard;

            await botClient.SendMessage(
                chat.Id,
                message,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task HandleInfoCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var info = Program.info;
            var message = user != null
                ? $"{user.TelegramUserName}, {info}"
                : info;

            var keyboard = user == null ? _startKeyboard : _mainKeyboard;

            await botClient.SendMessage(
                chat.Id,
                message,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task HandleAddTaskCommand(ITelegramBotClient botClient, Chat chat, long telegramUserId, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Сначала зарегистрируйтесь с помощью команды /start",
                    replyMarkup: _startKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            // Создаем контекст сценария
            var context = new ScenarioContext(ScenarioType.AddTask);
            await _contextRepository.SetContext(telegramUserId, context, cancellationToken);

            await botClient.SendMessage(
                chat.Id,
                "Начинаем добавление задачи. Используйте /cancel для отмены.",
                replyMarkup: _scenarioKeyboard,
                cancellationToken: cancellationToken);

            // Создаем сообщение для инициализации сценария
            var message = new Message
            {
                Chat = chat,
                From = new User { Id = telegramUserId },
                Text = ""
            };

            await ProcessScenario(botClient, context, message, cancellationToken);
        }

        private async Task HandleShowCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Сначала зарегистрируйтесь с помощью команды /start",
                    replyMarkup: _startKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            var lists = await _listService.GetUserLists(user.Id, cancellationToken);
            var buttons = new List<InlineKeyboardButton[]>();

            // Кнопка "Без списка" - используем ToDoListCallbackDto
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData(
            "📌 Без списка",
            new TelegramBot.Dto.ToDoListCallbackDto
            {
                Action = "show",
                ToDoListId = null
            }.ToString())
    });

            // Кнопки для каждого списка - используем ToDoListCallbackDto
            foreach (var list in lists)
            {
                buttons.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData(
                $"📁 {list.Name}",
                new TelegramBot.Dto.ToDoListCallbackDto
                {
                    Action = "show",
                    ToDoListId = list.Id
                }.ToString())
        });
            }

            // Кнопки действий - используем ПРОСТЫЕ строки!
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("🆕 Добавить список", "addlist"),
        InlineKeyboardButton.WithCallbackData("❌ Удалить список", "deletelist")
    });

            var keyboard = new InlineKeyboardMarkup(buttons);

            await botClient.SendMessage(
                chat.Id,
                "📋 *Выберите список для просмотра задач:*",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task HandleShowCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
        {
            var user = await _userService.GetUserAsync(callbackQuery.From.Id, ct);
            if (user == null)
            {
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Сначала зарегистрируйтесь с помощью /start",
                    showAlert: true,
                    cancellationToken: ct);
                return;
            }

            var callbackData = TelegramBot.Dto.ToDoListCallbackDto.FromString(callbackQuery.Data);
            var tasks = await _toDoService.GetByUserIdAndListAsync(user.Id, callbackData.ToDoListId, ct);
            var activeTasks = tasks.Where(t => t.State == ToDoItemState.Active).ToList();

            // Получаем название списка
            var listName = "без списка";
            if (callbackData.ToDoListId.HasValue)
            {
                var list = await _listService.Get(callbackData.ToDoListId.Value, ct);
                listName = list?.Name ?? "неизвестный список";
            }

            if (activeTasks.Count == 0)
            {
                await botClient.SendMessage(
                    callbackQuery.Message.Chat.Id,
                    $"📭 В списке \"{listName}\" нет активных задач.",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: ct);
                return;
            }

            var message = FormatTasksList(activeTasks, $"📋 *Задачи из списка \"{EscapeMarkdown(listName)}\":*", true);

            await botClient.SendMessage(
                callbackQuery.Message.Chat.Id,
                message,
                parseMode: ParseMode.MarkdownV2,
                replyMarkup: _mainKeyboard,
                cancellationToken: ct);
        }

        private async Task HandleAddListCommand(ITelegramBotClient botClient, Chat chat, long telegramUserId, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Сначала зарегистрируйтесь с помощью команды /start",
                    replyMarkup: _startKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            var context = new ScenarioContext(ScenarioType.AddList);
            await _contextRepository.SetContext(telegramUserId, context, cancellationToken);

            await botClient.SendMessage(
                chat.Id,
                "Начинаем создание списка. Используйте /cancel для отмены.",
                replyMarkup: _scenarioKeyboard,
                cancellationToken: cancellationToken);

            // Создаем сообщение для инициализации сценария
            var message = new Message
            {
                Chat = chat,
                From = new User { Id = telegramUserId },
                Text = ""
            };

            await ProcessScenario(botClient, context, message, cancellationToken);
        }

        private async Task HandleDeleteListCommand(ITelegramBotClient botClient, Chat chat, long telegramUserId, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Сначала зарегистрируйтесь с помощью команды /start",
                    replyMarkup: _startKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            var context = new ScenarioContext(ScenarioType.DeleteList);
            await _contextRepository.SetContext(telegramUserId, context, cancellationToken);

            await botClient.SendMessage(
                chat.Id,
                "Начинаем удаление списка. Используйте /cancel для отмены.",
                replyMarkup: _scenarioKeyboard,
                cancellationToken: cancellationToken);

            // Создаем сообщение для инициализации сценария
            var message = new Message
            {
                Chat = chat,
                From = new User { Id = telegramUserId },
                Text = ""
            };

            await ProcessScenario(botClient, context, message, cancellationToken);
        }

        private async Task HandleReportCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Сначала зарегистрируйтесь с помощью команды /start",
                    replyMarkup: _startKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            var stats = await _reportService.GetUserStatsAsync(user.Id, cancellationToken);
            var message = $"📊 *Статистика по задачам*\n" +
                         $"📅 На {FormatDateForMarkdown(stats.generatedAt)}\n\n" +
                         $"📂 Всего: *{stats.total}*\n" +
                         $"✅ Завершенных: *{stats.completed}*\n" +
                         $"🟢 Активных: *{stats.active}*";

            await botClient.SendMessage(
                chat.Id,
                message,
                parseMode: ParseMode.MarkdownV2,
                replyMarkup: _mainKeyboard,
                cancellationToken: cancellationToken);
        }

        private async Task HandleFindCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, string namePrefix, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Сначала зарегистрируйтесь с помощью команды /start",
                    replyMarkup: _startKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(namePrefix))
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Пожалуйста, укажите начало названия задачи. Пример: /find Позвонить",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            var foundTasks = await _toDoService.FindAsync(user, namePrefix, cancellationToken);

            if (foundTasks.Count == 0)
            {
                await botClient.SendMessage(
                    chat.Id,
                    $"🔍 Задачи, начинающиеся на '{namePrefix}', не найдены.",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            // Экранируем namePrefix для использования в Markdown
            var escapedPrefix = EscapeMarkdown(namePrefix);

            var message = $"🔍 *Найдено задач \\({foundTasks.Count}\\):*\n\n";
            for (int i = 0; i < foundTasks.Count; i++)
            {
                var task = foundTasks[i];
                string state = task.State == ToDoItemState.Active ? "🟢 Активна" : "✅ Завершена";

                // Экранируем state
                var escapedState = EscapeMarkdown(state);

                // Форматируем дедлайн с экранированием
                var deadlineText = task.Deadline.HasValue ?
                    $"\n   📅 Дедлайн: {EscapeMarkdown(task.Deadline.Value.ToString("dd\\.MM\\.yyyy"))}" : "";

                // Экранируем название задачи
                var escapedTaskName = EscapeMarkdown(task.Name);

                message += $"*{i + 1}\\.* {escapedTaskName} \n" +
                          $"   {escapedState}" +
                          $"{deadlineText}\n" +
                          $"   🕐 {FormatDateForMarkdown(task.CreatedAt)}\n" +
                          $"   🆔 `{task.Id}`\n\n";
            }

            await botClient.SendMessage(
                chat.Id,
                message,
                parseMode: ParseMode.MarkdownV2,
                replyMarkup: _mainKeyboard,
                cancellationToken: cancellationToken);
        }

        private async Task HandleCompleteTaskCommand(ITelegramBotClient botClient, Chat chat, string taskId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Не указан ID задачи. Использование: /completetask <ID_задачи>\n\n" +
                    "ID задачи можно посмотреть в командах /show",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            if (!Guid.TryParse(taskId, out Guid id))
            {
                await botClient.SendMessage(
                    chat.Id,
                    "❌ Неверный формат ID задачи.",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            try
            {
                await _toDoService.MarkCompletedAsync(id, cancellationToken);

                // Экранируем ID для Markdown
                var escapedId = EscapeMarkdown(id.ToString());

                await botClient.SendMessage(
                    chat.Id,
                    $"✅ Задача с ID `{escapedId}` завершена\\.",
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                // Экранируем сообщение об ошибке
                var escapedMessage = EscapeMarkdown(ex.Message);

                await botClient.SendMessage(
                    chat.Id,
                    $"❌ Ошибка при завершении задачи: {escapedMessage}",
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandleRemoveTaskCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, string taskNumber, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Сначала зарегистрируйтесь с помощью команды /start",
                    replyMarkup: _startKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(taskNumber))
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Не указан номер задачи. Использование: /removetask <номер_задачи>\n\n" +
                    "Номер задачи можно посмотреть в команде /show",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            try
            {
                var taskId = await ParseAndValidateTaskNumberAsync(taskNumber, user.Id, cancellationToken);
                await _toDoService.DeleteAsync(taskId, cancellationToken);
                await botClient.SendMessage(
                    chat.Id,
                    "✅ Задача успешно удалена.",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(
                    chat.Id,
                    $"❌ Ошибка при удалении задачи: {ex.Message}",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
        }

        private string FormatTasksList(IReadOnlyList<ToDoItem> tasks, string title, bool showList = false)
        {
            var message = new StringBuilder();
            message.AppendLine($"{title}\n");

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];

                // Проверяем, не превысим ли лимит Telegram (4096 символов)
                if (message.Length > 3500)
                {
                    message.AppendLine($"\n... и еще {tasks.Count - i} задач");
                    break;
                }

                var deadlineText = task.Deadline.HasValue ?
                    $"\n   📅 Дедлайн: {EscapeMarkdown(task.Deadline.Value.ToString("dd\\.MM\\.yyyy"))}" : "";

                message.AppendLine($"*{i + 1}\\.* {EscapeMarkdown(task.Name)}");
                message.AppendLine($"   🕐 {FormatDateForMarkdown(task.CreatedAt)}");

                if (!string.IsNullOrEmpty(deadlineText))
                    message.AppendLine(deadlineText);

                message.AppendLine($"   🆔 `{task.Id}`");
                message.AppendLine();
            }

            return message.ToString();
        }

        private string EscapeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Список всех специальных символов MarkdownV2
            var specialChars = new[] {
        '_', '*', '[', ']', '(', ')', '~', '`', '>', '#',
        '+', '-', '=', '|', '{', '}', '.', '!', ':', '\\'
    };

            var result = new StringBuilder();

            foreach (char ch in text)
            {
                if (Array.Exists(specialChars, c => c == ch))
                {
                    result.Append('\\');
                }
                result.Append(ch);
            }

            return result.ToString();
        }

        private string FormatDateForMarkdown(DateTime date)
        {
            return date.ToString("dd\\\\.MM\\\\.yyyy HH\\\\:mm\\\\:ss");
        }

        private IScenario GetScenario(ScenarioType scenario)
        {
            var handler = _scenarios.FirstOrDefault(s => s.CanHandle(scenario));

            if (handler == null)
            {
                throw new InvalidOperationException($"Сценарий {scenario} не найден");
            }

            return handler;
        }

        private async Task ProcessScenario(ITelegramBotClient botClient, ScenarioContext context, Message message, CancellationToken ct)
        {

            var scenario = GetScenario(context.CurrentScenario);
            var result = await scenario.HandleMessageAsync(botClient, context, message, ct);


            if (result == ScenarioResult.Completed)
            {
                await _contextRepository.ResetContext(message.From.Id, ct);
                await botClient.SendMessage(
                    message.Chat.Id,
                    "Сценарий завершен.",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: ct);
            }
            else
            {
                await _contextRepository.SetContext(message.From.Id, context, ct);
            }
        }

        private async Task<Guid> ParseAndValidateTaskNumberAsync(string taskNumber, Guid userId, CancellationToken cancellationToken)
        {
            if (!int.TryParse(taskNumber, out int number) || number < 1)
            {
                throw new ArgumentException("Неверный формат номера задачи.");
            }

            var tasks = await _toDoService.GetAllByUserIdAsync(userId, cancellationToken);
            if (number > tasks.Count)
            {
                throw new ArgumentException($"Задача с номером {number} не найдена.");
            }

            return tasks[number - 1].Id;
        }

        // Обработка ошибок
        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
