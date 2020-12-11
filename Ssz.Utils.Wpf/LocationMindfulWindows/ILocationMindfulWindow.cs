using System.Windows;

namespace Ssz.Utils.Wpf.LocationMindfulWindows
{
    public interface ILocationMindfulWindow
    {
        double Left { get; set; }
        double Top { get; set; }
        double Width { get; set; }
        double Height { get; set; }

        WindowStartupLocation WindowStartupLocation { get; set; }

        string Category { get; set; }
        int SlotNum { get; set; }
    }
}