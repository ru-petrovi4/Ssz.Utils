using System.Windows;

using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public class DsUIElementPropertyTypeEditor<T> : ITypeEditor
        where T : DsUIElementPropertySupplier
    {
        #region public functions

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            var typeEditorControl = new DsUIElementPropertyTypeEditorControl(typeof(T));
            typeEditorControl.DataContext = propertyItem;
            return typeEditorControl;
        }

        #endregion
    }
}