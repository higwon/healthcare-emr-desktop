using System;
using System.Windows;
using System.Windows.Threading;
using HealthNote.Desktop.Configuration;

namespace HealthNote.Desktop
{
    public partial class App : System.Windows.Application
    {
        private CompositionRoot? composition;
        private Action? stopSmoke;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DemoOptions options;
            try
            {
                options = DemoOptions.Parse(e.Args);
            }
            catch (ArgumentException)
            {
                Shutdown(3);
                return;
            }

            composition = new CompositionRoot(options, Dispatcher);
            MainWindow window = new MainWindow(composition.Shell);
            MainWindow = window;

            if (options.Smoke)
            {
                ConfigureSmoke(window);
            }

            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            stopSmoke?.Invoke();
            stopSmoke = null;
            composition?.Dispose();
            base.OnExit(e);
        }

        private void ConfigureSmoke(MainWindow window)
        {
            window.Opacity = 0;
            window.ShowInTaskbar = false;
            window.ShowActivated = false;

            DispatcherTimer watchdog = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            EventHandler rendered = delegate { };
            EventHandler expired = delegate { };

            stopSmoke = () =>
            {
                window.ContentRendered -= rendered;
                watchdog.Tick -= expired;
                watchdog.Stop();
            };

            void Finish(int exitCode)
            {
                stopSmoke?.Invoke();
                stopSmoke = null;
                Shutdown(exitCode);
            }

            rendered = (_, _) => Finish(0);
            expired = (_, _) => Finish(2);
            window.ContentRendered += rendered;
            watchdog.Tick += expired;
            watchdog.Start();
        }
    }
}
