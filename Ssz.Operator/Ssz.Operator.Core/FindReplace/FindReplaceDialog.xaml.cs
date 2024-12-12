using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.ControlsPlay.PanoramaPlay;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.FindReplace
{
    public partial class FindReplaceDialog : LocationMindfulWindow
    {
        #region construction and destruction

        protected FindReplaceDialog()
            : base("FindReplace", 1024)
        {
            InitializeComponent();

            DataContext = FindReplaceViewModel.Instance;
        }

        #endregion

        /*
        public void ShowAsFind(TextEditor target)
        {
            FindReplaceViewModel.Instance.CurrentEditor = target;
            ShowAsFind();
        }         

        public static void ShowAsReplace(object target)
        {
            FindReplaceViewModel.Instance.CurrentEditor = target;
            ShowAsReplace();
        }*/

        #region protected functions

        protected FindReplaceViewModel FindReplaceViewModel => (FindReplaceViewModel) DataContext;

        #endregion

        #region public functions

        public static void ShowAsPlayFind(Window? owner)
        {
            var viewModel = FindReplaceViewModel.Instance;
            viewModel.ShowDsShapePropertiesButtonVisibility = Visibility.Collapsed;
            viewModel.GoToDsPageButtonVisibility = Visibility.Visible;
            viewModel.FindPathButtonVisibility = Visibility.Collapsed;
            viewModel.AllowReplace = false;
            viewModel.OptionsExpanderVisibility = Visibility.Visible;
            viewModel.ShowSearchIn = false;
            viewModel.ShowSearchInProps = false;
            var dialog = ShowOrActivateDialog(owner);
            dialog.MainTab.SelectedIndex = 0;
            dialog.FindTextBox.Focus();
            dialog.FindTextBox.SelectAll();
            dialog.FindTextBox.IsEnabled = true;
        }


        public static void ShowAsPlayPanoramaFindPath(Window? owner)
        {
            var viewModel = FindReplaceViewModel.Instance;
            viewModel.ShowDsShapePropertiesButtonVisibility = Visibility.Collapsed;
            viewModel.GoToDsPageButtonVisibility =
                DsProject.Instance.Review ? Visibility.Visible : Visibility.Collapsed;
            viewModel.FindPathButtonVisibility = Visibility.Visible;
            viewModel.AllowReplace = false;
            viewModel.OptionsExpanderVisibility = Visibility.Visible;
            viewModel.ShowSearchIn = false;
            viewModel.ShowSearchInProps = false;
            var dialog = ShowOrActivateDialog(owner);
            dialog.MainTab.SelectedIndex = 0;
            dialog.FindTextBox.Focus();
            dialog.FindTextBox.SelectAll();
            dialog.FindTextBox.IsEnabled = true;
        }


        public static void ShowAsFind(Window owner)
        {
            var viewModel = FindReplaceViewModel.Instance;
            viewModel.ShowDsShapePropertiesButtonVisibility = Visibility.Visible;
            viewModel.GoToDsPageButtonVisibility = Visibility.Collapsed;
            viewModel.FindPathButtonVisibility = Visibility.Collapsed;
            viewModel.AllowReplace = true;
            viewModel.OptionsExpanderVisibility = Visibility.Visible;
            viewModel.ShowSearchIn = true;
            viewModel.ShowSearchInProps = true;
            var dialog = ShowOrActivateDialog(owner);
            dialog.MainTab.SelectedIndex = 0;
            dialog.FindTextBox.Focus();
            dialog.FindTextBox.SelectAll();
            dialog.FindTextBox.IsEnabled = true;
        }


        public static void ShowAsDebugFind(string queryString, Window owner)
        {
            var viewModel = FindReplaceViewModel.Instance;
            viewModel.ShowDsShapePropertiesButtonVisibility = Visibility.Visible;
            viewModel.GoToDsPageButtonVisibility = Visibility.Collapsed;
            viewModel.FindPathButtonVisibility = Visibility.Collapsed;
            if (queryString != viewModel.TextToFind) viewModel.SearchResultGroupsCollection.Clear();
            viewModel.AllowReplace = false;
            viewModel.OptionsExpanderVisibility = Visibility.Hidden;
            viewModel.ShowSearchIn = true;
            viewModel.ShowSearchInProps = false;
            var dialog = ShowOrActivateDialog(owner);
            dialog.MainTab.SelectedIndex = 0;
            dialog.FindReplaceViewModel.TextToFind = queryString;
            dialog.FindTextBox.IsEnabled = false;
        }


        public static void ShowAsReplace(Window owner)
        {
            var viewModel = FindReplaceViewModel.Instance;
            viewModel.ShowDsShapePropertiesButtonVisibility = Visibility.Visible;
            viewModel.GoToDsPageButtonVisibility = Visibility.Collapsed;
            viewModel.FindPathButtonVisibility = Visibility.Collapsed;
            viewModel.AllowReplace = true;
            viewModel.OptionsExpanderVisibility = Visibility.Visible;
            viewModel.ShowSearchIn = true;
            viewModel.ShowSearchInProps = true;
            var dialog = ShowOrActivateDialog(owner);
            dialog.MainTab.SelectedIndex = 1;
            dialog.Find2TextBox.Focus();
            dialog.Find2TextBox.SelectAll();
            dialog.FindTextBox.IsEnabled = true;
        }


        public static void CloseWindow()
        {
            if (_dialog is not null) _dialog.Close();
        }

        #endregion

        #region private functions

        private static FindReplaceDialog ShowOrActivateDialog(Window? owner)
        {
            if (_dialog is null)
            {
                _dialog = new FindReplaceDialog();
                _dialog.Closed += (sender, args) =>
                {
                    _dialog.StopSearch();
                    _dialog = null;
                };
                _dialog.Owner = owner;
                _dialog.Show();
            }
            else
            {
                _dialog.Activate();
            }

            return _dialog;
        }

        private async void FindButtonOnClickAsync(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FindReplaceViewModel.TextToFind))
            {
                MessageBoxHelper.ShowInfo(Properties.Resources.NoTextToFind);
                return;
            }

            using (DesignDsProjectViewModel.Instance.GetBusyCloser())
            using (FindReplaceViewModel.GetIsBusyCloser())
            {
                _cancellationTokenSource = new CancellationTokenSource();

                await FindReplaceViewModel.FindAsync(_cancellationTokenSource.Token);
            }
        }

        private async void ReplaceAllClickAsync(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FindReplaceViewModel.TextToFind))
            {
                MessageBoxHelper.ShowInfo(Properties.Resources.NoTextToFind);
                return;
            }

            using (DesignDsProjectViewModel.Instance.GetBusyCloser())
            using (FindReplaceViewModel.GetIsBusyCloser())
            {
                _cancellationTokenSource = new CancellationTokenSource();

                await FindReplaceViewModel.ReplaceAllAsync(_cancellationTokenSource.Token);
            }
        }

        private void WindowOnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void SearchResultsOnMouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var treeViewItem = sender as FrameworkElement;
                if (treeViewItem is not null)
                {
                    /*
                    var entityInfoViewModel = treeViewItem.DataContext as EntityInfoViewModel;
                    if (entityInfoViewModel is not null)
                    {
                        MainWindow.Instance.OpenDsPageDrawingAsync((DrawingInfo) entityInfoViewModel.EntityInfo);
                        return;
                    }*/
                    var searchResultViewModel = treeViewItem.DataContext as SearchResultViewModel;
                    if (searchResultViewModel is not null)
                    {
                        var owner = Owner;
                        if (owner is not null) owner.Activate();

                        if (FindReplaceViewModel.Instance.ShowDsShapePropertiesButtonVisibility == Visibility.Visible)
                        {
                            if (searchResultViewModel.RootDsShapeInfo is null) // Drawing Props
                                DesignDsProjectViewModel.Instance.ShowDrawingAndPropertiesAsync(searchResultViewModel
                                    .DrawingInfo.FileInfo);
                            else
                                DesignDsProjectViewModel.Instance.ShowDsShapeAndPropertiesAsync(
                                    searchResultViewModel.DrawingInfo.FileInfo,
                                    searchResultViewModel.RootDsShapeInfo);
                        }
                        else
                        {
                            if (FindReplaceViewModel.Instance.GoToDsPageButtonVisibility == Visibility.Visible)
                            {
                                var fileRelativePath =
                                    DsProject.Instance.GetFileRelativePath(searchResultViewModel.DrawingInfo.FileInfo
                                        .FullName);
                                if (fileRelativePath is not null)
                                    CommandsManager.NotifyCommand((owner as IPlayWindow)?.MainFrame,
                                        CommandsManager.JumpCommand,
                                        new JumpDsCommandOptions {FileRelativePath = fileRelativePath});
                            }
                        }
                    }
                }
            }
        }

        private void ExportResultsOnClick(object? sender, RoutedEventArgs e)
        {
            FindReplaceViewModel.ExportResultsToCsv();
        }

        private void CopyResultsOnClick(object? sender, RoutedEventArgs e)
        {
            FindReplaceViewModel.CopyResults();
        }

        private void BusyControlOnStopped(object? sender, EventArgs e)
        {
            StopSearch();
        }

        private void StopSearch()
        {
            if (_cancellationTokenSource is not null) _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
        }

        private void ShowDsShapePropertiesButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var selectedValue = TreeHelper.FindChild<TreeView>(this)?.SelectedValue;
            var searchResultViewModel = selectedValue as SearchResultViewModel;
            if (searchResultViewModel is not null)
            {
                if (searchResultViewModel.RootDsShapeInfo is null) // Drawing Props
                    DesignDsProjectViewModel.Instance.ShowDrawingAndPropertiesAsync(searchResultViewModel.DrawingInfo
                        .FileInfo);
                else
                    DesignDsProjectViewModel.Instance.ShowDsShapeAndPropertiesAsync(
                        searchResultViewModel.DrawingInfo.FileInfo,
                        searchResultViewModel.RootDsShapeInfo);
            }
        }

        private void FindPathButtonOnClick(object? sender, RoutedEventArgs e)
        {
            DrawingInfo? drawingInfo = null;

            var selectedValue = TreeHelper.FindChild<TreeView>(this)?.SelectedValue;
            var searchResultViewModel = selectedValue as SearchResultViewModel;
            if (searchResultViewModel is not null)
            {
                drawingInfo = searchResultViewModel.DrawingInfo;
            }
            else
            {
                var searchResultGroupViewModel = selectedValue as SearchResultGroupViewModel;
                if (searchResultGroupViewModel is not null)
                    drawingInfo = searchResultGroupViewModel.EntityInfo as DrawingInfo;
            }

            if (drawingInfo is not null)
            {
                var owner = Owner;
                if (owner is not null) owner.Activate();

                var fileRelativePath = DsProject.Instance.GetFileRelativePath(drawingInfo.FileInfo.FullName);
                if (fileRelativePath is not null)
                {
                    var panoramaAddon = DsProject.Instance.GetAddon<PanoramaAddon>();
                    Dispatcher.BeginInvoke(new Action(() =>
                        panoramaAddon.PanoPointsCollection.ShowPath(
                            Path.GetFileNameWithoutExtension(fileRelativePath))));
                }
            }
        }

        private void GoToDsPageButtonOnClick(object? sender, RoutedEventArgs e)
        {
            DrawingInfo? drawingInfo = null;

            var selectedValue = TreeHelper.FindChild<TreeView>(this)?.SelectedValue;
            var searchResultViewModel = selectedValue as SearchResultViewModel;
            if (searchResultViewModel is not null)
            {
                drawingInfo = searchResultViewModel.DrawingInfo;
            }
            else
            {
                var searchResultGroupViewModel = selectedValue as SearchResultGroupViewModel;
                if (searchResultGroupViewModel is not null)
                    drawingInfo = searchResultGroupViewModel.EntityInfo as DrawingInfo;
            }

            if (drawingInfo is not null)
            {
                var owner = Owner;
                if (owner is not null) owner.Activate();

                var fileRelativePath = DsProject.Instance.GetFileRelativePath(drawingInfo.FileInfo.FullName);
                if (fileRelativePath is not null)
                    CommandsManager.NotifyCommand((owner as IPlayWindow)?.MainFrame,
                        CommandsManager.JumpCommand, new JumpDsCommandOptions {FileRelativePath = fileRelativePath});
            }
        }

        #endregion

        #region private fields

        private static FindReplaceDialog? _dialog;
        private static CancellationTokenSource? _cancellationTokenSource;

        #endregion
    }
}