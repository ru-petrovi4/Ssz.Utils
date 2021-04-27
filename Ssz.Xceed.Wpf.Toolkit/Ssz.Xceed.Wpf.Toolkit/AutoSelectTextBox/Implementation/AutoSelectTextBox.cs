/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class AutoSelectTextBox : TextBox
    {
        #region QueryMoveFocus EVENT

        public static readonly RoutedEvent QueryMoveFocusEvent = EventManager.RegisterRoutedEvent("QueryMoveFocus",
            RoutingStrategy.Bubble,
            typeof(QueryMoveFocusEventHandler),
            typeof(AutoSelectTextBox));

        #endregion QueryMoveFocus EVENT

        static AutoSelectTextBox()
        {
            AutomationProperties.AutomationIdProperty.OverrideMetadata(typeof(AutoSelectTextBox),
                new UIPropertyMetadata("AutoSelectTextBox"));
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!AutoMoveFocus)
            {
                base.OnPreviewKeyDown(e);
                return;
            }

            if (e.Key == Key.Left
                && (Keyboard.Modifiers == ModifierKeys.None
                    || Keyboard.Modifiers == ModifierKeys.Control))
                e.Handled = MoveFocusLeft();

            if (e.Key == Key.Right
                && (Keyboard.Modifiers == ModifierKeys.None
                    || Keyboard.Modifiers == ModifierKeys.Control))
                e.Handled = MoveFocusRight();

            if ((e.Key == Key.Up || e.Key == Key.PageUp)
                && (Keyboard.Modifiers == ModifierKeys.None
                    || Keyboard.Modifiers == ModifierKeys.Control))
                e.Handled = MoveFocusUp();

            if ((e.Key == Key.Down || e.Key == Key.PageDown)
                && (Keyboard.Modifiers == ModifierKeys.None
                    || Keyboard.Modifiers == ModifierKeys.Control))
                e.Handled = MoveFocusDown();

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnPreviewGotKeyboardFocus(e);

            if (AutoSelectBehavior == AutoSelectBehavior.OnFocus)
                // If the focus was not in one of our child ( or popup ), we select all the text.
                if (!TreeHelper.IsDescendantOf(e.OldFocus as DependencyObject, this))
                    SelectAll();
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            if (AutoSelectBehavior == AutoSelectBehavior.Never)
                return;

            if (IsKeyboardFocusWithin == false)
            {
                Focus();
                e.Handled = true;
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            if (!AutoMoveFocus)
                return;

            if (Text.Length != 0
                && Text.Length == MaxLength
                && CaretIndex == MaxLength)
                if (CanMoveFocus(FocusNavigationDirection.Right, true))
                {
                    var direction = FlowDirection == FlowDirection.LeftToRight
                        ? FocusNavigationDirection.Right
                        : FocusNavigationDirection.Left;

                    MoveFocus(new TraversalRequest(direction));
                }
        }

        private bool CanMoveFocus(FocusNavigationDirection direction, bool reachedMax)
        {
            var e = new QueryMoveFocusEventArgs(direction, reachedMax);
            RaiseEvent(e);
            return e.CanMoveFocus;
        }

        private bool MoveFocusLeft()
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                //occurs only if the cursor is at the beginning of the text
                if (CaretIndex == 0 && SelectionLength == 0)
                {
                    if (ComponentCommands.MoveFocusBack.CanExecute(null, this))
                    {
                        ComponentCommands.MoveFocusBack.Execute(null, this);
                        return true;
                    }

                    if (CanMoveFocus(FocusNavigationDirection.Left, false))
                    {
                        MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
                        return true;
                    }
                }
            }
            else
            {
                //occurs only if the cursor is at the end of the text
                if (CaretIndex == Text.Length && SelectionLength == 0)
                {
                    if (ComponentCommands.MoveFocusBack.CanExecute(null, this))
                    {
                        ComponentCommands.MoveFocusBack.Execute(null, this);
                        return true;
                    }

                    if (CanMoveFocus(FocusNavigationDirection.Left, false))
                    {
                        MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
                        return true;
                    }
                }
            }

            return false;
        }

        private bool MoveFocusRight()
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                //occurs only if the cursor is at the beginning of the text
                if (CaretIndex == Text.Length && SelectionLength == 0)
                {
                    if (ComponentCommands.MoveFocusForward.CanExecute(null, this))
                    {
                        ComponentCommands.MoveFocusForward.Execute(null, this);
                        return true;
                    }

                    if (CanMoveFocus(FocusNavigationDirection.Right, false))
                    {
                        MoveFocus(new TraversalRequest(FocusNavigationDirection.Right));
                        return true;
                    }
                }
            }
            else
            {
                //occurs only if the cursor is at the end of the text
                if (CaretIndex == 0 && SelectionLength == 0)
                {
                    if (ComponentCommands.MoveFocusForward.CanExecute(null, this))
                    {
                        ComponentCommands.MoveFocusForward.Execute(null, this);
                        return true;
                    }

                    if (CanMoveFocus(FocusNavigationDirection.Right, false))
                    {
                        MoveFocus(new TraversalRequest(FocusNavigationDirection.Right));
                        return true;
                    }
                }
            }

            return false;
        }

        private bool MoveFocusUp()
        {
            var lineNumber = GetLineIndexFromCharacterIndex(SelectionStart);

            //occurs only if the cursor is on the first line
            if (lineNumber == 0)
            {
                if (ComponentCommands.MoveFocusUp.CanExecute(null, this))
                {
                    ComponentCommands.MoveFocusUp.Execute(null, this);
                    return true;
                }

                if (CanMoveFocus(FocusNavigationDirection.Up, false))
                {
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
                    return true;
                }
            }

            return false;
        }

        private bool MoveFocusDown()
        {
            var lineNumber = GetLineIndexFromCharacterIndex(SelectionStart);

            //occurs only if the cursor is on the first line
            if (lineNumber == LineCount - 1)
            {
                if (ComponentCommands.MoveFocusDown.CanExecute(null, this))
                {
                    ComponentCommands.MoveFocusDown.Execute(null, this);
                    return true;
                }

                if (CanMoveFocus(FocusNavigationDirection.Down, false))
                {
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                    return true;
                }
            }

            return false;
        }

        #region AutoSelectBehavior PROPERTY

        public AutoSelectBehavior AutoSelectBehavior
        {
            get => (AutoSelectBehavior) GetValue(AutoSelectBehaviorProperty);
            set => SetValue(AutoSelectBehaviorProperty, value);
        }

        public static readonly DependencyProperty AutoSelectBehaviorProperty =
            DependencyProperty.Register("AutoSelectBehavior", typeof(AutoSelectBehavior), typeof(AutoSelectTextBox),
                new UIPropertyMetadata(AutoSelectBehavior.Never));

        #endregion AutoSelectBehavior PROPERTY

        #region AutoMoveFocus PROPERTY

        public bool AutoMoveFocus
        {
            get => (bool) GetValue(AutoMoveFocusProperty);
            set => SetValue(AutoMoveFocusProperty, value);
        }

        public static readonly DependencyProperty AutoMoveFocusProperty =
            DependencyProperty.Register("AutoMoveFocus", typeof(bool), typeof(AutoSelectTextBox),
                new UIPropertyMetadata(false));

        #endregion AutoMoveFocus PROPERTY
    }
}