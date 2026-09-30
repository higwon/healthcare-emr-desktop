using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HealthNote.Desktop.Diagnostics;
using HealthNote.Desktop.Threading;
using HealthNote.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class ShellLifetimeTests
    {
        [TestMethod]
        public async Task CloseAndReopen_DisposesOldQueryAndKeepsNewScreen()
        {
            ControlledQuery query = new ControlledQuery();
            ConcurrentQueue<DiagnosticSample> samples = new ConcurrentQueue<DiagnosticSample>();
            using (ShellViewModel shell = new ShellViewModel(() =>
                new ConnectionViewModel(query, new InlineDispatcher(), new ClientDiagnostics(samples.Enqueue))))
            {
                Task initial = shell.InitializeAsync();
                ConnectionViewModel? first = shell.Current;
                Assert.IsNotNull(first);
                await shell.InitializeAsync();
                await shell.OpenConnectionCommand.ExecuteAsync(null);
                Assert.HasCount(1, query.Requests);
                shell.CloseConnectionCommand.Execute(null);
                Assert.IsNull(shell.Current);
                Assert.IsTrue(query.Requests[0].CancellationToken.IsCancellationRequested);
                Task reopen = shell.OpenConnectionCommand.ExecuteAsync(null);
                Assert.AreNotSame(first, shell.Current);
                query.Requests[1].Completion.SetResult(true);
                await TestCompletion.WaitAsync(reopen);
                query.Requests[0].Completion.SetResult(false);
                await TestCompletion.WaitAsync(initial);
                Assert.IsNotNull(shell.Current);
                Assert.AreEqual("데모 서버에 연결됐어요.", shell.Current.Status);
            }

            Assert.AreEqual(2, samples.Count(s => s.Kind == DiagnosticKind.ScopeClosed));
        }

        [TestMethod]
        public async Task RepeatedScreenScopes_CloseExactlyOnceEachAndRejectOpenAfterDispose()
        {
            ConcurrentQueue<DiagnosticSample> samples = new ConcurrentQueue<DiagnosticSample>();
            ClientDiagnostics diagnostics = new ClientDiagnostics(samples.Enqueue);
            ShellViewModel shell = new ShellViewModel(() =>
                new ConnectionViewModel(new ReadyQuery(), new InlineDispatcher(), diagnostics));
            for (int index = 0; index < 50; index++)
            {
                await shell.OpenConnectionCommand.ExecuteAsync(null);
                shell.CloseConnectionCommand.Execute(null);
            }

            shell.Dispose();
            shell.Dispose();
            await shell.OpenConnectionCommand.ExecuteAsync(null);
            Assert.IsNull(shell.Current);
            Assert.IsFalse(shell.OpenConnectionCommand.CanExecute(null));
            Assert.AreEqual(50, samples.Count(s => s.Kind == DiagnosticKind.ScopeOpened));
            Assert.AreEqual(50, samples.Count(s => s.Kind == DiagnosticKind.ScopeClosed));
            Assert.AreEqual(50, samples.Where(s => s.Kind == DiagnosticKind.ScopeOpened).Select(s => s.ScopeId).Distinct().Count());
        }

        [STATestMethod]
        public void WindowUnloaded_DoesNotEndScope_WindowClosedDoes()
        {
            ControlledQuery query = new ControlledQuery();
            using (ShellViewModel shell = new ShellViewModel(() =>
                new ConnectionViewModel(query, new WpfUiDispatcher(System.Windows.Threading.Dispatcher.CurrentDispatcher),
                    new ClientDiagnostics(_ => { }))))
            {
                MainWindow window = new MainWindow(shell);
                Task initial = shell.InitializeAsync();
                window.Show();
                Assert.HasCount(1, query.Requests);
                window.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Assert.IsFalse(query.Requests[0].CancellationToken.IsCancellationRequested);
                Assert.HasCount(1, query.Requests);
                window.Close();
                Assert.IsTrue(query.Requests[0].CancellationToken.IsCancellationRequested);
                Assert.IsNull(shell.Current);
                Assert.IsNull(window.DataContext);
                // Complete the ignored cancellation: queued apply must still reject this closed scope.
                query.Requests[0].Completion.SetResult(true);
                WpfTestPump.Until(initial);
            }
        }
    }
}
