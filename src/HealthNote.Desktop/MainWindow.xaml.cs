using System;
using System.Windows;
using HealthNote.Desktop.ViewModels;

namespace HealthNote.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly ShellViewModel shell;

        public MainWindow(ShellViewModel shell)
        {
            this.shell = shell;
            InitializeComponent();
            DataContext = shell;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // The VM guards repeated Loaded; querying errors become safe screen state.
            try
            {
                await shell.InitializeAsync();
            }
            catch (Exception)
            {
                MessageBox.Show(this, "화면을 초기화하지 못했어요.", "HealthNote");
            }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            Loaded -= OnLoaded;
            Closed -= OnClosed;
            shell.Dispose();
            DataContext = null;
        }
    }
}
