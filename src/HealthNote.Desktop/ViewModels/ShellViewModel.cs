using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HealthNote.Desktop.ViewModels
{
    public sealed class ShellViewModel : ObservableObject, IDisposable
    {
        private readonly Func<ConnectionViewModel> createConnection;
        private ConnectionViewModel? current;
        private bool initialized;
        private bool disposed;

        public ShellViewModel(Func<ConnectionViewModel> createConnection)
        {
            this.createConnection = createConnection;
            OpenConnectionCommand = new AsyncRelayCommand(OpenConnectionAsync,
                () => !disposed && Current == null, AsyncRelayCommandOptions.AllowConcurrentExecutions);
            CloseConnectionCommand = new RelayCommand(CloseConnection, () => !disposed && Current != null);
        }

        public IAsyncRelayCommand OpenConnectionCommand { get; }
        public IRelayCommand CloseConnectionCommand { get; }
        public ConnectionViewModel? Current
        {
            get => current;
            private set
            {
                SetProperty(ref current, value);
                OpenConnectionCommand.NotifyCanExecuteChanged();
                CloseConnectionCommand.NotifyCanExecuteChanged();
            }
        }

        public Task InitializeAsync()
        {
            if (initialized || disposed)
            {
                return Task.CompletedTask;
            }

            initialized = true;
            return OpenConnectionAsync();
        }

        private async Task OpenConnectionAsync()
        {
            if (disposed || Current != null)
            {
                return;
            }

            ConnectionViewModel screen = createConnection();
            Current = screen;
            await screen.InitializeAsync();
        }

        private void CloseConnection()
        {
            if (disposed)
            {
                return;
            }

            Current?.Dispose();
            Current = null;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Current?.Dispose();
            Current = null;
            OpenConnectionCommand.NotifyCanExecuteChanged();
            CloseConnectionCommand.NotifyCanExecuteChanged();
        }
    }
}
