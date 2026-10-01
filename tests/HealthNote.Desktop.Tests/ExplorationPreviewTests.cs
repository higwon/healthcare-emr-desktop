using System.Linq;
using HealthNote.Desktop.Diagnostics;
using HealthNote.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class ExplorationPreviewTests
    {
        [TestMethod]
        public void OverviewRecord_OpenTimeline_SelectsSameRecord()
        {
            using (ShellViewModel shell = CreateShell())
            {
                var record = shell.Overview.RecentRecords[2];
                shell.Overview.OpenRecordCommand.Execute(record);
                Assert.AreSame(shell.Timeline, shell.ActiveScreen);
                Assert.AreEqual(record.Id, shell.Timeline.Selected?.Id);
                Assert.IsTrue(shell.Timeline.DetailOpen);
                shell.Overview.OpenTrendCommand.Execute(null);
                Assert.AreSame(shell.Trend, shell.ActiveScreen);
            }
        }

        [TestMethod]
        public void Filter_RemovesSelectedRecord_DoesNotSelectFirstResult()
        {
            TimelinePreviewViewModel vm = new TimelinePreviewViewModel();
            vm.Selected = vm.Records[0];
            vm.Type = "증상";
            Assert.HasCount(1, vm.Records);
            Assert.IsNull(vm.Selected);
            Assert.IsFalse(vm.DetailOpen);
        }

        [TestMethod]
        public void Search_PreservesMatchingIdentity_AndClearsNonmatchingSelection()
        {
            TimelinePreviewViewModel vm = new TimelinePreviewViewModel();
            vm.Selected = vm.Records[0];
            vm.Search = "체성분";
            Assert.AreEqual("body-3", vm.Selected?.Id);
            vm.Search = "존재하지 않는 기록";
            Assert.HasCount(0, vm.Records);
            Assert.IsNull(vm.Selected);
            Assert.IsTrue(vm.Notice.Contains("일치하는 기록"));
        }

        [TestMethod]
        public void LongTitleRefresh_DoesNotReopenClosedDetail()
        {
            TimelinePreviewViewModel vm = new TimelinePreviewViewModel();
            vm.Selected = vm.Records[0];
            vm.CloseDetailCommand.Execute(null);
            vm.LongTitle = true;
            Assert.AreEqual("body-3", vm.Selected?.Id);
            Assert.IsFalse(vm.DetailOpen);
            Assert.IsGreaterThan(50, vm.Selected?.Title.Length ?? 0);
        }

        [TestMethod]
        public void LoadingCancel_IsIdle_NotSuccessfulEmpty_AndRetryRestoresFixture()
        {
            TimelinePreviewViewModel vm = new TimelinePreviewViewModel();
            vm.State = "조회 중";
            Assert.HasCount(0, vm.Records);
            vm.CancelCommand.Execute(null);
            Assert.AreEqual("조회 전 / 취소", vm.State);
            Assert.IsTrue(vm.Notice.Contains("아직 새 자료"));
            vm.State = "조회 실패";
            Assert.IsTrue(vm.IsError);
            vm.RetryCommand.Execute(null);
            Assert.HasCount(6, vm.Records);
            Assert.IsFalse(vm.IsError);
        }

        [TestMethod]
        public void StalePreview_LabelsPreviousPeriod_AndPeriodChangesAllScreens()
        {
            using (ShellViewModel shell = CreateShell())
            {
                shell.Period = "최근 1개월";
                Assert.HasCount(5, shell.Timeline.Records);
                Assert.HasCount(3, shell.Trend.Measurements);
                Assert.AreEqual(5, shell.Overview.RecordCount);
                shell.Timeline.State = "이전 조건의 자료";
                Assert.HasCount(6, shell.Timeline.Records);
                Assert.IsTrue(shell.Timeline.ResultContext.Contains(" 이전 최근 3개월"));
                Assert.IsTrue(shell.Timeline.ResultContext.Contains("현재 조건: 최근 1개월"));
            }
        }

        [TestMethod]
        public void TrendMissingSelection_HasNoValueUnit_AndMetricRefreshPreservesDate()
        {
            TrendPreviewViewModel vm = new TrendPreviewViewModel();
            vm.Selected = vm.Measurements.Single(m => !m.Value.HasValue);
            Assert.AreEqual("측정값 없음", vm.Selected.DisplayValue);
            Assert.AreEqual("결측", vm.Selected.TableValue);
            vm.Metric = "골격근량";
            Assert.AreEqual("2026.09.07", vm.Selected?.Date);
            Assert.AreEqual("측정값 없음", vm.Selected?.DisplayValue);
            vm.Selected = vm.Measurements[0];
            Assert.AreEqual("31.2 kg", vm.Selected.DisplayValue);
        }

        internal static ShellViewModel CreateShell()
        {
            return new ShellViewModel(() =>
                new ConnectionViewModel(new ReadyQuery(), new InlineDispatcher(), new ClientDiagnostics(_ => { })));
        }
    }
}
