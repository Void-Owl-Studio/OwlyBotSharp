using Telegram.Bot.Types;

class TUpdateHandler
{
    public TUpdateHandler(Func<Update, Func<ResponceMsg>, Task> handler,
                          Func<Update, bool> cond)
    {
        Handler = handler;
        Cond = cond;
    }
    public readonly Func<Update, Func<ResponceMsg>, Task> Handler;
    public readonly Func<Update, bool> Cond;
}
