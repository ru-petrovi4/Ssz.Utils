using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using Ssz.Utils;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class ComplexDsShapeNameTypeEditor : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region construction and destruction

        public ComplexDsShapeNameTypeEditor()
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
            var dsShapesDirectoryInfo = DsProject.Instance.DsShapesDirectoryInfo;
            if (dsShapesDirectoryInfo is null) return;

            var dlg = new OpenFileDialog
            {
                Filter = @"Controls (*" + DsProject.DsShapeFileExtension + ")|*" + DsProject.DsShapeFileExtension,
                InitialDirectory = dsShapesDirectoryInfo.FullName,
                FileName = ((PropertyItem) DataContext).Value as string ?? "",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dlg.ShowDialog() != true) return;

            var fileInfo = new FileInfo(dlg.FileName);

            if (!FileSystemHelper.Compare(fileInfo.Directory?.FullName, dsShapesDirectoryInfo?.FullName))
            {
                MessageBoxHelper.ShowError(Properties.Resources.FileMustBeInDsShapesDir);
                return;
            }

            try
            {
                ((PropertyItem) DataContext).Value = Path.GetFileNameWithoutExtension(fileInfo.Name);
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