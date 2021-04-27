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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Core;

namespace Ssz.Xceed.Wpf.Toolkit.Primitives
{
    public class ValueRangeTextBox : AutoSelectTextBox
    {
        private BitVector32 m_flags;
        private CachedTextInfo m_imePreCompositionCachedTextInfo;

        static ValueRangeTextBox()
        {
            TextProperty.OverrideMetadata(typeof(ValueRangeTextBox),
                new FrameworkPropertyMetadata(
                    null,
                    TextCoerceValueCallback));

            AcceptsReturnProperty.OverrideMetadata(typeof(ValueRangeTextBox),
                new FrameworkPropertyMetadata(
                    false, null, AcceptsReturnCoerceValueCallback));

            AcceptsTabProperty.OverrideMetadata(typeof(ValueRangeTextBox),
                new FrameworkPropertyMetadata(
                    false, null, AcceptsTabCoerceValueCallback));

            AutomationProperties.AutomationIdProperty.OverrideMetadata(typeof(ValueRangeTextBox),
                new UIPropertyMetadata("ValueRangeTextBox"));
        }

        #region IsInValueChanged property

        internal bool IsInValueChanged
        {
            get => m_flags[(int) ValueRangeTextBoxFlags.IsInValueChanged];
            private set => m_flags[(int) ValueRangeTextBoxFlags.IsInValueChanged] = value;
        }

        #endregion

        #region IsForcingValue property

        internal bool IsForcingValue
        {
            get => m_flags[(int) ValueRangeTextBoxFlags.IsForcingValue];
            private set => m_flags[(int) ValueRangeTextBoxFlags.IsForcingValue] = value;
        }

        #endregion

        #region IsForcingText property

        internal bool IsForcingText
        {
            get => m_flags[(int) ValueRangeTextBoxFlags.IsForcingText];
            private set => m_flags[(int) ValueRangeTextBoxFlags.IsForcingText] = value;
        }

        #endregion

        #region IsNumericValueDataType property

        internal bool IsNumericValueDataType
        {
            get => m_flags[(int) ValueRangeTextBoxFlags.IsNumericValueDataType];
            private set => m_flags[(int) ValueRangeTextBoxFlags.IsNumericValueDataType] = value;
        }

        #endregion

        #region IsTextReadyToBeParsed property

        internal virtual bool IsTextReadyToBeParsed => true;

        #endregion

        #region IsInIMEComposition property

        internal bool IsInIMEComposition => m_imePreCompositionCachedTextInfo != null;

        #endregion

        #region IsFinalizingInitialization Property

        private bool IsFinalizingInitialization
        {
            get => m_flags[(int) ValueRangeTextBoxFlags.IsFinalizingInitialization];
            set => m_flags[(int) ValueRangeTextBoxFlags.IsFinalizingInitialization] = value;
        }

        #endregion

        #region AcceptsReturn Property

        private static object AcceptsReturnCoerceValueCallback(DependencyObject sender, object value)
        {
            var acceptsReturn = (bool) value;

            if (acceptsReturn)
                throw new NotSupportedException("The ValueRangeTextBox does not support the AcceptsReturn property.");

            return false;
        }

        #endregion AcceptsReturn Property

        #region AcceptsTab Property

        private static object AcceptsTabCoerceValueCallback(DependencyObject sender, object value)
        {
            var acceptsTab = (bool) value;

            if (acceptsTab)
                throw new NotSupportedException("The ValueRangeTextBox does not support the AcceptsTab property.");

            return false;
        }

        #endregion AcceptsTab Property

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.ImeProcessedKey != Key.None && !IsInIMEComposition)
                // Start of an IME Composition.  Cache all the critical infos.
                StartIMEComposition();

            base.OnPreviewKeyDown(e);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            RefreshCurrentText(true);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            RefreshCurrentText(true);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            if (IsInIMEComposition)
                EndIMEComposition();

            base.OnTextInput(e);
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        protected virtual void ValidateValue(object value)
        {
            if (value == null)
                return;

            var validatingType = ValueDataType;

            if (validatingType == null)
                throw new InvalidOperationException(
                    "An attempt was made to set a value when the ValueDataType property is null.");

            if (value != DBNull.Value && value.GetType() != validatingType)
                throw new ArgumentException("The value is not of type " + validatingType.Name + ".", "Value");

            ValidateValueInRange(MinValue, MaxValue, value);
        }

        internal static bool IsNumericType(Type type)
        {
            if (type == null)
                return false;

            if (type.IsValueType)
                if (type == typeof(int) || type == typeof(double) || type == typeof(decimal)
                    || type == typeof(float) || type == typeof(short) || type == typeof(long)
                    || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong)
                    || type == typeof(byte)
                )
                    return true;

            return false;
        }

        internal void StartIMEComposition()
        {
            Debug.Assert(m_imePreCompositionCachedTextInfo == null,
                "EndIMEComposition should have been called before another IME Composition starts.");

            m_imePreCompositionCachedTextInfo = new CachedTextInfo(this);
        }

        internal void EndIMEComposition()
        {
            var cachedTextInfo = m_imePreCompositionCachedTextInfo.Clone() as CachedTextInfo;
            m_imePreCompositionCachedTextInfo = null;

            OnIMECompositionEnded(cachedTextInfo);
        }

        internal virtual void OnIMECompositionEnded(CachedTextInfo cachedTextInfo)
        {
        }

        internal virtual void RefreshConversionHelpers()
        {
        }

        internal IFormatProvider GetActiveFormatProvider()
        {
            var formatProvider = FormatProvider;

            if (formatProvider != null)
                return formatProvider;

            return CultureInfo.CurrentCulture;
        }

        internal CultureInfo GetCultureInfo()
        {
            var cultureInfo = GetActiveFormatProvider() as CultureInfo;

            if (cultureInfo != null)
                return cultureInfo;

            return CultureInfo.CurrentCulture;
        }

        internal virtual string GetCurrentText()
        {
            return Text;
        }

        internal virtual string GetParsableText()
        {
            return Text;
        }

        internal void ForceText(string text, bool preserveCaret)
        {
            IsForcingText = true;
            try
            {
                var oldCaretIndex = CaretIndex;

                Text = text;

                if (preserveCaret && IsLoaded)
                    try
                    {
                        SelectionStart = oldCaretIndex;
                    }
                    catch (NullReferenceException)
                    {
                    }
            }
            finally
            {
                IsForcingText = false;
            }
        }

        internal bool IsValueNull(object value)
        {
            if (value == null || value == DBNull.Value)
                return true;

            var type = ValueDataType;

            if (value.GetType() != type)
                value = Convert.ChangeType(value, type);

            var nullValue = NullValue;

            if (nullValue == null)
                return false;

            if (nullValue.GetType() != type)
                nullValue = Convert.ChangeType(nullValue, type);

            return nullValue.Equals(value);
        }

        internal void ForceValue(object value)
        {
            IsForcingValue = true;
            try
            {
                Value = value;
            }
            finally
            {
                IsForcingValue = false;
            }
        }

        internal void RefreshCurrentText(bool preserveCurrentCaretPosition)
        {
            var displayText = GetCurrentText();

            if (displayText != Text)
                ForceText(displayText, preserveCurrentCaretPosition);
        }

        internal void RefreshValue()
        {
            if (IsForcingValue || ValueDataType == null || IsInIMEComposition)
                return;

            object value;
            bool hasParsingError;

            if (IsTextReadyToBeParsed)
            {
                var parsableText = GetParsableText();

                value = GetValueFromText(parsableText, out hasParsingError);

                if (IsValueNull(value))
                    value = NullValue;
            }
            else
            {
                // We don't consider empty text as a parsing error.
                hasParsingError = !GetIsEditTextEmpty();
                value = NullValue;
            }

            SetHasParsingError(hasParsingError);

            var hasValidationError = hasParsingError;
            try
            {
                ValidateValue(value);

                SetIsValueOutOfRange(false);
            }
            catch (Exception exception)
            {
                hasValidationError = true;

                if (exception is ArgumentOutOfRangeException)
                    SetIsValueOutOfRange(true);

                value = NullValue;
            }

            if (!Equals(value, Value))
                ForceValue(value);

            SetHasValidationError(hasValidationError);
        }

        internal virtual bool GetIsEditTextEmpty()
        {
            return Text == string.Empty;
        }

        private static object ConvertValueToDataType(object value, Type type)
        {
            // We use InvariantCulture instead of the active format provider since the FormatProvider is only
            // used when the source type is String.  When we are converting from a string, we are
            // actually converting a value from XAML.  Therefore, if the string will have a period as a
            // decimal separator.  If we were using the active format provider, we could end up expecting a coma
            // as the decimal separator and the ChangeType method would throw.
            if (type == null)
                return null;

            if (value != null && value != DBNull.Value
                              && value.GetType() != type)
                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            return value;
        }

        private void CanEnterLineBreak(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        private void CanEnterParagraphBreak(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        private void ValidateValueInRange(object minValue, object maxValue, object value)
        {
            if (IsValueNull(value))
                return;

            var type = ValueDataType;

            if (value.GetType() != type)
                value = Convert.ChangeType(value, type);

            // Validate the value against the range.
            if (minValue != null)
            {
                var minValueComparable = (IComparable) minValue;

                if (maxValue != null && minValueComparable.CompareTo(maxValue) > 0)
                    throw new ArgumentOutOfRangeException("minValue", "MaxValue must be greater than MinValue.");

                if (minValueComparable.CompareTo(value) > 0)
                    throw new ArgumentOutOfRangeException("minValue", "Value must be greater than MinValue.");
            }

            if (maxValue != null)
            {
                var maxValueComparable = (IComparable) maxValue;

                if (maxValueComparable.CompareTo(value) < 0)
                    throw new ArgumentOutOfRangeException("maxValue", "Value must be less than MaxValue.");
            }
        }

        #region ISupportInitialize

        protected override void OnInitialized(EventArgs e)
        {
            IsFinalizingInitialization = true;
            try
            {
                CoerceValue(ValueDataTypeProperty);

                IsNumericValueDataType = IsNumericType(ValueDataType);
                RefreshConversionHelpers();

                CoerceValue(MinValueProperty);
                CoerceValue(MaxValueProperty);

                CoerceValue(ValueProperty);

                CoerceValue(NullValueProperty);

                CoerceValue(TextProperty);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Initialization of the ValueRangeTextBox failed.", exception);
            }
            finally
            {
                IsFinalizingInitialization = false;
            }

            base.OnInitialized(e);
        }

        #endregion ISupportInitialize

        [Flags]
        private enum ValueRangeTextBoxFlags
        {
            IsFinalizingInitialization = 1,
            IsForcingText = 2,
            IsForcingValue = 4,
            IsInValueChanged = 8,
            IsNumericValueDataType = 16
        }

        #region BeepOnError Property

        public bool BeepOnError
        {
            get => (bool) GetValue(BeepOnErrorProperty);
            set => SetValue(BeepOnErrorProperty, value);
        }

        public static readonly DependencyProperty BeepOnErrorProperty =
            DependencyProperty.Register("BeepOnError", typeof(bool), typeof(ValueRangeTextBox),
                new UIPropertyMetadata(false));

        #endregion BeepOnError Property

        #region FormatProvider Property

        public IFormatProvider FormatProvider
        {
            get => (IFormatProvider) GetValue(FormatProviderProperty);
            set => SetValue(FormatProviderProperty, value);
        }

        public static readonly DependencyProperty FormatProviderProperty =
            DependencyProperty.Register("FormatProvider", typeof(IFormatProvider), typeof(ValueRangeTextBox),
                new UIPropertyMetadata(null,
                    FormatProviderPropertyChangedCallback));

        private static void FormatProviderPropertyChangedCallback(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            var valueRangeTextBox = (ValueRangeTextBox) sender;

            if (!valueRangeTextBox.IsInitialized)
                return;

            valueRangeTextBox.OnFormatProviderChanged();
        }

        internal virtual void OnFormatProviderChanged()
        {
            RefreshConversionHelpers();
            RefreshCurrentText(false);
            RefreshValue();
        }

        #endregion FormatProvider Property

        #region MinValue Property

        public object MinValue
        {
            get => GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(object), typeof(ValueRangeTextBox),
                new UIPropertyMetadata(
                    null,
                    null,
                    MinValueCoerceValueCallback));

        private static object MinValueCoerceValueCallback(DependencyObject sender, object value)
        {
            var valueRangeTextBox = sender as ValueRangeTextBox;

            if (!valueRangeTextBox.IsInitialized)
                return DependencyProperty.UnsetValue;

            if (value == null)
                return value;

            var type = valueRangeTextBox.ValueDataType;

            if (type == null)
                throw new InvalidOperationException(
                    "An attempt was made to set a minimum value when the ValueDataType property is null.");

            if (valueRangeTextBox.IsFinalizingInitialization)
                value = ConvertValueToDataType(value, valueRangeTextBox.ValueDataType);

            if (value.GetType() != type)
                throw new ArgumentException("The value is not of type " + type.Name + ".", "MinValue");

            var comparable = value as IComparable;

            if (comparable == null)
                throw new InvalidOperationException("MinValue does not implement the IComparable interface.");

            // ValidateValueInRange will throw if it must.
            var maxValue = valueRangeTextBox.MaxValue;

            valueRangeTextBox.ValidateValueInRange(value, maxValue, valueRangeTextBox.Value);

            return value;
        }

        #endregion MinValue Property

        #region MaxValue Property

        public object MaxValue
        {
            get => GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(object), typeof(ValueRangeTextBox),
                new UIPropertyMetadata(
                    null,
                    null,
                    MaxValueCoerceValueCallback));

        private static object MaxValueCoerceValueCallback(DependencyObject sender, object value)
        {
            var valueRangeTextBox = sender as ValueRangeTextBox;

            if (!valueRangeTextBox.IsInitialized)
                return DependencyProperty.UnsetValue;

            if (value == null)
                return value;

            var type = valueRangeTextBox.ValueDataType;

            if (type == null)
                throw new InvalidOperationException(
                    "An attempt was made to set a maximum value when the ValueDataType property is null.");

            if (valueRangeTextBox.IsFinalizingInitialization)
                value = ConvertValueToDataType(value, valueRangeTextBox.ValueDataType);

            if (value.GetType() != type)
                throw new ArgumentException("The value is not of type " + type.Name + ".", "MinValue");

            var comparable = value as IComparable;

            if (comparable == null)
                throw new InvalidOperationException("MaxValue does not implement the IComparable interface.");

            var minValue = valueRangeTextBox.MinValue;

            // ValidateValueInRange will throw if it must.
            valueRangeTextBox.ValidateValueInRange(minValue, value, valueRangeTextBox.Value);

            return value;
        }

        #endregion MaxValue Property

        #region NullValue Property

        public object NullValue
        {
            get => GetValue(NullValueProperty);
            set => SetValue(NullValueProperty, value);
        }

        public static readonly DependencyProperty NullValueProperty =
            DependencyProperty.Register("NullValue", typeof(object), typeof(ValueRangeTextBox),
                new UIPropertyMetadata(
                    null,
                    NullValuePropertyChangedCallback,
                    NullValueCoerceValueCallback));

        private static object NullValueCoerceValueCallback(DependencyObject sender, object value)
        {
            var valueRangeTextBox = sender as ValueRangeTextBox;

            if (!valueRangeTextBox.IsInitialized)
                return DependencyProperty.UnsetValue;

            if (value == null || value == DBNull.Value)
                return value;

            var type = valueRangeTextBox.ValueDataType;

            if (type == null)
                throw new InvalidOperationException(
                    "An attempt was made to set a null value when the ValueDataType property is null.");

            if (valueRangeTextBox.IsFinalizingInitialization)
                value = ConvertValueToDataType(value, valueRangeTextBox.ValueDataType);

            if (value.GetType() != type)
                throw new ArgumentException("The value is not of type " + type.Name + ".", "NullValue");

            return value;
        }

        private static void NullValuePropertyChangedCallback(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            var valueRangeTextBox = sender as ValueRangeTextBox;

            if (e.OldValue == null)
            {
                if (valueRangeTextBox.Value == null)
                    valueRangeTextBox.RefreshValue();
            }
            else
            {
                if (e.OldValue.Equals(valueRangeTextBox.Value))
                    valueRangeTextBox.RefreshValue();
            }
        }

        #endregion NullValue Property

        #region Value Property

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(ValueRangeTextBox),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    ValuePropertyChangedCallback,
                    ValueCoerceValueCallback));

        private static object ValueCoerceValueCallback(object sender, object value)
        {
            var valueRangeTextBox = sender as ValueRangeTextBox;

            if (!valueRangeTextBox.IsInitialized)
                return DependencyProperty.UnsetValue;

            if (valueRangeTextBox.IsFinalizingInitialization)
                value = ConvertValueToDataType(value, valueRangeTextBox.ValueDataType);

            if (!valueRangeTextBox.IsForcingValue)
                valueRangeTextBox.ValidateValue(value);

            return value;
        }

        private static void ValuePropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            var valueRangeTextBox = sender as ValueRangeTextBox;

            if (valueRangeTextBox.IsForcingValue)
                return;

            // The ValueChangedCallback can be raised even though both values are the same since the property
            // datatype is Object.
            if (Equals(e.NewValue, e.OldValue))
                return;

            valueRangeTextBox.IsInValueChanged = true;
            try
            {
                valueRangeTextBox.Text = valueRangeTextBox.GetTextFromValue(e.NewValue);
            }
            finally
            {
                valueRangeTextBox.IsInValueChanged = false;
            }
        }

        #endregion Value Property

        #region ValueDataType Property

        public Type ValueDataType
        {
            get => (Type) GetValue(ValueDataTypeProperty);
            set => SetValue(ValueDataTypeProperty, value);
        }

        public static readonly DependencyProperty ValueDataTypeProperty =
            DependencyProperty.Register("ValueDataType", typeof(Type), typeof(ValueRangeTextBox),
                new UIPropertyMetadata(
                    null,
                    ValueDataTypePropertyChangedCallback,
                    ValueDataTypeCoerceValueCallback));

        private static object ValueDataTypeCoerceValueCallback(DependencyObject sender, object value)
        {
            var valueRangeTextBox = sender as ValueRangeTextBox;

            if (!valueRangeTextBox.IsInitialized)
                return DependencyProperty.UnsetValue;

            var valueDataType = value as Type;

            try
            {
                valueRangeTextBox.ValidateDataType(valueDataType);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("An error occured while trying to change the ValueDataType.", exception);
            }

            return value;
        }

        private static void ValueDataTypePropertyChangedCallback(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            var valueRangeTextBox = sender as ValueRangeTextBox;

            var valueDataType = e.NewValue as Type;

            valueRangeTextBox.IsNumericValueDataType = IsNumericType(valueDataType);

            valueRangeTextBox.RefreshConversionHelpers();

            valueRangeTextBox.ConvertValuesToDataType(valueDataType);
        }

        internal virtual void ValidateDataType(Type type)
        {
            // Null will always be valid and will reset the MinValue, MaxValue, NullValue and Value to null.
            if (type == null)
                return;

            // We use InvariantCulture instead of the active format provider since the FormatProvider is only
            // used when the source type is String.  When we are converting from a string, we are
            // actually converting a value from XAML.  Therefore, if the string will have a period as a
            // decimal separator.  If we were using the active format provider, we could end up expecting a coma
            // as the decimal separator and the ChangeType method would throw.

            var minValue = MinValue;

            if (minValue != null && minValue.GetType() != type)
                minValue = Convert.ChangeType(minValue, type, CultureInfo.InvariantCulture);

            var maxValue = MaxValue;

            if (maxValue != null && maxValue.GetType() != type)
                maxValue = Convert.ChangeType(maxValue, type, CultureInfo.InvariantCulture);

            var nullValue = NullValue;

            if (nullValue != null && nullValue != DBNull.Value
                                  && nullValue.GetType() != type)
                nullValue = Convert.ChangeType(nullValue, type, CultureInfo.InvariantCulture);

            var value = Value;

            if (value != null && value != DBNull.Value
                              && value.GetType() != type)
                value = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            if (minValue != null || maxValue != null
                                 || nullValue != null && nullValue != DBNull.Value)
            {
                // Value comparaisons will occur.  Therefore, the aspiring data type must implement IComparable.

                var iComparable = type.GetInterface("IComparable");

                if (iComparable == null)
                    throw new InvalidOperationException(
                        "MinValue, MaxValue, and NullValue must implement the IComparable interface.");
            }
        }

        private void ConvertValuesToDataType(Type type)
        {
            if (type == null)
            {
                MinValue = null;
                MaxValue = null;
                NullValue = null;

                Value = null;

                return;
            }

            var minValue = MinValue;

            if (minValue != null && minValue.GetType() != type)
                MinValue = ConvertValueToDataType(minValue, type);

            var maxValue = MaxValue;

            if (maxValue != null && maxValue.GetType() != type)
                MaxValue = ConvertValueToDataType(maxValue, type);

            var nullValue = NullValue;

            if (nullValue != null && nullValue != DBNull.Value
                                  && nullValue.GetType() != type)
                NullValue = ConvertValueToDataType(nullValue, type);

            var value = Value;

            if (value != null && value != DBNull.Value
                              && value.GetType() != type)
                Value = ConvertValueToDataType(value, type);
        }

        #endregion ValueDataType Property

        #region Text Property

        private static object TextCoerceValueCallback(object sender, object value)
        {
            var valueRangeTextBox = sender as ValueRangeTextBox;

            if (!valueRangeTextBox.IsInitialized)
                return DependencyProperty.UnsetValue;

            if (value == null)
                return string.Empty;

            return value;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            // If in IME Composition, RefreshValue already returns without doing anything.
            RefreshValue();

            base.OnTextChanged(e);
        }

        #endregion Text Property

        #region HasValidationError Property

        private static readonly DependencyPropertyKey HasValidationErrorPropertyKey =
            DependencyProperty.RegisterReadOnly("HasValidationError", typeof(bool), typeof(ValueRangeTextBox),
                new UIPropertyMetadata(false));

        public static readonly DependencyProperty HasValidationErrorProperty =
            HasValidationErrorPropertyKey.DependencyProperty;

        public bool HasValidationError => (bool) GetValue(HasValidationErrorProperty);

        private void SetHasValidationError(bool value)
        {
            SetValue(HasValidationErrorPropertyKey, value);
        }

        #endregion HasValidationError Property

        #region HasParsingError Property

        private static readonly DependencyPropertyKey HasParsingErrorPropertyKey =
            DependencyProperty.RegisterReadOnly("HasParsingError", typeof(bool), typeof(ValueRangeTextBox),
                new UIPropertyMetadata(false));

        public static readonly DependencyProperty HasParsingErrorProperty =
            HasParsingErrorPropertyKey.DependencyProperty;

        public bool HasParsingError => (bool) GetValue(HasParsingErrorProperty);

        internal void SetHasParsingError(bool value)
        {
            SetValue(HasParsingErrorPropertyKey, value);
        }

        #endregion HasParsingError Property

        #region IsValueOutOfRange Property

        private static readonly DependencyPropertyKey IsValueOutOfRangePropertyKey =
            DependencyProperty.RegisterReadOnly("IsValueOutOfRange", typeof(bool), typeof(ValueRangeTextBox),
                new UIPropertyMetadata(false));

        public static readonly DependencyProperty IsValueOutOfRangeProperty =
            IsValueOutOfRangePropertyKey.DependencyProperty;

        public bool IsValueOutOfRange => (bool) GetValue(IsValueOutOfRangeProperty);

        private void SetIsValueOutOfRange(bool value)
        {
            SetValue(IsValueOutOfRangePropertyKey, value);
        }

        #endregion IsValueOutOfRange Property

        #region TEXT FROM VALUE

        public event EventHandler<QueryTextFromValueEventArgs> QueryTextFromValue;

        internal string GetTextFromValue(object value)
        {
            var text = QueryTextFromValueCore(value);

            var e = new QueryTextFromValueEventArgs(value, text);

            OnQueryTextFromValue(e);

            return e.Text;
        }

        protected virtual string QueryTextFromValueCore(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            var formatProvider = GetActiveFormatProvider();

            var cultureInfo = formatProvider as CultureInfo;

            if (cultureInfo != null)
            {
                var converter = TypeDescriptor.GetConverter(value.GetType());

                if (converter.CanConvertTo(typeof(string)))
                    return (string) converter.ConvertTo(null, cultureInfo, value, typeof(string));
            }

            try
            {
                var result = Convert.ToString(value, formatProvider);

                return result;
            }
            catch
            {
            }

            return value.ToString();
        }

        private void OnQueryTextFromValue(QueryTextFromValueEventArgs e)
        {
            if (QueryTextFromValue != null)
                QueryTextFromValue(this, e);
        }

        #endregion TEXT FROM VALUE

        #region VALUE FROM TEXT

        public event EventHandler<QueryValueFromTextEventArgs> QueryValueFromText;

        internal object GetValueFromText(string text, out bool hasParsingError)
        {
            object value = null;
            var success = QueryValueFromTextCore(text, out value);

            var e = new QueryValueFromTextEventArgs(text, value);
            e.HasParsingError = !success;

            OnQueryValueFromText(e);

            hasParsingError = e.HasParsingError;

            return e.Value;
        }

        protected virtual bool QueryValueFromTextCore(string text, out object value)
        {
            value = null;

            var validatingType = ValueDataType;

            text = text.Trim();

            if (validatingType == null)
                return true;

            if (!validatingType.IsValueType && validatingType != typeof(string))
                return false;

            try
            {
                value = Convert.ChangeType(text, validatingType, GetActiveFormatProvider());
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void OnQueryValueFromText(QueryValueFromTextEventArgs e)
        {
            if (QueryValueFromText != null)
                QueryValueFromText(this, e);
        }

        #endregion VALUE FROM TEXT
    }
}