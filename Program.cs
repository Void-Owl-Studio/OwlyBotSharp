using System;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var secretConfig = new ConfigurationBuilder().AddIniFile("api.ini").Build();
var secretSection = secretConfig.GetSection("Tokens");

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(secretSection["Token"], cancellationToken: cts.Token);
bot.OnError += OnError;
bot.OnMessage += OnMessage;

Console.WriteLine("Bot is running... Press Enter to terminate");
Console.ReadLine();
cts.Cancel();

async Task OnError(Exception exception, HandleErrorSource source)
{
    Console.WriteLine(exception);
}

async Task OnMessage(Message msg, UpdateType type)
{
    if (msg.From == secretSection["Owner"])
    {
        if (msg.Text == "/start")
            await bot.SendMessage(msg.Chat, "Здесь будут сообщения от пользователей.");
    }
    else
    {
        /*
        var replyMarkup = new ReplyKeyboardMarkup(true)
        .AddButton("Запостить в канал")
        */
        var caption = $"<b>#тейк от <a href=\"tg://user?id={msg.Chat}\">{msg.From}</a></b>"

        await bot.CopyMessage(/*вставить ID чата owner'a*/, msg.Chat, msg.Id,
        caption: caption, parseMode: ParseMode.Html);
    }
}

async Task OnUpdate(Update update)
{
    if (update is { CallbackQuery: { } query })
    {
    }
}
