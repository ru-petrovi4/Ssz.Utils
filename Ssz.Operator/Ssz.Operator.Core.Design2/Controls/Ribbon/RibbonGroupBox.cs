using System;
using Avalonia;
using Avalonia.Controls;

namespace Ssz.Operator.Core.Design.Controls.Ribbon;

/// <summary>
///     A named group of buttons inside a <see cref="RibbonTabItem" />.
/// </summary>
public class RibbonGroupBox : ItemsControl
{
    #region public functions

    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<RibbonGroupBox, object?>(nameof(Header));

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    #endregion

    #region protected functions

    protected override Type StyleKeyOverride => typeof(RibbonGroupBox);

    #endregion
}
