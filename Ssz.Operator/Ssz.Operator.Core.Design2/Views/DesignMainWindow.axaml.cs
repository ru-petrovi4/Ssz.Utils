using Avalonia.Controls;

namespace Ssz.Operator.Core.Design.Views;

/// <summary>
///     Only a frame around <see cref="DesignMainView" />: everything the editor shows lives in that
///     view, so that the browser, which has no windows, shows exactly the same editor.
/// </summary>
public partial class DesignMainWindow : Window
{
    public DesignMainWindow()
    {
        InitializeComponent();
    }
}
