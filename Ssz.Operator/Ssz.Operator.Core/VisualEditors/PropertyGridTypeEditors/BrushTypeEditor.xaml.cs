using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class BrushTypeEditor : UserControl, ITypeEditor
    {
        #region construction and destruction

        public BrushTypeEditor()
        {
            InitializeComponent();

            MainButton.SetBinding(ContentProperty, new Binding
            {
                Path = new PropertyPath("Value"),
                Converter =
                    DsBrushToContentConverter
                        .Instance
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
            var dialog = new BrushEditorDialog
            {
                DsBrush = ((PropertyItem) DataContext).Value as DsBrushBase,
                Owner = Window.GetWindow(this)
            };

            dialog.MainControl.ParamNumBrushTabItem.Visibility = Visibility.Collapsed;

            if (dialog.ShowDialog() == true) ((PropertyItem) DataContext).Value = dialog.DsBrush;
        }

        #endregion
    }
}