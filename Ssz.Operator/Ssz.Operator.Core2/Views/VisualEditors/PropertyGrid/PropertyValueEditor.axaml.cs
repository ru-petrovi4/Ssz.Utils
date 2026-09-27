using Avalonia.Controls;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     The right-hand half of one row of the property grid: whatever the property is edited with.
    ///     <para>
    ///         It is a control of its own because a row shows it in two places - on its own, and in the
    ///         header of the expander when the property opens into properties of its own.
    ///     </para>
    /// </summary>
    public partial class PropertyValueEditor : UserControl
    {
        #region construction and destruction

        public PropertyValueEditor()
        {
            InitializeComponent();
        }

        #endregion
    }
}
