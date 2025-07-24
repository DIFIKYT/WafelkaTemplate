using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Newtonsoft.Json;
using WafelkaTemplate;

class Program
{
    static readonly ManualResetEvent shutdownEvent = new(false);

    static void Main(string[] args)
    {
        BotConfig? config = null;

        try
        {
            string configPath = "Config.json";
            config = GetJSONData(configPath);
        }
        catch
        {
            Console.WriteLine("Config is missing");
        }

        using CancellationTokenSource cancellationTokenSource = new();
        TelegramBotClient bot = new(config!.TelegramBotToken!, cancellationToken: cancellationTokenSource.Token);
        DailyMidnightTaskScheduler timer = new();
        GoogleSheetsHelper sheetsHelper = new(
            config: config,
            timer: timer
        );
        BotMessageHandler botMessageHandler = new(
            bot: bot,
            sheetsHelper: sheetsHelper,
            config: config
        );
        ReceiverOptions receiverOptions = new() { AllowedUpdates = { } };
        botMessageHandler.SubscribeOnSheetsHelper();

        async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message == null)
                    return;

                await botMessageHandler.MessageHandleAsync(update.Message, cancellationToken);
            }
            catch (Exception exception)
            {
                if (update.Message == null)
                    return;

                await bot.SendMessage(
                    chatId: update.Message.Chat.Id,
                    text: $"!Error!\n{exception.Message}",
                    cancellationToken: cancellationToken
                );

                await bot.SendMessage(
                    chatId: update.Message.Chat.Id,
                    text: $"ID чата - {update.Message.Chat.Id}\nНик - @{update.Message.From!.Username}\nТекст сообщения:\n{update.Message.Text}",
                    cancellationToken: cancellationToken
                );
            }
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"!Error!\n{exception.Message}");
            return Task.CompletedTask;
        }

        bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationTokenSource.Token
        );

        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Shutdown...");
            shutdownEvent.Set();
            e.Cancel = true;
        };

        Console.WriteLine("Bot started.");

        shutdownEvent.WaitOne();

        cancellationTokenSource.Cancel();
    }

    private static BotConfig? GetJSONData(string filePath)
    {
        return JsonConvert.DeserializeObject<BotConfig>(System.IO.File.ReadAllText(filePath));
    }
}