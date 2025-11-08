using System;

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
                "\n/exit - выйти из программы";
        private static string info = "Версия 0.0.1\n27.08.2025";

        static void Main(string[] args)
        {
            Console.WriteLine($"Привет! {iCanDo}");

            while (true)
            {
                Console.Write("Введите команду: ");
                string input = Console.ReadLine();
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
                        if (!string.IsNullOrEmpty(massege))
                            Console.WriteLine($"Я с удовольствием напомню тебе, {massege}, что я могу сделать {iCanDo}");
                        else Console.WriteLine($"Я с удовольствием напомню тебе, что я могу сделать {iCanDo}");
                        break;
                    case "/info":
                        if (!string.IsNullOrEmpty(massege))
                            Console.WriteLine($"{massege},{info}");
                        else Console.WriteLine(info);
                        break;
                    case "/echo":
                        if (!string.IsNullOrEmpty(massege))
                            Console.WriteLine(echo);
                        else
                            Console.WriteLine("Сначала представьтесь командой '/start'");
                        break;
                    default:
                        Console.WriteLine("Такой команды не знаю");
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
        }
    }
}
