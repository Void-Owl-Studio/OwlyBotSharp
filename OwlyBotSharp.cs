using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Owl;

var botConfig = new ConfigurationBuilder().AddIniFile("conf.ini").Build();
var apiSection = botConfig.GetSection("API");
var ownerNumber = Int64.Parse(apiSection["Owner"]!);

using var cts = new CancellationTokenSource();

var bot = new TelegramBotClient(apiSection["Token"]!, cancellationToken: cts.Token);

var handlers = new List<TUpdateHandler>();

handlers.Add(new(UpdateType.Message, OnOwnerMessage, (u) => u.Message!.Chat.Id == ownerNumber));
handlers.Add(new(UpdateType.Message, OnUserMessage, (u) => u.Message!.Chat.Id != ownerNumber));

handlers.Add(new(UpdateType.CallbackQuery, OnCallbackQuieryAdminPost,
       (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "post"));
handlers.Add(new(UpdateType.CallbackQuery, OnCallbackQuieryAdminDeny,
       (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "deny"));
/*
handlers.Add(new(UpdateType.CallbackQuery, OnCallbackQuieryAdminEdit,
       (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "edit"));
*/
handlers.Add(new(UpdateType.CallbackQuery, OnCallbackQuieryAdminBan,
       (u) => u.CallbackQuery!.Message!.Chat.Id == ownerNumber && u.CallbackQuery!.Data == "ban"));

using var ur = new UpdateRouter<TUpdateRunner, TypedUpdate, UpdateType>();
var runner = new TUpdateRunner();

runner.Run = async (i, o, r) => {
                var updates = await bot.GetUpdates(((TUpdateRunner)r).offset);
                foreach (var upd in updates)
                {
                    ((TUpdateRunner)r).offset = upd.Id + 1;
                    TypedUpdate? u;
                    if (i.TryDequeue(out u))
                    {
                        foreach (var h in handlers)
                        {
                            if (Equals(h.Type, u.Type) && h.Cond(u))
                            {
                                try { await Task.Run(async () => await h.Handler(u)); }
                                catch (Exception ex) { Console.WriteLine("Handler exited with exception: ", ex); }
                            }
                        }
                    }
                    if (cts.IsCancellationRequested) break;
                }
};

ur.Start(runner);

Console.WriteLine("Bot is running... Press Enter to terminate");

Console.ReadLine();



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

    var inlineMarkup = new InlineKeyboardMarkup()
    .AddButton("Запостить", "post")
    .AddButton("Отклонить", "deny")
    .AddNewRow()
    //.AddButton("Редактировать", "edit")
    .AddButton("Забанить", "ban");

    var caption = $"<b>#тейк от <a href=\"tg://user?id=\"{msg.Chat.Id}\"\">{msg.From!.FirstName}</a></b>";

    await bot.CopyMessage(apiSection["Owner"]!,
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
}
