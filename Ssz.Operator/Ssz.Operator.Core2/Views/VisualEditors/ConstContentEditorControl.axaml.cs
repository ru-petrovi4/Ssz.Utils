using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Ssz.Operator.Core.VisualEditors.SelectImageFromLibrary;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     What a shape shows: a picture from a file or from the library, or XAML, with a preview of it
    ///     and a say in how it fills the shape.
    ///     <para>
    ///         Ported from the WPF editor. The button that wrote the content out as an EMF file is
    ///         gone: that format is drawn by Windows itself and has no counterpart elsewhere.
    ///     </para>
    /// </summary>
    public partial class ConstContentEditorControl : UserControl
    {
        #region construction and destruction

        public ConstContentEditorControl()
        {
            InitializeComponent();

            DataContext = new ConstContentEditorViewModel();

            ContentStretchComboBox.ItemsSource = Enum.GetValues<Stretch>();
        }

        #endregion

        #region public functions

        public Size? ContentOriginalSize => _contentOriginalSize;

        public string Xaml
        {
            get => ViewModel.Xaml;
            set => ViewModel.Xaml = value;
        }

        #endregion

        #region private functions

        private ConstContentEditorViewModel ViewModel => (ConstContentEditorViewModel) DataContext!;

        private void ClearContentButtonOnClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.Xaml = @"";
        }

        private async void SelectFileButtonOnClick(object? sender, RoutedEventArgs e)
        {
            IStorageFile? storageFile = await FileDialogHelper.OpenFileAsync(
                Properties.Resources.ContentEditorSelectFileButtonText, FileDialogHelper.AllFilesFileType);
            if (storageFile is null) return;

            var path = storageFile.TryGetLocalPath();
            if (String.IsNullOrEmpty(path)) return;

            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists) return;

            ViewModel.Xaml = XamlHelper.GetXamlWithAbsolutePaths(fileInfo,
                ViewModel.ContentStretchComboBoxSelectedItem, out _contentOriginalSize);
        }

        private async void SelectFileFromLibraryButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is not Window ownerWindow) return;

            var dialog = new SelectImageFromLibraryDialog();
            await dialog.ShowDialog(ownerWindow);
            if (!dialog.DialogResult) return;

            FileInfo? fileInfo = dialog.SelectedImageFileInfo;
            if (fileInfo is null || !fileInfo.Exists) return;

            ViewModel.Xaml = XamlHelper.GetXamlWithAbsolutePaths(fileInfo, Stretch.Fill,
                out _contentOriginalSize);
        }

        private async void SaveOriginalContentToFileButtonOnClick(object? sender, RoutedEventArgs e)
        {
            await XamlHelper.SaveToXamlOrImageFileAsync(ViewModel.Xaml);
        }

        private async void SaveAsPngFileButtonOnClick(object? sender, RoutedEventArgs e)
        {
            await XamlHelper.SaveAsPngFileAsync(ViewModel.Xaml);
        }

        #endregion

        #region private fields

        private Size? _contentOriginalSize;

        #endregion
    }
}
