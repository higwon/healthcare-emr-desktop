using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HealthNote.Desktop.ViewModels;
using HealthNote.Desktop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class ExplorationWpfTests
    {
        [STATestMethod]
        public void ThreeScreens_RuntimeBindingsAndResources_ResolveWithoutErrors()
        {
            SourceLevels old = PresentationTraceSources.DataBindingSource.Switch.Level;
            using (StringWriter output = new StringWriter())
            using (TextWriterTraceListener listener = new TextWriterTraceListener(output))
            using (ShellViewModel shell = ExplorationPreviewTests.CreateShell())
            {
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
                PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
                MainWindow window = new MainWindow(shell);
                try
                {
                    window.Show();
                    Rect work = WindowSizing.GetWorkArea(window);
                    Assert.IsGreaterThanOrEqualTo(work.Left, window.Left);
                    Assert.IsGreaterThanOrEqualTo(work.Top, window.Top);
                    Assert.IsLessThanOrEqualTo(work.Right, window.Left + window.ActualWidth);
                    Assert.IsLessThanOrEqualTo(work.Bottom, window.Top + window.ActualHeight);
                    foreach (NavigationItem screen in shell.Navigation)
                    {
                        shell.SelectedNavigation = screen;
                        window.UpdateLayout();
                        Assert.IsGreaterThan(0, Children<UserControl>(window).Count);
                    }

                    shell.Trend.Selected = shell.Trend.Measurements[2];
                    window.UpdateLayout();
                    Assert.IsTrue(Children<TextBlock>(window).Any(t => t.Text == "측정값 없음"));
                    shell.Trend.Metric = "골격근량";
                    window.UpdateLayout();
                    Assert.AreEqual("2026.09.07", shell.Trend.Selected?.Date);
                    string[] keys = { "AppBackgroundBrush", "SurfaceBrush", "PrimaryTextBrush", "BorderBrush", "AccentBrush", "FocusBrush", "BooleanVisibility" };
                    foreach (string key in keys) Assert.IsNotNull(window.FindResource(key));
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

        [STATestMethod]
        public void NativePeers_NavigationAndRecordSelection_HaveNameRoleEnabledAndSelected()
        {
            using (ShellViewModel shell = ExplorationPreviewTests.CreateShell())
            {
                MainWindow window = new MainWindow(shell);
                try
                {
                    window.Show();
                    var navigation = (ListBox)window.FindName("NavigationList");
                    window.UpdateLayout();
                    FrameworkElementAutomationPeer peer = new ListBoxAutomationPeer(navigation);
                    Assert.AreEqual("주요 탐색", peer.GetName());
                    Assert.AreEqual(AutomationControlType.List, peer.GetAutomationControlType());
                    Assert.IsTrue(peer.IsEnabled());
                    var items = peer.GetChildren();
                    Assert.IsNotNull(items);
                    var selected = items.Single(p => p.GetName() == "건강 요약");
                    Assert.IsTrue(((ISelectionItemProvider)selected.GetPattern(PatternInterface.SelectionItem)).IsSelected);
                    shell.SelectedNavigation = shell.Navigation[1];
                    window.UpdateLayout();
                    TimelineView view = Children<TimelineView>(window).Single();
                    ListBox list = (ListBox)view.FindName("RecordList");
                    list.SelectedIndex = 0;
                    window.UpdateLayout();
                    var listPeer = new ListBoxAutomationPeer(list);
                    var recordPeer = listPeer.GetChildren()?.First();
                    Assert.IsNotNull(recordPeer);
                    Assert.AreEqual(AutomationControlType.ListItem, recordPeer.GetAutomationControlType());
                    Assert.IsTrue(recordPeer.GetName().Contains("체성분"));
                    Assert.IsTrue(((ISelectionItemProvider)recordPeer.GetPattern(PatternInterface.SelectionItem)).IsSelected);
                    Assert.AreEqual(AutomationControlType.ComboBox, new ComboBoxAutomationPeer((ComboBox)view.FindName("TypeFilter")).GetAutomationControlType());
                    Assert.AreEqual("화면 내 기록 찾기", new TextBoxAutomationPeer((TextBox)view.FindName("SearchInput")).GetName());
                }
                finally { window.Close(); }
            }
        }

        [STATestMethod]
        public void TimelineEnterEscapeAndClose_ReturnFocusToSelectedItem()
        {
            using (ShellViewModel shell = ExplorationPreviewTests.CreateShell())
            {
                shell.SelectedNavigation = shell.Navigation[1];
                MainWindow window = new MainWindow(shell);
                try
                {
                    window.Show();
                    window.Activate();
                    window.UpdateLayout();
                    TimelineView view = Children<TimelineView>(window).Single();
                    ListBox list = (ListBox)view.FindName("RecordList");
                    list.SelectedIndex = 0;
                    window.UpdateLayout();
                    var item = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
                    item.Focus();
                    RaiseKey(list, Key.Enter);
                    Assert.IsTrue(((Button)view.FindName("CloseDetail")).IsKeyboardFocused);
                    RaiseKey((UIElement)view.FindName("DetailSurface"), Key.Escape);
                    Assert.IsTrue(item.IsKeyboardFocused);
                    Assert.IsTrue(item.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                    Button close = (Button)view.FindName("CloseDetail");
                    Assert.IsTrue(close.IsKeyboardFocused);
                    Assert.IsTrue(close.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)));
                    Assert.IsTrue(item.IsKeyboardFocused);
                    shell.Timeline.CloseDetailCommand.Execute(null);
                    Assert.IsFalse(shell.Timeline.DetailOpen);
                    Assert.AreSame(shell.Timeline.Records[0], list.SelectedItem);
                    shell.Timeline.Type = "증상";
                    window.UpdateLayout();
                    Assert.IsNull(list.SelectedItem);
                    Assert.IsNull(shell.Timeline.Selected);
                }
                finally { window.Close(); }
            }
        }

        [STATestMethod]
        public void SmallWorkAreaAndLongTitle_ReflowKeepBoundedListAndDetail()
        {
            using (ShellViewModel shell = ExplorationPreviewTests.CreateShell())
            {
                shell.SelectedNavigation = shell.Navigation[1];
                shell.Timeline.LongTitle = true;
                shell.Timeline.Selected = shell.Timeline.Records[0];
                MainWindow window = new MainWindow(shell);
                try
                {
                    window.Show();
                    window.Width = window.MinWidth;
                    window.Height = window.MinHeight;
                    window.UpdateLayout();
                    TimelineView view = Children<TimelineView>(window).Single();
                    var detail = (Border)view.FindName("DetailSurface");
                    var list = (ListBox)view.FindName("RecordList");
                    Assert.AreEqual(1, Grid.GetRow(detail));
                    Assert.IsGreaterThan(0d, list.ActualHeight);
                    TextBlock rowTitle = Children<TextBlock>(list).First(t => t.Text.Length > 50);
                    Assert.IsGreaterThan(30d, rowTitle.ActualHeight);
                    Assert.IsLessThanOrEqualTo(list.ActualWidth, rowTitle.ActualWidth);
                    Assert.IsGreaterThan(0d, detail.ActualHeight);
                    Assert.IsGreaterThanOrEqualTo(36d, Children<ScrollViewer>(detail).Single().ViewportHeight);
                    Assert.IsTrue(Children<TextBlock>(detail).Any(t => t.Text.Length > 50 && t.TextWrapping == TextWrapping.Wrap));
                    shell.Timeline.DetailOpen = false;
                    window.UpdateLayout();
                    Assert.AreEqual(Visibility.Collapsed, detail.Visibility);
                    Assert.IsGreaterThan(0d, list.ActualHeight);
                }
                finally { window.Close(); }
            }
        }

        [STATestMethod]
        public void TimelineArrowSelection_UpdatesDetailAndKeepsListFocus()
        {
            using (ShellViewModel shell = ExplorationPreviewTests.CreateShell())
            {
                shell.SelectedNavigation = shell.Navigation[1];
                MainWindow window = new MainWindow(shell);
                try
                {
                    window.Show();
                    window.Activate();
                    window.UpdateLayout();
                    TimelineView view = Children<TimelineView>(window).Single();
                    ListBox list = (ListBox)view.FindName("RecordList");
                    list.SelectedIndex = 0;
                    window.UpdateLayout();
                    var item = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
                    item.Focus();
                    item.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(item),
                        Environment.TickCount, Key.Down)
                    { RoutedEvent = Keyboard.KeyDownEvent });
                    Assert.AreEqual(1, list.SelectedIndex);
                    Assert.AreSame(shell.Timeline.Records[1], shell.Timeline.Selected);
                    Assert.IsTrue(list.IsKeyboardFocusWithin);
                    Assert.IsTrue(shell.Timeline.DetailOpen);
                    shell.Timeline.Search = "사용자";
                    window.UpdateLayout();
                    Assert.AreEqual("symptom-2", shell.Timeline.Selected?.Id);
                    Assert.IsNotNull(list.SelectedItem);
                }
                finally { window.Close(); }
            }
        }

        [STATestMethod]
        public void WindowSizing_SyntheticSmallWorkArea_ConstrainsOuterWindowAndMinimum()
        {
            using (ShellViewModel shell = ExplorationPreviewTests.CreateShell())
            {
                MainWindow window = new MainWindow(shell);
                try
                {
                    WindowSizing.Fit(window, new Size(960, 540));
                    Assert.IsLessThanOrEqualTo(936d, window.Width);
                    Assert.IsLessThanOrEqualTo(516d, window.Height);
                    Assert.IsLessThanOrEqualTo(window.Width, window.MinWidth);
                    Assert.IsLessThanOrEqualTo(window.Height, window.MinHeight);
                }
                finally { window.Close(); }
            }
        }
        internal static List<T> Children<T>(DependencyObject root) where T : DependencyObject
        {
            List<T> found = new List<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) found.Add(match);
                found.AddRange(Children<T>(child));
            }
            return found;
        }

        private static void RaiseKey(UIElement target, Key key)
        {
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target),
                Environment.TickCount, key)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        }
    }
}
