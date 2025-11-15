using Homework1.Core.DataAccess;
using Homework1.Core.Services;
using Homework1.Infrastructure.DataAccess;
using Homework1.TelegramBot;
using Microsoft.VisualBasic;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using static Homework1.Core.Entities.ToDoItem;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Homework1
{
    internal class Program
    {
        private static string iCanDo = "\nЯ могу выполнить несколько команд:" +
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
        private static string info = "Версия 0.0.1\n27.08.2025";
        public static string separation = "---------------------------";
        public static int MaxTask;
        public static int MaxLength;

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

            // Создаем репозитории
            var userRepository = new InMemoryUserRepository();
            var toDoRepository = new InMemoryToDoRepository();

            // Создаем сервисы
            var userService = new UserService(userRepository);
            var toDoService = new ToDoService(toDoRepository, MaxTask, MaxLength);
            var reportService = new ToDoReportService(toDoRepository);

            // Создаем обработчик
            var updateHandler = new UpdateHandler(userService, toDoService, reportService);

            // Подписываемся на события
            updateHandler.OnHandleUpdateStarted += OnHandleUpdateStarted;
            updateHandler.OnHandleUpdateCompleted += OnHandleUpdateCompleted;

            // Создаем CancellationTokenSource
            using var cts = new CancellationTokenSource();

            try
            {
                // Запускаем бота с CancellationToken
                var botClient = new ConsoleBotClient();
                botClient.StartReceiving(updateHandler, cts.Token);
            }
            finally
            {
                // Отписываемся от событий
                updateHandler.OnHandleUpdateStarted -= OnHandleUpdateStarted;
                updateHandler.OnHandleUpdateCompleted -= OnHandleUpdateCompleted;
            }
        }

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
    }
}