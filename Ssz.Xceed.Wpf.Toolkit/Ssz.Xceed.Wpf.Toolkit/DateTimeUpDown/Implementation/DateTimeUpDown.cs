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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Primitives;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class DateTimeUpDown : UpDownBase<DateTime?>
    {
        #region Members

        private readonly List<DateTimeInfo> _dateTimeInfoList = new();
        private DateTimeInfo _selectedDateTimeInfo;
        private bool _fireSelectionChangedEvent = true;
        private bool _processTextChanged = true;

        #endregion //Members

        #region Properties

        #region Format

        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register("Format",
            typeof(DateTimeFormat), typeof(DateTimeUpDown),
            new UIPropertyMetadata(DateTimeFormat.FullDateTime, OnFormatChanged));

        public DateTimeFormat Format
        {
            get => (DateTimeFormat) GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        private static void OnFormatChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var dateTimeUpDown = o as DateTimeUpDown;
            if (dateTimeUpDown is not null)
                dateTimeUpDown.OnFormatChanged((DateTimeFormat) e.OldValue, (DateTimeFormat) e.NewValue);
        }

        protected virtual void OnFormatChanged(DateTimeFormat oldValue, DateTimeFormat newValue)
        {
            FormatUpdated();
        }

        #endregion //Format

        #region FormatString

        public static readonly DependencyProperty FormatStringProperty = DependencyProperty.Register("FormatString",
            typeof(string), typeof(DateTimeUpDown), new UIPropertyMetadata(default(string), OnFormatStringChanged),
            IsFormatStringValid);

        public string FormatString
        {
            get => (string) GetValue(FormatStringProperty);
            set => SetValue(FormatStringProperty, value);
        }

        internal static bool IsFormatStringValid(object value)
        {
            try
            {
                // Test the format string if it is used.
                DateTime.MinValue.ToString((string) value, CultureInfo.CurrentCulture);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static void OnFormatStringChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var dateTimeUpDown = o as DateTimeUpDown;
            if (dateTimeUpDown is not null)
                dateTimeUpDown.OnFormatStringChanged((string) e.OldValue, (string) e.NewValue);
        }

        protected virtual void OnFormatStringChanged(string oldValue, string newValue)
        {
            FormatUpdated();
        }

        #endregion //FormatString

        #endregion //Properties

        #region Constructors

        static DateTimeUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateTimeUpDown),
                new FrameworkPropertyMetadata(typeof(DateTimeUpDown)));
            MaximumProperty.OverrideMetadata(typeof(DateTimeUpDown), new FrameworkPropertyMetadata(DateTime.MaxValue));
            MinimumProperty.OverrideMetadata(typeof(DateTimeUpDown), new FrameworkPropertyMetadata(DateTime.MinValue));
        }

        public DateTimeUpDown()
        {
            InitializeDateTimeInfoList();
        }

        #endregion //Constructors

        #region Base Class Overrides

        public override void OnApplyTemplate()
        {
            if (TextBox is not null)
            {
                TextBox.SelectionChanged -= TextBox_SelectionChanged;
                TextBox.GotFocus -= TextBox_GotFocus;
            }

            base.OnApplyTemplate();

            if (TextBox is not null)
            {
                TextBox.SelectionChanged += TextBox_SelectionChanged;
                TextBox.GotFocus += TextBox_GotFocus;
            }
        }

        protected override void OnCultureInfoChanged(CultureInfo oldValue, CultureInfo newValue)
        {
            FormatUpdated();
        }

        protected override void OnIncrement()
        {
            if (IsCurrentValueValid())
            {
                if (Value.HasValue)
                    UpdateDateTime(1);
                else
                    Value = DefaultValue ?? DateTime.Now;
            }
        }

        protected override void OnDecrement()
        {
            if (IsCurrentValueValid())
            {
                if (Value.HasValue)
                    UpdateDateTime(-1);
                else
                    Value = DefaultValue ?? DateTime.Now;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            var selectionStart = _selectedDateTimeInfo is not null ? _selectedDateTimeInfo.StartPosition : 0;
            var selectionLength = _selectedDateTimeInfo is not null ? _selectedDateTimeInfo.Length : 0;

            switch (e.Key)
            {
                case Key.Enter:
                {
                    if (!IsReadOnly)
                    {
                        _fireSelectionChangedEvent = false;
                        var binding = BindingOperations.GetBindingExpression(TextBox, TextBox.TextProperty);
                        binding.UpdateSource();
                        _fireSelectionChangedEvent = true;
                    }

                    return;
                }
                case Key.Add:
                    if (AllowSpin && !IsReadOnly)
                    {
                        DoIncrement();
                        e.Handled = true;
                    }

                    _fireSelectionChangedEvent = false;
                    break;
                case Key.Subtract:
                    if (AllowSpin && !IsReadOnly)
                    {
                        DoDecrement();
                        e.Handled = true;
                    }

                    _fireSelectionChangedEvent = false;
                    break;
                case Key.OemSemicolon:
                    if (IsCurrentValueValid() && Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        PerformKeyboardSelection(selectionStart + selectionLength);
                        e.Handled = true;
                    }

                    _fireSelectionChangedEvent = false;
                    break;
                case Key.OemPeriod:
                case Key.OemComma:
                case Key.OemQuotes:
                case Key.OemMinus:
                case Key.Divide:
                case Key.Decimal:
                case Key.Right:
                    if (IsCurrentValueValid())
                    {
                        PerformKeyboardSelection(selectionStart + selectionLength);
                        e.Handled = true;
                    }

                    _fireSelectionChangedEvent = false;
                    break;
                case Key.Left:
                    if (IsCurrentValueValid())
                    {
                        PerformKeyboardSelection(selectionStart > 0 ? selectionStart - 1 : 0);
                        e.Handled = true;
                    }

                    _fireSelectionChangedEvent = false;
                    break;
                default:
                {
                    _fireSelectionChangedEvent = false;
                    break;
                }
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnTextChanged(string previousValue, string currentValue)
        {
            if (!_processTextChanged)
                return;

            base.OnTextChanged(previousValue, currentValue);
        }

        protected override DateTime? ConvertTextToValue(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            DateTime result;
            TryParseDateTime(text, out result);

            if (ClipValueToMinMax) return GetClippedMinMaxValue(result);

            ValidateDefaultMinMax(result);

            return result;
        }

        protected override string ConvertValueToText()
        {
            if (Value is null)
                return string.Empty;

            return Value.Value.ToString(GetFormatString(Format), CultureInfo);
        }

        protected override void SetValidSpinDirection()
        {
            var validDirections = ValidSpinDirections.None;

            if (!IsReadOnly)
            {
                if (IsLowerThan(Value, Maximum) || !Value.HasValue)
                    validDirections = validDirections | ValidSpinDirections.Increase;

                if (IsGreaterThan(Value, Minimum) || !Value.HasValue)
                    validDirections = validDirections | ValidSpinDirections.Decrease;
            }

            if (Spinner is not null)
                Spinner.ValidSpinDirection = validDirections;
        }

        protected override void OnValueChanged(DateTime? oldValue, DateTime? newValue)
        {
            //whenever the value changes we need to parse out the value into out DateTimeInfo segments so we can keep track of the individual pieces
            //but only if it is not null
            if (newValue is not null)
                ParseValueIntoDateTimeInfo();

            base.OnValueChanged(oldValue, newValue);
        }

        protected override void RaiseValueChangedEvent(DateTime? oldValue, DateTime? newValue)
        {
            if (TemplatedParent is TimePicker
                && ((TimePicker) TemplatedParent).TemplatedParent is DateTimePicker)
                return;

            base.RaiseValueChangedEvent(oldValue, newValue);
        }

        #endregion //Base Class Overrides

        #region Event Hanlders

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_fireSelectionChangedEvent)
                PerformMouseSelection();
            else
                _fireSelectionChangedEvent = true;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_selectedDateTimeInfo is null) Select(GetDateTimeInfo(0));
        }

        #endregion //Event Hanlders

        #region Methods

        public void SelectAll()
        {
            _fireSelectionChangedEvent = false;
            TextBox.SelectAll();
            _fireSelectionChangedEvent = true;
        }

        private void FormatUpdated()
        {
            InitializeDateTimeInfoList();
            if (Value is not null)
                ParseValueIntoDateTimeInfo();

            // Update the Text representation of the value.
            _processTextChanged = false;

            SyncTextAndValueProperties(false, null);

            _processTextChanged = true;
        }

        private void InitializeDateTimeInfoList()
        {
            _dateTimeInfoList.Clear();
            _selectedDateTimeInfo = null;

            var format = GetFormatString(Format);

            if (string.IsNullOrEmpty(format))
                return;

            while (format.Length > 0)
            {
                var elementLength = GetElementLengthByFormat(format);
                DateTimeInfo info = null;

                switch (format[0])
                {
                    case '"':
                    case '\'':
                    {
                        var closingQuotePosition = format.IndexOf(format[0], 1);
                        info = new DateTimeInfo
                        {
                            IsReadOnly = true,
                            Type = DateTimePart.Other,
                            Length = 1,
                            Content = format.Substring(1, Math.Max(1, closingQuotePosition - 1))
                        };
                        elementLength = Math.Max(1, closingQuotePosition + 1);
                        break;
                    }
                    case 'D':
                    case 'd':
                    {
                        var d = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            d = "%" + d;

                        if (elementLength > 2)
                            info = new DateTimeInfo
                            {
                                IsReadOnly = true,
                                Type = DateTimePart.DayName,
                                Format = d
                            };
                        else
                            info = new DateTimeInfo
                            {
                                IsReadOnly = false,
                                Type = DateTimePart.Day,
                                Format = d
                            };
                        break;
                    }
                    case 'F':
                    case 'f':
                    {
                        var f = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            f = "%" + f;

                        info = new DateTimeInfo
                        {
                            IsReadOnly = false,
                            Type = DateTimePart.Millisecond,
                            Format = f
                        };
                        break;
                    }
                    case 'h':
                    {
                        var h = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            h = "%" + h;

                        info = new DateTimeInfo
                        {
                            IsReadOnly = false,
                            Type = DateTimePart.Hour12,
                            Format = h
                        };
                        break;
                    }
                    case 'H':
                    {
                        var H = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            H = "%" + H;

                        info = new DateTimeInfo
                        {
                            IsReadOnly = false,
                            Type = DateTimePart.Hour24,
                            Format = H
                        };
                        break;
                    }
                    case 'M':
                    {
                        var M = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            M = "%" + M;

                        if (elementLength >= 3)
                            info = new DateTimeInfo
                            {
                                IsReadOnly = false,
                                Type = DateTimePart.MonthName,
                                Format = M
                            };
                        else
                            info = new DateTimeInfo
                            {
                                IsReadOnly = false,
                                Type = DateTimePart.Month,
                                Format = M
                            };
                        break;
                    }
                    case 'S':
                    case 's':
                    {
                        var s = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            s = "%" + s;

                        info = new DateTimeInfo
                        {
                            IsReadOnly = false,
                            Type = DateTimePart.Second,
                            Format = s
                        };
                        break;
                    }
                    case 'T':
                    case 't':
                    {
                        var t = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            t = "%" + t;

                        info = new DateTimeInfo
                        {
                            IsReadOnly = false,
                            Type = DateTimePart.AmPmDesignator,
                            Format = t
                        };
                        break;
                    }
                    case 'Y':
                    case 'y':
                    {
                        var y = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            y = "%" + y;

                        info = new DateTimeInfo
                        {
                            IsReadOnly = false,
                            Type = DateTimePart.Year,
                            Format = y
                        };
                        break;
                    }
                    case '\\':
                    {
                        if (format.Length >= 2)
                        {
                            info = new DateTimeInfo
                            {
                                IsReadOnly = true,
                                Content = format.Substring(1, 1),
                                Length = 1,
                                Type = DateTimePart.Other
                            };
                            elementLength = 2;
                        }

                        break;
                    }
                    case 'g':
                    {
                        var g = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            g = "%" + g;

                        info = new DateTimeInfo
                        {
                            IsReadOnly = true,
                            Type = DateTimePart.Period,
                            Format = format.Substring(0, elementLength)
                        };
                        break;
                    }
                    case 'm':
                    {
                        var m = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            m = "%" + m;

                        info = new DateTimeInfo
                        {
                            IsReadOnly = false,
                            Type = DateTimePart.Minute,
                            Format = m
                        };
                        break;
                    }
                    case 'z':
                    {
                        var z = format.Substring(0, elementLength);
                        if (elementLength == 1)
                            z = "%" + z;

                        info = new DateTimeInfo
                        {
                            IsReadOnly = true,
                            Type = DateTimePart.TimeZone,
                            Format = z
                        };
                        break;
                    }
                    default:
                    {
                        elementLength = 1;
                        info = new DateTimeInfo
                        {
                            IsReadOnly = true,
                            Length = 1,
                            Content = format[0].ToString(),
                            Type = DateTimePart.Other
                        };
                        break;
                    }
                }

                _dateTimeInfoList.Add(info);
                format = format.Substring(elementLength);
            }
        }

        private static int GetElementLengthByFormat(string format)
        {
            for (var i = 1; i < format.Length; i++)
                if (string.Compare(format[i].ToString(), format[0].ToString(), false) != 0)
                    return i;
            return format.Length;
        }

        private void ParseValueIntoDateTimeInfo()
        {
            var text = string.Empty;

            _dateTimeInfoList.ForEach(info =>
            {
                if (info.Format is null)
                {
                    info.StartPosition = text.Length;
                    info.Length = info.Content.Length;
                    text += info.Content;
                }
                else
                {
                    var date = DateTime.Parse(Value.ToString());
                    info.StartPosition = text.Length;
                    info.Content = date.ToString(info.Format, CultureInfo.DateTimeFormat);
                    info.Length = info.Content.Length;
                    text += info.Content;
                }
            });
        }

        private void PerformMouseSelection()
        {
            Select(GetDateTimeInfo(TextBox.SelectionStart));
        }

        private void PerformKeyboardSelection(int nextSelectionStart)
        {
            TextBox.Focus();

            CommitInput();

            var selectedDateStartPosition = _selectedDateTimeInfo is not null ? _selectedDateTimeInfo.StartPosition : 0;
            var direction = nextSelectionStart - selectedDateStartPosition;
            if (direction > 0)
                Select(GetNextDateTimeInfo(nextSelectionStart));
            else
                Select(GetPreviousDateTimeInfo(nextSelectionStart - 1));
        }

        private DateTimeInfo GetDateTimeInfo(int selectionStart)
        {
            return _dateTimeInfoList.FirstOrDefault(info =>
                info.StartPosition <= selectionStart && selectionStart < info.StartPosition + info.Length);
        }

        private DateTimeInfo GetNextDateTimeInfo(int nextSelectionStart)
        {
            var nextDateTimeInfo = GetDateTimeInfo(nextSelectionStart);
            if (nextDateTimeInfo is null) nextDateTimeInfo = _dateTimeInfoList.First();

            var initialDateTimeInfo = nextDateTimeInfo;

            while (nextDateTimeInfo.Type == DateTimePart.Other)
            {
                nextDateTimeInfo = GetDateTimeInfo(nextDateTimeInfo.StartPosition + nextDateTimeInfo.Length);
                if (nextDateTimeInfo is null) nextDateTimeInfo = _dateTimeInfoList.First();
                if (Equals(nextDateTimeInfo, initialDateTimeInfo))
                    throw new InvalidOperationException("Couldn't find a valid DateTimeInfo.");
            }

            return nextDateTimeInfo;
        }

        private DateTimeInfo GetPreviousDateTimeInfo(int previousSelectionStart)
        {
            var previousDateTimeInfo = GetDateTimeInfo(previousSelectionStart);
            if (previousDateTimeInfo is null) previousDateTimeInfo = _dateTimeInfoList.Last();

            var initialDateTimeInfo = previousDateTimeInfo;

            while (previousDateTimeInfo.Type == DateTimePart.Other)
            {
                previousDateTimeInfo = GetDateTimeInfo(previousDateTimeInfo.StartPosition - 1);
                if (previousDateTimeInfo is null) previousDateTimeInfo = _dateTimeInfoList.Last();
                if (Equals(previousDateTimeInfo, initialDateTimeInfo))
                    throw new InvalidOperationException("Couldn't find a valid DateTimeInfo.");
            }

            return previousDateTimeInfo;
        }

        private void Select(DateTimeInfo info)
        {
            if (info is not null)
            {
                _fireSelectionChangedEvent = false;
                TextBox.Select(info.StartPosition, info.Length);
                _fireSelectionChangedEvent = true;
                _selectedDateTimeInfo = info;
            }
        }

        private string GetFormatString(DateTimeFormat dateTimeFormat)
        {
            switch (dateTimeFormat)
            {
                case DateTimeFormat.ShortDate:
                    return CultureInfo.DateTimeFormat.ShortDatePattern;
                case DateTimeFormat.LongDate:
                    return CultureInfo.DateTimeFormat.LongDatePattern;
                case DateTimeFormat.ShortTime:
                    return CultureInfo.DateTimeFormat.ShortTimePattern;
                case DateTimeFormat.LongTime:
                    return CultureInfo.DateTimeFormat.LongTimePattern;
                case DateTimeFormat.FullDateTime:
                    return CultureInfo.DateTimeFormat.FullDateTimePattern;
                case DateTimeFormat.MonthDay:
                    return CultureInfo.DateTimeFormat.MonthDayPattern;
                case DateTimeFormat.RFC1123:
                    return CultureInfo.DateTimeFormat.RFC1123Pattern;
                case DateTimeFormat.SortableDateTime:
                    return CultureInfo.DateTimeFormat.SortableDateTimePattern;
                case DateTimeFormat.UniversalSortableDateTime:
                    return CultureInfo.DateTimeFormat.UniversalSortableDateTimePattern;
                case DateTimeFormat.YearMonth:
                    return CultureInfo.DateTimeFormat.YearMonthPattern;
                case DateTimeFormat.Custom:
                {
                    switch (FormatString)
                    {
                        case "d":
                            return CultureInfo.DateTimeFormat.ShortDatePattern;
                        case "t":
                            return CultureInfo.DateTimeFormat.ShortTimePattern;
                        case "T":
                            return CultureInfo.DateTimeFormat.LongTimePattern;
                        case "D":
                            return CultureInfo.DateTimeFormat.LongDatePattern;
                        case "f":
                            return CultureInfo.DateTimeFormat.LongDatePattern + " " +
                                   CultureInfo.DateTimeFormat.ShortTimePattern;
                        case "F":
                            return CultureInfo.DateTimeFormat.FullDateTimePattern;
                        case "g":
                            return CultureInfo.DateTimeFormat.ShortDatePattern + " " +
                                   CultureInfo.DateTimeFormat.ShortTimePattern;
                        case "G":
                            return CultureInfo.DateTimeFormat.ShortDatePattern + " " +
                                   CultureInfo.DateTimeFormat.LongTimePattern;
                        case "m":
                            return CultureInfo.DateTimeFormat.MonthDayPattern;
                        case "y":
                            return CultureInfo.DateTimeFormat.YearMonthPattern;
                        case "r":
                            return CultureInfo.DateTimeFormat.RFC1123Pattern;
                        case "s":
                            return CultureInfo.DateTimeFormat.SortableDateTimePattern;
                        case "u":
                            return CultureInfo.DateTimeFormat.UniversalSortableDateTimePattern;
                        default:
                            return FormatString;
                    }
                }
                default:
                    throw new ArgumentException("Not a supported format");
            }
        }

        private void UpdateDateTime(int value)
        {
            _fireSelectionChangedEvent = false;
            var info = _selectedDateTimeInfo;

            //this only occurs when the user manually type in a value for the Value Property
            if (info is null)
                info = _dateTimeInfoList[0];

            DateTime? result = null;

            try
            {
                switch (info.Type)
                {
                    case DateTimePart.Year:
                    {
                        result = ((DateTime) Value).AddYears(value);
                        break;
                    }
                    case DateTimePart.Month:
                    case DateTimePart.MonthName:
                    {
                        result = ((DateTime) Value).AddMonths(value);
                        break;
                    }
                    case DateTimePart.Day:
                    case DateTimePart.DayName:
                    {
                        result = ((DateTime) Value).AddDays(value);
                        break;
                    }
                    case DateTimePart.Hour12:
                    case DateTimePart.Hour24:
                    {
                        result = ((DateTime) Value).AddHours(value);
                        break;
                    }
                    case DateTimePart.Minute:
                    {
                        result = ((DateTime) Value).AddMinutes(value);
                        break;
                    }
                    case DateTimePart.Second:
                    {
                        result = ((DateTime) Value).AddSeconds(value);
                        break;
                    }
                    case DateTimePart.Millisecond:
                    {
                        result = ((DateTime) Value).AddMilliseconds(value);
                        break;
                    }
                    case DateTimePart.AmPmDesignator:
                    {
                        result = ((DateTime) Value).AddHours(value * 12);
                        break;
                    }
                }
            }
            catch
            {
                //this can occur if the date/time = 1/1/0001 12:00:00 AM which is the smallest date allowed.
                //I could write code that would validate the date each and everytime but I think that it would be more
                //efficient if I just handle the edge case and allow an exeption to occur and swallow it instead.
            }

            Value = CoerceValueMinMax(result);

            //we loose our selection when the Value is set so we need to reselect it without firing the selection changed event
            TextBox.Select(info.StartPosition, info.Length);
            _fireSelectionChangedEvent = true;
        }

        private DateTime? CoerceValueMinMax(DateTime? value)
        {
            if (IsLowerThan(value, Minimum))
                return Minimum;
            if (IsGreaterThan(value, Maximum))
                return Maximum;
            return value;
        }

        private bool IsLowerThan(DateTime? value1, DateTime? value2)
        {
            if (value1 is null || value2 is null)
                return false;

            return value1.Value < value2.Value;
        }

        private bool IsGreaterThan(DateTime? value1, DateTime? value2)
        {
            if (value1 is null || value2 is null)
                return false;

            return value1.Value > value2.Value;
        }

        private bool TryParseDateTime(string text, out DateTime result)
        {
            var isValid = false;
            result = DateTime.Now;

            var current = Value.HasValue
                ? Value.Value
                : DateTime.Parse(DateTime.Now.ToString(), CultureInfo.DateTimeFormat);
            isValid = DateTimeParser.TryParse(text, GetFormatString(Format), current, CultureInfo, out result);

            if (!isValid)
                isValid = DateTime.TryParseExact(text, GetFormatString(Format), CultureInfo, DateTimeStyles.None,
                    out result);

            return isValid;
        }

        private bool IsCurrentValueValid()
        {
            DateTime result;

            if (string.IsNullOrEmpty(TextBox.Text))
                return true;

            return TryParseDateTime(TextBox.Text, out result);
        }

        private void ValidateDefaultMinMax(DateTime? value)
        {
            // DefaultValue is always accepted.
            if (Equals(value, DefaultValue))
                return;

            if (IsLowerThan(value, Minimum))
                throw new ArgumentOutOfRangeException("Minimum",
                    string.Format("Value must be greater than MinValue of {0}", Minimum));
            if (IsGreaterThan(value, Maximum))
                throw new ArgumentOutOfRangeException("Maximum",
                    string.Format("Value must be less than MaxValue of {0}", Maximum));
        }

        private DateTime? GetClippedMinMaxValue(DateTime? value)
        {
            if (IsGreaterThan(value, Maximum))
                return Maximum;
            if (IsLowerThan(value, Minimum))
                return Minimum;
            return value;
        }

        #endregion //Methods
    }
}