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

using var er = new EventRouter<TypedUpdate, UpdateType>();

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

er.Start();

Console.WriteLine("Bot is running... Press Enter to terminate");

int? offset = null;
while (!cts.IsCancellationRequested)
{
    var updates = await bot.GetUpdates(offset);
    foreach (var upd in updates)
    {
        offset = upd.Id + 1;
        er.Push((TypedUpdate)upd);
        if (cts.IsCancellationRequested) break;
    }
    if (Console.KeyAvailable)
        if (Console.ReadKey(true).Key == ConsoleKey.Enter) break;
}



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
    .AddButton("Редактировать", "edit")
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

async Task OnCallbackQuieryAdminEdit(Update a)
{
    var query = a.CallbackQuery!;
    await bot.AnswerCallbackQuery(query.Id);
}

async Task OnCallbackQuieryAdminBan(Update a)
{
    var query = a.CallbackQuery!;
    await bot.AnswerCallbackQuery(query.Id);
}
