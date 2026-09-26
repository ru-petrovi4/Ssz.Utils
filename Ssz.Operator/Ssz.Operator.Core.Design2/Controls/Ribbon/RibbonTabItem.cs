using System;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace Ssz.Operator.Core.Design.Controls.Ribbon;

/// <summary>
///     One tab of the <see cref="Ribbon" />. Its children are groups, which a
///     <see cref="TabItem" /> alone cannot hold: the groups go into an items control that becomes
///     the content of the tab.
/// </summary>
public class RibbonTabItem : TabItem
{
    #region construction and destruction

    public RibbonTabItem()
    {
        Content = new ItemsControl
        {
            ItemsSource = Groups,
            ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel
            {
                Orientation = Orientation.Horizontal
            })
        };
    }

    #endregion

    #region public functions

    [Content]
    public AvaloniaList<RibbonGroupBox> Groups { get; } = new();

    #endregion

    #region protected functions

    protected override Type StyleKeyOverride => typeof(RibbonTabItem);

    #endregion
}
