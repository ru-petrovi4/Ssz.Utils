using Avalonia.Controls;
using Avalonia.Media;

namespace Ssz.Operator.Core.VisualEditors.GradientBrushEditor
{
    /// <summary>
    ///     Building a gradient brush: its kind, its geometry, and the stops along it.
    /// </summary>
    public partial class GradientBrushEditorControl : UserControl
    {
        #region construction and destruction

        public GradientBrushEditorControl()
        {
            InitializeComponent();

            DataContext = new GradientBrushEditorViewModel();

            // A stop of a gradient is a colour and nothing else, so there is no constant to name here.
            SolidColorPicker.HideConstants();
        }

        #endregion

        #region public functions

        public IBrush? Brush
        {
            get => ((GradientBrushEditorViewModel) DataContext!).Brush;
            set => ((GradientBrushEditorViewModel) DataContext!).Brush = value;
        }

        #endregion
    }
}
