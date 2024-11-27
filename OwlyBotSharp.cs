using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Owl;

var botConfig = new ConfigurationBuilder().AddIniFile("conf.ini").Build();
var apiSection = botConfig.GetSection("API");
Int64 ownerNumber = Int64.Parse(apiSection["Owner"]!);

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(apiSection["Token"]!, cancellationToken: cts.Token);

using var sqlur = new UpdateRouter<SQLUpdateRunner, AbstractSQLMsg>();
sqlur.Runner = new("Data Source=users.db");

var handlers = new Dictionary<UpdateType, TUpdateHandler[]>();

handlers.Add(UpdateType.Message,
             new TUpdateHandler[]
             {
                 new(OnOwnerMessage, (u) => u.Message!.Chat.Id == ownerNumber),
                 new(OnUserMessage, (u) => u.Message!.Chat.Id != ownerNumber && !Banned(u.Message!.Chat.Id))
            });

handlers.Add(UpdateType.CallbackQuery,
             new TUpdateHandler[]
             {
                new(OnCallbackQuieryAdminPost,
                    (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "post"),
                new(OnCallbackQuieryAdminDeny,
                    (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "deny"),
             /*
                new(OnCallbackQuieryAdminEdit,
                    (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "edit"),
             */
                new(OnCallbackQuieryAdminBan,
                    (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "ban")
            });

sqlur.Push(new SQLMsg(SQLActionTypes.OPEN_CONN));

Console.WriteLine("Bot is running... Press Enter to terminate");

int? offset = null;
while (!cts.IsCancellationRequested)
{
    var updates = await bot.GetUpdates(offset);
    offset = updates[0].Id + updates.Length;
    foreach (var u in updates)
    {
        var hs = handlers[u.Type];
        foreach (var h in hs)
            await Task.Factory.StartNew(async () => {
                try { if (h.Cond(u)) await h.Handler(u); }
                catch (Exception ex) { Console.WriteLine("Handler exited with exception: ", ex); }
            });
    }
    if (Console.KeyAvailable)
        if (Console.ReadKey().Key == ConsoleKey.Enter)
            cts.Cancel();
}

sqlur.Push(new SQLMsg(SQLActionTypes.CLOSE_CONN));

bool Banned(long tid)
{
    string cmdStr = "SELECT EXISTS(SELECT 1 FROM banned WHERE tid=$tid);";

    var batch = new SQLMsgBatch();
    batch.Sender = Task.CurrentId;
    batch.Add(new SQLMsg(SQLActionTypes.NEW_COMMAND, cmdStr));
    batch.Add(new SQLMsg(SQLActionTypes.ADD_COMMAND_PARAMS, "$tid", tid));
    batch.Add(new SQLMsg(SQLActionTypes.EXEC_SCALAR));
    sqlur.Push(batch);

    int exists;
    while (true)
    {
        var msg = sqlur.TryPeek();
        if (msg is not null)
            if(msg.Sender == Task.CurrentId)
            {
                exists = (int)((ResponceMsg)sqlur.TryPull()!).Obj!;
                break;
            }
    }

    return exists == 1 ? true : false;
}

async Task OnOwnerMessage(Update a)
{
    Message msg = a.Message!;
    switch (msg.Text)
    {
        case "/start":
            await bot!.SendMessage(msg.Chat.Id, "Здесь будут сообщения от пользователей.");
            break;
    }
}

async Task OnUserMessage(Update a)
{
    Message msg = a.Message!;

    var inlineMarkup = new InlineKeyboardMarkup()
    .AddButton("Запостить", "post")
    .AddButton("Отклонить", "deny")
    .AddNewRow()
    //.AddButton("Редактировать", "edit")
    .AddButton("Забанить", "ban");

    var caption = $"<b>#тейк от <a href=\"tg://user?id=\"{msg.Chat.Id}\"\">{msg.From!.FirstName}</a></b>";

    await bot.CopyMessage(apiSection!["Owner"]!,
                           msg.Chat.Id,
                           msg.MessageId,
                           replyMarkup: inlineMarkup,
                           parseMode: ParseMode.Html,
                           caption: caption);
}

async Task OnCallbackQuieryAdminPost(Update a)
{
    var query = a.CallbackQuery!;
    await bot.AnswerCallbackQuery(query.Id);

    await bot.CopyMessage(apiSection["Channel"]!,
                           apiSection["Owner"]!,
                           query.Message!.MessageId,
                           replyMarkup: new ReplyKeyboardRemove());
}

async Task OnCallbackQuieryAdminDeny(Update a)
{
    var query = a.CallbackQuery!;
    await bot.AnswerCallbackQuery(query.Id);

    await bot.DeleteMessage(apiSection["Owner"]!, query.Message!.MessageId);
}
/*
async Task OnCallbackQuieryAdminEdit(Update a)
{
    var query = a.CallbackQuery!;
    await bot.AnswerCallbackQuery(query.Id);
}
*/
async Task OnCallbackQuieryAdminBan(Update a)
{
    var query = a.CallbackQuery!;
    await bot.AnswerCallbackQuery(query.Id);

    string cmdStr = "INSERT INTO banned (tid, reason) VALUES ($tid, $reason)";

    var batch = new SQLMsgBatch();
    batch.Sender = Task.CurrentId;
    batch.Add(new SQLMsg(SQLActionTypes.NEW_COMMAND, cmdStr));
    batch.Add(new SQLMsg(SQLActionTypes.ADD_COMMAND_PARAMS, "$tid", query.Message!.ForwardFrom!.Id));
    batch.Add(new SQLMsg(SQLActionTypes.ADD_COMMAND_PARAMS, "$reason", ""));
    batch.Add(new SQLMsg(SQLActionTypes.EXEC_NON_QUERY));
    sqlur.Push(batch);
}
