using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthNote.Application.Emr;
using HealthNote.Domain.Emr;
using HealthNote.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class EmrWorkspaceTests
    {
        [TestMethod]
        public async Task SearchSupersedesRequest_DuplicateGuardAndLateCompletionDoNotClearNewBusy()
        {
            var requests = new List<TaskCompletionSource<Page<Patient>>>();
            var tokens = new List<CancellationToken>();
            var query = new EmrQueryStub { Patients = (_, _, _, token) =>
            {
                var pending = new TaskCompletionSource<Page<Patient>>(TaskCreationOptions.RunContinuationsAsynchronously);
                requests.Add(pending);
                tokens.Add(token);
                return pending.Task;
            }};
            using (var vm = new EmrWorkspaceViewModel(query, new InlineDispatcher()))
            {
                Task old = vm.InitializeAsync();
                await vm.InitializeAsync();
                await vm.SearchCommand.ExecuteAsync(null);
                Assert.HasCount(1, requests);
                vm.Search = "new";
                Task current = vm.SearchCommand.ExecuteAsync(null);
                Assert.IsTrue(tokens[0].IsCancellationRequested);
                requests[0].SetResult(new Page<Patient>(new[] { EmrQueryStub.A }, 1, 1, 20));
                await TestCompletion.WaitAsync(old);
                Assert.IsTrue(vm.PatientsBusy);
                Assert.HasCount(0, vm.Patients);
                requests[1].SetResult(new Page<Patient>(new[] { EmrQueryStub.B }, 1, 1, 20));
                await TestCompletion.WaitAsync(current);
                Assert.AreSame(EmrQueryStub.B, vm.Patients[0]);
                Assert.IsFalse(vm.PatientsBusy);
                Assert.IsNull(vm.SelectedPatient);
            }
        }

        [TestMethod]
        public async Task PatientSwitch_RejectsLateHistoryAndDetailFromPreviousPatient()
        {
            var history = new List<TaskCompletionSource<Page<EncounterSummary>>>();
            var detail = new TaskCompletionSource<EncounterNote>(TaskCreationOptions.RunContinuationsAsynchronously);
            var query = new EmrQueryStub { History = (_, _, _, _) =>
            {
                var pending = new TaskCompletionSource<Page<EncounterSummary>>(TaskCreationOptions.RunContinuationsAsynchronously);
                history.Add(pending);
                return pending.Task;
            }, Detail = (_, _, _) => detail.Task };
            using (var vm = new EmrWorkspaceViewModel(query, new InlineDispatcher()))
            {
                await vm.InitializeAsync();
                vm.SelectedPatient = EmrQueryStub.A;
                Task old = vm.LoadHistoryCommand.ExecutionTask ?? throw new InvalidOperationException();
                vm.SelectedPatient = EmrQueryStub.B;
                Task current = vm.LoadHistoryCommand.ExecutionTask ?? throw new InvalidOperationException();
                var recordB = EmrQueryStub.Record(EmrQueryStub.B);
                history[1].SetResult(new Page<EncounterSummary>(new[] { recordB }, 1, 1, 20));
                await TestCompletion.WaitAsync(current);
                history[0].SetResult(new Page<EncounterSummary>(new[] { EmrQueryStub.Record(EmrQueryStub.A) }, 1, 1, 20));
                await TestCompletion.WaitAsync(old);
                Assert.AreSame(recordB, vm.History[0]);
                vm.SelectedRecord = recordB;
                Task pendingDetail = vm.LoadDetailCommand.ExecutionTask ?? throw new InvalidOperationException();
                vm.SelectedPatient = EmrQueryStub.A;
                Assert.IsNull(vm.Detail);
                Assert.HasCount(0, vm.History);
                detail.SetResult(EmrQueryStub.Note(recordB));
                await TestCompletion.WaitAsync(pendingDetail);
                Assert.IsNull(vm.Detail);
                Assert.IsTrue(vm.HistoryBusy);
                vm.Dispose();
                history[2].SetResult(new Page<EncounterSummary>(Array.Empty<EncounterSummary>(), 0, 1, 20));
                await TestCompletion.WaitAsync(vm.LoadHistoryCommand.ExecutionTask ?? throw new InvalidOperationException());
            }
        }

        [TestMethod]
        public async Task QueuedApplyAfterDispose_DoesNotPopulateScreen()
        {
            var dispatcher = new PausedDispatcher();
            var vm = new EmrWorkspaceViewModel(new EmrQueryStub(), dispatcher);
            Task pending = vm.InitializeAsync();
            await TestCompletion.WaitAsync(dispatcher.Pending.Task);
            vm.Dispose();
            dispatcher.Apply();
            await TestCompletion.WaitAsync(pending);
            Assert.HasCount(0, vm.Patients);
            Assert.IsFalse(vm.SearchCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task ErrorsRetryAndPaging_KeepAppliedSearchSnapshot()
        {
            bool fail = true;
            string received = string.Empty;
            int receivedPage = 0;
            var query = new EmrQueryStub { Patients = (search, page, size, _) =>
            {
                received = search;
                receivedPage = page;
                return fail ? Task.FromException<Page<Patient>>(new InvalidOperationException("private payload"))
                    : Task.FromResult(new Page<Patient>(new[] { EmrQueryStub.A }, 21, page, size));
            }};
            using (var vm = new EmrWorkspaceViewModel(query, new InlineDispatcher()))
            {
                vm.Search = " original ";
                await vm.SearchCommand.ExecuteAsync(null);
                Assert.IsTrue(vm.PatientsStatus.Contains("못했"));
                Assert.IsFalse(vm.PatientsStatus.Contains("payload"));
                vm.Search = "edited";
                fail = false;
                await vm.RetryPatientsCommand.ExecuteAsync(null);
                Assert.AreEqual("original", received);
                Assert.IsTrue(vm.NextPatientsCommand.CanExecute(null));
                vm.SelectedPatient = EmrQueryStub.A;
                await TestCompletion.WaitAsync(vm.LoadHistoryCommand.ExecutionTask ?? Task.CompletedTask);
                Assert.IsTrue(vm.HistoryStatus.Contains("없어요"));
                await vm.NextPatientsCommand.ExecuteAsync(null);
                Assert.AreEqual(2, receivedPage);
                Assert.AreEqual("original", received);
                Assert.IsNull(vm.SelectedPatient);
                Assert.IsNull(vm.SelectedRecord);
                Assert.IsNull(vm.Detail);
            }
        }

        [TestMethod]
        public async Task DetailFailureRetryAndCancel_CannotRestoreOldRecord()
        {
            bool fail = true;
            var record = EmrQueryStub.Record(EmrQueryStub.A);
            var query = new EmrQueryStub
            {
                History = (_, page, size, _) => Task.FromResult(new Page<EncounterSummary>(new[] { record }, 1, page, size)),
                Detail = (_, _, _) => fail ? Task.FromException<EncounterNote>(new InvalidOperationException())
                    : Task.FromResult(EmrQueryStub.Note(record))
            };
            using (var vm = new EmrWorkspaceViewModel(query, new InlineDispatcher()))
            {
                await vm.InitializeAsync();
                vm.SelectedPatient = EmrQueryStub.A;
                await TestCompletion.WaitAsync(vm.LoadHistoryCommand.ExecutionTask ?? Task.CompletedTask);
                vm.SelectedRecord = record;
                await TestCompletion.WaitAsync(vm.LoadDetailCommand.ExecutionTask ?? Task.CompletedTask);
                Assert.IsNull(vm.Detail);
                Assert.IsTrue(vm.DetailStatus.Contains("못했"));
                fail = false;
                await vm.LoadDetailCommand.ExecuteAsync(null);
                Assert.AreEqual(record.Id, vm.Detail?.Id);
                var pending = new TaskCompletionSource<EncounterNote>(TaskCreationOptions.RunContinuationsAsynchronously);
                query.Detail = (_, _, _) => pending.Task;
                Task late = vm.LoadDetailCommand.ExecuteAsync(null);
                vm.CancelCommand.Execute(null);
                pending.SetResult(EmrQueryStub.Note(record));
                await TestCompletion.WaitAsync(late);
                Assert.IsNull(vm.Detail);
                Assert.IsNull(vm.SelectedRecord);
            }
        }
    }
}
