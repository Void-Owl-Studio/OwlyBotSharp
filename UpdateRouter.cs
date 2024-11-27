using System.Collections.Concurrent;

namespace Owl
{
    sealed class UpdateRouter<R, U> : IDisposable
        where R : UpdateRunner<U>
    {
        private InputOnlyQueue<U> Input;
        private OutputOnlyQueue<U> Output;

        public Task MainTask;

        public R? Runner {private get; set;}

        public UpdateRouter()
        {
            Input = new();
            Output = new();
            MainTask = new(() => { Runner!.Run(Input, Output); },
                           TaskCreationOptions.LongRunning);
        }

        public void Dispose()
        {
            MainTask.Wait(Runner!.Token);
            Runner!.Dispose();
        }

        public void Enqueue(U upd)
        {
            Input.Enqueue(upd);
            if (MainTask.IsCompleted) MainTask.Start();
        }

        public U? TryDequeue()
        {
            U? o;
            Output.TryDequeue(out o);
            return o;
        }

        public U? TryPeek()
        {
            U? o;
            Output.TryPeek(out o);
            return o;
        }

        public void Start() {
            MainTask.Start();
        }
    }

    abstract class UpdateRunner<U> : IDisposable
    {
        protected CancellationTokenSource TSource = new();
        public CancellationToken Token
        {
            private init { Token = TSource.Token; }
            get { return Token; }
        }
        public abstract void Run(InputOnlyQueue<U> i, OutputOnlyQueue<U> o);
        public virtual void Dispose() => TSource.Dispose();
    }

    class OutputOnlyQueue<U> : ConcurrentQueue<U>
    {
        new void Enqueue(U u)
        {
            throw new InvalidOperationException("Trying to push to the output");
        }
    }

    class InputOnlyQueue<U> : ConcurrentQueue<U>
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
