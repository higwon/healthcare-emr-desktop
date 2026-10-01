using System;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        private NavigationItem? _selectedNavigation;
        private string _period = "최근 3개월";

        public ShellViewModel(Func<ConnectionViewModel> createConnection)
        {
            this.createConnection = createConnection;
            Timeline = new TimelinePreviewViewModel();
            Trend = new TrendPreviewViewModel();
            Overview = new OverviewPreviewViewModel(OpenTimelineRecord, OpenTrend);
            Navigation = new[]
            {
                new NavigationItem("건강 요약", "◫", Overview),
                new NavigationItem("기록 탐색", "≡", Timeline),
                new NavigationItem("지표 추이", "↗", Trend)
            };
            _selectedNavigation = Navigation[0];
            OpenConnectionCommand = new AsyncRelayCommand(OpenConnectionAsync,
                () => !disposed && Current == null, AsyncRelayCommandOptions.AllowConcurrentExecutions);
            CloseConnectionCommand = new RelayCommand(CloseConnection, () => !disposed && Current != null);
        }

        public IAsyncRelayCommand OpenConnectionCommand { get; }
        public IReadOnlyList<NavigationItem> Navigation { get; }
        public IReadOnlyList<string> Periods { get; } = new[] { "최근 3개월", "최근 1개월" };
        public OverviewPreviewViewModel Overview { get; }
        public TimelinePreviewViewModel Timeline { get; }
        public TrendPreviewViewModel Trend { get; }
        public object? ActiveScreen => SelectedNavigation?.Screen;
        public NavigationItem? SelectedNavigation
        {
            get => _selectedNavigation;
            set
            {
                if (!disposed && value != null && SetProperty(ref _selectedNavigation, value))
                {
                    OnPropertyChanged(nameof(ActiveScreen));
                }
            }
        }

        public string Period
        {
            get => _period;
            set
            {
                if (SetProperty(ref _period, value))
                {
                    Overview.Period = value;
                    Timeline.Period = value;
                    Trend.Period = value;
                }
            }
        }
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

        private void OpenTimelineRecord(HealthNote.Desktop.Preview.RecordPreview? record)
        {
            SelectedNavigation = Navigation[1];
            if (record != null)
            {
                Timeline.Type = "전체";
                Timeline.Search = string.Empty;
                Timeline.State = "정상";
                Timeline.Selected = record;
            }
        }

        private void OpenTrend()
        {
            SelectedNavigation = Navigation[2];
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
