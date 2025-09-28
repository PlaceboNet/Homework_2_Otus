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
        public static int MaxTask;
        public static int MaxLength;
        static void Main(string[] args)
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
            while (true)
            {
                try
                {
                    Console.Write("Введите команду: ");
                    input = Console.ReadLine();
                    string echo = string.Empty;
                    if (input.Contains(" "))
                    {
                        echo = input.Substring(input.Split(" ")[0].Length).Trim();
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
                            UnknownCommand();
                            break;
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(separation);
                }
                catch (TaskCountLimitException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(separation);
                }
                catch (TaskLengthLimitException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(separation);
                }
                catch (DuplicateTaskException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(separation);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Произошла непредвиденная ошибка: {ex.Message}");
                    Console.WriteLine(separation);
                }

            }
        }
        private static void UnknownCommand()
        {
            Console.WriteLine("Такой команды не знаю");
            Console.WriteLine(separation);
        }
        public static int ParseAndValidateInt(string? str, int min, int max)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentException("Входная строка не может быть пустой или null.");
            }

            if (!int.TryParse(str, out int result))
            {
                throw new ArgumentException($"Невозможно преобразовать '{str}' в число.");
            }

            if (result < min || result > max)
            {
                throw new ArgumentException($"Число {result} должно быть в диапазоне от {min} до {max}.");
            }

            return result;
        }
        public static void ValidateString(string? str)
        {
            if (str == null)
            {
                throw new ArgumentException("Строка не может быть null.");
            }

            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentException("Строка не может быть пустой.");
            }

            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentException("Строка не может содержать только пробелы.");
            }
        }

        public static void StartMessage()
        {
            if (!string.IsNullOrEmpty(massege))
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
            if (doList.Count >= MaxTask)
            {
                throw new TaskCountLimitException(MaxTask);
            }

            Console.WriteLine("\nНапишите необходимую задачу:");
            string whatNeedDo = Console.ReadLine().Trim();
            if (whatNeedDo.Length > MaxLength)
            {
                throw new TaskLengthLimitException(whatNeedDo.Length, MaxLength);
            }
            if (doList.Contains(whatNeedDo))
            {
                throw new DuplicateTaskException(whatNeedDo);
            }
            while (string.IsNullOrEmpty(whatNeedDo))
            {
                Console.WriteLine("\nЗадача должна содержать текст!");
                whatNeedDo = Console.ReadLine().Trim();
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
                Console.WriteLine($"{i + 1} {doList[i]}");

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

            try
            {
                int nTask = ParseAndValidateInt(inputRemove, 1, doList.Count);
                doList.RemoveAt(nTask - 1);
                Console.WriteLine("Задача успешно удалена.");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine(separation);
        }
    }
}
