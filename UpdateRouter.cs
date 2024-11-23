using System.Collections.Concurrent;

namespace Owl
{
    sealed class UpdateRouter<R, U, T> : IDisposable where R : UpdateRunner<U, T>, new() where U : IUpdateMsg<T> where T : IComparable
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
            while (!TSource.IsCancellationRequested)
                await runner.Run!(((DeniedOutputQueue<U>)Input), ((DeniedInputQueue<U>)Output), runner);
        });
    }

    abstract class UpdateRunner<U, T> where U : IUpdateMsg<T>
    {
        public Func<DeniedOutputQueue<U>, DeniedInputQueue<U>, UpdateRunner<U, T>, Task>? Run;
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
            throw new InvalidOperationException("Trying to pull from the input");
        }
    }
}
