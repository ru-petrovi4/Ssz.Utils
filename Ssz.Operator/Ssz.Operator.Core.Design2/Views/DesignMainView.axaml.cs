using Avalonia.Controls;

namespace Ssz.Operator.Core.Design.Views;

/// <summary>
///     The editor itself. It is a view and not a window, because in the browser there are no windows:
///     the desktop puts it in <see cref="DesignMainWindow" /> and the browser makes it the single view.
/// </summary>
public partial class DesignMainView : UserControl
{
    public DesignMainView()
    {
        InitializeComponent();

        StatusTextBlock.Text = DsProject.Instance.IsInitialized
            ? DsProject.Instance.DsProjectFileFullName
            : @"";
    }
}
