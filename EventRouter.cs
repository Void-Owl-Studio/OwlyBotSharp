using System.Collections.Concurrent;

namespace Owl
{
    public sealed class EventRouter<U, T> : IDisposable where U : IUpdateMsg<T> where T : IComparable
    {
        private CancellationTokenSource TSource;

        private ConcurrentQueue<U> Input;

        private List<EventHandler<U, T>> Handlers;

        public EventRouter()
        {
            TSource = new();
            Input = new();
            Handlers = new();
        }

        public void Dispose()
        {
            if (!TSource.IsCancellationRequested) TSource.Cancel();
            TSource.Dispose();
        }

        public void Push(U upd) => Input.Enqueue(upd);

        public void Add(T ut, Func<U, Task> handler, Func<U, bool> cond)
        {
            var h = new EventHandler<U, T>(ut, handler, cond);
            Handlers.Add(h);
        }

        private void QueuePolling()
        {
            while (!TSource.IsCancellationRequested)
            {
                U? te;
                if (Input.TryDequeue(out te))
                {
                    foreach (var h in Handlers)
                    {
                        if (Equals(h.Type, te.Type) && h.Cond(te))
                        {
                            try { Task.Run(() => h.Handler(te)); }
                            catch (Exception ex) { Console.WriteLine("Handler exited with exception: ", ex); }
                        }
                    }
                }
            }
        }

        public void Start() => Task.Run((Action)QueuePolling);
    }

    struct EventHandler<U, T> where U : IUpdateMsg<T>
    {
        public EventHandler(T _type, Func<U, Task> _handler, Func<U, bool> _cond)
        {
            Type = _type;
            Handler = _handler;
            Cond = _cond;
        }

        public readonly T Type {get;}
        public readonly Func<U, Task> Handler {get;}
        public readonly Func<U, bool> Cond {get;}
    }

    public interface IUpdateMsg<T>
    {
        public T Type => default!;
    }
}
