using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class FoundationSmokeTests
    {
        [STATestMethod]
        public void StartupWindow_CreateAndLayout_LoadsCompiledXaml()
        {
            MainWindow window = new MainWindow();
            try
            {
                Assert.IsNotNull(window.Content);
                FrameworkElement content = (FrameworkElement)window.Content;
                content.Measure(new Size(960, 640));
                content.Arrange(new Rect(0, 0, 960, 640));
                Assert.IsGreaterThan(0d, content.ActualWidth);
                Assert.IsGreaterThan(0d, content.ActualHeight);
            }
            finally
            {
                window.Close();
            }
        }

        [TestMethod]
        public void DesktopExecutable_SmokeStartup_ExitsAfterContentRendered()
        {
            ProcessStartInfo start = new ProcessStartInfo(typeof(MainWindow).Assembly.Location, "--smoke")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (Process process = Process.Start(start) ?? throw new InvalidOperationException("Desktop process did not start."))
            {
                bool exited = process.WaitForExit(15000);
                if (!exited)
                {
                    process.Kill();
                }

                Assert.IsTrue(exited, "Desktop startup exceeded the smoke timeout.");
                Assert.AreEqual(0, process.ExitCode, "Compiled application failed its startup smoke.");
            }
        }
    }
}
