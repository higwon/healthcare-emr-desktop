using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace HealthNote.Desktop.Threading
{
    public sealed class WpfUiDispatcher : IUiDispatcher
    {
        private readonly Dispatcher dispatcher;

        public WpfUiDispatcher(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public async Task ApplyAsync(Action action)
        {
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
                return;
            }

            if (dispatcher.CheckAccess())
            {
                action();
                return;
            }

            try
            {
                await dispatcher.InvokeAsync(action).Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
                // Shutdown may abort an operation that was already queued.
            }
        }
    }
}
