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
        new KeyboardButton[] { "/addtask", "/showtasks" },
        new KeyboardButton[] { "/showalltasks", "/find" },
        new KeyboardButton[] { "/removetask", "/completetask" },
        new KeyboardButton[] { "/report", "/help", "/info" }
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
            IScenarioContextRepository contextRepository,
            IEnumerable<IScenario> scenarios)
        {
            _userService = userService;
            _toDoService = toDoService;
            _reportService = reportService;
            _contextRepository = contextRepository;
            _scenarios = scenarios.ToList();
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var messageText = update.Message?.Text ?? string.Empty;

            try
            {
                OnHandleUpdateStarted?.Invoke(messageText);

                if (update.Message?.Text == null)
                    return;

                var chat = update.Message.Chat;
                var telegramUserId = update.Message.From.Id;

                // Проверяем команду /cancel до обработки сценариев
                if (messageText.StartsWith("/cancel"))
                {
                    await _contextRepository.ResetContext(telegramUserId, cancellationToken);
                    await botClient.SendMessage(
                        chat.Id,
                        "Сценарий отменен.",
                        replyMarkup: _mainKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }

                // Проверяем активный сценарий
                var context = await _contextRepository.GetContext(telegramUserId, cancellationToken);
                if (context != null && context.CurrentScenario != ScenarioType.None)
                {
                    await ProcessScenario(botClient, context, update.Message, cancellationToken);
                    return;
                }

                // Обработка обычных команд
                await HandleRegularCommands(botClient, update, cancellationToken);
            }
            finally
            {
                OnHandleUpdateCompleted?.Invoke(messageText);
            }
        }

        private async Task HandleRegularCommands(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var chat = update.Message.Chat;
            var telegramUserId = update.Message.From.Id;
            var telegramUserName = update.Message.From.Username ??
                                  $"{update.Message.From.FirstName} {update.Message.From.LastName}".Trim();
            var messageText = update.Message.Text;

            var user = await _userService.GetUserAsync(telegramUserId, cancellationToken);
            var keyboard = user == null ? _startKeyboard : _mainKeyboard;

            if (user == null && !messageText.StartsWith("/start") &&
                !messageText.StartsWith("/help") && !messageText.StartsWith("/info"))
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Пожалуйста, сначала зарегистрируйтесь с помощью команды /start",
                    replyMarkup: _startKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            if (messageText.StartsWith("/"))
            {
                var commandParts = messageText.Split(' ', 2);
                var command = commandParts[0];
                var argument = commandParts.Length > 1 ? commandParts[1] : string.Empty;

                switch (command)
                {
                    case "/start":
                        await HandleStartCommand(botClient, chat, telegramUserId, telegramUserName, cancellationToken);
                        break;
                    case "/help":
                        await HandleHelpCommand(botClient, chat, user, cancellationToken);
                        break;
                    case "/info":
                        await HandleInfoCommand(botClient, chat, user, cancellationToken);
                        break;
                    case "/addtask":
                        await HandleAddTaskCommand(botClient, chat, telegramUserId, cancellationToken);
                        break;
                    case "/showtasks":
                        await HandleShowTasksCommand(botClient, chat, user, cancellationToken);
                        break;
                    case "/showalltasks":
                        await HandleShowAllTasksCommand(botClient, chat, user, cancellationToken);
                        break;
                    case "/completetask":
                        await HandleCompleteTaskCommand(botClient, chat, argument, cancellationToken);
                        break;
                    case "/removetask":
                        await HandleRemoveTaskCommand(botClient, chat, user, argument, cancellationToken);
                        break;
                    case "/report":
                        await HandleReportCommand(botClient, chat, user, cancellationToken);
                        break;
                    case "/find":
                        await HandleFindCommand(botClient, chat, user, argument, cancellationToken);
                        break;
                    case "/exit":
                        await botClient.SendMessage(
                            chat.Id,
                            "До свидания!",
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken);
                        break;
                    default:
                        await botClient.SendMessage(
                            chat.Id,
                            "Такой команды не знаю",
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken);
                        break;
                }
            }
            else
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Пожалуйста, используйте команды из меню или введите /help для справки",
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
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
            var helpText = Program.iCanDo;
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

            var context = new ScenarioContext(ScenarioType.AddTask);
            await _contextRepository.SetContext(telegramUserId, context, cancellationToken);

            await botClient.SendMessage(
                chat.Id,
                "Начинаем добавление задачи. Используйте /cancel для отмены.",
                replyMarkup: _scenarioKeyboard,
                cancellationToken: cancellationToken);

            await ProcessScenario(botClient, context, new Message
            {
                Chat = chat,
                From = new User { Id = telegramUserId }
            }, cancellationToken);
        }

        private async Task HandleShowTasksCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
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

            var activeTasks = await _toDoService.GetActiveByUserIdAsync(user.Id, cancellationToken);

            if (activeTasks.Count == 0)
            {
                await botClient.SendMessage(
                    chat.Id,
                    "📭 Список активных задач пуст.\nИспользуйте /addtask чтобы добавить первую задачу.",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            // Проверяем длину сообщения
            var message = FormatTasksList(activeTasks, "📋 *Активные задачи:*");

            // Отправляем отладочную информацию в консоль
            Console.WriteLine($"Длина сообщения: {message.Length}");
            Console.WriteLine($"Первые 500 символов сообщения: {message.Substring(0, Math.Min(500, message.Length))}");

            try
            {
                await botClient.SendMessage(
                    chat.Id,
                    message,
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");

                // Пробуем отправить без Markdown
                await botClient.SendMessage(
                    chat.Id,
                    "Не удалось отформатировать список задач. Пожалуйста, попробуйте еще раз.",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandleShowAllTasksCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
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

            var allTasks = await _toDoService.GetAllByUserIdAsync(user.Id, cancellationToken);

            if (allTasks.Count == 0)
            {
                await botClient.SendMessage(
                    chat.Id,
                    "📭 Список задач пуст.\nИспользуйте /addtask чтобы добавить первую задачу.",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            // Проверяем длину сообщения
            var message = FormatTasksList(allTasks, "📚 *Все задачи:*", showState: true);

            Console.WriteLine($"Длина сообщения (все задачи): {message.Length}");

            try
            {
                await botClient.SendMessage(
                    chat.Id,
                    message,
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения (все задачи): {ex.Message}");

                // Пробуем разбить на несколько сообщений
                await SendTasksInChunks(botClient, chat, allTasks, "Все задачи", cancellationToken);
            }
        }

        private string FormatTasksList(IReadOnlyList<ToDoItem> tasks, string title, bool showState = false)
        {
            var message = $"{title}\n\n";

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];

                // Проверяем, не превысим ли лимит Telegram
                if (message.Length > 3500) // Оставляем запас
                {
                    message += $"\n... и еще {tasks.Count - i} задач";
                    break;
                }

                string state = "";
                if (showState)
                {
                    state = task.State == ToDoItemState.Active ? "🟢 Активна" : "✅ Завершена";
                }

                var deadlineText = task.Deadline.HasValue ?
                    $"\n   📅 Дедлайн: {EscapeMarkdown(task.Deadline.Value.ToString("dd\\.MM\\.yyyy"))}" : "";

                message += $"*{i + 1}\\.* {EscapeMarkdown(task.Name)} \n" +
                          $"{(showState ? $"   {EscapeMarkdown(state)}\n" : "")}" +
                          $"   🕐 {FormatDateForMarkdown(task.CreatedAt)}" +
                          $"{deadlineText}\n" +
                          $"   🆔 `{task.Id}`\n\n";
            }

            return message;
        }

        private async Task SendTasksInChunks(ITelegramBotClient botClient, Chat chat, IReadOnlyList<ToDoItem> tasks, string title, CancellationToken cancellationToken)
        {
            var chunkSize = 10; // По 10 задач в сообщении
            var chunks = tasks.Select((task, index) => new { task, index })
                             .GroupBy(x => x.index / chunkSize)
                             .Select(g => g.Select(x => x.task).ToList())
                             .ToList();

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var message = $"{title} (часть {i + 1}/{chunks.Count}):\n\n";

                for (int j = 0; j < chunk.Count; j++)
                {
                    var task = chunk[j];
                    var globalIndex = i * chunkSize + j + 1;
                    message += $"{globalIndex}. {task.Name} \n" +
                              $"   🆔 {task.Id}\n" +
                              $"   📅 {task.CreatedAt:dd.MM.yyyy HH:mm}\n\n";
                }

                await botClient.SendMessage(
                    chat.Id,
                    message,
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);

                // Небольшая пауза между сообщениями
                await Task.Delay(500, cancellationToken);
            }
        }

        private async Task HandleCompleteTaskCommand(ITelegramBotClient botClient, Chat chat, string taskId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Не указан ID задачи. Использование: /completetask <ID_задачи>\n\n" +
                    "ID задачи можно посмотреть в командах /showtasks или /showalltasks",
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
                await botClient.SendMessage(
                    chat.Id,
                    $"✅ Задача с ID `{id}` завершена\\.",
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(
                    chat.Id,
                    $"❌ Ошибка при завершении задачи: {EscapeMarkdown(ex.Message)}",
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
                    "Номер задачи можно посмотреть в командах /showtasks или /showalltasks",
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

            var message = $"🔍 *Найдено задач \\({foundTasks.Count}\\):*\n\n";
            for (int i = 0; i < foundTasks.Count; i++)
            {
                var task = foundTasks[i];
                string state = task.State == ToDoItemState.Active ? "🟢 Активна" : "✅ Завершена";
                var deadlineText = task.Deadline.HasValue ?
                    $"\n   📅 Дедлайн: {task.Deadline.Value:dd\\.MM\\.yyyy}" : "";
                message += $"*{i + 1}\\.* {EscapeMarkdown(task.Name)} \n" +
                          $"   {state}" +
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

        private IScenario GetScenario(ScenarioType scenario)
        {
            var handler = _scenarios.FirstOrDefault(s => s.CanHandle(scenario));
            return handler ?? throw new InvalidOperationException($"Сценарий {scenario} не найден");
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

        // Улучшенный метод экранирования
        private string EscapeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Список специальных символов MarkdownV2
            var specialChars = new[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!', ':', '\\' };
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
            // Экранируем все специальные символы в дате
            return date.ToString("dd\\\\.MM\\\\.yyyy HH\\\\:mm\\\\:ss");
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
