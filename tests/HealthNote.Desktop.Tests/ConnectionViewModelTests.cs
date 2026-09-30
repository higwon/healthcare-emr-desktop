using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using HealthNote.Desktop.Diagnostics;
using HealthNote.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class ConnectionViewModelTests
    {
        [TestMethod]
        public async Task DuplicateCommandAndInitialization_OnlyIssueOneQuery()
        {
            ControlledQuery query = new ControlledQuery();
            using (ConnectionViewModel vm = Create(query))
            {
                Task first = vm.InitializeAsync();
                await vm.InitializeAsync();
                await vm.RefreshCommand.ExecuteAsync(null);
                Assert.HasCount(1, query.Requests);
                Assert.IsFalse(vm.RefreshCommand.CanExecute(null));
                Assert.IsTrue(vm.CancelCommand.CanExecute(null));
                query.Requests[0].Completion.SetResult(true);
                await TestCompletion.WaitAsync(first);
                Assert.AreEqual("데모 서버에 연결됐어요.", vm.Status);
                Assert.IsTrue(vm.RefreshCommand.CanExecute(null));
                Assert.IsFalse(vm.CancelCommand.CanExecute(null));
            }
        }

        [TestMethod]
        public async Task CancelThenRetry_LateOldResponseCannotReplaceNewResult()
        {
            ControlledQuery query = new ControlledQuery();
            using (ConnectionViewModel vm = Create(query))
            {
                Task old = vm.RefreshCommand.ExecuteAsync(null);
                vm.CancelCommand.Execute(null);
                Assert.IsTrue(query.Requests[0].CancellationToken.IsCancellationRequested);
                Assert.IsFalse(vm.IsBusy);
                Task current = vm.RefreshCommand.ExecuteAsync(null);
                Assert.HasCount(2, query.Requests);
                query.Requests[1].Completion.SetResult(true);
                await TestCompletion.WaitAsync(current);
                query.Requests[0].Completion.SetResult(false);
                await TestCompletion.WaitAsync(old);
                Assert.AreEqual("데모 서버에 연결됐어요.", vm.Status);
                Assert.IsFalse(vm.IsBusy);
            }
        }

        [TestMethod]
        public async Task OldResponseWhileNewQueryIsPending_DoesNotClearBusy()
        {
            ControlledQuery query = new ControlledQuery();
            using (ConnectionViewModel vm = Create(query))
            {
                Task old = vm.RefreshCommand.ExecuteAsync(null);
                vm.CancelCommand.Execute(null);
                Task current = vm.RefreshCommand.ExecuteAsync(null);
                query.Requests[0].Completion.SetResult(true);
                await TestCompletion.WaitAsync(old);
                Assert.IsTrue(vm.IsBusy);
                Assert.AreEqual("연결 확인 중…", vm.Status);
                query.Requests[1].Completion.SetResult(false);
                await TestCompletion.WaitAsync(current);
                Assert.AreEqual("데모 서버가 아직 준비되지 않았어요.", vm.Status);
            }
        }

        [TestMethod]
        public async Task DisposeWhileApplyIsQueued_BlocksMutationAndClosesScopeOnce()
        {
            ControlledQuery query = new ControlledQuery();
            PausedDispatcher dispatcher = new PausedDispatcher();
            ConcurrentQueue<DiagnosticSample> samples = new ConcurrentQueue<DiagnosticSample>();
            ConnectionViewModel vm = new ConnectionViewModel(query, dispatcher, new ClientDiagnostics(samples.Enqueue));
            Task request = vm.InitializeAsync();
            query.Requests[0].Completion.SetResult(true);
            await TestCompletion.WaitAsync(dispatcher.Pending.Task);
            string pendingStatus = vm.Status;
            int changes = 0;
            vm.PropertyChanged += (_, _) => changes++;
            vm.Dispose();
            vm.Dispose();
            Assert.IsTrue(query.Requests[0].CancellationToken.IsCancellationRequested);
            dispatcher.Apply();
            await TestCompletion.WaitAsync(request);
            await vm.RefreshCommand.ExecuteAsync(null);
            Assert.AreEqual(pendingStatus, vm.Status);
            Assert.AreEqual(0, changes);
            Assert.HasCount(1, query.Requests);
            Assert.AreEqual(1, samples.Count(s => s.Kind == DiagnosticKind.ScopeOpened));
            Assert.AreEqual(1, samples.Count(s => s.Kind == DiagnosticKind.ScopeClosed));
            Assert.AreEqual(0, samples.Count(s => s.Kind == DiagnosticKind.ApplyCompleted));
            Assert.IsFalse(vm.RefreshCommand.CanExecute(null));
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task QueryException_ShowsSafeErrorAndAllowsExplicitRetry()
        {
            ControlledQuery query = new ControlledQuery();
            ConcurrentQueue<DiagnosticSample> samples = new ConcurrentQueue<DiagnosticSample>();
            using (ConnectionViewModel vm = new ConnectionViewModel(query, new InlineDispatcher(), new ClientDiagnostics(samples.Enqueue)))
            {
                Task request = vm.InitializeAsync();
                query.Requests[0].Completion.SetException(new InvalidOperationException("PRIVATE_PAYLOAD_DO_NOT_LOG"));
                await TestCompletion.WaitAsync(request);
                Assert.IsFalse(vm.Status.Contains("PRIVATE"));
                Assert.IsFalse(vm.IsBusy);
                Assert.IsTrue(vm.RefreshCommand.CanExecute(null));
                await vm.InitializeAsync();
                Assert.HasCount(1, query.Requests);
                Task retry = vm.RefreshCommand.ExecuteAsync(null);
                query.Requests[1].Completion.SetResult(true);
                await TestCompletion.WaitAsync(retry);
                Assert.AreEqual("데모 서버에 연결됐어요.", vm.Status);
                Assert.AreEqual(1, samples.Count(s => s.Kind == DiagnosticKind.QueryFailed));
                Assert.AreEqual(2, samples.Count(s => s.Kind == DiagnosticKind.QueryCompleted));
                Assert.AreEqual(2, samples.Count(s => s.Kind == DiagnosticKind.ApplyCompleted));
                Assert.IsTrue(samples.All(s => s.ScopeId > 0 && s.Elapsed >= TimeSpan.Zero));
            }
        }

        [TestMethod]
        public async Task ExpectedCancellation_CompletesWithoutErrorState()
        {
            ControlledQuery query = new ControlledQuery();
            using (ConnectionViewModel vm = Create(query))
            {
                Task request = vm.InitializeAsync();
                vm.CancelCommand.Execute(null);
                query.Requests[0].Completion.SetCanceled();
                await TestCompletion.WaitAsync(request);
                Assert.AreEqual("연결 확인을 취소했어요.", vm.Status);
            }
        }

        [TestMethod]
        public async Task DiagnosticsFailure_DoesNotBreakCommandOrCleanup()
        {
            using (ConnectionViewModel vm = new ConnectionViewModel(new ReadyQuery(), new InlineDispatcher(),
                new ClientDiagnostics(_ => throw new InvalidOperationException("sink failure"))))
            {
                await vm.InitializeAsync();
                Assert.AreEqual("데모 서버에 연결됐어요.", vm.Status);
            }
        }

        private static ConnectionViewModel Create(ControlledQuery query) =>
            new ConnectionViewModel(query, new InlineDispatcher(), new ClientDiagnostics(_ => { }));
    }
}
