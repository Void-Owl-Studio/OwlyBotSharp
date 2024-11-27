using Telegram.Bot.Types;

class TUpdateHandler
{
    public TUpdateHandler(Func<Update, Task> _handler, Func<Update, bool> _cond)
    {
        Handler = _handler;
        Cond = _cond;
    }
    public readonly Func<Update, Task> Handler;
    public readonly Func<Update, bool> Cond;
}
