using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HealthNote.Desktop.ViewModels;

namespace HealthNote.Desktop.Views
{
    public partial class EmrWorkspaceView : UserControl
    {
        public EmrWorkspaceView() => InitializeComponent();

        private void OnSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is EmrWorkspaceViewModel vm)
            {
                vm.SearchCommand.Execute(null);
                e.Handled = true;
            }
        }
        private void OnPatientKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { RecordList.Focus(); e.Handled = true; }
        }
        private void OnRecordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { DetailHeading.Focus(); e.Handled = true; }
            else if (e.Key == Key.Escape) { FocusSelected(PatientList); e.Handled = true; }
        }
        private void OnDetailKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { FocusSelected(RecordList); e.Handled = true; }
        }
        private static void FocusSelected(ListBox list)
        {
            if (list.SelectedItem != null)
            {
                list.ScrollIntoView(list.SelectedItem);
                list.UpdateLayout();
                if (list.ItemContainerGenerator.ContainerFromItem(list.SelectedItem) is ListBoxItem item)
                {
                    item.Focus();
                    return;
                }
            }
            list.Focus();
        }
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WorkspaceGrid == null) return;
            bool narrow = ActualWidth < 900;
            WorkspaceScroll.VerticalScrollBarVisibility = narrow ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
            PatientColumn.Width = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(220);
            HistoryColumn.Width = narrow ? new GridLength(0) : new GridLength(260);
            DetailColumn.Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            PatientRow.Height = narrow ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
            HistoryRow.Height = narrow ? GridLength.Auto : new GridLength(0);
            DetailRow.Height = narrow ? GridLength.Auto : new GridLength(0);
            Grid.SetColumn(HistoryPanel, narrow ? 0 : 1);
            Grid.SetColumn(DetailPanel, narrow ? 0 : 2);
            Grid.SetRow(HistoryPanel, narrow ? 1 : 0);
            Grid.SetRow(DetailPanel, narrow ? 2 : 0);
            HistoryPanel.Margin = DetailPanel.Margin = narrow ? new Thickness(0, 12, 0, 0) : new Thickness(12, 0, 0, 0);
            PatientList.Height = RecordList.Height = narrow ? 220 : double.NaN;
            DetailPanel.MaxHeight = narrow ? 500 : double.PositiveInfinity;
        }
    }
}
