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
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit
{
    [TemplatePart(Name = PART_CalculatorPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PART_Calculator, Type = typeof(Calculator))]
    public class CalculatorUpDown : DecimalUpDown
    {
        private const string PART_CalculatorPopup = "PART_CalculatorPopup";
        private const string PART_Calculator = "PART_Calculator";

        #region Methods

        private void CloseCalculatorUpDown(bool isFocusOnTextBox)
        {
            if (IsOpen)
                IsOpen = false;
            ReleaseMouseCapture();

            if (isFocusOnTextBox && TextBox is not null)
                TextBox.Focus();
        }

        #endregion //Methods

        #region Members

        private Popup _calculatorPopup;
        private Calculator _calculator;
        private decimal? _initialValue;

        #endregion //Members

        #region Properties

        #region DisplayText

        public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register("DisplayText",
            typeof(string), typeof(CalculatorUpDown), new UIPropertyMetadata("0"));

        public string DisplayText
        {
            get => (string) GetValue(DisplayTextProperty);
            set => SetValue(DisplayTextProperty, value);
        }

        #endregion //DisplayText

        #region EnterClosesCalculator

        public static readonly DependencyProperty EnterClosesCalculatorProperty =
            DependencyProperty.Register("EnterClosesCalculator", typeof(bool), typeof(CalculatorUpDown),
                new UIPropertyMetadata(false));

        public bool EnterClosesCalculator
        {
            get => (bool) GetValue(EnterClosesCalculatorProperty);
            set => SetValue(EnterClosesCalculatorProperty, value);
        }

        #endregion //EnterClosesCalculator

        #region IsOpen

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register("IsOpen", typeof(bool),
            typeof(CalculatorUpDown), new UIPropertyMetadata(false, OnIsOpenChanged));

        public bool IsOpen
        {
            get => (bool) GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private static void OnIsOpenChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var calculatorUpDown = o as CalculatorUpDown;
            if (calculatorUpDown is not null)
                calculatorUpDown.OnIsOpenChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        protected virtual void OnIsOpenChanged(bool oldValue, bool newValue)
        {
            if (newValue)
                _initialValue = Value;
        }

        #endregion //IsOpen

        #region Memory

        public static readonly DependencyProperty MemoryProperty = DependencyProperty.Register("Memory",
            typeof(decimal), typeof(CalculatorUpDown), new UIPropertyMetadata(default(decimal)));

        public decimal Memory
        {
            get => (decimal) GetValue(MemoryProperty);
            set => SetValue(MemoryProperty, value);
        }

        #endregion //Memory

        #region Precision

        public static readonly DependencyProperty PrecisionProperty =
            DependencyProperty.Register("Precision", typeof(int), typeof(CalculatorUpDown), new UIPropertyMetadata(6));

        public int Precision
        {
            get => (int) GetValue(PrecisionProperty);
            set => SetValue(PrecisionProperty, value);
        }

        #endregion //Precision

        #endregion //Properties

        #region Constructors

        static CalculatorUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CalculatorUpDown),
                new FrameworkPropertyMetadata(typeof(CalculatorUpDown)));
        }

        public CalculatorUpDown()
        {
            Keyboard.AddKeyDownHandler(this, OnKeyDown);
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, OnMouseDownOutsideCapturedElement);
        }

        #endregion //Constructors

        #region Base Class Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_calculatorPopup is not null)
                _calculatorPopup.Opened -= CalculatorPopup_Opened;

            _calculatorPopup = GetTemplateChild(PART_CalculatorPopup) as Popup;

            if (_calculatorPopup is not null)
                _calculatorPopup.Opened += CalculatorPopup_Opened;

            if (_calculator is not null)
                _calculator.ValueChanged -= OnCalculatorValueChanged;

            _calculator = GetTemplateChild(PART_Calculator) as Calculator;

            if (_calculator is not null)
                _calculator.ValueChanged += OnCalculatorValueChanged;
        }

        private void OnCalculatorValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsValid(_calculator.Value)) Value = _calculator.Value;
        }

        #endregion //Base Class Overrides

        #region Event Handlers

        private void CalculatorPopup_Opened(object sender, EventArgs e)
        {
            if (_calculator is not null)
            {
                _calculator.InitializeToValue(Value);
                _calculator.Focus();
            }
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            if (IsOpen && EnterClosesCalculator)
            {
                var buttonType = CalculatorUtilities.GetCalculatorButtonTypeFromText(e.Text);
                if (buttonType == Calculator.CalculatorButtonType.Equal) CloseCalculatorUpDown(true);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsOpen)
            {
                if (KeyboardUtilities.IsKeyModifyingPopupState(e))
                {
                    IsOpen = true;
                    // Calculator will get focus in CalculatorPopup_Opened().
                    e.Handled = true;
                }
            }
            else
            {
                if (KeyboardUtilities.IsKeyModifyingPopupState(e))
                {
                    CloseCalculatorUpDown(true);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    if (EnterClosesCalculator)
                        Value = _initialValue;
                    CloseCalculatorUpDown(true);
                    e.Handled = true;
                }
            }
        }

        private void OnMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            CloseCalculatorUpDown(false);
        }

        #endregion //Event Handlers
    }
}