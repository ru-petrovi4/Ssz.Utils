using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Ssz.WindowsAPICodePack.Dialogs;

namespace Ssz.Operator.Core.VisualEditors.LibraryPath
{
    public partial class LibraryPathControl : UserControl
    {
        #region private fields

        private static DirectoryInfo? _staticLibraryDirectoryInfo;

        #endregion

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

            Loaded += (sender, args) => { GoLibraryComboBox.SelectedIndex = 0; };

            Unloaded += (sender, args) => libraryPathViewModel.Dispose();
        }

        #endregion

        #region public functions

        public DirectoryInfo? LibraryDirectoryInfo => _staticLibraryDirectoryInfo;

        public event EventHandler? LibraryDirectoryInfoChanged;

        #endregion

        #region private functions

        private void BrowseButtonOnClick(object? sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                var result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Ok)
                {
                    GoLibraryComboBox.Text = dialog.FileName;

                    GoLibraryButtonOnClick(this, null);
                }
            }
        }

        private void GoLibraryButtonOnClick(object? sender, RoutedEventArgs? e)
        {
            var libraryDirectoryInfo =
                ((LibraryPathViewModel) DataContext).GetLibraryDirectoryInfo(GoLibraryComboBox.Text);
            if (libraryDirectoryInfo is not null) GoLibraryComboBox.SelectedIndex = 0;

            _staticLibraryDirectoryInfo = libraryDirectoryInfo;

            if (LibraryDirectoryInfoChanged is not null) LibraryDirectoryInfoChanged(this, EventArgs.Empty);
        }

        #endregion
    }
}