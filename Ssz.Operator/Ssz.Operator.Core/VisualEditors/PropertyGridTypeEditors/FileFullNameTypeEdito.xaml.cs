using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class FileFullNameTypeEditor : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region construction and destruction

        public FileFullNameTypeEditor()
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
            var dlg = new OpenFileDialog
            {
                Filter = @"All files (*.*)|*.*",
                InitialDirectory = DsProject.Instance.DsProjectPath,
                FileName = ((PropertyItem) DataContext).Value as string,
                CheckFileExists = false,
                CheckPathExists = false
            };

            if (dlg.ShowDialog() != true) return;

            var fileInfo = new FileInfo(dlg.FileName ?? "");

            try
            {
                ((PropertyItem) DataContext).Value = fileInfo.FullName;
            }
            catch (Exception)
            {
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