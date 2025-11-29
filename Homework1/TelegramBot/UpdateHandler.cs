using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using Homework1.Core.Services;
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
        new KeyboardButton[] { "/showtasks", "/showalltasks" },
        new KeyboardButton[] { "/report", "/help" },
        new KeyboardButton[] { "/addtask", "/find" }
    })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        public UpdateHandler(IUserService userService, IToDoService toDoService, IToDoReportService reportService)
        {
            _userService = userService;
            _toDoService = toDoService;
            _reportService = reportService;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var messageText = update.Message?.Text ?? string.Empty;

            try
            {
                // Вызываем событие начала обработки
                OnHandleUpdateStarted?.Invoke(messageText);

                if (update.Message?.Text == null)
                    return;

                var chat = update.Message.Chat;
                var telegramUserId = update.Message.From.Id;
                var telegramUserName = update.Message.From.Username ??
                                      $"{update.Message.From.FirstName} {update.Message.From.LastName}".Trim();

                var user = await _userService.GetUserAsync(telegramUserId, cancellationToken);

                // Определяем клавиатуру в зависимости от статуса пользователя
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
                            await HandleAddTaskCommand(botClient, chat, user, argument, cancellationToken);
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
                    // Обработка обычных сообщений (не команд)
                    await botClient.SendMessage(
                        chat.Id,
                        "Пожалуйста, используйте команды из меню или введите /help для справки",
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken);
                }
            }
            finally
            {
                // Вызываем событие завершения обработки
                OnHandleUpdateCompleted?.Invoke(messageText);
            }
        }

        // Исправленный метод HandleErrorAsync с правильной сигнатурой
        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Вывод информации об ошибке в консоль
            Console.WriteLine($"Произошла ошибка при опросе: {exception.Message}");
            if (exception.InnerException != null)
            {
                Console.WriteLine($"Внутренняя ошибка: {exception.InnerException.Message}");
            }
            Console.WriteLine(Program.separation);

            // Ждем некоторое время перед повторной попыткой
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        // Альтернативный вариант - если нужен HandleErrorAsync с HandleErrorSource
        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            // Вывод информации об ошибке в консоль с указанием источника
            Console.WriteLine($"Произошла ошибка (источник: {source}): {exception.Message}");
            if (exception.InnerException != null)
            {
                Console.WriteLine($"Внутренняя ошибка: {exception.InnerException.Message}");
            }
            Console.WriteLine(Program.separation);

            return Task.CompletedTask;
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

        private async Task HandleAddTaskCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, string taskName, CancellationToken cancellationToken)
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

            if (string.IsNullOrWhiteSpace(taskName))
            {
                await botClient.SendMessage(
                    chat.Id,
                    "Пожалуйста, укажите название задачи. Пример: /addtask Новая задача",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            try
            {
                var task = await _toDoService.AddAsync(user, taskName.Trim(), cancellationToken);
                await botClient.SendMessage(
                    chat.Id,
                    $"✅ Задача добавлена с ID: `{task.Id}`",
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(
                    chat.Id,
                    $"❌ Ошибка при добавлении задачи: {ex.Message}",
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
            }
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

            var message = "📋 *Активные задачи:*\n\n";
            for (int i = 0; i < activeTasks.Count; i++)
            {
                var task = activeTasks[i];
                message += $"*{i + 1}\\.* {EscapeMarkdown(task.Name)} \n" +
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

            var message = "📚 *Все задачи:*\n\n";
            for (int i = 0; i < allTasks.Count; i++)
            {
                var task = allTasks[i];
                string state = task.State == ToDoItemState.Active ? "🟢 Активна" : "✅ Завершена";
                message += $"*{i + 1}\\.* {EscapeMarkdown(task.Name)} \n" +
                          $"   {state}\n" +
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
                    $"🔍 Задачи, начинающиеся на '{EscapeMarkdown(namePrefix)}', не найдены\\.",
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: _mainKeyboard,
                    cancellationToken: cancellationToken);
                return;
            }

            var message = $"🔍 *Найдено задач \\({foundTasks.Count}\\):*\n\n";
            for (int i = 0; i < foundTasks.Count; i++)
            {
                var task = foundTasks[i];
                string state = task.State == ToDoItemState.Active ? "🟢 Активна" : "✅ Завершена";
                message += $"*{i + 1}\\.* {EscapeMarkdown(task.Name)} \n" +
                          $"   {state}\n" +
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

        // Метод для экранирования специальных символов Markdown
        private string EscapeMarkdown(string text)
        {
            var specialChars = new[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!', ':', '\\' };
            foreach (var ch in specialChars)
            {
                text = text.Replace(ch.ToString(), $"\\{ch}");
            }
            return text;
        }

        private string FormatDateForMarkdown(DateTime date)
        {
            return date.ToString("dd\\\\.MM\\\\.yyyy HH\\\\:mm\\\\:ss");
        }
    }
}
