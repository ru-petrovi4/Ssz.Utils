using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards;

public class ClickOrTouchButton : Button
{
    #region public functions

    public static readonly StyledProperty<bool> IsClickedOrTouchedProperty =
        AvaloniaProperty.Register<ClickOrTouchButton, bool>(nameof(IsClickedOrTouched), defaultValue: false);

    public static readonly StyledProperty<ICommand?> ClickOrTouchDownCommandProperty =
        AvaloniaProperty.Register<ClickOrTouchButton, ICommand?>(nameof(ClickOrTouchDownCommand));

    public static readonly StyledProperty<ICommand?> ClickOrTouchUpCommandProperty =
        AvaloniaProperty.Register<ClickOrTouchButton, ICommand?>(nameof(ClickOrTouchUpCommand));

    public bool IsClickedOrTouched
    {
        get => GetValue(IsClickedOrTouchedProperty);
        set => SetValue(IsClickedOrTouchedProperty, value);
    }

    public ICommand? ClickOrTouchDownCommand
    {
        get => GetValue(ClickOrTouchDownCommandProperty);
        set => SetValue(ClickOrTouchDownCommandProperty, value);
    }

    public ICommand? ClickOrTouchUpCommand
    {
        get => GetValue(ClickOrTouchUpCommandProperty);
        set => SetValue(ClickOrTouchUpCommandProperty, value);
    }

    #endregion

    #region protected functions

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsPressedProperty)
        {
            bool isPressed = (bool)(change.NewValue ?? false);
            if (isPressed)
            {
                if (_isClickedOrTouched) return;
                _isClickedOrTouched = true;
                IsClickedOrTouched = true;
                if (ClickOrTouchDownCommand is not null && ClickOrTouchDownCommand.CanExecute(CommandParameter))
                    ClickOrTouchDownCommand.Execute(CommandParameter);
            }
            else
            {
                if (!_isClickedOrTouched) return;
                _isClickedOrTouched = false;
                IsClickedOrTouched = false;
                if (ClickOrTouchUpCommand is not null && ClickOrTouchUpCommand.CanExecute(CommandParameter))
                    ClickOrTouchUpCommand.Execute(CommandParameter);
            }
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);

        // Handle touch/pointer enter (equivalent to WPF OnTouchEnter)
        if (e.Pointer.Type == PointerType.Touch)
        {
            if (_isClickedOrTouched) return;
            _isClickedOrTouched = true;
            IsClickedOrTouched = true;
            if (ClickOrTouchDownCommand is not null && ClickOrTouchDownCommand.CanExecute(CommandParameter))
                ClickOrTouchDownCommand.Execute(CommandParameter);
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        // Handle touch/pointer leave (equivalent to WPF OnTouchLeave)
        if (e.Pointer.Type == PointerType.Touch)
        {
            if (!_isClickedOrTouched) return;
            _isClickedOrTouched = false;
            IsClickedOrTouched = false;
            if (ClickOrTouchUpCommand is not null && ClickOrTouchUpCommand.CanExecute(CommandParameter))
                ClickOrTouchUpCommand.Execute(CommandParameter);
        }
    }

    #endregion

    #region private fields

    private bool _isClickedOrTouched;

    #endregion
}
