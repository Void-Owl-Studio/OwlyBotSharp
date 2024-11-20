using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Owl;

var botConfig = new ConfigurationBuilder().AddIniFile("conf.ini").Build();
var secretSection = botConfig.GetSection("Tokens");
var ownerNumber = Int64.Parse(secretSection["Owner"]!);

var DBConnection = new SqliteConnection("Data Source=tgbot.db");
DBConnection.Open();

var er = new EventRouter();

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(secretSection["Token"]!, cancellationToken: cts.Token);
bot.OnError += async (exception, source) => await er.Push(new ErrEventArgs(exception, source));
bot.OnMessage += async (msg, type) => await er.Push(new MsgEventArgs(msg, type));
bot.OnUpdate += async (update) => await er.Push(new UpdEventArgs(update));

er.Add(OnMessage, TEventType.Message);
er.Add(OnUpdate, TEventType.Update);
await er.Process();

Console.WriteLine("Bot is running... Press Enter to terminate");
Console.ReadLine();

cts.Cancel();

er.Stop();

DBConnection.Close();



async Task OnMessage(TEventArgs a)
{
    Message msg = ((MsgEventArgs)a).msg;
    UpdateType type = ((MsgEventArgs)a).type;

    if (msg.Chat.Id == ownerNumber)
    {
        if (msg.Text == "/start")
            await bot.SendMessage(msg.Chat.Id, "Здесь будут сообщения от пользователей.");
    }
    else
    {
        /*
        var command = DBConnection.CreateCommand();
        command.CommandText = "INSERT INTO users VALUES ($tid, $handle, $name, $banned)";
        command.Parameters.AddWithValue("$tid", msg.Chat.Id);
        command.Parameters.AddWithValue("$handle", msg.Chat);
        command.Parameters.AddWithValue("$name", msg.From!.Username);
        command.Parameters.AddWithValue("$banned", false);
        command.ExecuteNonQuery();
        */

        var inlineMarkup = new InlineKeyboardMarkup()
        .AddButton("Запостить", "post")
        .AddButton("Отклонить", "deny")
        .AddNewRow()
        .AddButton("Редактировать", "edit")
        .AddButton("Забанить", "ban");

        var caption = $"<b>#тейк от <a href=\"tg://user?id={msg.From!.Id}\">{msg.From.FirstName}</a></b>";

        await bot.CopyMessage(secretSection["Owner"]!, msg.Chat.Id, msg.MessageId,
        caption: caption, replyMarkup: inlineMarkup, parseMode: ParseMode.Html);
    }
}

async Task OnUpdate(TEventArgs a)
{
    Update update = ((UpdEventArgs)a).update;

    if (update is { CallbackQuery: { } query })
    {
        await bot.AnswerCallbackQuery(query.Id);

        switch(query.Data)
        {
            case "post":
                await bot.CopyMessage(secretSection["Channel"]!,
                                      secretSection["Owner"]!,
                                      query.Message!.Id,
                                      replyMarkup: new ReplyKeyboardRemove());
                break;
            case "deny":
                await bot.DeleteMessage(secretSection["Owner"]!, query.Message!.MessageId);
                break;
            case "edit": break;
            case "ban":  break;
            default: throw new Exception("Impossible button press");
        }
    }
}
