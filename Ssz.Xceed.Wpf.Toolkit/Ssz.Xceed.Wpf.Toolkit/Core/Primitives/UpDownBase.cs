/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Core.Input;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;

namespace Ssz.Xceed.Wpf.Toolkit.Primitives
{
    [TemplatePart(Name = PART_TextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_Spinner, Type = typeof(Spinner))]
    public abstract class UpDownBase<T> : InputBase, IValidateInput, IPropertyGridItem
    {
        #region Constructors

        internal UpDownBase()
        {
        }

        #endregion //Constructors

        public void RefreshForPropertyGrid()
        {
        }

        public void EndEditInPropertyGrid()
        {
        }

        #region Event Handlers

        private void OnSpinnerSpin(object sender, SpinEventArgs e)
        {
            if (AllowSpin && !IsReadOnly)
            {
                var activeTrigger = MouseWheelActiveTrigger;
                var spin = !e.UsingMouseWheel;
                spin |= activeTrigger == MouseWheelActiveTrigger.MouseOver;
                spin |= TextBox.IsFocused && activeTrigger == MouseWheelActiveTrigger.Focused;

                if (spin) OnSpin(e);
            }
        }

        #endregion //Event Handlers

        #region Members

        /// <summary>
        ///     Name constant for Text template part.
        /// </summary>
        internal const string PART_TextBox = "PART_TextBox";

        /// <summary>
        ///     Name constant for Spinner template part.
        /// </summary>
        internal const string PART_Spinner = "PART_Spinner";

        /// <summary>
        ///     Flags if the Text and Value properties are in the process of being sync'd
        /// </summary>
        private bool _isSyncingTextAndValueProperties;

        private bool _isTextChangedFromUI;

        #endregion //Members

        #region Properties

        protected Spinner Spinner { get; private set; }

        protected TextBox TextBox { get; private set; }

        #region AllowSpin

        public static readonly DependencyProperty AllowSpinProperty = DependencyProperty.Register("AllowSpin",
            typeof(bool), typeof(UpDownBase<T>), new UIPropertyMetadata(true));

        public bool AllowSpin
        {
            get => (bool) GetValue(AllowSpinProperty);
            set => SetValue(AllowSpinProperty, value);
        }

        #endregion //AllowSpin

        #region ClipValueToMinMax

        public static readonly DependencyProperty ClipValueToMinMaxProperty =
            DependencyProperty.Register("ClipValueToMinMax", typeof(bool), typeof(UpDownBase<T>),
                new UIPropertyMetadata(false));

        public bool ClipValueToMinMax
        {
            get => (bool) GetValue(ClipValueToMinMaxProperty);
            set => SetValue(ClipValueToMinMaxProperty, value);
        }

        #endregion //ClipValueToMinMax

        #region DisplayDefaultValueOnEmptyText

        public static readonly DependencyProperty DisplayDefaultValueOnEmptyTextProperty =
            DependencyProperty.Register("DisplayDefaultValueOnEmptyText", typeof(bool), typeof(UpDownBase<T>),
                new UIPropertyMetadata(false, OnDisplayDefaultValueOnEmptyTextChanged));

        public bool DisplayDefaultValueOnEmptyText
        {
            get => (bool) GetValue(DisplayDefaultValueOnEmptyTextProperty);
            set => SetValue(DisplayDefaultValueOnEmptyTextProperty, value);
        }

        private static void OnDisplayDefaultValueOnEmptyTextChanged(DependencyObject source,
            DependencyPropertyChangedEventArgs args)
        {
            ((UpDownBase<T>) source).OnDisplayDefaultValueOnEmptyTextChanged((bool) args.OldValue,
                (bool) args.NewValue);
        }

        private void OnDisplayDefaultValueOnEmptyTextChanged(bool oldValue, bool newValue)
        {
            if (IsInitialized && string.IsNullOrEmpty(Text)) SyncTextAndValueProperties(true, Text);
        }

        #endregion //DisplayDefaultValueOnEmptyText

        #region DefaultValue

        public static readonly DependencyProperty DefaultValueProperty = DependencyProperty.Register("DefaultValue",
            typeof(T), typeof(UpDownBase<T>), new UIPropertyMetadata(default(T), OnDefaultValueChanged));

        public T DefaultValue
        {
            get => (T) GetValue(DefaultValueProperty);
            set => SetValue(DefaultValueProperty, value);
        }

        private static void OnDefaultValueChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            ((UpDownBase<T>) source).OnDefaultValueChanged((T) args.OldValue, (T) args.NewValue);
        }

        private void OnDefaultValueChanged(T oldValue, T newValue)
        {
            if (IsInitialized && string.IsNullOrEmpty(Text)) SyncTextAndValueProperties(true, Text);
        }

        #endregion //DefaultValue

        #region Maximum

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(T),
            typeof(UpDownBase<T>), new UIPropertyMetadata(default(T), OnMaximumChanged, OnCoerceMaximum));

        public T Maximum
        {
            get => (T) GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        private static void OnMaximumChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var upDown = o as UpDownBase<T>;
            if (upDown is not null)
                upDown.OnMaximumChanged((T) e.OldValue, (T) e.NewValue);
        }

        protected virtual void OnMaximumChanged(T oldValue, T newValue)
        {
            if (IsInitialized) SetValidSpinDirection();
        }

        private static object OnCoerceMaximum(DependencyObject d, object baseValue)
        {
            var upDown = d as UpDownBase<T>;
            if (upDown is not null)
                return upDown.OnCoerceMaximum((T) baseValue);

            return baseValue;
        }

        protected virtual T OnCoerceMaximum(T baseValue)
        {
            return baseValue;
        }

        #endregion //Maximum

        #region Minimum

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(T),
            typeof(UpDownBase<T>), new UIPropertyMetadata(default(T), OnMinimumChanged, OnCoerceMinimum));

        public T Minimum
        {
            get => (T) GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        private static void OnMinimumChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var upDown = o as UpDownBase<T>;
            if (upDown is not null)
                upDown.OnMinimumChanged((T) e.OldValue, (T) e.NewValue);
        }

        protected virtual void OnMinimumChanged(T oldValue, T newValue)
        {
            if (IsInitialized) SetValidSpinDirection();
        }

        private static object OnCoerceMinimum(DependencyObject d, object baseValue)
        {
            var upDown = d as UpDownBase<T>;
            if (upDown is not null)
                return upDown.OnCoerceMinimum((T) baseValue);

            return baseValue;
        }

        protected virtual T OnCoerceMinimum(T baseValue)
        {
            return baseValue;
        }

        #endregion //Minimum

        #region MouseWheelActiveTrigger

        /// <summary>
        ///     Identifies the MouseWheelActiveTrigger dependency property
        /// </summary>
        public static readonly DependencyProperty MouseWheelActiveTriggerProperty =
            DependencyProperty.Register("MouseWheelActiveTrigger", typeof(MouseWheelActiveTrigger),
                typeof(UpDownBase<T>), new UIPropertyMetadata(MouseWheelActiveTrigger.Focused));

        /// <summary>
        ///     Get or set when the mouse wheel event should affect the value.
        /// </summary>
        public MouseWheelActiveTrigger MouseWheelActiveTrigger
        {
            get => (MouseWheelActiveTrigger) GetValue(MouseWheelActiveTriggerProperty);
            set => SetValue(MouseWheelActiveTriggerProperty, value);
        }

        #endregion //MouseWheelActiveTrigger

        #region MouseWheelActiveOnFocus

        [Obsolete("Use MouseWheelActiveTrigger property instead")]
        public static readonly DependencyProperty MouseWheelActiveOnFocusProperty =
            DependencyProperty.Register("MouseWheelActiveOnFocus", typeof(bool), typeof(UpDownBase<T>),
                new UIPropertyMetadata(true, OnMouseWheelActiveOnFocusChanged));

        [Obsolete("Use MouseWheelActiveTrigger property instead")]
        public bool MouseWheelActiveOnFocus
        {
            get
            {
#pragma warning disable 618
                return (bool) GetValue(MouseWheelActiveOnFocusProperty);
#pragma warning restore 618
            }
            set
            {
#pragma warning disable 618
                SetValue(MouseWheelActiveOnFocusProperty, value);
#pragma warning restore 618
            }
        }

        private static void OnMouseWheelActiveOnFocusChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var upDownBase = o as UpDownBase<T>;
            if (upDownBase is not null)
                upDownBase.MouseWheelActiveTrigger = (bool) e.NewValue
                    ? MouseWheelActiveTrigger.Focused
                    : MouseWheelActiveTrigger.MouseOver;
        }

        #endregion //MouseWheelActiveOnFocus

        #region ShowButtonSpinner

        public static readonly DependencyProperty ShowButtonSpinnerProperty =
            DependencyProperty.Register("ShowButtonSpinner", typeof(bool), typeof(UpDownBase<T>),
                new UIPropertyMetadata(true));

        public bool ShowButtonSpinner
        {
            get => (bool) GetValue(ShowButtonSpinnerProperty);
            set => SetValue(ShowButtonSpinnerProperty, value);
        }

        #endregion //ShowButtonSpinner

        #region Value

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(T),
            typeof(UpDownBase<T>),
            new FrameworkPropertyMetadata(default(T), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged, OnCoerceValue, false, UpdateSourceTrigger.PropertyChanged));

        public T Value
        {
            get => (T) GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        private static object OnCoerceValue(DependencyObject o, object basevalue)
        {
            return ((UpDownBase<T>) o).OnCoerceValue(basevalue);
        }

        protected virtual object OnCoerceValue(object newValue)
        {
            return newValue;
        }

        private static void OnValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var upDownBase = o as UpDownBase<T>;
            if (upDownBase is not null)
                upDownBase.OnValueChanged((T) e.OldValue, (T) e.NewValue);
        }

        protected virtual void OnValueChanged(T oldValue, T newValue)
        {
            if (IsInitialized) SyncTextAndValueProperties(false, null);

            SetValidSpinDirection();

            RaiseValueChangedEvent(oldValue, newValue);
        }

        #endregion //Value

        #endregion //Properties

        #region Base Class Overrides

        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            if (TextBox is not null)
                TextBox.Focus();

            base.OnAccessKey(e);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            TextBox = GetTemplateChild(PART_TextBox) as TextBox;
            if (TextBox is not null)
            {
                TextBox.Text = Text;
                TextBox.LostFocus += TextBox_LostFocus;
                TextBox.TextChanged += TextBox_TextChanged;
            }

            if (Spinner is not null)
                Spinner.Spin -= OnSpinnerSpin;

            Spinner = GetTemplateChild(PART_Spinner) as Spinner;

            if (Spinner is not null)
                Spinner.Spin += OnSpinnerSpin;

            SetValidSpinDirection();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                {
                    // Commit Text on "Enter" to raise Error event 
                    var commitSuccess = CommitInput();
                    //Only handle if an exception is detected (Commit fails)
                    e.Handled = !commitSuccess;
                    break;
                }
            }
        }

        protected override void OnTextChanged(string oldValue, string newValue)
        {
            if (IsInitialized) SyncTextAndValueProperties(true, Text);
        }

        protected override void OnCultureInfoChanged(CultureInfo oldValue, CultureInfo newValue)
        {
            if (IsInitialized) SyncTextAndValueProperties(false, null);
        }

        protected override void OnReadOnlyChanged(bool oldValue, bool newValue)
        {
            SetValidSpinDirection();
        }

        #endregion //Base Class Overrides

        #region Events

        public event InputValidationErrorEventHandler InputValidationError;

        #region ValueChanged Event

        //Due to a bug in Visual Studio, you cannot create event handlers for generic T args in XAML, so I have to use object instead.
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object>), typeof(UpDownBase<T>));

        public event RoutedPropertyChangedEventHandler<object> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        #endregion

        #endregion //Events

        #region Methods

        protected virtual void OnSpin(SpinEventArgs e)
        {
            if (e is null)
                throw new ArgumentNullException("e");

            if (e.Direction == SpinDirection.Increase)
                DoIncrement();
            else
                DoDecrement();
        }

        protected virtual void RaiseValueChangedEvent(T oldValue, T newValue)
        {
            var args = new RoutedPropertyChangedEventArgs<object>(oldValue, newValue);
            args.RoutedEvent = ValueChangedEvent;
            RaiseEvent(args);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            // When both Value and Text are initialized, Value has priority.
            // To be sure that the value is not initialized, it should
            // have no local value, no binding, and equal to the default value.
            var updateValueFromText =
                ReadLocalValue(ValueProperty) == DependencyProperty.UnsetValue
                && BindingOperations.GetBinding(this, ValueProperty) is null
                && Equals(Value, ValueProperty.DefaultMetadata.DefaultValue);

            SyncTextAndValueProperties(updateValueFromText, Text, !updateValueFromText);
        }

        /// <summary>
        ///     Performs an increment if conditions allow it.
        /// </summary>
        internal void DoDecrement()
        {
            if (Spinner is null || (Spinner.ValidSpinDirection & ValidSpinDirections.Decrease) ==
                ValidSpinDirections.Decrease)
            {
                OnDecrement();

                var ex = BindingOperations.GetBindingExpressionBase(this, ValueProperty);
                if (ex is not null) ex.UpdateSource();
            }
        }

        /// <summary>
        ///     Performs a decrement if conditions allow it.
        /// </summary>
        internal void DoIncrement()
        {
            if (Spinner is null || (Spinner.ValidSpinDirection & ValidSpinDirections.Increase) ==
                ValidSpinDirections.Increase)
            {
                OnIncrement();

                var ex = BindingOperations.GetBindingExpressionBase(this, ValueProperty);
                if (ex is not null) ex.UpdateSource();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsKeyboardFocusWithin)
                return;

            try
            {
                _isTextChangedFromUI = true;
                Text = ((TextBox) sender).Text;
            }
            finally
            {
                _isTextChangedFromUI = false;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitInput();
        }

        private void RaiseInputValidationError(Exception e)
        {
            if (InputValidationError is not null)
            {
                var args = new InputValidationErrorEventArgs(e);
                InputValidationError(this, args);
                if (args.ThrowException) throw args.Exception;
            }
        }

        public bool CommitInput()
        {
            return SyncTextAndValueProperties(true, Text);
        }

        protected bool SyncTextAndValueProperties(bool updateValueFromText, string text)
        {
            return SyncTextAndValueProperties(updateValueFromText, text, false);
        }

        private bool SyncTextAndValueProperties(bool updateValueFromText, string text, bool forceTextUpdate)
        {
            if (_isSyncingTextAndValueProperties)
                return true;

            _isSyncingTextAndValueProperties = true;
            var parsedTextIsValid = true;
            try
            {
                if (updateValueFromText)
                {
                    if (string.IsNullOrEmpty(text))
                        // An empty input sets the value to the default value.
                        Value = DefaultValue;
                    else
                        try
                        {
                            Value = ConvertTextToValue(text);
                        }
                        catch (Exception e)
                        {
                            parsedTextIsValid = false;

                            // From the UI, just allow any input.
                            if (!_isTextChangedFromUI)
                                // This call may throw an exception. 
                                // See RaiseInputValidationError() implementation.
                                RaiseInputValidationError(e);
                        }
                }

                // Do not touch the ongoing text input from user.
                if (!_isTextChangedFromUI)
                {
                    // Don't replace the empty Text with the non-empty representation of DefaultValue.
                    var shouldKeepEmpty = !forceTextUpdate && string.IsNullOrEmpty(Text) &&
                                          Equals(Value, DefaultValue) && !DisplayDefaultValueOnEmptyText;
                    if (!shouldKeepEmpty) Text = ConvertValueToText();

                    // Sync Text and textBox
                    if (TextBox is not null)
                        TextBox.Text = Text;
                }

                if (_isTextChangedFromUI && !parsedTextIsValid)
                {
                    // Text input was made from the user and the text
                    // repesents an invalid value. Disable the spinner
                    // in this case.
                    if (Spinner is not null) Spinner.ValidSpinDirection = ValidSpinDirections.None;
                }
                else
                {
                    SetValidSpinDirection();
                }
            }
            finally
            {
                _isSyncingTextAndValueProperties = false;
            }

            return parsedTextIsValid;
        }

        #region Abstract

        /// <summary>
        ///     Converts the formatted text to a value.
        /// </summary>
        protected abstract T ConvertTextToValue(string text);

        /// <summary>
        ///     Converts the value to formatted text.
        /// </summary>
        /// <returns></returns>
        protected abstract string ConvertValueToText();

        /// <summary>
        ///     Called by OnSpin when the spin direction is SpinDirection.Increase.
        /// </summary>
        protected abstract void OnIncrement();

        /// <summary>
        ///     Called by OnSpin when the spin direction is SpinDirection.Descrease.
        /// </summary>
        protected abstract void OnDecrement();

        /// <summary>
        ///     Sets the valid spin directions.
        /// </summary>
        protected abstract void SetValidSpinDirection();

        #endregion //Abstract

        #endregion //Methods
    }
}