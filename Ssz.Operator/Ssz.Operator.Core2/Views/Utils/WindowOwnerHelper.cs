using Avalonia.Controls;

namespace Ssz.Operator.Core.Utils
{
    /// <summary>
    ///     Avalonia's Window.Owner has no public setter - the owner is passed to Show()/ShowDialog().
    ///     These helpers keep the WPF-era call sites, where the owner may be null, readable.
    /// </summary>
    public static class WindowOwnerHelper
    {
        public static void ShowOwned(this Window window, Window? owner)
        {
            if (owner is not null)
                window.Show(owner);
            else
                window.Show();
        }
    }
}
