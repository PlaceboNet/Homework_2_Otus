using System;

namespace Homework1
{
    internal class Program
    {
        private static string massege;

        static void Main(string[] args)
        {
            Console.WriteLine("Привет!" +
                "\nЯ могу выполнить несколько команд:" +
                "\n/start - программа просит ввести имя" +
                "\n/help - краткая справочная информация о том, как пользоваться программой" +
                "\n/info - предоставляет информацию о версии программы и дате её создания" +
                "\n/echo - после ввода имени данная команда становится доступной" +
                "\n/exit - выйти из программы");

            while (true)
            {
                Console.Write("Введите команду: ");
                string input = Console.ReadLine();

                if (input == "/exit") break;

                switch (input)
                {
                    case "/start":
                        var instance = new Program();
                        instance.StartMessage();
                        break;
                    case "/help":
                        if (!string.IsNullOrEmpty(massege))
                            Console.WriteLine($"Я с удовольствием напомню тебе, {massege}, что я могу сделать" +
                "\n/start - программа просит ввести имя" +
                "\n/help - краткая справочная информация о том, как пользоваться программой" +
                "\n/info - предоставляет информацию о версии программы и дате её создания" +
                "\n/echo - после ввода имени данная команда становится доступной" +
                "\n/exit - выйти из программы");
                        else Console.WriteLine($"Я с удовольствием напомню тебе, что я могу сделать" +
                "\n/start - программа просит ввести имя" +
                "\n/help - краткая справочная информация о том, как пользоваться программой" +
                "\n/info - предоставляет информацию о версии программы и дате её создания" +
                "\n/echo - после ввода имени данная команда становится доступной" +
                "\n/exit - выйти из программы");
                        break;
                    case "/info":
                        if (!string.IsNullOrEmpty(massege))
                            Console.WriteLine($"{massege},\nВерсия 0.0.1\n27.08.2025");
                        else Console.WriteLine("Версия 0.0.1\n27.08.2025");
                        break;
                    case "/echo":
                        if (!string.IsNullOrEmpty(massege))
                            Console.WriteLine($"Привет, {massege}");
                        else
                            Console.WriteLine("Сначала представьтесь командой '/start'");
                        break;
                    default:
                        Console.WriteLine("Такой команды не знаю");
                        break;
                }
            }
        }

        public void StartMessage()
        {
            Console.WriteLine("Введите имя:");
            massege = Console.ReadLine();
            Console.WriteLine($"Привет, {massege}");
        }
    }
}
