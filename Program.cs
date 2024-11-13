using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var secretConfig = new ConfigurationBuilder().AddIniFile("api.ini").Build();
var secretSection = secretConfig.GetSection("Tokens");

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(secretSection["Token"]!, cancellationToken: cts.Token);
bot.OnError += OnError;
bot.OnMessage += OnMessage;
bot.OnUpdate += OnUpdate;

Console.WriteLine("Bot is running... Press Enter to terminate");
Console.ReadLine();
cts.Cancel();

async Task OnError(Exception exception, HandleErrorSource source)
{
    Console.WriteLine(exception);
}

async Task OnMessage(Message msg, UpdateType type)
{
    if (msg.From!.Id == Int32.Parse(secretSection["Owner"]!))
    {
        if (msg.Text == "/start")
            await bot.SendMessage(msg.Chat.Id, "Здесь будут сообщения от пользователей.");
    }
    else
    {
        /*
        var replyMarkup = new ReplyKeyboardMarkup(true)
        .AddButton("Запостить в канал")
        */
        var caption = $"<b>#тейк от <a href=\"tg://user?id={msg.From.Id}\">{msg.From.FirstName}</a></b>";

        await bot.CopyMessage(secretSection["Owner"]!, msg.Chat.Id, msg.Id,
        caption: caption, parseMode: ParseMode.Html);
    }
}

async Task OnUpdate(Update update)
{
    if (update is { CallbackQuery: { } query })
    {
    }
}
