using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using static Homework1.ToDoItem;

namespace Homework1
{
    internal class Program
    {
        private static ToDoUser currentUser;
        private static string massege;
        private static string iCanDo = "\nЯ могу выполнить несколько команд:" +
                "\n/start - программа просит ввести имя" +
                "\n/help - краткая справочная информация о том, как пользоваться программой" +
                "\n/info - предоставляет информацию о версии программы и дате её создания" +
                "\n/echo - после ввода имени данная команда становится доступной" +
                "\n/exit - выйти из программы" +
                "\n/addtask - добавить новую задачу в список" +
                "\n/showtasks - отобразить список всех добавленных задач" +
                "\n/removetask - удалять задачи по номеру в списке" +
                "\n/completetask - найти задачу по id" +
                "\n/showalltasks - выводить команды с любым State\n";
        private static string info = "Версия 0.0.1\n27.08.2025";
        public static List<ToDoItem> doList = new List<ToDoItem>();
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
                        case "/showalltasks":
                            ShowAllTasks();
                            break;
                        case "/completetask":
                            CompleteTask(echo);
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
            if (currentUser != null)
                return;
            Console.WriteLine("Введите имя:");
            string userName = Console.ReadLine();
            currentUser = new ToDoUser(userName);
            Console.WriteLine($"Привет, {currentUser.TelegramUserName}");
            Console.WriteLine(separation);
        }
        public static void HelpMessage()
        {
            if (currentUser != null)
                Console.WriteLine($"Я с удовольствием напомню тебе, {currentUser.TelegramUserName}, что я могу сделать {iCanDo}");
            else
                Console.WriteLine($"Я с удовольствием напомню тебе, что я могу сделать {iCanDo}");
            Console.WriteLine(separation);
        }
        public static void InfoMessage()
        {
            if (currentUser != null)
                Console.WriteLine($"{currentUser.TelegramUserName},{info}");
            else
                Console.WriteLine(info);
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

            if (currentUser == null)
            {
                Console.WriteLine("Сначала представьтесь командой '/start'");
                Console.WriteLine(separation);
                return;
            }

            Console.WriteLine("\nНапишите необходимую задачу:");
            string whatNeedDo = Console.ReadLine().Trim();

            if (whatNeedDo.Length > MaxLength)
            {
                throw new TaskLengthLimitException(whatNeedDo.Length, MaxLength);
            }

            // Проверка на дубликаты по имени задачи
            if (doList.Any(task => task.Name.Equals(whatNeedDo, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateTaskException(whatNeedDo);
            }

            while (string.IsNullOrEmpty(whatNeedDo))
            {
                Console.WriteLine("\nЗадача должна содержать текст!");
                whatNeedDo = Console.ReadLine().Trim();
            }

            ToDoItem newTask = new ToDoItem(currentUser, whatNeedDo);
            doList.Add(newTask);
            Console.WriteLine($"Задача добавлена с ID: {newTask.Id}");
            Console.WriteLine(separation);
        }

        public static bool ShowTasks()
        {
            var activeTasks = doList.Where(task => task.State == ToDoItemState.Active).ToList();

            if (activeTasks.Count == 0)
            {
                Console.WriteLine("Список активных задач пуст. Для начала введите задачи с помощью команды '/addtask'");
                Console.WriteLine(separation);
                return false;
            }

            Console.WriteLine("\nВот список активных дел:");
            for (int i = 0; i < activeTasks.Count; i++)
            {
                var task = activeTasks[i];
                Console.WriteLine($"{i + 1}. {task.Name} - {task.CreatedAt:dd.MM.yyyy HH:mm:ss} - {task.Id}");
            }
            Console.WriteLine(separation);
            return true;
        }
        public static void ShowAllTasks()
        {
            if (doList.Count == 0)
            {
                Console.WriteLine("Список задач пуст. Для начала введите задачи с помощью команды '/addtask'");
                Console.WriteLine(separation);
                return;
            }

            Console.WriteLine("\nВот список всех задач:");
            for (int i = 0; i < doList.Count; i++)
            {
                var task = doList[i];
                string state = task.State == ToDoItemState.Active ? "(Active)" : "(Completed)";
                Console.WriteLine($"{i + 1}. {state} {task.Name} - {task.CreatedAt:dd.MM.yyyy HH:mm:ss} - {task.Id}");
            }
            Console.WriteLine(separation);
        }
        public static void CompleteTask(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                Console.WriteLine("Не указан ID задачи. Использование: /completetask <ID_задачи>");
                Console.WriteLine(separation);
                return;
            }

            if (!Guid.TryParse(taskId, out Guid id))
            {
                Console.WriteLine("Неверный формат ID задачи.");
                Console.WriteLine(separation);
                return;
            }

            var task = doList.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                Console.WriteLine($"Задача с ID {id} не найдена.");
                Console.WriteLine(separation);
                return;
            }

            if (task.State == ToDoItemState.Completed)
            {
                Console.WriteLine("Задача уже завершена.");
                Console.WriteLine(separation);
                return;
            }

            task.Complete();
            Console.WriteLine($"Задача '{task.Name}' завершена.");
            Console.WriteLine(separation);
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
