using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Owl;

class TypedUpdate : Update, IUpdateMsg<UpdateType> {}

struct TUpdateHandler : IUpdateMsg<UpdateType>
{
    public TUpdateHandler(UpdateType _type, Func<Update, Task> _handler, Func<Update, bool> _cond)
    {
        Type = _type;
        Handler = _handler;
        Cond = _cond;
    }

    public readonly UpdateType Type {get;}
    public readonly Func<Update, Task> Handler {get;}
    public readonly Func<Update, bool> Cond {get;}
}

class TUpdateRunner : UpdateRunner<TypedUpdate, UpdateType>
{
    int? offset = null;

    IConfigurationSection apiSection;

    readonly Int64 ownerNumber;

    List<TUpdateHandler> handlers;

    TelegramBotClient bot;

    public TUpdateRunner()
    {
        var botConfig = new ConfigurationBuilder().AddIniFile("conf.ini").Build();
        apiSection = botConfig.GetSection("API");
        ownerNumber = Int64.Parse(apiSection["Owner"]!);

        bot = new(apiSection["Token"]!, cancellationToken: TSource!.Token);

        handlers = new();

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
    }

    public override async Task Run(DeniedOutputQueue<TypedUpdate> i, DeniedInputQueue<TypedUpdate> o)
    {
        var updates = await bot.GetUpdates(this.offset);
        foreach (var u in updates)
        {
            this.offset = u.Id + 1;
            foreach (var h in handlers)
            {
                if (Equals(h.Type, u.Type) && h.Cond(u))
                {
                    try { await Task.Run(async () => await h.Handler(u)); }
                    catch (Exception ex) { Console.WriteLine("Handler exited with exception: ", ex); }
                }
            }
            if (TSource!.IsCancellationRequested) break;
        }
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
}
