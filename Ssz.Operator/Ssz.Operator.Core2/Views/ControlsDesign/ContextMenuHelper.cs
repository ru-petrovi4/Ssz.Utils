using Avalonia;
using Avalonia.Controls;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     Gives a control of the drawing surface the menu it is right clicked with.
    ///     <para>
    ///         The menus belong to the editor, which this project does not know of, so they are asked
    ///         of the application resources by name. The WPF editor reached them the same way, through
    ///         a DynamicResource of the merged dictionary.
    ///     </para>
    /// </summary>
    public static class ContextMenuHelper
    {
        #region public functions

        public const string DesignDsShapeContextMenuKey = @"DesignDsShapeContextMenu";

        public const string DesignDrawingContextMenuKey = @"DesignDrawingContextMenu";

        /// <summary>
        ///     Does nothing when the application has no such menu, which is what the play runtime looks
        ///     like.
        /// </summary>
        public static void Attach(Control control, string resourceKey)
        {
            if (Application.Current is null) return;

            if (Application.Current.TryGetResource(resourceKey, Application.Current.ActualThemeVariant,
                    out object? resource) &&
                resource is ContextMenu contextMenu)
                control.ContextMenu = contextMenu;
        }

        #endregion
    }
}
