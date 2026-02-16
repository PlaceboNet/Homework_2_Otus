using Homework1.Core.DataAccess;
using Homework1.Core.Services;
using Homework1.Infrastructure.DataAccess;
using Homework1.Infrastructure.DataAccess.Repositories;
using Homework1.Infrastructure;
using Homework1.TelegramBot;
using Homework1.TelegramBot.Scenario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Homework1
{
    internal class Program
    {
        public static string iCanDo = "\nЯ могу выполнить следующие команды:" +
            "\n/search - найти информацию в энциклопедии" +
            "\n/all - показать список всех статей" +
            "\n/favorites - ваши сохраненные статьи" +
            "\n/cancel - отменить текущий сценарий" +
            "\n\nКоманды администратора:" +
            "\n/admin - проверка новых статей" +
            "\n/import [Название] - загрузить статью из Wiki" +
            "\n/promote - получить права администратора" +
            "\n\nОбщие команды:" +
            "\n/help - справка по командам" +
            "\n/info - информация о проекте";

        public static string info = "Abiotic Factor Encyclopedia Bot v1.0\nРусское сообщество";

        private static ITelegramBotClient? _botClient;
        private static CancellationTokenSource? _cts;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Запуск бота...");

            var connectionString = "Host=localhost;Port=5432;Database=Database;Username=postgres;Password=Password;";
            
            var contextFactory = new DataContextFactory(connectionString);
            contextFactory.EnsureCreated();

            var userRepository = new SqlUserRepository(contextFactory);
            var articleRepository = new SqlArticleRepository(contextFactory);
            var favoriteRepository = new SqlFavoriteRepository(contextFactory);
            var contextRepository = new InMemoryScenarioContextRepository();

            var userService = new UserService(userRepository);
            var articleService = new ArticleService(articleRepository, favoriteRepository);
            var notificationService = new NotificationService(contextFactory);
            
            var translationHttpClient = new HttpClient();
            var translationService = new TranslationService(translationHttpClient);
            var wikiParserService = new WikiParserService(articleService, translationService);

            var scenarios = new List<IScenario>
            {
                new SearchScenario(articleService),
                new FavoritesScenario(articleService, userService),
                new AdminScenario(articleService, userService),
                new EditArticleScenario(articleService),
                new ListArticlesScenario(articleService)
            };

            var botToken = "Token";
            var updateHandler = new UpdateHandler(userService, articleService, wikiParserService, contextRepository, scenarios);

            var backgroundTaskRunner = new Homework1.BackgroundTasks.BackgroundTaskRunner();
            
            // Фоновая задача парсинга Вики (каждую минуту)
            backgroundTaskRunner.AddTask(new Homework1.BackgroundTasks.WikiParserBackgroundTask(wikiParserService));

            // Фоновая задача уведомлений (каждую минуту)
            backgroundTaskRunner.AddTask(new Homework1.BackgroundTasks.NotificationBackgroundTask(
                notificationService,
                new TelegramBotClient(botToken)));

            try
            {
                _botClient = new TelegramBotClient(botToken);
                _cts = new CancellationTokenSource();

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
                    DropPendingUpdates = true
                };

                backgroundTaskRunner.StartTasks(_cts.Token);
                
                // Регистрация команд в меню Telegram
                await _botClient.SetMyCommands(new[]
                {
                    new BotCommand { Command = "search", Description = "Найти информацию" },
                    new BotCommand { Command = "all", Description = "Список всех статей" },
                    new BotCommand { Command = "favorites", Description = "Ваши избранные статьи" },
                    new BotCommand { Command = "admin", Description = "Панель администратора" },
                    new BotCommand { Command = "help", Description = "Справка по командам" },
                    new BotCommand { Command = "info", Description = "Инфо о боте" },
                    new BotCommand { Command = "cancel", Description = "Отменить текущее действие" }
                }, cancellationToken: _cts.Token);

                _botClient.StartReceiving(
                    updateHandler: updateHandler,
                    receiverOptions: receiverOptions,
                    cancellationToken: _cts.Token
                );

                var me = await _botClient.GetMe();
                Console.WriteLine($"{me.FirstName} запущен!");
                Console.WriteLine("Нажмите 'A' для выхода");
                Console.WriteLine("Нажмите 'Q' для запуска/остановки парсинга");

                while (!_cts.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.A) break;
                        if (key.Key == ConsoleKey.Q)
                        {
                            wikiParserService.ToggleEnabled();
                        }
                    }
                    await Task.Delay(100);
                }
                _cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                backgroundTaskRunner.Dispose();
                _cts?.Dispose();
            }
        }
    }
}