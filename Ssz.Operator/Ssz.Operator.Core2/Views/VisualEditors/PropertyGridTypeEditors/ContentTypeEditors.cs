using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Platform.Storage;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.SelectImageFromLibrary;
using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.VisualEditors.Windows;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    /// <summary>
    ///     The font a shape writes its text in.
    /// </summary>
    public class DsFontTypeEditor : DialogTypeEditorBase
    {
        #region protected functions

        protected override async Task OnButtonClickAsync()
        {
            var dialog = new DsFontEditorDialog
            {
                DsFont = PropertyItem.Value as DsFont
            };

            if (await ShowDialogAsync(dialog)) PropertyItem.Value = dialog.DsFont;
        }

        #endregion
    }

    /// <summary>
    ///     The content a shape shows: a picture or XAML.
    /// </summary>
    public class XamlTypeEditor : DialogTypeEditorBase
    {
        #region protected functions

        protected override IValueConverter ValueConverter => XamlToContentConverter.Instance;

        protected override async Task OnButtonClickAsync()
        {
            var dialog = new ConstContentEditorDialog
            {
                Xaml = (PropertyItem.Value as DsXaml)?.Xaml ?? @""
            };

            if (await ShowDialogAsync(dialog)) PropertyItem.Value = new DsXaml { Xaml = dialog.Xaml };
        }

        #endregion
    }

    /// <summary>
    ///     A file of the project, named by where it sits inside it.
    ///     <para>
    ///         The name is typed in the button itself, as it was in WPF, and the button picks the file
    ///         from disc. A file outside the project cannot be named, because the project would not
    ///         find it again.
    ///     </para>
    /// </summary>
    public class FileNameTypeEditor : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region construction and destruction

        public FileNameTypeEditor()
        {
            _mainButton = new EditorButton();
            _mainButton.Click += async (sender, e) => await PickFileAsync();

            _textBox = new TextBox
            {
                BorderThickness = new Avalonia.Thickness(0),
                MinWidth = 100
            };
            _mainButton.Content = _textBox;

            Content = _mainButton;
        }

        #endregion

        #region public functions

        public Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataContext = propertyItem;

            _textBox.IsReadOnly = propertyItem.IsReadOnly;
            _textBox.Bind(TextBox.TextProperty, new Binding(nameof(PropertyItemViewModel.ValueAsString))
            {
                Source = propertyItem,
                Mode = BindingMode.TwoWay
            });

            return this;
        }

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
        }

        public void EndEditInPropertyGrid()
        {
        }

        #endregion

        #region private functions

        private async Task PickFileAsync()
        {
            DirectoryInfo? dsPagesDirectoryInfo = DsProject.Instance.DsPagesDirectoryInfo;
            if (dsPagesDirectoryInfo is null) return;

            IReadOnlyList<IStorageFile> files = await FileDialogHelper.OpenFilesAsync(
                Properties.Resources.ContentEditorSelectFileButtonText, dsPagesDirectoryInfo.FullName,
                FileDialogHelper.AllFilesFileType);
            if (files.Count == 0) return;

            var path = files[0].TryGetLocalPath();
            if (String.IsNullOrEmpty(path)) return;

            var fileRelativePath = DsProject.Instance.GetFileRelativePath(path);
            if (String.IsNullOrWhiteSpace(fileRelativePath))
            {
                MessageBoxHelper.ShowError(Properties.Resources.FileMustBeInDsProjectDir);
                return;
            }

            ((PropertyItemViewModel) DataContext!).Value = fileRelativePath;
        }

        #endregion

        #region private fields

        private readonly EditorButton _mainButton;
        private readonly TextBox _textBox;

        #endregion
    }
}
