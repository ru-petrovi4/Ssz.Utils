using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Ssz.Operator.Core.VisualEditors.SelectImageFromLibrary;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class ConstContentEditorControl : UserControl
    {
        #region private fields

        private Size? _contentOriginalSize;

        #endregion

        #region construction and destruction

        public ConstContentEditorControl()
        {
            InitializeComponent();

            DataContext = new ConstContentEditorViewModel();
        }

        #endregion

        #region public functions

        public Size? ContentOriginalSize => _contentOriginalSize;

        public string Xaml
        {
            get => ((ConstContentEditorViewModel) DataContext).Xaml;
            set => ((ConstContentEditorViewModel) DataContext).Xaml = value;
        }

        #endregion

        #region private functions

        private void ClearContentButtonOnClick(object? sender, RoutedEventArgs e)
        {
            ((ConstContentEditorViewModel) DataContext).Xaml = "";
        }

        private void SelectFileButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = @"All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != true)
                return;
            var fileInfo = new FileInfo(dialog.FileName);
            if (!fileInfo.Exists) return;
            ((ConstContentEditorViewModel) DataContext).Xaml = XamlHelper.GetXamlWithAbsolutePaths(fileInfo,
                ((ConstContentEditorViewModel)DataContext).ContentStretchComboBoxSelectedItem, out _contentOriginalSize);
        }

        private void SelectFileFromLibraryButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var dlg = new SelectImageFromLibraryDialog
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() != true)
                return;
            var fileInfo = dlg.SelectedImageFileInfo;
            if (fileInfo is null || !fileInfo.Exists) return;
            ((ConstContentEditorViewModel) DataContext).Xaml = XamlHelper.GetXamlWithAbsolutePaths(fileInfo,
                Stretch.Fill, out _contentOriginalSize);
        }

        private void SaveOriginalContentToFileButtonOnClick(object? sender, RoutedEventArgs e)
        {
            XamlHelper.SaveToXamlOrImageFile(((ConstContentEditorViewModel) DataContext).Xaml);
        }

        private void SaveAsPngFileButtonOnClick(object? sender, RoutedEventArgs e)
        {
            XamlHelper.SaveAsPngFile(((ConstContentEditorViewModel) DataContext).Xaml);
        }

        private void SaveAsEmfFileButtonOnClick(object? sender, RoutedEventArgs e)
        {
            XamlHelper.SaveAsEmfFile(((ConstContentEditorViewModel) DataContext).Xaml);
        }

        #endregion
    }
}