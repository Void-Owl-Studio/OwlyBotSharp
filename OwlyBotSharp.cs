using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Owl;

var botConfig = new ConfigurationBuilder().AddIniFile("conf.ini").Build();
var apiSection = botConfig.GetSection("API");
var ownerNumber = Int64.Parse(apiSection["Owner"]!);

var DBConnection = new SqliteConnection("Data Source=tgbot.db");
DBConnection.Open();

SpinLock DBSpinlock = new SpinLock();

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(apiSection["Token"]!, cancellationToken: cts.Token);

var er = new EventRouter();

er.Add(UpdateType.Message, OnOwnerMessage, (u) => u.Message!.Chat.Id == ownerNumber);
er.Add(UpdateType.Message, OnUserMessage, (u) => u.Message!.Chat.Id != ownerNumber);

er.Add(UpdateType.CallbackQuery, OnCallbackQuieryAdminPost,
       (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "post");
er.Add(UpdateType.CallbackQuery, OnCallbackQuieryAdminDeny,
       (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "deny");
er.Add(UpdateType.CallbackQuery, OnCallbackQuieryAdminEdit,
       (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "edit");
er.Add(UpdateType.CallbackQuery, OnCallbackQuieryAdminBan,
       (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "ban");

await er.Process();

Console.WriteLine("Bot is running... Press Enter to terminate");

int? offset = null;
while (!cts.IsCancellationRequested)
{
    var updates = await bot.GetUpdates(offset);
    foreach (var upd in updates)
    {
        offset = upd.Id + 1;
        await er.Push(upd);
        if (cts.IsCancellationRequested) break;
    }
    if (Console.KeyAvailable)
        if (Console.ReadKey(true).Key == ConsoleKey.Enter) break;
}

er.Stop();

DBConnection.Close();



async Task OnOwnerMessage(Update a)
{
    Message msg = a.Message!;
    switch (msg.Text)
    {
        case "/start":
            await bot.SendMessage(msg.Chat.Id, "Здесь будут сообщения от пользователей.");
            break;
    }
}

async Task OnUserMessage(Update a)
{
    Message msg = a.Message!;

    int responce = 0;

    bool lockTaken = false;
    try
    {
        DBSpinlock.Enter(ref lockTaken);
        var command = DBConnection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM banned WHERE tid = $tid);";
        command.Parameters.AddWithValue("$tid", msg.Chat.Id);
        responce = (int)command.ExecuteScalar()!;
    }
    finally
    {
        if (lockTaken) DBSpinlock.Exit();
    }

    if (responce == 0)
    {
        var inlineMarkup = new InlineKeyboardMarkup()
        .AddButton("Запостить", "post")
        .AddButton("Отклонить", "deny")
        .AddNewRow()
        .AddButton("Редактировать", "edit")
        .AddButton("Забанить", "ban");

        var forwarded = await bot.ForwardMessage(apiSection["Owner"]!, msg.Chat.Id, msg.MessageId);
        await bot.SendMessage(apiSection["Owner"]!,
                              "Выберите действие:",
                              protectContent: true,
                              replyParameters: forwarded!.MessageId,
                              replyMarkup: inlineMarkup);
    }
}

async Task OnCallbackQuieryAdminPost(Update a)
{
    var query = a.CallbackQuery!;
    await bot.AnswerCallbackQuery(query.Id);

    var origMsg = query.Message!.ReplyToMessage!;

    var caption = $"<b>#тейк от <a href=\"tg://user?id=\"{origMsg.ForwardFrom!.Id}\"\">{origMsg.ForwardFrom.FirstName}</a></b>";
            await bot.CopyMessage(apiSection["Channel"]!,
                                  apiSection["Owner"]!,
                                  origMsg.MessageId,
                                  parseMode: ParseMode.Html,
                                  caption: caption);
}

async Task OnCallbackQuieryAdminDeny(Update a)
{
    var query = a.CallbackQuery!;
    await bot.AnswerCallbackQuery(query.Id);

    var origMsg = query.Message!.ReplyToMessage!;

    await bot.DeleteMessage(apiSection["Owner"]!, origMsg.MessageId);
    await bot.DeleteMessage(apiSection["Owner"]!, query.Message!.MessageId);
}

async Task OnCallbackQuieryAdminEdit(Update a)
{
    var query = a.CallbackQuery!;
    await bot.AnswerCallbackQuery(query.Id);

    var origMsg = query.Message!.ReplyToMessage!;
}

async Task OnCallbackQuieryAdminBan(Update a)
{
    var query = a.CallbackQuery!;
    await bot.AnswerCallbackQuery(query.Id);

    var origMsg = query.Message!.ReplyToMessage!;

    bool lockTaken = false;
    try
    {
        DBSpinlock.Enter(ref lockTaken);
        var command = DBConnection.CreateCommand();
        command.CommandText = "INSERT INTO banned VALUES ($tid, $handle, $name);";
        command.Parameters.AddWithValue("$tid", origMsg.ForwardFrom!.Id);
        command.Parameters.AddWithValue("$handle", origMsg.ForwardFromChat);
        command.Parameters.AddWithValue("$name", origMsg.ForwardFrom!.Username);
        command.ExecuteNonQuery();
    }
    finally
    {
        if (lockTaken) DBSpinlock.Exit();
    }
}
