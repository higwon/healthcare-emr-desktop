using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using HealthNote.Desktop.Diagnostics;
using HealthNote.Desktop.Threading;
using HealthNote.Desktop.ViewModels;
using HealthNote.Desktop.Views;
using HealthNote.Infrastructure.Emr;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class EmrWpfTests
    {
        [STATestMethod]
        public void ActualHttpToWpf_SelectsPatientRecordAndDetailWithoutBindingErrors()
        {
            using (var api = new EmrApiFixture())
            {
                WpfTestPump.Until(api.StartAsync());
                var vm = new EmrWorkspaceViewModel(new HttpEmrQuery(api.Client), new WpfUiDispatcher(Dispatcher.CurrentDispatcher));
                WpfTestPump.Until(vm.InitializeAsync());
                using (var shell = Shell(vm))
                using (var output = new StringWriter())
                using (var listener = new TextWriterTraceListener(output))
                {
                    SourceLevels old = PresentationTraceSources.DataBindingSource.Switch.Level;
                    PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
                    PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
                    var window = new MainWindow(shell);
                    try
                    {
                        window.Show();
                        window.Activate();
                        window.UpdateLayout();
                        Assert.HasCount(3, vm.Patients);
                        Assert.HasCount(1, shell.Navigation);
                        var view = ExplorationWpfTests.Children<EmrWorkspaceView>(window).Single();
                        var patients = (ListBox)view.FindName("PatientList");
                        patients.SelectedIndex = 0;
                        WpfTestPump.Until(vm.LoadHistoryCommand.ExecutionTask ?? throw new InvalidOperationException());
                        window.UpdateLayout();
                        var records = (ListBox)view.FindName("RecordList");
                        Assert.HasCount(1, vm.History);
                        records.SelectedIndex = 0;
                        WpfTestPump.Until(vm.LoadDetailCommand.ExecutionTask ?? throw new InvalidOperationException());
                        window.UpdateLayout();
                        Assert.AreEqual(api.RecordId, vm.Detail?.Id.ToString("D"));
                        Assert.AreEqual(api.PatientId, vm.Detail?.PatientId.ToString("D"));
                        Assert.AreSame(vm.History[0], records.SelectedItem);
                        Assert.IsTrue(ExplorationWpfTests.Children<TextBlock>(view).Any(t => t.Text == "합성 평가 기록입니다."));
                        var peer = new ListBoxAutomationPeer(records).GetChildren().Single();
                        Assert.IsTrue(((ISelectionItemProvider)peer.GetPattern(PatternInterface.SelectionItem)).IsSelected);
                        var item = (ListBoxItem)records.ItemContainerGenerator.ContainerFromIndex(0);
                        item.Focus();
                        RaiseKey(records, Key.Enter);
                        Assert.IsTrue(((TextBlock)view.FindName("DetailHeading")).IsKeyboardFocused);
                        RaiseKey((UIElement)view.FindName("DetailPanel"), Key.Escape);
                        Assert.IsTrue(item.IsKeyboardFocused);
                        patients.SelectedIndex = 1;
                        Assert.IsNull(vm.Detail);
                        WpfTestPump.Until(vm.LoadHistoryCommand.ExecutionTask ?? throw new InvalidOperationException());
                        Assert.HasCount(0, vm.History);
                        Assert.IsTrue(vm.HistoryStatus.Contains("없어요"));
                        window.UpdateLayout();
                        listener.Flush();
                        Assert.AreEqual(string.Empty, output.ToString());
                    }
                    finally
                    {
                        window.Close();
                        PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
                        PresentationTraceSources.DataBindingSource.Switch.Level = old;
                    }
                }
            }
        }

        internal static ShellViewModel Shell(EmrWorkspaceViewModel vm) => new ShellViewModel(
            () => new ConnectionViewModel(new ReadyQuery(), new InlineDispatcher(), new ClientDiagnostics(_ => { })), vm);
        private static void RaiseKey(UIElement target, Key key) => target.RaiseEvent(
            new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target), Environment.TickCount, key)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
    }
}
