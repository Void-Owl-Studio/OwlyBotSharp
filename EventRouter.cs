using System.Collections.Concurrent;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Owl
{
    sealed class EventRouter
    {
        private ConcurrentQueue<Update> Input;

        private List<EventHandler> Handlers;

        private long running = 0;

        public EventRouter()
        {
            Input = new();
            Handlers = new();
        }

        public async Task Push(Update upd) => await Task.Run(() => Input.Enqueue(upd));

        public void Add(UpdateType ut, Func<Update, Task> handler, Func<Update, bool> cond)
        {
            var h = new EventHandler(ut, handler, cond);
            Handlers.Add(h);
        }

        public async Task Process()
        {
            Interlocked.Increment(ref running);
            while (Interlocked.Read(ref running) == 1)
            {
                Update? te;
                if (Input.TryDequeue(out te))
                {
                    foreach (var h in Handlers)
                    {
                        if(h.Type == te.Type && h.Cond(te))
                        {
                            try { await h.Handler(te); }
                            catch (Exception ex) { Console.WriteLine("Handler exited with exception: ", ex); }
                        }
                    }
                }
            }
        }

        public void Stop() => Interlocked.Decrement(ref running);
    }

    struct EventHandler
    {
        public EventHandler(UpdateType _type, Func<Update, Task> _handler, Func<Update, bool> _cond)
        {
            Type = _type;
            Handler = _handler;
            Cond = _cond;
        }

        public readonly UpdateType Type;
        public readonly Func<Update, Task> Handler;
        public readonly Func<Update, bool> Cond;
    }
}
