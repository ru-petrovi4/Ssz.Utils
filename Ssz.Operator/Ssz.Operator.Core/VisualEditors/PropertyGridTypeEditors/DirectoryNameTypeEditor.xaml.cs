using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ssz.WindowsAPICodePack.Dialogs;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class DirectoryNameTypeEditor : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region construction and destruction

        public DirectoryNameTypeEditor()
        {
            InitializeComponent();

            var textBox = new TextBox
            {
                BorderThickness = new Thickness(0),
                MinWidth = 600,
                IsReadOnly = false
            };

            MainButton.Content = textBox;

            textBox.SetBinding(TextBox.TextProperty, new Binding
            {
                Path = new PropertyPath("Value")
            });
        }

        #endregion

        #region protected functions

        protected virtual void ButtonClick(object? sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.InitialDirectory = DsProject.Instance.DsProjectPath;

                if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

                try
                {
                    ((PropertyItem) DataContext).Value = dialog.FileName;
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion

        #region public functions

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            DataContext = propertyItem;
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
    }
}