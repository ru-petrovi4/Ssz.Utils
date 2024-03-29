﻿/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;

namespace Ssz.Xceed.Wpf.Toolkit
{
  /// <summary>
  ///     Represents a spinner control that includes two Buttons.
  /// </summary>
  [TemplatePart(Name = PART_IncreaseButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_DecreaseButton, Type = typeof(ButtonBase))]
    [ContentProperty("Content")]
    public class ButtonSpinner : Spinner
    {
        private const string PART_IncreaseButton = "PART_IncreaseButton";
        private const string PART_DecreaseButton = "PART_DecreaseButton";

        #region Constructors

        static ButtonSpinner()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ButtonSpinner),
                new FrameworkPropertyMetadata(typeof(ButtonSpinner)));
        }

        #endregion //Constructors

        #region Event Handlers

        /// <summary>
        ///     Handle click event of IncreaseButton and DecreaseButton template parts,
        ///     translating Click to appropriate Spin event.
        /// </summary>
        /// <param name="sender">Event sender, should be either IncreaseButton or DecreaseButton template part.</param>
        /// <param name="e">Event args.</param>
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (AllowSpin)
            {
                var direction = sender == IncreaseButton ? SpinDirection.Increase : SpinDirection.Decrease;
                OnSpin(new SpinEventArgs(direction));
            }
        }

        #endregion //Event Handlers

        #region Properties

        #region AllowSpin

        public static readonly DependencyProperty AllowSpinProperty = DependencyProperty.Register("AllowSpin",
            typeof(bool), typeof(ButtonSpinner), new UIPropertyMetadata(true, AllowSpinPropertyChanged));

        public bool AllowSpin
        {
            get => (bool) GetValue(AllowSpinProperty);
            set => SetValue(AllowSpinProperty, value);
        }

        private static void AllowSpinPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = d as ButtonSpinner;
            source.OnAllowSpinChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        #endregion //AllowSpin

        #region Content

        /// <summary>
        ///     Identifies the Content dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content",
            typeof(object), typeof(ButtonSpinner), new PropertyMetadata(null, OnContentPropertyChanged));

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        ///     ContentProperty property changed handler.
        /// </summary>
        /// <param name="d">ButtonSpinner that changed its Content.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = d as ButtonSpinner;
            source.OnContentChanged(e.OldValue, e.NewValue);
        }

        #endregion //Content

        #region DecreaseButton

        private ButtonBase _decreaseButton;

        /// <summary>
        ///     Gets or sets the DecreaseButton template part.
        /// </summary>
        private ButtonBase DecreaseButton
        {
            get => _decreaseButton;
            set
            {
                if (_decreaseButton is not null) _decreaseButton.Click -= OnButtonClick;

                _decreaseButton = value;

                if (_decreaseButton is not null) _decreaseButton.Click += OnButtonClick;
            }
        }

        #endregion //DecreaseButton

        #region IncreaseButton

        private ButtonBase _increaseButton;

        /// <summary>
        ///     Gets or sets the IncreaseButton template part.
        /// </summary>
        private ButtonBase IncreaseButton
        {
            get => _increaseButton;
            set
            {
                if (_increaseButton is not null) _increaseButton.Click -= OnButtonClick;

                _increaseButton = value;

                if (_increaseButton is not null) _increaseButton.Click += OnButtonClick;
            }
        }

        #endregion //IncreaseButton

        #region ShowButtonSpinner

        public static readonly DependencyProperty ShowButtonSpinnerProperty =
            DependencyProperty.Register("ShowButtonSpinner", typeof(bool), typeof(ButtonSpinner),
                new UIPropertyMetadata(true));

        public bool ShowButtonSpinner
        {
            get => (bool) GetValue(ShowButtonSpinnerProperty);
            set => SetValue(ShowButtonSpinnerProperty, value);
        }

        #endregion //ShowButtonSpinner

        #endregion //Properties

        #region Base Class Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            IncreaseButton = GetTemplateChild(PART_IncreaseButton) as ButtonBase;
            DecreaseButton = GetTemplateChild(PART_DecreaseButton) as ButtonBase;

            SetButtonUsage();
        }

        /// <summary>
        ///     Cancel LeftMouseButtonUp events originating from a button that has
        ///     been changed to disabled.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            Point mousePosition;
            if (IncreaseButton is not null && IncreaseButton.IsEnabled == false)
            {
                mousePosition = e.GetPosition(IncreaseButton);
                if (mousePosition.X > 0 && mousePosition.X < IncreaseButton.ActualWidth &&
                    mousePosition.Y > 0 && mousePosition.Y < IncreaseButton.ActualHeight)
                    e.Handled = true;
            }

            if (DecreaseButton is not null && DecreaseButton.IsEnabled == false)
            {
                mousePosition = e.GetPosition(DecreaseButton);
                if (mousePosition.X > 0 && mousePosition.X < DecreaseButton.ActualWidth &&
                    mousePosition.Y > 0 && mousePosition.Y < DecreaseButton.ActualHeight)
                    e.Handled = true;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                {
                    if (AllowSpin)
                    {
                        OnSpin(new SpinEventArgs(SpinDirection.Increase));
                        e.Handled = true;
                    }

                    break;
                }
                case Key.Down:
                {
                    if (AllowSpin)
                    {
                        OnSpin(new SpinEventArgs(SpinDirection.Decrease));
                        e.Handled = true;
                    }

                    break;
                }
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (!e.Handled && AllowSpin)
            {
                if (e.Delta < 0)
                    OnSpin(new SpinEventArgs(SpinDirection.Decrease, true));
                else if (0 < e.Delta) OnSpin(new SpinEventArgs(SpinDirection.Increase, true));

                e.Handled = true;
            }
        }

        /// <summary>
        ///     Called when valid spin direction changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected override void OnValidSpinDirectionChanged(ValidSpinDirections oldValue, ValidSpinDirections newValue)
        {
            SetButtonUsage();
        }

        #endregion //Base Class Overrides

        #region Methods

        /// <summary>
        ///     Occurs when the Content property value changed.
        /// </summary>
        /// <param name="oldValue">The old value of the Content property.</param>
        /// <param name="newValue">The new value of the Content property.</param>
        protected virtual void OnContentChanged(object oldValue, object newValue)
        {
        }

        protected virtual void OnAllowSpinChanged(bool oldValue, bool newValue)
        {
            SetButtonUsage();
        }

        /// <summary>
        ///     Disables or enables the buttons based on the valid spin direction.
        /// </summary>
        private void SetButtonUsage()
        {
            // buttonspinner adds buttons that spin, so disable accordingly.
            if (IncreaseButton is not null)
                IncreaseButton.IsEnabled = AllowSpin &&
                                           (ValidSpinDirection & ValidSpinDirections.Increase) ==
                                           ValidSpinDirections.Increase;

            if (DecreaseButton is not null)
                DecreaseButton.IsEnabled = AllowSpin &&
                                           (ValidSpinDirection & ValidSpinDirections.Decrease) ==
                                           ValidSpinDirections.Decrease;
        }

        #endregion //Methods
    }
}