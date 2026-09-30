using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthNote.Application;
using HealthNote.Desktop.Diagnostics;
using HealthNote.Desktop.Threading;

namespace HealthNote.Desktop.ViewModels
{
    public sealed class ConnectionViewModel : ObservableObject, IDisposable
    {
        private readonly IClientReadinessQuery query;
        private readonly IUiDispatcher dispatcher;
        private readonly ClientDiagnostics diagnostics;
        private readonly long scopeId;
        private CancellationTokenSource? activeQuery;
        private long generation;
        private bool initialized;
        private bool disposed;
        private bool isBusy;
        private string status = "데모 서버 연결을 확인해 주세요.";

        public ConnectionViewModel(IClientReadinessQuery query, IUiDispatcher dispatcher, ClientDiagnostics diagnostics)
        {
            this.query = query;
            this.dispatcher = dispatcher;
            this.diagnostics = diagnostics;
            scopeId = diagnostics.OpenScope();
            RefreshCommand = new AsyncRelayCommand(RefreshAsync, CanRefresh,
                AsyncRelayCommandOptions.AllowConcurrentExecutions);
            CancelCommand = new RelayCommand(CancelQuery, () => !disposed && IsBusy);
        }

        public IAsyncRelayCommand RefreshCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public string Status
        {
            get => status;
            private set => SetProperty(ref status, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    RefreshCommand.NotifyCanExecuteChanged();
                    CancelCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public Task InitializeAsync()
        {
            if (initialized || disposed)
            {
                return Task.CompletedTask;
            }

            initialized = true;
            return RefreshAsync();
        }

        private bool CanRefresh() => !disposed && !IsBusy;

        private async Task RefreshAsync()
        {
            // CanExecute is a UI hint; direct ExecuteAsync calls need the same guard.
            if (!CanRefresh())
            {
                return;
            }

            CancellationTokenSource cancellation = new CancellationTokenSource();
            activeQuery = cancellation;
            long requestGeneration = ++generation;
            IsBusy = true;
            Status = "연결 확인 중…";
            Stopwatch queryWatch = Stopwatch.StartNew();
            bool ready = false;
            bool failed = false;
            try
            {
                ready = await query.IsReadyAsync(cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Cancellation changes generation in CancelQuery/Dispose.
            }
            catch (Exception)
            {
                failed = true;
                diagnostics.Record(DiagnosticKind.QueryFailed, scopeId, queryWatch.Elapsed);
            }
            finally
            {
                queryWatch.Stop();
                diagnostics.Record(DiagnosticKind.QueryCompleted, scopeId, queryWatch.Elapsed);
            }

            try
            {
                await dispatcher.ApplyAsync(() =>
                {
                    if (disposed || requestGeneration != generation || cancellation.IsCancellationRequested)
                    {
                        return;
                    }

                    Stopwatch applyWatch = Stopwatch.StartNew();
                    activeQuery = null;
                    Status = failed ? "연결을 확인하지 못했어요. 서버 상태를 확인한 뒤 다시 시도해 주세요."
                        : ready ? "데모 서버에 연결됐어요." : "데모 서버가 아직 준비되지 않았어요.";
                    IsBusy = false;
                    diagnostics.Record(DiagnosticKind.ApplyCompleted, scopeId, applyWatch.Elapsed);
                }).ConfigureAwait(false);
            }
            finally
            {
                // The request owns this CTS until its provider and queued apply finish.
                cancellation.Dispose();
            }
        }

        private void CancelQuery()
        {
            if (disposed || !IsBusy)
            {
                return;
            }

            ++generation;
            CancellationTokenSource? previous = activeQuery;
            activeQuery = null;
            previous?.Cancel();
            IsBusy = false;
            Status = "연결 확인을 취소했어요.";
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            ++generation;
            CancellationTokenSource? previous = activeQuery;
            activeQuery = null;
            previous?.Cancel();
            isBusy = false;
            RefreshCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
            diagnostics.Record(DiagnosticKind.ScopeClosed, scopeId, TimeSpan.Zero);
        }
    }
}
