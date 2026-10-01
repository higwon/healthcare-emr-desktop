using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthNote.Desktop.Preview;

namespace HealthNote.Desktop.ViewModels
{
    public sealed class OverviewPreviewViewModel : ObservableObject
    {
        private string _period = "최근 3개월";

        public OverviewPreviewViewModel(Action<RecordPreview?> openTimeline, Action openTrend)
        {
            OpenTimelineCommand = new RelayCommand(() => openTimeline(null));
            OpenRecordCommand = new RelayCommand<RecordPreview>(record => openTimeline(record));
            OpenTrendCommand = new RelayCommand(openTrend);
        }

        public string Period
        {
            get => _period;
            set
            {
                if (SetProperty(ref _period, value))
                {
                    OnPropertyChanged(nameof(RecordCount));
                    OnPropertyChanged(nameof(TypeCount));
                    OnPropertyChanged(nameof(MeasurementCount));
                    OnPropertyChanged(nameof(RecentRecords));
                }
            }
        }

        public int RecordCount => Period == "최근 1개월" ? 5 : 6;
        public int TypeCount => RecordCount;
        public int MeasurementCount => Period == "최근 1개월" ? 3 : 5;
        public IReadOnlyList<RecordPreview> RecentRecords => ExplorationFixture.Records().Take(4).ToArray();
        public IRelayCommand OpenTimelineCommand { get; }
        public IRelayCommand<RecordPreview> OpenRecordCommand { get; }
        public IRelayCommand OpenTrendCommand { get; }
    }
}
