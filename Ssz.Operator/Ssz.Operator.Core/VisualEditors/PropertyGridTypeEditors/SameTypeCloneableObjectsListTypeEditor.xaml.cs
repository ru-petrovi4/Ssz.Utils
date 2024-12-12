using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class SameTypeCloneableObjectsListTypeEditor : UserControl, ITypeEditor
    {
        #region construction and destruction

        public SameTypeCloneableObjectsListTypeEditor()
        {
            InitializeComponent();

            MainButton.SetBinding(ContentProperty, new Binding
            {
                Path = new PropertyPath("Value")
            });
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

            var dialog = new SameTypeCloneableObjectsListEditorDialog
            {
                Owner = Window.GetWindow(this),
                Collection = propertyItem.Value
            };

            if (dialog.ShowDialog() == true) propertyItem.Value = dialog.Collection;
        }

        #endregion
    }
}