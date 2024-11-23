using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Owl;

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
    public int? offset = null;
}

class TypedUpdate : Update, IUpdateMsg<UpdateType> {}
