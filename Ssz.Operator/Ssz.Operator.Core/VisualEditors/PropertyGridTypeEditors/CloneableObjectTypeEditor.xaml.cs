using System;
using System.Windows;
using System.Windows.Controls;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class CloneableObjectTypeEditor : UserControl, ITypeEditor
    {
        #region construction and destruction

        public CloneableObjectTypeEditor()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            DataContext = propertyItem;
            return this;
        }

        #endregion

        #region protected functions

        protected virtual void ButtonClick(object? sender, RoutedEventArgs e)
        {
            var propertyItem = DataContext as PropertyItem;
            if (propertyItem is null)
                return;

            var valueCloneable = propertyItem.Value as ICloneable;
            if (valueCloneable is null) return;

            var valueResult = CloneableObjectPropertiesDialog.ShowDialog(valueCloneable);
            if (valueResult is not null) propertyItem.Value = valueResult;
        }

        #endregion
    }
}