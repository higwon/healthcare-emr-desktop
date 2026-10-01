using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HealthNote.Desktop.Diagnostics;
using HealthNote.Desktop.Threading;
using HealthNote.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class WpfBehaviorTests
    {
        [STATestMethod]
        public void ShellBindings_ResolveCommandsAndReadyStateWithoutBindingErrors()
        {
            SourceLevels originalLevel = PresentationTraceSources.DataBindingSource.Switch.Level;
            using (StringWriter output = new StringWriter())
            using (TextWriterTraceListener listener = new TextWriterTraceListener(output))
            using (ShellViewModel shell = new ShellViewModel(() =>
                new ConnectionViewModel(new ReadyQuery(), new WpfUiDispatcher(Dispatcher.CurrentDispatcher), new ClientDiagnostics(_ => { }))))
            {
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
                PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
                MainWindow window = new MainWindow(shell) { ShowInTaskbar = false, ShowActivated = false };
                try
                {
                    ((Expander)window.FindName("ConnectionTools")).IsExpanded = true;
                    window.Show();
                    window.UpdateLayout();
                    List<Button> buttons = FindChildren<Button>(window);
                    Assert.IsTrue(buttons.Exists(button => button.Content as string == "연결 확인"));
                    Assert.IsTrue(buttons.TrueForAll(button => button.Command != null));
                    Assert.IsTrue(FindChildren<TextBlock>(window).Exists(text => text.Text == "데모 서버에 연결됐어요."));
                    shell.CloseConnectionCommand.Execute(null);
                    WpfTestPump.Until(shell.OpenConnectionCommand.ExecuteAsync(null));
                    window.UpdateLayout();

                    listener.Flush();
                    Assert.AreEqual(string.Empty, output.ToString(), "Compiled bindings emitted WPF errors.");
                }
                finally
                {
                    window.Close();
                    PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
                    PresentationTraceSources.DataBindingSource.Switch.Level = originalLevel;
                }
            }
        }

        [STATestMethod]
        public void BackgroundQueryCompletion_AppliesOnUiThread()
        {
            int uiThread = Thread.CurrentThread.ManagedThreadId;
            int appliedThread = 0;
            ControlledQuery query = new ControlledQuery();
            using (ConnectionViewModel vm = new ConnectionViewModel(query, new WpfUiDispatcher(Dispatcher.CurrentDispatcher),
                new ClientDiagnostics(sample =>
                {
                    if (sample.Kind == DiagnosticKind.ApplyCompleted)
                    {
                        appliedThread = Thread.CurrentThread.ManagedThreadId;
                    }
                })))
            {
                Task request = vm.InitializeAsync();
                _ = Task.Run(() => query.Requests[0].Completion.SetResult(true));
                WpfTestPump.Until(request);
                Assert.AreEqual(uiThread, appliedThread);
                Assert.AreEqual("데모 서버에 연결됐어요.", vm.Status);
            }
        }

        private static List<T> FindChildren<T>(DependencyObject root) where T : DependencyObject
        {
            List<T> matches = new List<T>();
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, index);
                if (child is T match)
                {
                    matches.Add(match);
                }

                matches.AddRange(FindChildren<T>(child));
            }

            return matches;
        }

    }

    internal static class WpfTestPump
    {
        public static void Until(Task task)
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            DispatcherFrame frame = new DispatcherFrame();
            DispatcherTimer watchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            EventHandler expired = (_, _) => frame.Continue = false;
            watchdog.Tick += expired;
            _ = task.ContinueWith(_ => dispatcher.BeginInvoke(new Action(() => frame.Continue = false)),
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            try
            {
                watchdog.Start();
                Dispatcher.PushFrame(frame);
                Assert.IsTrue(task.IsCompleted, "WPF operation exceeded test watchdog.");
                Assert.IsFalse(task.IsCanceled);
                Assert.IsNull(task.Exception);
            }
            finally
            {
                watchdog.Stop();
                watchdog.Tick -= expired;
            }
        }
    }
}
