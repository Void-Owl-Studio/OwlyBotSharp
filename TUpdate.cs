using Telegram.Bot.Types;

class TUpdateHandler
{
    public TUpdateHandler(Func<Update, Task<ResponceMsg>, Task> handler,
                          Func<Update, bool> cond)
    {
        Handler = handler;
        Cond = cond;
    }
    public readonly Func<Update, Task<ResponceMsg>, Task> Handler;
    public readonly Func<Update, bool> Cond;
}
