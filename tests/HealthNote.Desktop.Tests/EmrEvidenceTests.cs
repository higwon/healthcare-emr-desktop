using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HealthNote.Desktop.Threading;
using HealthNote.Desktop.ViewModels;
using HealthNote.Desktop.Views;
using HealthNote.Infrastructure.Emr;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class EmrEvidenceTests
    {
        [STATestMethod]
        [TestCategory("Evidence")]
        public void ActualApiNativeWpfCaptures_AreSeparateFromCorrectness()
        {
            using (var api = new EmrApiFixture())
            {
                WpfTestPump.Until(api.StartAsync());
                Capture(api, "emr-record", false);
                Capture(api, "emr-small", true);
            }
        }

        private static void Capture(EmrApiFixture api, string name, bool small)
        {
            var vm = new EmrWorkspaceViewModel(new HttpEmrQuery(api.Client), new WpfUiDispatcher(Dispatcher.CurrentDispatcher));
            WpfTestPump.Until(vm.InitializeAsync());
            vm.SelectedPatient = vm.Patients[0];
            WpfTestPump.Until(vm.LoadHistoryCommand.ExecutionTask ?? throw new InvalidOperationException());
            vm.SelectedRecord = vm.History[0];
            WpfTestPump.Until(vm.LoadDetailCommand.ExecutionTask ?? throw new InvalidOperationException());
            using (var shell = EmrWpfTests.Shell(vm))
            {
                var window = new MainWindow(shell);
                if (small) { window.Width = 720; window.Height = 520; }
                try
                {
                    window.Show();
                    window.UpdateLayout();
                    if (small)
                    {
                        var view = ExplorationWpfTests.Children<EmrWorkspaceView>(window).Single();
                        ((FrameworkElement)view.FindName("DetailPanel")).BringIntoView();
                    }
                    WpfTestPump.Until(window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task);
                    window.UpdateLayout();
                    FrameworkElement content = (FrameworkElement)window.Content;
                    DpiScale dpi = VisualTreeHelper.GetDpi(content);
                    var bitmap = new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth * dpi.DpiScaleX),
                        (int)Math.Ceiling(content.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
                    var background = new DrawingVisual();
                    using (var drawing = background.RenderOpen())
                        drawing.DrawRectangle(window.Background, null, new Rect(0, 0, content.ActualWidth, content.ActualHeight));
                    bitmap.Render(background);
                    bitmap.Render(content);
                    string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/ui/emr102"));
                    Directory.CreateDirectory(directory);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var stream = File.Create(Path.Combine(directory, name + ".png"))) encoder.Save(stream);
                    string source = typeof(MainWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
                    File.WriteAllText(Path.Combine(directory, name + ".txt"),
                        "Actual loopback API -> HttpEmrQuery -> VM -> native WPF content render. Not OS screenshot or DPI matrix." + Environment.NewLine
                        + "Build: Release / " + source + Environment.NewLine
                        + "OS: " + Environment.OSVersion + Environment.NewLine
                        + "DPI: " + dpi.PixelsPerInchX + " x " + dpi.PixelsPerInchY + Environment.NewLine
                        + "Client DIP: " + content.ActualWidth + " x " + content.ActualHeight + Environment.NewLine
                        + "Outer DIP: " + window.ActualWidth + " x " + window.ActualHeight + Environment.NewLine
                        + "Work area DIP: " + WindowSizing.GetWorkArea(window) + Environment.NewLine
                        + "HighContrast: " + SystemParameters.HighContrast + Environment.NewLine);
                }
                finally { window.Close(); }
            }
        }
    }
}
