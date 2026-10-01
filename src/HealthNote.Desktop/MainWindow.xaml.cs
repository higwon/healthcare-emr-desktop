using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using HealthNote.Desktop.ViewModels;

namespace HealthNote.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly ShellViewModel shell;
        private bool _closed;

        public MainWindow(ShellViewModel shell)
        {
            this.shell = shell;
            InitializeComponent();
            DataContext = shell;
            Loaded += OnLoaded;
            Closed += OnClosed;
            SourceInitialized += OnSourceInitialized;
            SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
            ApplySystemContrast();
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            Rect work = WindowSizing.GetWorkArea(this);
            WindowSizing.Fit(this, work.Size);
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = work.Left + (work.Width - Width) / 2;
            Top = work.Top + (work.Height - Height) / 2;
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            if (!_closed && IsLoaded)
            {
                WindowSizing.Fit(this, WindowSizing.GetWorkSize(this));
            }
        }

        private void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemParameters.HighContrast))
            {
                if (Dispatcher.CheckAccess()) ApplySystemContrast();
                else Dispatcher.BeginInvoke(new Action(ApplySystemContrast));
            }
        }

        private void ApplySystemContrast()
        {
            if (_closed)
            {
                return;
            }
            string[] keys = { "AppBackgroundBrush", "SurfaceBrush", "PrimaryTextBrush", "SecondaryTextBrush", "BorderBrush", "AccentBrush", "SelectionBrush", "SelectionTextBrush", "ErrorBrush", "FocusBrush" };
            foreach (string key in keys)
            {
                Resources.Remove(key);
            }
            if (!SystemParameters.HighContrast)
            {
                return;
            }
            Resources["AppBackgroundBrush"] = FindResource(SystemColors.WindowBrushKey);
            Resources["SurfaceBrush"] = FindResource(SystemColors.WindowBrushKey);
            Resources["PrimaryTextBrush"] = FindResource(SystemColors.WindowTextBrushKey);
            Resources["SecondaryTextBrush"] = FindResource(SystemColors.WindowTextBrushKey);
            Resources["BorderBrush"] = FindResource(SystemColors.WindowTextBrushKey);
            Resources["AccentBrush"] = FindResource(SystemColors.HighlightBrushKey);
            Resources["SelectionBrush"] = FindResource(SystemColors.HighlightBrushKey);
            Resources["SelectionTextBrush"] = FindResource(SystemColors.HighlightTextBrushKey);
            Resources["ErrorBrush"] = FindResource(SystemColors.WindowTextBrushKey);
            Resources["FocusBrush"] = FindResource(SystemColors.HighlightBrushKey);
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
            _closed = true;
            SourceInitialized -= OnSourceInitialized;
            SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
            shell.Dispose();
            DataContext = null;
        }
    }
}
