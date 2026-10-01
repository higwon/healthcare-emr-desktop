using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HealthNote.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class ExplorationEvidenceTests
    {
        [STATestMethod]
        [TestCategory("Evidence")]
        public void NativePreviewCaptures_WriteImagesAndEnvironment_SeparateFromCorrectness()
        {
            string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/ui/hc103"));
            Directory.CreateDirectory(directory);
            Capture(directory, "overview", _ => { });
            Capture(directory, "timeline-detail", PrepareTimeline);
            Capture(directory, "timeline-long", shell =>
            {
                shell.Timeline.LongTitle = true;
                PrepareTimeline(shell);
            });
            Capture(directory, "timeline-closed", shell => shell.SelectedNavigation = shell.Navigation[1]);
            Capture(directory, "trend", shell => shell.SelectedNavigation = shell.Navigation[2]);
            Capture(directory, "trend-missing", shell =>
            {
                shell.SelectedNavigation = shell.Navigation[2];
                shell.Trend.Selected = shell.Trend.Measurements[2];
            });
            Capture(directory, "timeline-small", shell =>
            {
                shell.Timeline.LongTitle = true;
                PrepareTimeline(shell);
            }, new Size(900, 520));
        }

        private static void PrepareTimeline(ShellViewModel shell)
        {
            shell.SelectedNavigation = shell.Navigation[1];
            shell.Timeline.Selected = shell.Timeline.Records[0];
        }

        private static void Capture(string directory, string name, Action<ShellViewModel> prepare, Size? size = null)
        {
            using (ShellViewModel shell = ExplorationPreviewTests.CreateShell())
            {
                prepare(shell);
                MainWindow window = new MainWindow(shell) { ShowInTaskbar = false };
                if (size.HasValue)
                {
                    window.Width = size.Value.Width;
                    window.Height = size.Value.Height;
                }

                try
                {
                    window.Show();
                    window.UpdateLayout();
                    WpfTestPump.Until(window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task);
                    window.UpdateLayout();
                    WriteCapture(window, directory, name);
                }
                finally
                {
                    window.Close();
                }
            }
        }

        private static void WriteCapture(MainWindow window, string directory, string name)
        {
            FrameworkElement content = (FrameworkElement)window.Content;
            DpiScale dpi = VisualTreeHelper.GetDpi(content);
            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                Math.Max(1, (int)Math.Ceiling(content.ActualWidth * dpi.DpiScaleX)),
                Math.Max(1, (int)Math.Ceiling(content.ActualHeight * dpi.DpiScaleY)),
                dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            DrawingVisual background = new DrawingVisual();
            using (DrawingContext drawing = background.RenderOpen())
            {
                drawing.DrawRectangle(window.Background, null, new Rect(0, 0, content.ActualWidth, content.ActualHeight));
            }
            bitmap.Render(background);
            bitmap.Render(content);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = File.Create(Path.Combine(directory, name + ".png")))
            {
                encoder.Save(stream);
            }

            string version = typeof(MainWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
            File.WriteAllText(Path.Combine(directory, name + ".txt"),
                "Native WPF content render; not an OS desktop screenshot or a multi-DPI matrix." + Environment.NewLine
                + "OS: " + Environment.OSVersion + Environment.NewLine
                + "Runtime: " + Environment.Version + Environment.NewLine
                + "Build: Release / " + version + Environment.NewLine
                + "DPI: " + dpi.PixelsPerInchX + " x " + dpi.PixelsPerInchY + Environment.NewLine
                + "Client DIP: " + content.ActualWidth + " x " + content.ActualHeight + Environment.NewLine
                + "Window DIP: " + window.ActualWidth + " x " + window.ActualHeight + Environment.NewLine
                + "Screen DIP: " + SystemParameters.PrimaryScreenWidth + " x " + SystemParameters.PrimaryScreenHeight + Environment.NewLine
                + "System work area DIP: " + SystemParameters.WorkArea + Environment.NewLine
                + "Current monitor usable DIP: " + WindowSizing.GetWorkSize(window) + Environment.NewLine
                + "High Contrast OS: " + SystemParameters.HighContrast + Environment.NewLine);
        }
    }
}
