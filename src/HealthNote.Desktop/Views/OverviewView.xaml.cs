using System.Windows.Controls;
using System.Windows;

namespace HealthNote.Desktop.Views
{
    public partial class OverviewView : UserControl
    {
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            bool narrow = ActualWidth < 900;
            MeasurementColumn.Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            Grid.SetColumn(LatestMeasurements, narrow ? 0 : 1);
            Grid.SetRow(LatestMeasurements, narrow ? 1 : 0);
            LatestMeasurements.Margin = narrow ? new Thickness(0, 16, 0, 0) : new Thickness(16, 0, 0, 0);
        }

        public OverviewView()
        {
            InitializeComponent();
        }
    }
}
