using System.Collections.Concurrent;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Owl
{
    sealed class EventRouter
    {
        private ConcurrentQueue<TEventArgs> input;

        private List<Func<TEventArgs, Task>> MsgHandlers, UpdHandlers, ErrHandlers;

        private bool running = false;

        public EventRouter()
        {
            input = new();
            MsgHandlers = new();
            UpdHandlers = new();
            ErrHandlers = new();
        }

        public async Task Push(TEventArgs ev) => await Task.Run(() => input.Enqueue(ev));

        public void Add(Func<TEventArgs, Task> handler, TEventType type)
        {
            switch(type)
            {
                case TEventType.Message: MsgHandlers.Add(handler); break;
                case TEventType.Update: UpdHandlers.Add(handler); break;
                case TEventType.Error: ErrHandlers.Add(handler); break;
                default: throw new Exception("Impossible event type");
            }
        }

        public async Task Process()
        {
            running = true;
            while (running)
            {
                TEventArgs te;
                List<Func<TEventArgs, Task>> UsedHandlers;
                if (input.TryDequeue(out te!))
                {
                    switch (te)
                    {
                        case MsgEventArgs: UsedHandlers = MsgHandlers; break;
                        case UpdEventArgs: UsedHandlers = UpdHandlers; break;
                        case ErrEventArgs: UsedHandlers = ErrHandlers; break;
                        default: throw new Exception("No suitable handlers");
                    }
                    foreach (var handler in UsedHandlers) await handler(te);
                }
            }
        }

        public void Stop() => running = false;
    }

    enum TEventType {Message, Update, Error};

    abstract class TEventArgs
    {
        public TEventArgs(TEventType _tt) => tt = _tt;
        public readonly TEventType tt;
    }

    class MsgEventArgs : TEventArgs
    {
        public MsgEventArgs(Message _msg, UpdateType _type) : base(TEventType.Message)
        {
            msg = _msg;
            type = _type;
        }
        public Message msg {get;}
        public UpdateType type {get;}
    }

    class UpdEventArgs : TEventArgs
    {
        public UpdEventArgs(Update _update) : base(TEventType.Update)
        {
            update = _update;
        }
        public Update update {get;}
    }

    class ErrEventArgs : TEventArgs
    {
        public ErrEventArgs(Exception _exception, HandleErrorSource _source) : base(TEventType.Error)
        {
            exception = _exception;
            source = _source;
        }
        public Exception exception {get;}
        public HandleErrorSource source {get;}
    }
}
