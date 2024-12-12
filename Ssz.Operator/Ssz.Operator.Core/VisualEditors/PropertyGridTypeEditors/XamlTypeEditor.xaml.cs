using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class XamlTypeEditor : UserControl, ITypeEditor
    {
        #region construction and destruction

        public XamlTypeEditor()
        {
            InitializeComponent();

            MainButton.SetBinding(ContentProperty, new Binding
            {
                Path = new PropertyPath("Value"),
                Converter =
                    XamlToContentConverter
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
            var dialog = new ConstContentEditorDialog
            {
                Xaml = ((DsXaml) ((PropertyItem) DataContext).Value).Xaml,
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true) ((PropertyItem) DataContext).Value = new DsXaml {Xaml = dialog.Xaml};
        }

        #endregion
    }
}