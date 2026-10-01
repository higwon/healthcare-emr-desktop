using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
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

        public ShellViewModel(Func<ConnectionViewModel> createConnection, EmrWorkspaceViewModel? emr = null)
        {
            this.createConnection = createConnection;
            Timeline = new TimelinePreviewViewModel();
            Trend = new TrendPreviewViewModel();
            Overview = new OverviewPreviewViewModel(OpenTimelineRecord, OpenTrend);
            Emr = emr;
            Navigation = emr != null ? new[] { new NavigationItem("환자 진료 기록", "≡", emr) } : new[]
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
        public EmrWorkspaceViewModel? Emr { get; }
        public bool IsEmrWorkspace => Emr != null;
        public string Brand => IsEmrWorkspace ? "진료노트" : "건강노트";
        public string ContextTitle => IsEmrWorkspace ? "환자 진료 기록" : "데모 프로필";
        public string ContextDescription => IsEmrWorkspace ? "합성 환자 데모 · 저장된 기록 조회" : "합성 데이터 Preview · 실제 조회/저장 없음";
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
                Timeline.Selected = Timeline.Records.FirstOrDefault(item => item.Id == record.Id);
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
            return Emr != null ? Task.WhenAll(OpenConnectionAsync(), Emr.InitializeAsync()) : OpenConnectionAsync();
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
            Emr?.Dispose();
            Current?.Dispose();
            Current = null;
            OpenConnectionCommand.NotifyCanExecuteChanged();
            CloseConnectionCommand.NotifyCanExecuteChanged();
        }
    }
}
