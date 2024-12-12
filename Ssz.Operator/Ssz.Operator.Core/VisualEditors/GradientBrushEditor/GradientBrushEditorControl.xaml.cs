using System.Windows.Controls;
using System.Windows.Media;

namespace Ssz.Operator.Core.VisualEditors.GradientBrushEditor
{
    public partial class GradientBrushEditorControl : UserControl
    {
        #region construction and destruction

        public GradientBrushEditorControl()
        {
            InitializeComponent();

            DataContext = new GradientBrushEditorViewModel();

            SolidColorPicker.HideConstants();
        }

        #endregion

        #region public functions

        public Brush? Brush
        {
            get => ((GradientBrushEditorViewModel) DataContext).Brush;
            set => ((GradientBrushEditorViewModel) DataContext).Brush = value;
        }

        #endregion
    }
}