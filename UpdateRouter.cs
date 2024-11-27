using System.Collections.Concurrent;

namespace Owl
{
    sealed class UpdateRouter<R, U> : IDisposable
        where R : IUpdateRunner<U>
    {

        private ConcurrentQueue<U> Input, Output;

        public R Runner {private get; set;}

        private IUpdateRunner<U>.RunStarter Starter;

        private event EventHandler? QueueUpdateHandler;

        public UpdateRouter()
        {
            Starter = new(Runner!.Run);
            Input = new();
            Output = new();
        }

        public void Dispose()
        {
            if (Starter.GetInvocationList().GetLength(0) == 0)
                Starter.EndInvoke(null);
            Runner.Dispose();
        }

        public void Push(U upd)
        {
            Input.Enqueue(upd);
            if (Starter.GetInvocationList().GetLength(0) == 0)
                Starter.BeginInvoke(((DeniedOutputQueue<U>)Input), ((DeniedInputQueue<U>)Output), null, null);
        }

        public U? TryPull()
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
            Starter.BeginInvoke(((DeniedOutputQueue<U>)Input), ((DeniedInputQueue<U>)Output), null, null);
        }
    }

    interface IUpdateRunner<U> : IDisposable
    {
        public void Run(DeniedOutputQueue<U> i, DeniedInputQueue<U> o);
        public delegate void RunStarter(DeniedOutputQueue<U> i, DeniedInputQueue<U> o);
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
