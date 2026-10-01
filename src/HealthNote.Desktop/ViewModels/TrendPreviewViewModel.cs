using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HealthNote.Desktop.Preview;

namespace HealthNote.Desktop.ViewModels
{
    public sealed class TrendPreviewViewModel : ObservableObject
    {
        private string _metric = "체중";
        private string _period = "최근 3개월";
        private MeasurementPreview? _selected;
        private IReadOnlyList<MeasurementPreview> _measurements;

        public TrendPreviewViewModel()
        {
            _measurements = ExplorationFixture.Measurements(_metric);
            _selected = _measurements[0];
        }

        public IReadOnlyList<string> Metrics { get; } = new[] { "체중", "골격근량" };
        public IReadOnlyList<MeasurementPreview> Measurements => _measurements;
        public string Metric
        {
            get => _metric;
            set
            {
                if (SetProperty(ref _metric, value))
                {
                    Refresh();
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
                    Refresh();
                }
            }
        }

        public MeasurementPreview? Selected { get => _selected; set => SetProperty(ref _selected, value); }

        private void Refresh()
        {
            string? selectedDate = Selected?.Date;
            _measurements = ExplorationFixture.Measurements(Metric)
                .Where(m => Period != "최근 1개월" || m.Date.StartsWith("2026.09", System.StringComparison.Ordinal)).ToArray();
            Selected = _measurements.FirstOrDefault(m => m.Date == selectedDate);
            OnPropertyChanged(nameof(Measurements));
        }
    }
}
