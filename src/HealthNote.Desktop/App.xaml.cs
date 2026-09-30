using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace HealthNote.Desktop
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow window = new MainWindow();
            MainWindow = window;

            if (e.Args.Contains("--smoke"))
            {
                ConfigureSmoke(window);
            }

            window.Show();
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

            void Finish(int exitCode)
            {
                window.ContentRendered -= rendered;
                watchdog.Tick -= expired;
                watchdog.Stop();
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
