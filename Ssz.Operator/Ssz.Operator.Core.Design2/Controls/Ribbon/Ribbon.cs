using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Ssz.Operator.Core.Design.Controls.Ribbon;

/// <summary>
///     The ribbon of the editor. Fluent.Ribbon has no Avalonia counterpart, and the editor uses a
///     small part of it: tabs holding groups of buttons, a quick access bar and an application menu.
///     That part is what these controls are - the markup shape is kept close to Fluent.Ribbon's so
///     that the editor markup reads the same.
///     <para>
///         It is a <see cref="TabControl" />, so tab selection, keyboard navigation and the content
///         presenter come from Avalonia itself.
///     </para>
/// </summary>
[TemplatePart("PART_ApplicationMenuButton", typeof(Button))]
public class Ribbon : TabControl
{
    #region public functions

    public static readonly StyledProperty<object?> MenuProperty =
        AvaloniaProperty.Register<Ribbon, object?>(nameof(Menu));

    public static readonly StyledProperty<object?> MenuHeaderProperty =
        AvaloniaProperty.Register<Ribbon, object?>(nameof(MenuHeader));

    public static readonly StyledProperty<bool> IsQuickAccessToolBarVisibleProperty =
        AvaloniaProperty.Register<Ribbon, bool>(nameof(IsQuickAccessToolBarVisible), true);

    /// <summary>
    ///     The application menu, shown in the button at the left of the tab strip. The editor puts
    ///     the project commands and the recent files list in it.
    /// </summary>
    public object? Menu
    {
        get => GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    /// <summary>
    ///     What the button that opens the <see cref="Menu" /> shows. Without it the button is hidden.
    /// </summary>
    public object? MenuHeader
    {
        get => GetValue(MenuHeaderProperty);
        set => SetValue(MenuHeaderProperty, value);
    }

    public bool IsQuickAccessToolBarVisible
    {
        get => GetValue(IsQuickAccessToolBarVisibleProperty);
        set => SetValue(IsQuickAccessToolBarVisibleProperty, value);
    }

    /// <summary>
    ///     The buttons repeated in the bar above the tabs. They are the very buttons of the tabs,
    ///     named in the markup, so that a command has one definition only.
    /// </summary>
    public AvaloniaList<QuickAccessMenuItem> QuickAccessItems { get; } = new();

    #endregion

    #region protected functions

    protected override Type StyleKeyOverride => typeof(Ribbon);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        var applicationMenuButton = e.NameScope.Find<Button>(@"PART_ApplicationMenuButton");
        if (applicationMenuButton is not null)
            applicationMenuButton.Flyout = Menu is null
                ? null
                : new Flyout
                {
                    Content = Menu,
                    Placement = PlacementMode.BottomEdgeAlignedLeft
                };

        var quickAccessToolBar = e.NameScope.Find<Panel>(@"PART_QuickAccessToolBar");
        if (quickAccessToolBar is not null)
            FillInQuickAccessToolBar(quickAccessToolBar);
    }

    #endregion

    #region private functions

    /// <summary>
    ///     A control has one visual parent, so the buttons of the tabs cannot appear in the bar as
    ///     well: each one gets a small button beside it that carries the same command and icon.
    /// </summary>
    private void FillInQuickAccessToolBar(Panel quickAccessToolBar)
    {
        quickAccessToolBar.Children.Clear();

        foreach (QuickAccessMenuItem quickAccessMenuItem in QuickAccessItems)
        {
            Control? target = quickAccessMenuItem.Target;
            if (!quickAccessMenuItem.IsChecked || target is null)
                continue;

            var button = new RibbonButton
            {
                SizeDefinition = RibbonControlSize.Small,
                Icon = RibbonControl.GetIcon(target),
                Command = (target as Button)?.Command
            };
            ToolTip.SetTip(button, ToolTip.GetTip(target));

            quickAccessToolBar.Children.Add(button);
        }
    }

    #endregion
}

/// <summary>
///     Names a button of the ribbon that is repeated in the quick access bar.
/// </summary>
public class QuickAccessMenuItem : AvaloniaObject
{
    public static readonly StyledProperty<Control?> TargetProperty =
        AvaloniaProperty.Register<QuickAccessMenuItem, Control?>(nameof(Target));

    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<QuickAccessMenuItem, bool>(nameof(IsChecked), true);

    public Control? Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    /// <summary>
    ///     Whether the button is in the bar. Fluent.Ribbon lets the user turn these on and off; here
    ///     it is only what the markup asks for.
    /// </summary>
    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
}
