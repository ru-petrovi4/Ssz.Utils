using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.SelectImageFromLibrary
{
    public partial class SelectImageFromLibraryDialog : LocationMindfulWindow
    {
        #region private fields

        private CancellationTokenSource? _cancellationTokenSource;

        #endregion

        #region construction and destruction

        public SelectImageFromLibraryDialog()
            : base("SelectImageFromLibrary", 1000, 600)
        {
            InitializeComponent();

            DataContext = new SelectImageFromLibraryViewModel();

            Loaded +=
                (sender, args) =>
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    ((SelectImageFromLibraryViewModel) DataContext).GoLibraryAsync(
                        LibraryPathControl.LibraryDirectoryInfo,
                        _cancellationTokenSource.Token);
                };

            Closed += (sender, args) => StopGoLibrary();
        }

        #endregion

        #region public functions

        public FileInfo? SelectedImageFileInfo
        {
            get
            {
                var imageViewModel = ((SelectImageFromLibraryViewModel) DataContext).SelectedImage as ImageViewModel;
                if (imageViewModel is null) return null;
                return imageViewModel.FileInfo;
            }
        }

        #endregion

        #region private functions

        private void OnLibraryDirectoryInfoChanged(object? sender, EventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            ((SelectImageFromLibraryViewModel) DataContext).GoLibraryAsync(LibraryPathControl.LibraryDirectoryInfo,
                _cancellationTokenSource.Token);
        }

        private void ImageOnMouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                DialogResult = true;
                Close();
            }
        }

        private void BusyControlOnStopped(object? sender, EventArgs e)
        {
            StopGoLibrary();
        }

        private void OkButtonOnClick(object? sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButtonOnClick(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void StopGoLibrary()
        {
            if (_cancellationTokenSource is not null) _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
        }

        #endregion
    }
}