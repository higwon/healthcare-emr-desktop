using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HealthNote.Desktop.ViewModels;

namespace HealthNote.Desktop.Views
{
    public partial class TimelineView : UserControl
    {
        public TimelineView()
        {
            InitializeComponent();
        }

        private void OnListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is TimelinePreviewViewModel vm && vm.Selected != null)
            {
                vm.DetailOpen = true;
                UpdateLayout();
                CloseDetail.Focus();
                e.Handled = true;
            }
        }

        private void OnDetailKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ReturnToList();
                e.Handled = true;
            }
        }

        private void OnCloseDetail(object sender, RoutedEventArgs e)
        {
            ReturnToList();
        }

        private void ReturnToList()
        {
            if (RecordList.SelectedItem != null)
            {
                RecordList.ScrollIntoView(RecordList.SelectedItem);
                RecordList.UpdateLayout();
                if (RecordList.ItemContainerGenerator.ContainerFromItem(RecordList.SelectedItem) is ListBoxItem item)
                {
                    item.Focus();
                    return;
                }
            }

            TypeFilter.Focus();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Removing a filtered selection must not leave keyboard focus in a hidden detail.
            if (RecordList.SelectedItem == null && DetailSurface.IsKeyboardFocusWithin)
            {
                TypeFilter.Focus();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Reflow();
        }

        private void OnDetailVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Reflow();
        }

        private void Reflow()
        {
            if (MasterDetail == null || DetailSurface == null)
            {
                return;
            }

            ScreenHeading.Visibility = ActualHeight < 440 ? Visibility.Collapsed : Visibility.Visible;
            bool open = DetailSurface.Visibility == Visibility.Visible;
            bool narrow = ActualWidth < 820;
            DetailColumn.Width = open && !narrow ? new GridLength(340) : new GridLength(0);
            DetailRow.Height = open && narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            Grid.SetColumn(DetailSurface, narrow ? 0 : 1);
            Grid.SetRow(DetailSurface, narrow ? 1 : 0);
            DetailSurface.Padding = new Thickness(narrow ? 12 : 20);
            DetailSurface.Margin = narrow ? new Thickness(0, 12, 0, 0) : new Thickness(16, 0, 0, 0);
        }
    }
}
