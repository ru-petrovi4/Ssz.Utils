using System;
using System.IO;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.SelectImageFromLibrary
{
    /// <summary>
    ///     Picking a picture out of a library of pages and shapes.
    /// </summary>
    public partial class SelectImageFromLibraryDialog : LocationMindfulWindow
    {
        #region construction and destruction

        public SelectImageFromLibraryDialog()
            : base(@"SelectImageFromLibrary", 1000, 600)
        {
            InitializeComponent();

            DataContext = new SelectImageFromLibraryViewModel();

            Loaded += (sender, args) =>
            {
                _cancellationTokenSource = new CancellationTokenSource();
                ((SelectImageFromLibraryViewModel) DataContext!).GoLibraryAsync(
                    LibraryPathControl.LibraryDirectoryInfo, _cancellationTokenSource.Token);
            };

            Closed += (sender, args) => StopGoLibrary();
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Whether the author accepted the picture they picked.
        /// </summary>
        public bool DialogResult { get; private set; }

        public FileInfo? SelectedImageFileInfo =>
            (((SelectImageFromLibraryViewModel) DataContext!).SelectedImage as ImageViewModel)?.FileInfo;

        #endregion

        #region private functions

        private void OnLibraryDirectoryInfoChanged(object? sender, EventArgs e)
        {
            StopGoLibrary();

            _cancellationTokenSource = new CancellationTokenSource();
            ((SelectImageFromLibraryViewModel) DataContext!).GoLibraryAsync(
                LibraryPathControl.LibraryDirectoryInfo, _cancellationTokenSource.Token);
        }

        private void ImageOnDoubleTapped(object? sender, TappedEventArgs e)
        {
            DialogResult = true;
            Close();
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
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        #endregion

        #region private fields

        private CancellationTokenSource? _cancellationTokenSource;

        #endregion
    }
}
