using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Ssz.Operator.Core.VisualEditors.AddDrawingsFromLibrary
{
    public static class VirtualToggleButton
    {
        #region public functions

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.RegisterAttached("IsChecked", typeof(bool?), typeof(VirtualToggleButton),
                new FrameworkPropertyMetadata((bool?) false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                    OnIsCheckedChanged));


        public static readonly DependencyProperty IsThreeStateProperty =
            DependencyProperty.RegisterAttached("IsThreeState", typeof(bool), typeof(VirtualToggleButton),
                new FrameworkPropertyMetadata(false));


        public static readonly DependencyProperty IsVirtualToggleButtonProperty =
            DependencyProperty.RegisterAttached("IsVirtualToggleButton", typeof(bool), typeof(VirtualToggleButton),
                new FrameworkPropertyMetadata(false,
                    OnIsVirtualToggleButtonChanged));


        public static bool? GetIsChecked(DependencyObject d)
        {
            return (bool?) d.GetValue(IsCheckedProperty);
        }


        public static void SetIsChecked(DependencyObject d, bool? value)
        {
            d.SetValue(IsCheckedProperty, value);
        }


        public static bool GetIsThreeState(DependencyObject d)
        {
            return (bool) d.GetValue(IsThreeStateProperty);
        }


        public static void SetIsThreeState(DependencyObject d, bool value)
        {
            d.SetValue(IsThreeStateProperty, value);
        }


        public static bool GetIsVirtualToggleButton(DependencyObject d)
        {
            return (bool) d.GetValue(IsVirtualToggleButtonProperty);
        }


        public static void SetIsVirtualToggleButton(DependencyObject d, bool value)
        {
            d.SetValue(IsVirtualToggleButtonProperty, value);
        }

        #endregion

        #region internal functions

        internal static RoutedEventArgs? RaiseCheckedEvent(UIElement? target)
        {
            if (target is null) return null;

            var args = new RoutedEventArgs();
            args.RoutedEvent = ToggleButton.CheckedEvent;
            RaiseEvent(target, args);
            return args;
        }


        internal static RoutedEventArgs? RaiseUncheckedEvent(UIElement? target)
        {
            if (target is null) return null;

            var args = new RoutedEventArgs();
            args.RoutedEvent = ToggleButton.UncheckedEvent;
            RaiseEvent(target, args);
            return args;
        }


        internal static RoutedEventArgs? RaiseIndeterminateEvent(UIElement? target)
        {
            if (target is null) return null;

            var args = new RoutedEventArgs();
            args.RoutedEvent = ToggleButton.IndeterminateEvent;
            RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region private functions

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pseudobutton = d as UIElement;
            if (pseudobutton is not null)
            {
                var newValue = (bool?) e.NewValue;
                if (newValue == true)
                    RaiseCheckedEvent(pseudobutton);
                else if (newValue == false)
                    RaiseUncheckedEvent(pseudobutton);
                else
                    RaiseIndeterminateEvent(pseudobutton);
            }
        }


        private static void OnIsVirtualToggleButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as IInputElement;
            if (element is not null)
            {
                if ((bool) e.NewValue)
                {
                    element.MouseLeftButtonDown += OnMouseLeftButtonDown;
                    element.KeyDown += OnKeyDown;
                }
                else
                {
                    element.MouseLeftButtonDown -= OnMouseLeftButtonDown;
                    element.KeyDown -= OnKeyDown;
                }
            }
        }

        private static void OnMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            UpdateIsChecked(sender as DependencyObject);
        }

        private static void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                var depdencyObj = sender as DependencyObject;
                if (depdencyObj is not null)
                {
                    if (e.Key == Key.Space)
                    {
                        // ignore alt+space which invokes the system menu
                        if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) return;

                        UpdateIsChecked(depdencyObj);
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Enter &&
                             (bool) depdencyObj.GetValue(KeyboardNavigation.AcceptsReturnProperty))
                    {
                        UpdateIsChecked(depdencyObj);
                        e.Handled = true;
                    }
                }
            }
        }

        private static void UpdateIsChecked(DependencyObject? d)
        {
            if (d is null) return; //Fail early

            var isChecked = GetIsChecked(d);
            if (isChecked == true)
                SetIsChecked(d, GetIsThreeState(d) ? null : (bool?) false);
            else
                SetIsChecked(d, isChecked.HasValue);
        }

        private static void RaiseEvent(DependencyObject target, RoutedEventArgs args)
        {
            if (target is UIElement uiElement)
                uiElement.RaiseEvent(args);
            else if (target is ContentElement contentElement) contentElement.RaiseEvent(args);
        }

        #endregion
    }
}