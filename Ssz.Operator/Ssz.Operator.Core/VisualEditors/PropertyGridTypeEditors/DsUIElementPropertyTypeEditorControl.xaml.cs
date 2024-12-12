using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class DsUIElementPropertyTypeEditorControl : UserControl
    {
        #region private fields

        private readonly Type _propertyInfoSupplierType;

        #endregion

        #region construction and destruction

        public DsUIElementPropertyTypeEditorControl(Type propertyInfoSupplierType)
        {
            InitializeComponent();

            _propertyInfoSupplierType = propertyInfoSupplierType;

            MainButton.SetBinding(ContentProperty, new Binding
            {
                Path = new PropertyPath("Value")
            });
        }

        #endregion

        #region protected functions

        protected virtual void ButtonClick(object? sender, RoutedEventArgs e)
        {
            var propertyInfo = (DsUIElementProperty)
                ((DsUIElementProperty) ((PropertyItem) DataContext).Value).Clone();
            var dialog = new DsUIElementPropertyEditorDialog(_propertyInfoSupplierType)
            {
                StyleInfo = propertyInfo,
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true) ((PropertyItem) DataContext).Value = dialog.StyleInfo;
        }

        #endregion
    }
}