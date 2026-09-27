using Avalonia.Controls;
using Avalonia.Input;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     Shows the author that the editor is busy.
    ///     <para>
    ///         WPF had a static Mouse.OverrideCursor that covered the whole application. Avalonia sets
    ///         the cursor on a control, so this sets it on the top level, which amounts to the same for
    ///         everything the editor shows.
    ///     </para>
    /// </summary>
    public static class CursorHelper
    {
        #region public functions

        public static void SetWaitCursor(bool isWaiting)
        {
            TopLevel? topLevel = ClipboardHelper.GetTopLevel();
            if (topLevel is null) return;

            topLevel.Cursor = isWaiting ? new Cursor(StandardCursorType.Wait) : Cursor.Default;
        }

        #endregion
    }
}
