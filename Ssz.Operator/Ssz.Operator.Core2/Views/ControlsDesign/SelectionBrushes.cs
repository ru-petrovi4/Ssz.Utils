using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     How the lists of the editor paint a selected row. The item first selected is the one a
    ///     command works from, so it is shown apart from the rest of the selection.
    ///     <para>
    ///         The WPF lists did this with two multi-value converters over IsSelected and
    ///         IsFirstSelected; here each view model answers with a brush, and the brushes are these.
    ///     </para>
    /// </summary>
    public static class SelectionBrushes
    {
        #region public functions

        public static IBrush Background(bool isSelected, bool isFirstSelected)
        {
            if (isFirstSelected) return FirstSelectedBackground;
            if (isSelected) return SelectedBackground;
            return Brushes.Transparent;
        }

        public static IBrush Foreground(bool isSelected, bool isFirstSelected)
        {
            return isFirstSelected ? Brushes.White : Brushes.Black;
        }

        #endregion

        #region private fields

        private static readonly IBrush FirstSelectedBackground =
            new SolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF));

        private static readonly IBrush SelectedBackground =
            new SolidColorBrush(Color.FromRgb(0x87, 0xCE, 0xFA));

        #endregion
    }
}
