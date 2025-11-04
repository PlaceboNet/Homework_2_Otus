using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Homework1.ToDoItem;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace Homework1
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;

        public UpdateHandler(IUserService userService, IToDoService toDoService)
        {
            _userService = userService;
            _toDoService = toDoService;
        }

        public void HandleUpdateAsync(ITelegramBotClient botClient, Update update)
        {
            try
            {
                if (update.Message?.Text == null)
                    return;

                var messageText = update.Message.Text;
                var chat = update.Message.Chat;
                var telegramUserId = update.Message.From.Id;
                var telegramUserName = update.Message.From.Username ?? $"User_{telegramUserId}";

                var user = _userService.GetUser(telegramUserId);

                if (user == null && !messageText.StartsWith("/start") &&
                    !messageText.StartsWith("/help") && !messageText.StartsWith("/info"))
                {
                    // Просто вызываем метод без .GetAwaiter().GetResult()
                    botClient.SendMessage(chat, "Пожалуйста, сначала зарегистрируйтесь с помощью команды /start");
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
                            HandleStartCommand(botClient, chat, telegramUserId, telegramUserName);
                            break;
                        case "/help":
                            HandleHelpCommand(botClient, chat, user);
                            break;
                        case "/info":
                            HandleInfoCommand(botClient, chat, user);
                            break;
                        case "/addtask":
                            HandleAddTaskCommand(botClient, chat, user, argument);
                            break;
                        case "/showtasks":
                            HandleShowTasksCommand(botClient, chat, user);
                            break;
                        case "/showalltasks":
                            HandleShowAllTasksCommand(botClient, chat, user);
                            break;
                        case "/completetask":
                            HandleCompleteTaskCommand(botClient, chat, argument);
                            break;
                        case "/removetask":
                            HandleRemoveTaskCommand(botClient, chat, user, argument);
                            break;
                        case "/exit":
                            botClient.SendMessage(chat, "До свидания!");
                            break;
                        default:
                            botClient.SendMessage(chat, "Такой команды не знаю");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (update.Message != null)
                {
                    botClient.SendMessage(update.Message.Chat, $"Произошла ошибка: {ex.Message}");
                }
            }
        }

        private void HandleStartCommand(ITelegramBotClient botClient, Chat chat, long telegramUserId, string telegramUserName)
        {
            var user = _userService.GetUser(telegramUserId);
            if (user == null)
            {
                user = _userService.RegisterUser(telegramUserId, telegramUserName);
                botClient.SendMessage(chat, $"Привет, {user.TelegramUserName}! Добро пожаловать!");
            }
            else
            {
                botClient.SendMessage(chat, $"С возвращением, {user.TelegramUserName}!");
            }
        }

        private void HandleHelpCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user)
        {
            var helpText = "\nЯ могу выполнить несколько команд:" +
                    "\n/start - программа просит ввести имя" +
                    "\n/help - краткая справочная информация о том, как пользоваться программой" +
                    "\n/info - предоставляет информацию о версии программы и дате её создания" +
                    "\n/exit - выйти из программы" +
                    "\n/addtask - добавить новую задачу в список" +
                    "\n/showtasks - отобразить список всех добавленных задач" +
                    "\n/removetask - удалять задачи по номеру в списке" +
                    "\n/completetask - найти задачу по id" +
                    "\n/showalltasks - выводить команды с любым State\n";

            var message = user != null
                ? $"{user.TelegramUserName}, вот что я могу сделать:{helpText}"
                : $"Вот что я могу сделать:{helpText}";

            botClient.SendMessage(chat, message);
        }

        private void HandleInfoCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user)
        {
            var info = "Версия 0.0.1\n27.08.2025";
            var message = user != null
                ? $"{user.TelegramUserName}, {info}"
                : info;

            botClient.SendMessage(chat, message);
        }

        private void HandleAddTaskCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, string taskName)
        {
            if (user == null)
            {
                botClient.SendMessage(chat, "Сначала зарегистрируйтесь с помощью команды /start");
                return;
            }

            if (string.IsNullOrWhiteSpace(taskName))
            {
                botClient.SendMessage(chat, "Пожалуйста, укажите название задачи. Пример: /addtask Новая задача");
                return;
            }

            try
            {
                var task = _toDoService.Add(user, taskName.Trim());
                botClient.SendMessage(chat, $"Задача добавлена с ID: {task.Id}");
            }
            catch (Exception ex)
            {
                botClient.SendMessage(chat, $"Ошибка при добавлении задачи: {ex.Message}");
            }
        }

        private void HandleShowTasksCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user)
        {
            if (user == null) return;

            var activeTasks = _toDoService.GetActiveByUserId(user.Id);

            if (activeTasks.Count == 0)
            {
                botClient.SendMessage(chat, "Список активных задач пуст. Для начала добавьте задачи с помощью команды '/addtask'");
                return;
            }

            var message = "Вот список активных дел:\n";
            for (int i = 0; i < activeTasks.Count; i++)
            {
                var task = activeTasks[i];
                message += $"{i + 1}. {task.Name} - {task.CreatedAt:dd.MM.yyyy HH:mm:ss} - {task.Id}\n";
            }

            botClient.SendMessage(chat, message);
        }

        private void HandleShowAllTasksCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user)
        {
            if (user == null) return;

            var allTasks = _toDoService.GetAllByUserId(user.Id);

            if (allTasks.Count == 0)
            {
                botClient.SendMessage(chat, "Список задач пуст. Для начала добавьте задачи с помощью команды '/addtask'");
                return;
            }

            var message = "Вот список всех задач:\n";
            for (int i = 0; i < allTasks.Count; i++)
            {
                var task = allTasks[i];
                string state = task.State == ToDoItemState.Active ? "(Active)" : "(Completed)";
                message += $"{i + 1}. {state} {task.Name} - {task.CreatedAt:dd.MM.yyyy HH:mm:ss} - {task.Id}\n";
            }

            botClient.SendMessage(chat, message);
        }

        private void HandleCompleteTaskCommand(ITelegramBotClient botClient, Chat chat, string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                botClient.SendMessage(chat, "Не указан ID задачи. Использование: /completetask <ID_задачи>");
                return;
            }

            if (!Guid.TryParse(taskId, out Guid id))
            {
                botClient.SendMessage(chat, "Неверный формат ID задачи.");
                return;
            }

            try
            {
                _toDoService.MarkCompleted(id);
                botClient.SendMessage(chat, $"Задача с ID {id} завершена.");
            }
            catch (Exception ex)
            {
                botClient.SendMessage(chat, $"Ошибка при завершении задачи: {ex.Message}");
            }
        }

        private void HandleRemoveTaskCommand(ITelegramBotClient botClient, Chat chat, ToDoUser user, string taskNumber)
        {
            if (user == null) return;

            if (string.IsNullOrWhiteSpace(taskNumber))
            {
                botClient.SendMessage(chat, "Не указан номер задачи. Использование: /removetask <номер_задачи>");
                return;
            }

            try
            {
                var taskId = ParseAndValidateTaskNumber(taskNumber, user.Id);
                _toDoService.Delete(taskId);
                botClient.SendMessage(chat, "Задача успешно удалена.");
            }
            catch (Exception ex)
            {
                botClient.SendMessage(chat, $"Ошибка при удалении задачи: {ex.Message}");
            }
        }

        private Guid ParseAndValidateTaskNumber(string taskNumber, Guid userId)
        {
            if (!int.TryParse(taskNumber, out int number) || number < 1)
            {
                throw new ArgumentException("Неверный формат номера задачи.");
            }

            var tasks = _toDoService.GetAllByUserId(userId);
            if (number > tasks.Count)
            {
                throw new ArgumentException($"Задача с номером {number} не найдена.");
            }

            return tasks[number - 1].Id;
        }
    }
}
