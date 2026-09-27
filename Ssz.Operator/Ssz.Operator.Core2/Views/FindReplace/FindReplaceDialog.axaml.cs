using System;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.Drawings;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.FindReplace
{
    /// <summary>
    ///     Finding text across the drawings of a project, replacing it, and the four searches the Debug
    ///     tab asks for.
    ///     <para>
    ///         Ported from the WPF editor. There is one of these windows at a time, as before; asking
    ///         for it again brings the one that is open to the front, set up for what was asked.
    ///     </para>
    /// </summary>
    public partial class FindReplaceDialog : LocationMindfulWindow
    {
        #region construction and destruction

        public FindReplaceDialog()
            : base(@"FindReplace", 1024)
        {
            InitializeComponent();

            DataContext = FindReplaceViewModel.Instance;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     The scopes the drop-downs offer, in the order the WPF dialog listed them.
        /// </summary>
        public static SearchScope[] SearchScopes { get; } =
        {
            SearchScope.CurrentDrawing,
            SearchScope.AllOpenedDrawings,
            SearchScope.AllDsPageDrawings,
            SearchScope.AllDsShapeDrawings
        };

        public static SearchScopeProps[] SearchScopesProps { get; } =
        {
            SearchScopeProps.ConstantsOnly,
            SearchScopeProps.AllProperties
        };

        public static void ShowAsPlayFind(Window? owner)
        {
            FindReplaceViewModel viewModel = FindReplaceViewModel.Instance;
            viewModel.ShowDsShapePropertiesButtonIsVisible = false;
            viewModel.GoToDsPageButtonIsVisible = true;
            viewModel.FindPathButtonIsVisible = false;
            viewModel.AllowReplace = false;
            viewModel.OptionsExpanderIsVisible = true;
            viewModel.ShowSearchIn = false;
            viewModel.ShowSearchInProps = false;

            FindReplaceDialog dialog = ShowOrActivateDialog(owner);
            dialog.MainTab.SelectedIndex = 0;
            dialog.FindTextBox.IsEnabled = true;
            dialog.FindTextBox.Focus();
            dialog.FindTextBox.SelectAll();
        }

        public static void ShowAsPlayPanoramaFindPath(Window? owner)
        {
            FindReplaceViewModel viewModel = FindReplaceViewModel.Instance;
            viewModel.ShowDsShapePropertiesButtonIsVisible = false;
            viewModel.GoToDsPageButtonIsVisible = DsProject.Instance.Review;
            viewModel.FindPathButtonIsVisible = true;
            viewModel.AllowReplace = false;
            viewModel.OptionsExpanderIsVisible = true;
            viewModel.ShowSearchIn = false;
            viewModel.ShowSearchInProps = false;

            FindReplaceDialog dialog = ShowOrActivateDialog(owner);
            dialog.MainTab.SelectedIndex = 0;
            dialog.FindTextBox.IsEnabled = true;
            dialog.FindTextBox.Focus();
            dialog.FindTextBox.SelectAll();
        }

        public static void ShowAsFind(Window? owner)
        {
            FindReplaceViewModel viewModel = FindReplaceViewModel.Instance;
            viewModel.ShowDsShapePropertiesButtonIsVisible = true;
            viewModel.GoToDsPageButtonIsVisible = false;
            viewModel.FindPathButtonIsVisible = false;
            viewModel.AllowReplace = true;
            viewModel.OptionsExpanderIsVisible = true;
            viewModel.ShowSearchIn = true;
            viewModel.ShowSearchInProps = true;

            FindReplaceDialog dialog = ShowOrActivateDialog(owner);
            dialog.MainTab.SelectedIndex = 0;
            dialog.FindTextBox.IsEnabled = true;
            dialog.FindTextBox.Focus();
            dialog.FindTextBox.SelectAll();
        }

        /// <summary>
        ///     One of the four searches of the Debug tab: the text to find is a query the search knows
        ///     by name, so the author does not type it.
        /// </summary>
        public static void ShowAsDebugFind(string queryString, Window? owner)
        {
            FindReplaceViewModel viewModel = FindReplaceViewModel.Instance;
            viewModel.ShowDsShapePropertiesButtonIsVisible = true;
            viewModel.GoToDsPageButtonIsVisible = false;
            viewModel.FindPathButtonIsVisible = false;

            if (queryString != viewModel.TextToFind) viewModel.SearchResultGroupsCollection.Clear();

            viewModel.AllowReplace = false;
            viewModel.OptionsExpanderIsVisible = false;
            viewModel.ShowSearchIn = true;
            viewModel.ShowSearchInProps = false;

            FindReplaceDialog dialog = ShowOrActivateDialog(owner);
            dialog.MainTab.SelectedIndex = 0;
            viewModel.TextToFind = queryString;
            dialog.FindTextBox.IsEnabled = false;
        }

        public static void ShowAsReplace(Window? owner)
        {
            FindReplaceViewModel viewModel = FindReplaceViewModel.Instance;
            viewModel.ShowDsShapePropertiesButtonIsVisible = true;
            viewModel.GoToDsPageButtonIsVisible = false;
            viewModel.FindPathButtonIsVisible = false;
            viewModel.AllowReplace = true;
            viewModel.OptionsExpanderIsVisible = true;
            viewModel.ShowSearchIn = true;
            viewModel.ShowSearchInProps = true;

            FindReplaceDialog dialog = ShowOrActivateDialog(owner);
            dialog.MainTab.SelectedIndex = 1;
            dialog.FindTextBox.IsEnabled = true;
            dialog.Find2TextBox.Focus();
            dialog.Find2TextBox.SelectAll();
        }

        public static void CloseWindow()
        {
            _dialog?.Close();
        }

        #endregion

        #region private functions

        private FindReplaceViewModel FindReplaceViewModel => (FindReplaceViewModel) DataContext!;

        private static FindReplaceDialog ShowOrActivateDialog(Window? owner)
        {
            if (_dialog is null)
            {
                _dialog = new FindReplaceDialog();
                _dialog.Closed += (sender, args) =>
                {
                    _dialog?.StopSearch();
                    _dialog = null;
                };
                _dialog._owner = owner;
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
            if (String.IsNullOrEmpty(FindReplaceViewModel.TextToFind))
            {
                MessageBoxHelper.ShowInfo(Res.NoTextToFind);
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
            if (String.IsNullOrEmpty(FindReplaceViewModel.TextToFind))
            {
                MessageBoxHelper.ShowInfo(Res.NoTextToFind);
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
            if (e.Key == Key.Escape) Close();
        }

        private void SearchResultOnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is not Control control ||
                control.DataContext is not SearchResultViewModel searchResultViewModel) return;

            GoToSearchResult(searchResultViewModel);

            e.Handled = true;
        }

        private void GoToSearchResult(SearchResultViewModel searchResultViewModel)
        {
            _owner?.Activate();

            if (FindReplaceViewModel.Instance.ShowDsShapePropertiesButtonIsVisible)
            {
                if (searchResultViewModel.RootDsShapeInfo is null) // Drawing Props
                    DesignDsProjectViewModel.Instance.ShowDrawingAndPropertiesAsync(
                        new FileInfo(searchResultViewModel.DrawingInfo.FileFullName));
                else
                    DesignDsProjectViewModel.Instance.ShowDsShapeAndPropertiesAsync(
                        new FileInfo(searchResultViewModel.DrawingInfo.FileFullName),
                        searchResultViewModel.RootDsShapeInfo);

                return;
            }

            if (FindReplaceViewModel.Instance.GoToDsPageButtonIsVisible)
                JumpToDsPage(searchResultViewModel.DrawingInfo);
        }

        private async void ExportResultsOnClick(object? sender, RoutedEventArgs e)
        {
            await FindReplaceViewModel.ExportResultsToCsvAsync();
        }

        private async void CopyResultsOnClick(object? sender, RoutedEventArgs e)
        {
            await FindReplaceViewModel.CopyResultsAsync();
        }

        private void BusyControlOnStopped(object? sender, EventArgs e)
        {
            StopSearch();
        }

        private void StopSearch()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        private void ShowDsShapePropertiesButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (SearchResultsTreeView.SelectedItem is not SearchResultViewModel searchResultViewModel) return;

            if (searchResultViewModel.RootDsShapeInfo is null) // Drawing Props
                DesignDsProjectViewModel.Instance.ShowDrawingAndPropertiesAsync(
                    new FileInfo(searchResultViewModel.DrawingInfo.FileFullName));
            else
                DesignDsProjectViewModel.Instance.ShowDsShapeAndPropertiesAsync(
                    new FileInfo(searchResultViewModel.DrawingInfo.FileFullName),
                    searchResultViewModel.RootDsShapeInfo);
        }

        /// <summary>
        ///     Shows on the panorama the way to the page a result belongs to.
        ///     <para>
        ///         The drawing of that way is part of the panorama of the play runtime, which is not
        ///         ported yet; the button this answers is only shown there, never in the editor. What
        ///         is left here is the page the way would lead to.
        ///     </para>
        /// </summary>
        private void FindPathButtonOnClick(object? sender, RoutedEventArgs e)
        {
            DrawingInfo? drawingInfo = SelectedDrawingInfo;
            if (drawingInfo is null) return;

            _owner?.Activate();

            var fileRelativePath = DsProject.Instance.GetFileRelativePath(drawingInfo.FileFullName);
            if (String.IsNullOrEmpty(fileRelativePath)) return;

            FindPathRequested?.Invoke(Path.GetFileNameWithoutExtension(fileRelativePath));
        }

        /// <summary>
        ///     Asked when the author wants the way to a page shown. The panorama of the play runtime
        ///     answers it once it is ported.
        /// </summary>
        public static event Action<string>? FindPathRequested;

        private void GoToDsPageButtonOnClick(object? sender, RoutedEventArgs e)
        {
            DrawingInfo? drawingInfo = SelectedDrawingInfo;
            if (drawingInfo is null) return;

            _owner?.Activate();

            JumpToDsPage(drawingInfo);
        }

        private void JumpToDsPage(DrawingInfo drawingInfo)
        {
            var fileRelativePath = DsProject.Instance.GetFileRelativePath(drawingInfo.FileFullName);
            if (String.IsNullOrEmpty(fileRelativePath)) return;

            CommandsManager.NotifyCommand((_owner as IPlayWindow)?.MainFrame,
                CommandsManager.JumpCommand,
                new JumpDsCommandOptions { FileRelativePath = fileRelativePath });
        }

        private DrawingInfo? SelectedDrawingInfo
        {
            get
            {
                switch (SearchResultsTreeView.SelectedItem)
                {
                    case SearchResultViewModel searchResultViewModel:
                        return searchResultViewModel.DrawingInfo;
                    case SearchResultGroupViewModel searchResultGroupViewModel:
                        return searchResultGroupViewModel.EntityInfo as DrawingInfo;
                    default:
                        return null;
                }
            }
        }

        #endregion

        #region private fields

        private static FindReplaceDialog? _dialog;
        private static CancellationTokenSource? _cancellationTokenSource;

        private Window? _owner;

        #endregion
    }
}
