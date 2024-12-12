using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Ssz.Operator.Core.VisualEditors.ValueConverters;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection
{
    public partial class ColorCellControl : UserControl
    {
        #region public functions

        public static readonly DependencyProperty DsBrushProperty = DependencyProperty.Register(
            "DsBrush",
            typeof(DsBrushBase),
            typeof(ColorCellControl));

        #endregion

        #region construction and destruction

        public ColorCellControl()
        {
            InitializeComponent();

            SetBinding(DsBrushProperty, new Binding
            {
                Path = new PropertyPath("Value"),
                Converter =
                    StringToSolidDsBrushConverter
                        .Instance
            });

            SetBinding(ContentProperty, new Binding
            {
                Path = new PropertyPath("DsBrush"),
                Source = this,
                Converter =
                    DsBrushToContentConverter
                        .Instance
            });
        }

        #endregion
    }
}