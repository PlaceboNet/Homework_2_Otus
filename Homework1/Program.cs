using Homework1.Core.DataAccess;
using Homework1.Core.Services;
using Homework1.Infrastructure.DataAccess;
using Homework1.Infrastructure.DataAccess.Repositories;
using Homework1.TelegramBot;
using Homework1.TelegramBot.Scenario;
using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Homework1.Core.Entities;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Scenarios;

namespace Homework1
{
    internal class Program
    {
        public static string iCanDo = "\nЯ могу выполнить несколько команд:" +
            "\n/start - программа просит ввести имя" +
            "\n/help - краткая справочная информация о том, как пользоваться программой" +
            "\n/info - предоставляет информацию о версии программы и дате её создания" +
            "\n/addtask - добавить новую задачу в список" +
            "\n/show - отобразить задачи по спискам" +
            "\n/report - показать статистику по задачам" +
            "\n/find - найти задачи по названию";

        public static string info = "Версия 0.0.1\n27.08.2025";
        public static string separation = "---------------------------";
        public static int MaxTask;
        public static int MaxLength;

        private static ITelegramBotClient? _botClient;
        private static CancellationTokenSource? _cts;

        // Обработчики событий
        private static void OnHandleUpdateStarted(string message)
        {
            // Логируем только команды
            if (message.StartsWith("/"))
            {
                Console.WriteLine($"Команда: {message}");
            }
        }

        private static void OnHandleUpdateCompleted(string message)
        {
            // Оставлено пустым
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine($"Привет! {iCanDo}");

            // Запрос максимального количества задач
            Console.WriteLine("Введите максимально допустимое количество задач");
            MaxTask = Convert.ToInt32(Console.ReadLine());
            if (MaxTask > 100 || MaxTask < 1)
            {
                throw new ArgumentException("Максимальное количество задач должно быть числом от 1 до 100.");
            }

            // Запрос максимальной длины задачи
            Console.WriteLine("Введите максимально допустимую длину задачи");
            MaxLength = Convert.ToInt32(Console.ReadLine());
            if (MaxLength > 100 || MaxLength < 1)
            {
                throw new ArgumentException("Максимально допустимая длина задачи должно быть числом от 1 до 100.");
            }

            // Строка подключения к PostgreSQL
            var connectionString = "Host=localhost;Port=5432;Database=ToDoList;Username=postgres;Password=8888;";

            // Создаем фабрику контекста данных
            var contextFactory = new DataContextFactory(connectionString);
            contextFactory.EnsureCreated();

            // Создаем файловые репозитории
            var currentDirectory = Directory.GetCurrentDirectory();
            var usersBasePath = Path.Combine(currentDirectory, "Data", "Users");
            var tasksBasePath = Path.Combine(currentDirectory, "Data", "Tasks");
            var listsBasePath = Path.Combine(currentDirectory, "Data", "Lists");

            // Создаем SQL репозитории
            var userRepository = new SqlUserRepository(contextFactory);
            var toDoRepository = new SqlToDoRepository(contextFactory);
            var listRepository = new SqlToDoListRepository(contextFactory);
            var contextRepository = new InMemoryScenarioContextRepository();

            // Создаем сервисы
            var userService = new UserService(userRepository);
            var toDoService = new ToDoService(toDoRepository, MaxTask, MaxLength);
            var listService = new ToDoListService(listRepository);
            var reportService = new ToDoReportService(toDoRepository);

            // Создаем сценарии
            var scenarios = new List<IScenario>
            {
                new AddTaskScenario(userService, toDoService, listService),
                new AddListScenario(userService, listService),
                new DeleteListScenario(userService, listService, toDoService),
                new DeleteTaskScenario(toDoService)
            };

            // Создаем обработчик
            var updateHandler = new UpdateHandler(userService, toDoService, reportService, listService, contextRepository, scenarios);

            // Подписываемся на события
            updateHandler.OnHandleUpdateStarted += OnHandleUpdateStarted;
            updateHandler.OnHandleUpdateCompleted += OnHandleUpdateCompleted;

            try
            {
                // ВСТРОЕННЫЙ ТОКЕН
                var botToken = "8497959131:AAEmacvZvvg-7LKyw4-gOftldYnsPchHeKU";

                // Создаем клиент Telegram Bot
                _botClient = new TelegramBotClient(botToken);
                _cts = new CancellationTokenSource();

                // Настраиваем команды бота
                await SetBotCommands(_botClient);

                // Настраиваем опции получения обновлений
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }, // ДОБАВИТЬ CallbackQuery!
                    DropPendingUpdates = true
                };

                // Запускаем получение обновлений
                _botClient.StartReceiving(
                    updateHandler: updateHandler,
                    receiverOptions: receiverOptions,
                    cancellationToken: _cts.Token
                );

                // Получаем информацию о боте
                User me;
                try
                {
                    me = await _botClient.GetMe();
                }
                catch
                {
                    me = _botClient.GetMe().GetAwaiter().GetResult();
                }

                Console.WriteLine($"{me.FirstName} запущен!");

                // Ожидаем нажатие 'A' для выхода
                Console.WriteLine("Нажмите 'A' для выхода");

                while (!_cts.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.A)
                        {
                            Console.WriteLine("\nЗавершение работы...");
                            _cts.Cancel();
                            break;
                        }
                    }
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске бота: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
            finally
            {
                // Отписываемся от событий
                updateHandler.OnHandleUpdateStarted -= OnHandleUpdateStarted;
                updateHandler.OnHandleUpdateCompleted -= OnHandleUpdateCompleted;
                _cts?.Dispose();
            }
        }

        // Метод для настройки команд бота
        private static async Task SetBotCommands(ITelegramBotClient botClient)
        {
            var commands = new[]
            {
        new BotCommand { Command = "start", Description = "Начать работу с ботом" },
        new BotCommand { Command = "help", Description = "Показать справку по командам" },
        new BotCommand { Command = "info", Description = "Информация о версии бота" },
        new BotCommand { Command = "addtask", Description = "Добавить новую задачу" },
        new BotCommand { Command = "show", Description = "Показать задачи по спискам" },
        new BotCommand { Command = "report", Description = "Статистика по задачам" },
        new BotCommand { Command = "find", Description = "Найти задачи по названию" },
        new BotCommand { Command = "cancel", Description = "Отменить текущий сценарий" }
    };

            try
            {
                await botClient.SetMyCommands(commands);
                Console.WriteLine("Команды бота успешно настроены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при настройке команд бота: {ex.Message}");
            }
        }
    }
}

    // Делегат для событий
    public delegate void MessageEventHandler(string message);