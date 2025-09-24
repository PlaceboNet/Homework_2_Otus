using Microsoft.VisualBasic;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Homework1
{
    internal class Program
    {
        private static string massege;
        private static string iCanDo = "\nЯ могу выполнить несколько команд:" +
                "\n/start - программа просит ввести имя" +
                "\n/help - краткая справочная информация о том, как пользоваться программой" +
                "\n/info - предоставляет информацию о версии программы и дате её создания" +
                "\n/echo - после ввода имени данная команда становится доступной" +
                "\n/exit - выйти из программы" +
                "\n/addtask - добавить новую задачу в список" +
                "\n/showtasks - отобразить список всех добавленных задач" +
                "\n/removetask - удалять задачи по номеру в списке\n";
        private static string info = "Версия 0.0.1\n27.08.2025";
        public static List<string> doList = new List<string>();
        public static string input;
        public static string echo;
        public static string separation = "---------------------------";

        static void Main(string[] args)
        {
            Console.WriteLine($"Привет! {iCanDo}");

            while (true)
            {
                Console.Write("Введите команду: ");
                input = Console.ReadLine();
                string echo = string.Empty;
                if (input.Contains(" "))
                {
                    echo = input.Split(" ")[1];
                    input = input.Split(" ")[0];
                }

                if (input == "/exit") break;

                switch (input)
                {
                    case "/start":
                        StartMessage();
                        break;
                    case "/help":
                        HelpMessage();
                        break;
                    case "/info":
                        InfoMessage();
                        break;
                    case "/echo":
                        EchoMessage(echo);
                        break;
                    case "/addtask":
                        AddTask();
                        break;
                    case "/showtasks":
                        ShowTasks();
                        break;
                    case "/removetask":
                        RemoveTask();
                        break;
                    default:
                        Console.WriteLine("Такой команды не знаю");
                        Console.WriteLine(separation);
                        break;
                }
            }
        }
        public static void StartMessage()
        {
            if(!string.IsNullOrEmpty(massege))
                return;
            Console.WriteLine("Введите имя:");
            massege = Console.ReadLine();
            Console.WriteLine($"Привет, {massege}");
            Console.WriteLine(separation);
        }
        public static void HelpMessage()
        {
            if (!string.IsNullOrEmpty(massege))
                Console.WriteLine($"Я с удовольствием напомню тебе, {massege}, что я могу сделать {iCanDo}");
            else Console.WriteLine($"Я с удовольствием напомню тебе, что я могу сделать {iCanDo}");
            Console.WriteLine(separation);

        }
        public static void InfoMessage()
        {
            if (!string.IsNullOrEmpty(massege))
                Console.WriteLine($"{massege},{info}");
            else Console.WriteLine(info);
            Console.WriteLine(separation);
        }
        public static void EchoMessage(string massege)
        {
            if (!string.IsNullOrEmpty(massege))
                Console.WriteLine(massege);
            else
                Console.WriteLine("Сначала представьтесь командой '/start'");
            Console.WriteLine(separation);

        }
        public static void AddTask()
        {
            Console.WriteLine("\nНапишите необходимую задачу:");
            string whatNeedDo = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(whatNeedDo))
            {
                Console.WriteLine("\nНапишите необходимую задачу:");
                whatNeedDo = Console.ReadLine();
            }
            doList.Add(whatNeedDo);
            Console.WriteLine(separation);
        }
        public static bool ShowTasks()
        {
            if (doList.Count == 0)
            {
                Console.WriteLine("Список для удаления пуст. Для начала введите задачи с помощью команды '/addtask'");
                Console.WriteLine(separation);
                return false;
            }
            Console.WriteLine("\nВот список дел:");
            for (int i = 0; i < doList.Count; i++)
            {
                Console.WriteLine($"{i+1} {doList[i]}");
                
            }
            Console.WriteLine(separation);
            return true;
        }
        public static void RemoveTask()
        {
            if (!ShowTasks())
                return;

            Console.WriteLine("Введите номер задачи для удаления:");
            string inputRemove = Console.ReadLine();

            int nTask = 0;
            if (!int.TryParse(inputRemove, out nTask))
            {
                Console.WriteLine("Неверный ввод. Укажите число.");
                Console.WriteLine(separation);
                return;
            }

            if (nTask > doList.Count || nTask <= 0)
            {
                Console.WriteLine("Нет задачи с таким номером, проверьте введенный номер.");
                Console.WriteLine(separation);
                return;
            }

            doList.RemoveAt(nTask - 1);
            Console.WriteLine("Задача успешно удалена.");
            Console.WriteLine(separation);
        }
    }
}
