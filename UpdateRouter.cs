using System.Collections.Concurrent;

namespace Owl
{
    sealed class UpdateRouter<R, U, T> : IDisposable where R : UpdateRunner<U, T> where U : IUpdateMsg<T> where T : IComparable
    {
        private CancellationTokenSource TSource;

        private ConcurrentQueue<U> Input, Output;

        public UpdateRouter()
        {
            TSource = new();
            Input = new();
            Output = new();
        }

        public void Dispose()
        {
            if (!TSource.IsCancellationRequested) TSource.Cancel();
            TSource.Dispose();
        }

        public void Push(U upd) => Input.Enqueue(upd);

        public U? TryPull() {
            U? o;
            Output.TryDequeue(out o);
            return o;
        }

        public void Start(R runner) => Task.Run(async () => {
            runner.TSource = TSource;
            while (true)
                await runner.Run(((DeniedOutputQueue<U>)Input), ((DeniedInputQueue<U>)Output));
        });
    }

    abstract class UpdateRunner<U, T> where U : IUpdateMsg<T>
    {
        public CancellationTokenSource? TSource {get; set;}
        public abstract Task Run(DeniedOutputQueue<U> i, DeniedInputQueue<U> o);
    }

    interface IUpdateMsg<T>
    {
        public T Type => default!;
    }

    class DeniedInputQueue<U> : ConcurrentQueue<U>
    {
        new void Enqueue(U u)
        {
            throw new InvalidOperationException("Trying to push to the output");
        }
    }

    class DeniedOutputQueue<U> : ConcurrentQueue<U>
    {
        new bool TryDequeue(out U? u)
        {
            u = default;
            throw new InvalidOperationException("Trying to pull from the input");
        }

        new bool TryPeek(out U? u)
        {
            u = default;
            throw new InvalidOperationException("Trying to peek from the input");
        }
    }
}
