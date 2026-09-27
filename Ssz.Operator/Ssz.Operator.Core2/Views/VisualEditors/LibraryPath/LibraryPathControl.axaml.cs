using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.VisualEditors.LibraryPath
{
    /// <summary>
    ///     Says which library of pages and shapes the dialog above it is looking at.
    ///     <para>
    ///         Ported from the WPF editor. Which library that is outlives the dialog, as it did there,
    ///         so that opening the dialog again lands where the author left off.
    ///     </para>
    /// </summary>
    public partial class LibraryPathControl : UserControl
    {
        #region construction and destruction

        public LibraryPathControl()
        {
            InitializeComponent();

            var libraryPathViewModel = new LibraryPathViewModel();
            if (_staticLibraryDirectoryInfo is null)
                _staticLibraryDirectoryInfo =
                    libraryPathViewModel.GetLibraryDirectoryInfo(LibraryPathViewModel.LocalLibraryString);
            else
                libraryPathViewModel.GetLibraryDirectoryInfo(_staticLibraryDirectoryInfo.FullName);

            DataContext = libraryPathViewModel;

            Loaded += (sender, args) =>
                GoLibraryTextBox.Text = _staticLibraryDirectoryInfo?.FullName
                                        ?? LibraryPathViewModel.LocalLibraryString;

            Unloaded += (sender, args) => libraryPathViewModel.Dispose();
        }

        #endregion

        #region public functions

        public DirectoryInfo? LibraryDirectoryInfo => _staticLibraryDirectoryInfo;

        public event EventHandler? LibraryDirectoryInfoChanged;

        #endregion

        #region private functions

        private void GoLibraryComboBoxOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (GoLibraryComboBox.SelectedItem is RecentFile recentFile)
            {
                GoLibraryTextBox.Text = recentFile.FullFileName;
                GoLibraryButtonOnClick(this, null);
            }

            GoLibraryComboBox.SelectedItem = null;
        }

        private async void BrowseButtonOnClick(object? sender, RoutedEventArgs e)
        {
            IStorageFolder? storageFolder =
                await FolderDialogHelper.OpenFolderAsync(Properties.Resources.LibPathLabel);
            if (storageFolder is null) return;

            var path = storageFolder.TryGetLocalPath();
            if (String.IsNullOrEmpty(path)) return;

            GoLibraryTextBox.Text = path;

            GoLibraryButtonOnClick(this, null);
        }

        private void GoLibraryButtonOnClick(object? sender, RoutedEventArgs? e)
        {
            DirectoryInfo? libraryDirectoryInfo =
                ((LibraryPathViewModel) DataContext!).GetLibraryDirectoryInfo(GoLibraryTextBox.Text);

            _staticLibraryDirectoryInfo = libraryDirectoryInfo;

            LibraryDirectoryInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region private fields

        private static DirectoryInfo? _staticLibraryDirectoryInfo;

        #endregion
    }
}
