﻿/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit
{
    [TemplatePart(Name = PART_DropDownButton, Type = typeof(ToggleButton))]
    [TemplatePart(Name = PART_ContentPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    public class DropDownButton : ContentControl, ICommandSource
    {
        private const string PART_DropDownButton = "PART_DropDownButton";
        private const string PART_ContentPresenter = "PART_ContentPresenter";
        private const string PART_Popup = "PART_Popup";

        #region Base Class Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Button = GetTemplateChild(PART_DropDownButton) as ToggleButton;

            _contentPresenter = GetTemplateChild(PART_ContentPresenter) as ContentPresenter;

            if (_popup is not null)
                _popup.Opened -= Popup_Opened;

            _popup = GetTemplateChild(PART_Popup) as Popup;

            if (_popup is not null)
                _popup.Opened += Popup_Opened;
        }

        #endregion //Base Class Overrides

        #region Members

        private ContentPresenter _contentPresenter;
        private Popup _popup;

        #endregion

        #region Constructors

        static DropDownButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownButton),
                new FrameworkPropertyMetadata(typeof(DropDownButton)));
        }

        public DropDownButton()
        {
            Keyboard.AddKeyDownHandler(this, OnKeyDown);
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, OnMouseDownOutsideCapturedElement);
        }

        #endregion //Constructors

        #region Properties

        private ButtonBase _button;

        protected ButtonBase Button
        {
            get => _button;
            set
            {
                if (_button is not null)
                    _button.Click -= DropDownButton_Click;

                _button = value;

                if (_button is not null)
                    _button.Click += DropDownButton_Click;
            }
        }

        #region DropDownContent

        public static readonly DependencyProperty DropDownContentProperty =
            DependencyProperty.Register("DropDownContent", typeof(object), typeof(DropDownButton),
                new UIPropertyMetadata(null, OnDropDownContentChanged));

        public object DropDownContent
        {
            get => GetValue(DropDownContentProperty);
            set => SetValue(DropDownContentProperty, value);
        }

        private static void OnDropDownContentChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var dropDownButton = o as DropDownButton;
            if (dropDownButton is not null)
                dropDownButton.OnDropDownContentChanged(e.OldValue, e.NewValue);
        }

        protected virtual void OnDropDownContentChanged(object oldValue, object newValue)
        {
            // TODO: Add your property changed side-effects. Descendants can override as well.
        }

        #endregion //DropDownContent

        #region IsOpen

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register("IsOpen", typeof(bool),
            typeof(DropDownButton), new UIPropertyMetadata(false, OnIsOpenChanged));

        public bool IsOpen
        {
            get => (bool) GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private static void OnIsOpenChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var dropDownButton = o as DropDownButton;
            if (dropDownButton is not null)
                dropDownButton.OnIsOpenChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        protected virtual void OnIsOpenChanged(bool oldValue, bool newValue)
        {
            if (newValue)
                RaiseRoutedEvent(OpenedEvent);
            else
                RaiseRoutedEvent(ClosedEvent);
        }

        #endregion //IsOpen

        #endregion //Properties

        #region Events

        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent("Click",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DropDownButton));

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        public static readonly RoutedEvent OpenedEvent = EventManager.RegisterRoutedEvent("Opened",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DropDownButton));

        public event RoutedEventHandler Opened
        {
            add => AddHandler(OpenedEvent, value);
            remove => RemoveHandler(OpenedEvent, value);
        }

        public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent("Closed",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DropDownButton));

        public event RoutedEventHandler Closed
        {
            add => AddHandler(ClosedEvent, value);
            remove => RemoveHandler(ClosedEvent, value);
        }

        #endregion //Events

        #region Event Handlers

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsOpen)
            {
                if (KeyboardUtilities.IsKeyModifyingPopupState(e))
                {
                    IsOpen = true;
                    // ContentPresenter items will get focus in Popup_Opened().
                    e.Handled = true;
                }
            }
            else
            {
                if (KeyboardUtilities.IsKeyModifyingPopupState(e))
                {
                    CloseDropDown(true);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    CloseDropDown(true);
                    e.Handled = true;
                }
            }
        }

        private void OnMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            CloseDropDown(false);
        }

        private void DropDownButton_Click(object sender, RoutedEventArgs e)
        {
            OnClick();
        }

        private void CanExecuteChanged(object sender, EventArgs e)
        {
            CanExecuteChanged();
        }

        private void Popup_Opened(object sender, EventArgs e)
        {
            // Set the focus on the content of the ContentPresenter.
            if (_contentPresenter is not null)
                _contentPresenter.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }

        #endregion //Event Handlers

        #region Methods

        private void CanExecuteChanged()
        {
            if (Command is not null)
            {
                var command = Command as RoutedCommand;

                // If a RoutedCommand.
                if (command is not null)
                    IsEnabled = command.CanExecute(CommandParameter, CommandTarget) ? true : false;
                // If a not RoutedCommand.
                else
                    IsEnabled = Command.CanExecute(CommandParameter) ? true : false;
            }
        }

        /// <summary>
        ///     Closes the drop down.
        /// </summary>
        private void CloseDropDown(bool isFocusOnButton)
        {
            if (IsOpen)
                IsOpen = false;
            ReleaseMouseCapture();

            if (isFocusOnButton)
                Button.Focus();
        }

        protected virtual void OnClick()
        {
            RaiseRoutedEvent(ClickEvent);
            RaiseCommand();
        }

        /// <summary>
        ///     Raises routed events.
        /// </summary>
        private void RaiseRoutedEvent(RoutedEvent routedEvent)
        {
            var args = new RoutedEventArgs(routedEvent, this);
            RaiseEvent(args);
        }

        /// <summary>
        ///     Raises the command's Execute event.
        /// </summary>
        private void RaiseCommand()
        {
            if (Command is not null)
            {
                var routedCommand = Command as RoutedCommand;

                if (routedCommand is null)
                    Command.Execute(CommandParameter);
                else
                    routedCommand.Execute(CommandParameter, CommandTarget);
            }
        }

        /// <summary>
        ///     Unhooks a command from the Command property.
        /// </summary>
        /// <param name="oldCommand">The old command.</param>
        /// <param name="newCommand">The new command.</param>
        private void UnhookCommand(ICommand oldCommand, ICommand newCommand)
        {
            EventHandler handler = CanExecuteChanged;
            oldCommand.CanExecuteChanged -= handler;
        }

        /// <summary>
        ///     Hooks up a command to the CanExecuteChnaged event handler.
        /// </summary>
        /// <param name="oldCommand">The old command.</param>
        /// <param name="newCommand">The new command.</param>
        private void HookUpCommand(ICommand oldCommand, ICommand newCommand)
        {
            EventHandler handler = CanExecuteChanged;
            canExecuteChangedHandler = handler;
            if (newCommand is not null)
                newCommand.CanExecuteChanged += canExecuteChangedHandler;
        }

        #endregion //Methods

        #region ICommandSource Members

        // Keeps a copy of the CanExecuteChnaged handler so it doesn't get garbage collected.
        private EventHandler canExecuteChangedHandler;

        #region Command

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command",
            typeof(ICommand), typeof(DropDownButton), new PropertyMetadata(null, OnCommandChanged));

        [TypeConverter(typeof(CommandConverter))]
        public ICommand Command
        {
            get => (ICommand) GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dropDownButton = d as DropDownButton;
            if (dropDownButton is not null)
                dropDownButton.OnCommandChanged((ICommand) e.OldValue, (ICommand) e.NewValue);
        }

        protected virtual void OnCommandChanged(ICommand oldValue, ICommand newValue)
        {
            // If old command is not null, then we need to remove the handlers.
            if (oldValue is not null)
                UnhookCommand(oldValue, newValue);

            HookUpCommand(oldValue, newValue);

            CanExecuteChanged(); //may need to call this when changing the command parameter or target.
        }

        #endregion //Command

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(DropDownButton),
                new PropertyMetadata(null));

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register("CommandTarget",
            typeof(IInputElement), typeof(DropDownButton), new PropertyMetadata(null));

        public IInputElement CommandTarget
        {
            get => (IInputElement) GetValue(CommandTargetProperty);
            set => SetValue(CommandTargetProperty, value);
        }

        #endregion //ICommandSource Members
    }
}