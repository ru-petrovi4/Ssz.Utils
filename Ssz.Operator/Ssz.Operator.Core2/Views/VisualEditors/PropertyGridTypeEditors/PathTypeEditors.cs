using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform.Storage;
using Ssz.Utils;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    /// <summary>
    ///     A path typed into the button itself, with the button offering to pick it instead.
    ///     <para>
    ///         Ported from the WPF editors of file and folder names, which were this same control with
    ///         a different picker behind the button.
    ///     </para>
    /// </summary>
    public abstract class PathTypeEditorBase : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region construction and destruction

        protected PathTypeEditorBase()
        {
            MainButton = new EditorButton();
            MainButton.Click += async (sender, e) => await PickAsync();

            _textBox = new TextBox
            {
                BorderThickness = new Thickness(0),
                MinWidth = 100
            };
            MainButton.Content = _textBox;

            Content = MainButton;
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

        #region protected functions

        protected EditorButton MainButton { get; }

        protected PropertyItemViewModel PropertyItem => (PropertyItemViewModel) DataContext!;

        protected abstract Task PickAsync();

        #endregion

        #region private fields

        private readonly TextBox _textBox;

        #endregion
    }

    /// <summary>
    ///     The complex shape a shape stands for, named by the drawing it comes from.
    /// </summary>
    public class ComplexDsShapeNameTypeEditor : PathTypeEditorBase
    {
        #region protected functions

        protected override async Task PickAsync()
        {
            DirectoryInfo? dsShapesDirectoryInfo = DsProject.Instance.DsShapesDirectoryInfo;
            if (dsShapesDirectoryInfo is null) return;

            IReadOnlyList<IStorageFile> files = await FileDialogHelper.OpenFilesAsync(
                PropertyItem.DisplayName, dsShapesDirectoryInfo.FullName,
                new FilePickerFileType(@"Controls")
                {
                    Patterns = new[] { @"*" + DsProject.DsShapeFileExtension }
                });
            if (files.Count == 0) return;

            var path = files[0].TryGetLocalPath();
            if (String.IsNullOrEmpty(path)) return;

            var fileInfo = new FileInfo(path);
            if (!FileSystemHelper.Compare(fileInfo.Directory?.FullName, dsShapesDirectoryInfo.FullName))
            {
                MessageBoxHelper.ShowError(Res.FileMustBeInDsShapesDir);
                return;
            }

            PropertyItem.Value = Path.GetFileNameWithoutExtension(fileInfo.Name);
        }

        #endregion
    }

    /// <summary>
    ///     A folder, named in full.
    /// </summary>
    public class DirectoryNameTypeEditor : PathTypeEditorBase
    {
        #region protected functions

        protected override async Task PickAsync()
        {
            IStorageFolder? storageFolder = await FolderDialogHelper.OpenFolderAsync(PropertyItem.DisplayName);
            if (storageFolder is null) return;

            var path = storageFolder.TryGetLocalPath();
            if (String.IsNullOrEmpty(path)) return;

            PropertyItem.Value = path;
        }

        #endregion
    }

    /// <summary>
    ///     A file, named in full. Unlike a file of the project, this one may be anywhere.
    /// </summary>
    public class FileFullNameTypeEditor : PathTypeEditorBase
    {
        #region protected functions

        protected override async Task PickAsync()
        {
            IReadOnlyList<IStorageFile> files = await FileDialogHelper.OpenFilesAsync(
                PropertyItem.DisplayName, DsProject.Instance.DsProjectPath,
                FileDialogHelper.AllFilesFileType);
            if (files.Count == 0) return;

            var path = files[0].TryGetLocalPath();
            if (String.IsNullOrEmpty(path)) return;

            PropertyItem.Value = new FileInfo(path).FullName;
        }

        #endregion
    }
}
