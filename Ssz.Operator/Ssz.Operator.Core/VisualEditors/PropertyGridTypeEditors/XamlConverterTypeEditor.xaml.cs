using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ssz.Operator.Core.MultiValueConverters;
using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class XamlConverterTypeEditor : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region private fields

        private IValueDataBinding? _dataSourceInfo;

        #endregion

        #region construction and destruction

        public XamlConverterTypeEditor()
        {
            InitializeComponent();

            MainButton.SetBinding(ContentProperty, new Binding
            {
                Path = new PropertyPath("Value"),
                Converter =
                    ValueConverterToTextConverter
                        .Instance
            });

            MainButton.SetBinding(ForegroundProperty, new Binding
            {
                Path = new PropertyPath("Value"),
                Converter =
                    ValueConverterToBrushConverter
                        .Instance
            });
        }

        #endregion

        #region private functions

        private void ButtonClick(object? sender, RoutedEventArgs e)
        {
            /*
            if (_dataSourceInfo.DataBindingItemsCollection.Count == 0)
            {
                MessageBoxHelper.ShowInfo(Properties.Resources.MustDefineDataItemsMessage);
                return;
            }*/

            var originalValueConverter = ((PropertyItem) DataContext).Value as ValueConverterBase;

            var dialog = new XamlConverterDialog
            {
                XamlConverter = originalValueConverter,
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true) ((PropertyItem) DataContext).Value = dialog.XamlConverter;
        }

        #endregion

        #region public functions

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            _dataSourceInfo = propertyItem.Instance as IValueDataBinding;
            DataContext = propertyItem;
            return this;
        }

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            if (_dataSourceInfo is not null && _dataSourceInfo.DataBindingItemsCollection.Count == 0)
                _dataSourceInfo.Converter = null;
        }

        public void EndEditInPropertyGrid()
        {
            RefreshForPropertyGrid();
        }

        #endregion
    }
}