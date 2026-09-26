using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Ssz.Operator.Core.Design.Controls.Ribbon;

/// <summary>
///     How much room a ribbon button takes: the icon above the text, or the icon alone in a row of
///     small buttons. Fluent.Ribbon calls this SizeDefinition.
/// </summary>
public enum RibbonControlSize
{
    Large,
    Small
}

/// <summary>
///     Carries what a ribbon button shows. <see cref="RibbonButton" /> and
///     <see cref="RibbonToggleButton" /> cannot share a base class - one is a Button and the other a
///     ToggleButton - so the properties are attached ones, defined once and set on either.
/// </summary>
public static class RibbonControl
{
    #region public functions

    public static readonly AttachedProperty<object?> HeaderProperty =
        AvaloniaProperty.RegisterAttached<Control, object?>(@"Header", typeof(RibbonControl));

    public static readonly AttachedProperty<IImage?> IconProperty =
        AvaloniaProperty.RegisterAttached<Control, IImage?>(@"Icon", typeof(RibbonControl));

    public static readonly AttachedProperty<IImage?> LargeIconProperty =
        AvaloniaProperty.RegisterAttached<Control, IImage?>(@"LargeIcon", typeof(RibbonControl));

    public static readonly AttachedProperty<RibbonControlSize> SizeDefinitionProperty =
        AvaloniaProperty.RegisterAttached<Control, RibbonControlSize>(@"SizeDefinition",
            typeof(RibbonControl), RibbonControlSize.Large);

    public static object? GetHeader(Control control) => control.GetValue(HeaderProperty);

    public static void SetHeader(Control control, object? value) => control.SetValue(HeaderProperty, value);

    public static IImage? GetIcon(Control control) => control.GetValue(IconProperty);

    public static void SetIcon(Control control, IImage? value) => control.SetValue(IconProperty, value);

    public static IImage? GetLargeIcon(Control control) => control.GetValue(LargeIconProperty);

    public static void SetLargeIcon(Control control, IImage? value) => control.SetValue(LargeIconProperty, value);

    public static RibbonControlSize GetSizeDefinition(Control control) => control.GetValue(SizeDefinitionProperty);

    public static void SetSizeDefinition(Control control, RibbonControlSize value) =>
        control.SetValue(SizeDefinitionProperty, value);

    #endregion
}

/// <summary>
///     A button of the ribbon. The properties it shows are its own, so that the markup reads like
///     Fluent.Ribbon's; they forward to the attached ones the control theme binds to.
/// </summary>
public class RibbonButton : Button
{
    #region public functions

    public static readonly StyledProperty<object?> HeaderProperty =
        RibbonControl.HeaderProperty.AddOwner<RibbonButton>();

    public static readonly StyledProperty<IImage?> IconProperty =
        RibbonControl.IconProperty.AddOwner<RibbonButton>();

    public static readonly StyledProperty<IImage?> LargeIconProperty =
        RibbonControl.LargeIconProperty.AddOwner<RibbonButton>();

    public static readonly StyledProperty<RibbonControlSize> SizeDefinitionProperty =
        RibbonControl.SizeDefinitionProperty.AddOwner<RibbonButton>();

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public IImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public IImage? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    public RibbonControlSize SizeDefinition
    {
        get => GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    #endregion

    #region protected functions

    protected override Type StyleKeyOverride => typeof(RibbonButton);

    #endregion
}

/// <summary>
///     A ribbon button that stays pressed, for the editor switches such as the discrete mode.
/// </summary>
public class RibbonToggleButton : ToggleButton
{
    #region public functions

    public static readonly StyledProperty<object?> HeaderProperty =
        RibbonControl.HeaderProperty.AddOwner<RibbonToggleButton>();

    public static readonly StyledProperty<IImage?> IconProperty =
        RibbonControl.IconProperty.AddOwner<RibbonToggleButton>();

    public static readonly StyledProperty<IImage?> LargeIconProperty =
        RibbonControl.LargeIconProperty.AddOwner<RibbonToggleButton>();

    public static readonly StyledProperty<RibbonControlSize> SizeDefinitionProperty =
        RibbonControl.SizeDefinitionProperty.AddOwner<RibbonToggleButton>();

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public IImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public IImage? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    public RibbonControlSize SizeDefinition
    {
        get => GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    #endregion

    #region protected functions

    protected override Type StyleKeyOverride => typeof(RibbonToggleButton);

    #endregion
}
