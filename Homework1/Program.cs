using Homework1.Core.DataAccess;
using Homework1.Core.Services;
using Homework1.Infrastructure.DataAccess;
using Homework1.TelegramBot;
using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Homework1.Core.Entities.ToDoItem;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Homework1
{
    internal class Program
    {
        public static string iCanDo = "\nЯ могу выполнить несколько команд:" +
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
        public static string info = "Версия 0.0.1\n27.08.2025";
        public static string separation = "---------------------------";
        public static int MaxTask;
        public static int MaxLength;

        private static ITelegramBotClient? _botClient;
        private static CancellationTokenSource? _cts;

        // Обработчики событий
        private static void OnHandleUpdateStarted(string message)
        {
            Console.WriteLine($"Началась обработка сообщения '{message}'");
        }

        private static void OnHandleUpdateCompleted(string message)
        {
            Console.WriteLine($"Закончилась обработка сообщения '{message}'");
            Console.WriteLine(separation);
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine($"Привет! {iCanDo}");
            Console.WriteLine("Введите максимально допустимое количество задач");
            MaxTask = Convert.ToInt32(Console.ReadLine());
            if (MaxTask > 100 || MaxTask < 1)
            {
                throw new ArgumentException("Максимальное количество задач должно быть числом от 1 до 100.");
            }
            Console.WriteLine("Введите максимально допустимую длину задачи");
            MaxLength = Convert.ToInt32(Console.ReadLine());
            if (MaxLength > 100 || MaxLength < 1)
            {
                throw new ArgumentException("Максимально допустимая длина задачи должно быть числом от 1 до 100.");
            }

            // Создаем файловые репозитории
            var usersBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Users");
            var tasksBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Tasks");

            var userRepository = new FileUserRepository(usersBasePath);
            var toDoRepository = new FileToDoRepository(tasksBasePath);

            // Создаем сервисы
            var userService = new UserService(userRepository);
            var toDoService = new ToDoService(toDoRepository, MaxTask, MaxLength);
            var reportService = new ToDoReportService(toDoRepository);

            // Создаем обработчик
            var updateHandler = new UpdateHandler(userService, toDoService, reportService);

            // Подписываемся на события
            updateHandler.OnHandleUpdateStarted += OnHandleUpdateStarted;
            updateHandler.OnHandleUpdateCompleted += OnHandleUpdateCompleted;

            try
            {
                // ВСТРОЕННЫЙ ТОКЕН
                var botToken = "8497959131:AAHM51vUlbO2gdk4PxdlYsfpwXobLyLzEpU";

                // Создаем клиент Telegram Bot
                _botClient = new TelegramBotClient(botToken);
                _cts = new CancellationTokenSource();

                // Настраиваем команды бота
                await SetBotCommands(_botClient);

                // Настраиваем опции получения обновлений
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[] { UpdateType.Message },
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

                // Ожидаем нажатия клавиши A для выхода
                Console.WriteLine("Нажмите клавишу A для выхода");

                while (!_cts.Token.IsCancellationRequested)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.A)
                    {
                        Console.WriteLine("\nЗавершение работы...");
                        _cts.Cancel();
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"\nБот: {me.FirstName} (@{me.Username})");
                        Console.WriteLine("Нажмите клавишу A для выхода");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
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
            new BotCommand { Command = "showtasks", Description = "Показать активные задачи" },
            new BotCommand { Command = "showalltasks", Description = "Показать все задачи" },
            new BotCommand { Command = "removetask", Description = "Удалить задачу по номеру" },
            new BotCommand { Command = "completetask", Description = "Завершить задачу по ID" },
            new BotCommand { Command = "report", Description = "Статистика по задачам" },
            new BotCommand { Command = "find", Description = "Найти задачи по названию" }
        };

            await botClient.SetMyCommands(commands);
        }
    }
}