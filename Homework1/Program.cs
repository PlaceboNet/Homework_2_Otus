using Microsoft.VisualBasic;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using static Homework1.ToDoItem;
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
            "\n/removetask - удалять задачи по номеру в списке" +
            "\n/completetask - найти задачу по id" +
            "\n/showalltasks - выводить команды с любым State\n";
        private static string info = "Версия 0.0.1\n27.08.2025";
        public static string separation = "---------------------------";
        public static int MaxTask;
        public static int MaxLength;

        static void Main(Update update, ConsoleBotClient botClient)
        {
            do
            {
                try
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
                    Console.Write("Введите команду: ");
                    var input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input))
                    {
                        Console.WriteLine("Пустая команда. Пожалуйста, введите команду из списка:");
                        Console.WriteLine(iCanDo);
                        Console.WriteLine(separation);
                        continue;
                    }

                    if (input == "/exit")
                        break;

                    var userService = new UserService();
                    var toDoService = new ToDoService(MaxTask, MaxLength);
                    var updateHandler = new UpdateHandler(userService, toDoService);
                    updateHandler.HandleUpdateAsync(botClient, update);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Произошла ошибка: {ex.Message}");
                    Console.WriteLine(separation);
                }
                {

                }
            }


            while (true);
        }
    }
}