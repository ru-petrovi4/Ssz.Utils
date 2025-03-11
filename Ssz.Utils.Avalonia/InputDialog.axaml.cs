using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DialogHostAvalonia;
using System.Threading.Tasks;

namespace Ssz.Utils.Avalonia;

public partial class InputDialog : UserControl
{
    public InputDialog(string label)
    {
        InitializeComponent();

        LabelTextBlock.Text = label;
    }
}