using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Homework1.Core.Entities.ToDoItem;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;
using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using Homework1.Core.Services;

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
                var telegramUserName = update.Message.From.Username ?? $"User_{telegramUserId}";

                var user = await _userService.GetUserAsync(telegramUserId, cancellationToken);

                if (user == null && !messageText.StartsWith("/start") &&
                    !messageText.StartsWith("/help") && !messageText.StartsWith("/info"))
                {
                    await botClient.SendMessage(chat, "Пожалуйста, сначала зарегистрируйтесь с помощью команды /start", cancellationToken);
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
                            await botClient.SendMessage(chat, "До свидания!", cancellationToken);
                            break;
                        default:
                            await botClient.SendMessage(chat, "Такой команды не знаю", cancellationToken);
                            break;
                    }
                }
            }
            finally
            {
                // Вызываем событие завершения обработки
                OnHandleUpdateCompleted?.Invoke(messageText);
            }
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Вывод информации об ошибке в консоль
            Console.WriteLine($"Произошла ошибка: {exception.Message}");
            Console.WriteLine(Program.separation);
            return Task.CompletedTask;
        }

        private async Task HandleStartCommand(ITelegramBotClient botClient, Chat chat, long telegramUserId, string telegramUserName, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                user = await _userService.RegisterUserAsync(telegramUserId, telegramUserName, cancellationToken);
                await botClient.SendMessage(chat, $"Привет, {user.TelegramUserName}! Добро пожаловать!", cancellationToken);
            }
            else
            {
                await botClient.SendMessage(chat, $"С возвращением, {user.TelegramUserName}!", cancellationToken);
            }
        }

        private async Task HandleHelpCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var helpText = "\nЯ могу выполнить несколько команд:" +
                    "\n/start - программа просит ввести имя" +
                    "\n/help - краткая справочная информация о том, как пользоваться программой" +
                    "\n/info - предоставляет информацию о версии программы и дате её создания" +
                    "\n/exit - выйти из программы" +
                    "\n/addtask - добавить новую задачу в список" +
                    "\n/showtasks - отобразить список всех добавленных задач" +
                    "\n/showalltasks - выводить команды с любым State" +
                    "\n/removetask - удалять задачи по номеру в списке" +
                    "\n/completetask - найти задачу по id" +
                    "\n/report - показать статистику по задачам" +
                    "\n/find - найти задачи по названию\n";

            var message = user != null
                ? $"{user.TelegramUserName}, вот что я могу сделать:{helpText}"
                : $"Вот что я могу сделать:{helpText}";

            await botClient.SendMessage(chat, message, cancellationToken);
        }

        private async Task HandleInfoCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var info = "Версия 0.0.1\n27.08.2025";
            var message = user != null
                ? $"{user.TelegramUserName}, {info}"
                : info;

            await botClient.SendMessage(chat, message, cancellationToken);
        }

        private async Task HandleAddTaskCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, string taskName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(chat, "Сначала зарегистрируйтесь с помощью команды /start", cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(taskName))
            {
                await botClient.SendMessage(chat, "Пожалуйста, укажите название задачи. Пример: /addtask Новая задача", cancellationToken);
                return;
            }

            try
            {
                var task = await _toDoService.AddAsync(user, taskName.Trim(), cancellationToken);
                await botClient.SendMessage(chat, $"Задача добавлена с ID: {task.Id}", cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chat, $"Ошибка при добавлении задачи: {ex.Message}", cancellationToken);
            }
        }

        private async Task HandleShowTasksCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(chat, "Сначала зарегистрируйтесь с помощью команды /start", cancellationToken);
                return;
            }

            var activeTasks = await _toDoService.GetActiveByUserIdAsync(user.Id, cancellationToken);

            if (activeTasks.Count == 0)
            {
                await botClient.SendMessage(chat, "Список активных задач пуст. Для начала добавьте задачи с помощью команды '/addtask'", cancellationToken);
                return;
            }

            var message = "Вот список активных дел:\n";
            for (int i = 0; i < activeTasks.Count; i++)
            {
                var task = activeTasks[i];
                message += $"{i + 1}. {task.Name} - {task.CreatedAt:dd.MM.yyyy HH:mm:ss} - {task.Id}\n";
            }

            await botClient.SendMessage(chat, message, cancellationToken);
        }

        private async Task HandleShowAllTasksCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(chat, "Сначала зарегистрируйтесь с помощью команды /start", cancellationToken);
                return;
            }

            var allTasks = await _toDoService.GetAllByUserIdAsync(user.Id, cancellationToken);

            if (allTasks.Count == 0)
            {
                await botClient.SendMessage(chat, "Список задач пуст. Для начала добавьте задачи с помощью команды '/addtask'", cancellationToken);
                return;
            }

            var message = "Вот список всех задач:\n";
            for (int i = 0; i < allTasks.Count; i++)
            {
                var task = allTasks[i];
                string state = task.State == ToDoItemState.Active ? "(Active)" : "(Completed)";
                message += $"{i + 1}. {state} {task.Name} - {task.CreatedAt:dd.MM.yyyy HH:mm:ss} - {task.Id}\n";
            }

            await botClient.SendMessage(chat, message, cancellationToken);
        }

        private async Task HandleCompleteTaskCommand(ITelegramBotClient botClient, Chat chat, string taskId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                await botClient.SendMessage(chat, "Не указан ID задачи. Использование: /completetask <ID_задачи>", cancellationToken);
                return;
            }

            if (!Guid.TryParse(taskId, out Guid id))
            {
                await botClient.SendMessage(chat, "Неверный формат ID задачи.", cancellationToken);
                return;
            }

            try
            {
                await _toDoService.MarkCompletedAsync(id, cancellationToken);
                await botClient.SendMessage(chat, $"Задача с ID {id} завершена.", cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chat, $"Ошибка при завершении задачи: {ex.Message}", cancellationToken);
            }
        }

        private async Task HandleRemoveTaskCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, string taskNumber, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(chat, "Сначала зарегистрируйтесь с помощью команды /start", cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(taskNumber))
            {
                await botClient.SendMessage(chat, "Не указан номер задачи. Использование: /removetask <номер_задачи>", cancellationToken);
                return;
            }

            try
            {
                var taskId = await ParseAndValidateTaskNumberAsync(taskNumber, user.Id, cancellationToken);
                await _toDoService.DeleteAsync(taskId, cancellationToken);
                await botClient.SendMessage(chat, "Задача успешно удалена.", cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chat, $"Ошибка при удалении задачи: {ex.Message}", cancellationToken);
            }
        }

        private async Task HandleReportCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(chat, "Сначала зарегистрируйтесь с помощью команды /start", cancellationToken);
                return;
            }

            var stats = await _reportService.GetUserStatsAsync(user.Id, cancellationToken);
            var message = $"Статистика по задачам на {stats.generatedAt:dd.MM.yyyy HH:mm:ss}. " +
                         $"Всего: {stats.total}; Завершенных: {stats.completed}; Активных: {stats.active};";

            await botClient.SendMessage(chat, message, cancellationToken);
        }

        private async Task HandleFindCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, string namePrefix, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(chat, "Сначала зарегистрируйтесь с помощью команды /start", cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(namePrefix))
            {
                await botClient.SendMessage(chat, "Пожалуйста, укажите начало названия задачи. Пример: /find Позвонить", cancellationToken);
                return;
            }

            var foundTasks = await _toDoService.FindAsync(user, namePrefix, cancellationToken);

            if (foundTasks.Count == 0)
            {
                await botClient.SendMessage(chat, $"Задачи, начинающиеся на '{namePrefix}', не найдены.", cancellationToken);
                return;
            }

            var message = $"Найдено задач ({foundTasks.Count}):\n";
            for (int i = 0; i < foundTasks.Count; i++)
            {
                var task = foundTasks[i];
                string state = task.State == ToDoItemState.Active ? "(Active)" : "(Completed)";
                message += $"{i + 1}. {state} {task.Name} - {task.CreatedAt:dd.MM.yyyy HH:mm:ss} - {task.Id}\n";
            }

            await botClient.SendMessage(chat, message, cancellationToken);
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
    }
}
