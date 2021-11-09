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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit
{
    [TemplatePart(Name = PART_TimeListItems, Type = typeof(ListBox))]
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    public class TimePicker : Control
    {
        private const string PART_TimeListItems = "PART_TimeListItems";
        private const string PART_Popup = "PART_Popup";

        #region Base Class Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_popup is not null)
                _popup.Opened -= Popup_Opened;

            _popup = GetTemplateChild(PART_Popup) as Popup;

            if (_popup is not null)
                _popup.Opened += Popup_Opened;

            if (_timeListBox is not null)
            {
                _timeListBox.SelectionChanged -= TimeListBox_SelectionChanged;
                _timeListBox.MouseUp -= TimeListBox_MouseUp;
            }

            _timeListBox = GetTemplateChild(PART_TimeListItems) as ListBox;

            if (_timeListBox is not null)
            {
                _timeListBox.SelectionChanged += TimeListBox_SelectionChanged;
                _timeListBox.MouseUp += TimeListBox_MouseUp;

                UpdateListBoxItems();
            }
        }

        #endregion //Base Class Overrides

        #region Members

        private ListBox _timeListBox;
        private Popup _popup;

        private DateTimeFormatInfo DateTimeFormatInfo { get; }

        private DateTime? _initialValue;
        internal static readonly TimeSpan EndTimeDefaultValue = new(23, 59, 0);
        internal static readonly TimeSpan StartTimeDefaultValue = new(0, 0, 0);
        internal static readonly TimeSpan TimeIntervalDefaultValue = new(1, 0, 0);

        #endregion //Members

        #region Properties

        #region AllowSpin

        public static readonly DependencyProperty AllowSpinProperty =
            DependencyProperty.Register("AllowSpin", typeof(bool), typeof(TimePicker), new UIPropertyMetadata(true));

        public bool AllowSpin
        {
            get => (bool) GetValue(AllowSpinProperty);
            set => SetValue(AllowSpinProperty, value);
        }

        #endregion //AllowSpin

        #region ClipValueToMinMax

        public static readonly DependencyProperty ClipValueToMinMaxProperty =
            DependencyProperty.Register("ClipValueToMinMax", typeof(bool), typeof(TimePicker),
                new UIPropertyMetadata(false));

        public bool ClipValueToMinMax
        {
            get => (bool) GetValue(ClipValueToMinMaxProperty);
            set => SetValue(ClipValueToMinMaxProperty, value);
        }

        #endregion //ClipValueToMinMax

        #region EndTime

        public static readonly DependencyProperty EndTimeProperty = DependencyProperty.Register("EndTime",
            typeof(TimeSpan), typeof(TimePicker),
            new UIPropertyMetadata(EndTimeDefaultValue, OnEndTimeChanged, OnCoerceEndTime));

        private static object OnCoerceEndTime(DependencyObject o, object value)
        {
            var timePicker = o as TimePicker;
            if (timePicker is not null)
                return timePicker.OnCoerceEndTime((TimeSpan) value);
            return value;
        }

        private static void OnEndTimeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var timePicker = o as TimePicker;
            if (timePicker is not null)
                timePicker.OnEndTimeChanged((TimeSpan) e.OldValue, (TimeSpan) e.NewValue);
        }

        protected virtual TimeSpan OnCoerceEndTime(TimeSpan value)
        {
            ValidateTime(value);
            return value;
        }

        protected virtual void OnEndTimeChanged(TimeSpan oldValue, TimeSpan newValue)
        {
            UpdateListBoxItems();
        }

        public TimeSpan EndTime
        {
            get => (TimeSpan) GetValue(EndTimeProperty);
            set => SetValue(EndTimeProperty, value);
        }

        #endregion //EndTime

        #region Format

        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register("Format",
            typeof(TimeFormat), typeof(TimePicker), new UIPropertyMetadata(TimeFormat.ShortTime, OnFormatChanged));

        public TimeFormat Format
        {
            get => (TimeFormat) GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        private static void OnFormatChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var timePicker = o as TimePicker;
            if (timePicker is not null)
                timePicker.OnFormatChanged((TimeFormat) e.OldValue, (TimeFormat) e.NewValue);
        }

        protected virtual void OnFormatChanged(TimeFormat oldValue, TimeFormat newValue)
        {
            UpdateListBoxItems();
        }

        #endregion //Format

        #region FormatString

        public static readonly DependencyProperty FormatStringProperty = DependencyProperty.Register("FormatString",
            typeof(string), typeof(TimePicker), new UIPropertyMetadata(default(string), OnFormatStringChanged),
            IsFormatStringValid);

        public string FormatString
        {
            get => (string) GetValue(FormatStringProperty);
            set => SetValue(FormatStringProperty, value);
        }

        internal static bool IsFormatStringValid(object value)
        {
            return DateTimeUpDown.IsFormatStringValid(value);
        }

        private static void OnFormatStringChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var timePicker = o as TimePicker;
            if (timePicker is not null)
                timePicker.OnFormatStringChanged((string) e.OldValue, (string) e.NewValue);
        }

        protected virtual void OnFormatStringChanged(string oldValue, string newValue)
        {
            if (Format == TimeFormat.Custom) UpdateListBoxItems();
        }

        #endregion //FormatString

        #region IsOpen

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register("IsOpen", typeof(bool),
            typeof(TimePicker), new UIPropertyMetadata(false, OnIsOpenChanged));

        public bool IsOpen
        {
            get => (bool) GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var timePicker = (TimePicker) d;
            if (timePicker is not null)
                timePicker.OnIsOpenChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        private void OnIsOpenChanged(bool oldValue, bool newValue)
        {
            if (newValue)
                _initialValue = Value;
        }

        #endregion //IsOpen

        #region Maximum

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum",
            typeof(DateTime?), typeof(TimePicker), new UIPropertyMetadata(DateTime.MaxValue));

        public DateTime? Maximum
        {
            get => (DateTime?) GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        #endregion //Maximum

        #region Minimum

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum",
            typeof(DateTime?), typeof(TimePicker), new UIPropertyMetadata(DateTime.MinValue));

        public DateTime? Minimum
        {
            get => (DateTime?) GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        #endregion //Minimum

        #region ShowButtonSpinner

        public static readonly DependencyProperty ShowButtonSpinnerProperty =
            DependencyProperty.Register("ShowButtonSpinner", typeof(bool), typeof(TimePicker),
                new UIPropertyMetadata(true));

        public bool ShowButtonSpinner
        {
            get => (bool) GetValue(ShowButtonSpinnerProperty);
            set => SetValue(ShowButtonSpinnerProperty, value);
        }

        #endregion //ShowButtonSpinner

        #region StartTime

        public static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register("StartTime",
            typeof(TimeSpan), typeof(TimePicker),
            new UIPropertyMetadata(StartTimeDefaultValue, OnStartTimeChanged, OnCoerceStartTime));

        private static object OnCoerceStartTime(DependencyObject o, object value)
        {
            var timePicker = o as TimePicker;
            if (timePicker is not null)
                return timePicker.OnCoerceStartTime((TimeSpan) value);
            return value;
        }

        private static void OnStartTimeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var timePicker = o as TimePicker;
            if (timePicker is not null)
                timePicker.OnStartTimeChanged((TimeSpan) e.OldValue, (TimeSpan) e.NewValue);
        }

        protected virtual TimeSpan OnCoerceStartTime(TimeSpan value)
        {
            ValidateTime(value);
            return value;
        }

        protected virtual void OnStartTimeChanged(TimeSpan oldValue, TimeSpan newValue)
        {
            UpdateListBoxItems();
        }

        public TimeSpan StartTime
        {
            get => (TimeSpan) GetValue(StartTimeProperty);
            set => SetValue(StartTimeProperty, value);
        }

        #endregion //StartTime

        #region TextAlignment

        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register("TextAlignment",
            typeof(TextAlignment), typeof(TimePicker), new UIPropertyMetadata(TextAlignment.Left));

        public TextAlignment TextAlignment
        {
            get => (TextAlignment) GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        #endregion //TextAlignment

        #region TimeInterval

        public static readonly DependencyProperty TimeIntervalProperty = DependencyProperty.Register("TimeInterval",
            typeof(TimeSpan), typeof(TimePicker),
            new UIPropertyMetadata(TimeIntervalDefaultValue, OnTimeIntervalChanged));

        public TimeSpan TimeInterval
        {
            get => (TimeSpan) GetValue(TimeIntervalProperty);
            set => SetValue(TimeIntervalProperty, value);
        }

        private static object OnCoerceTimeInterval(DependencyObject o, object value)
        {
            var timePicker = o as TimePicker;
            if (timePicker is not null)
                return timePicker.OnCoerceTimeInterval((TimeSpan) value);
            return value;
        }

        private static void OnTimeIntervalChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var timePicker = o as TimePicker;
            if (timePicker is not null)
                timePicker.OnTimeIntervalChanged((TimeSpan) e.OldValue, (TimeSpan) e.NewValue);
        }

        protected virtual TimeSpan OnCoerceTimeInterval(TimeSpan value)
        {
            ValidateTime(value);

            if (value.Ticks == 0L)
                throw new ArgumentException("TimeInterval must be greater than zero");

            return value;
        }


        protected virtual void OnTimeIntervalChanged(TimeSpan oldValue, TimeSpan newValue)
        {
            UpdateListBoxItems();
        }

        #endregion //TimeInterval

        #region Value

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value",
            typeof(DateTime?), typeof(TimePicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public DateTime? Value
        {
            get => (DateTime?) GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var timePicker = o as TimePicker;
            if (timePicker is not null)
                timePicker.OnValueChanged((DateTime?) e.OldValue, (DateTime?) e.NewValue);
        }

        protected virtual void OnValueChanged(DateTime? oldValue, DateTime? newValue)
        {
            UpdateListBoxSelectedItem();

            var args = new RoutedPropertyChangedEventArgs<object>(oldValue, newValue);
            args.RoutedEvent = ValueChangedEvent;
            RaiseEvent(args);
        }

        #endregion //Value

        #region Watermark

        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register("Watermark", typeof(object), typeof(TimePicker), new UIPropertyMetadata(null));

        public object Watermark
        {
            get => GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        #endregion //Watermark

        #region WatermarkTemplate

        public static readonly DependencyProperty WatermarkTemplateProperty =
            DependencyProperty.Register("WatermarkTemplate", typeof(DataTemplate), typeof(TimePicker),
                new UIPropertyMetadata(null));

        public DataTemplate WatermarkTemplate
        {
            get => (DataTemplate) GetValue(WatermarkTemplateProperty);
            set => SetValue(WatermarkTemplateProperty, value);
        }

        #endregion //WatermarkTemplate

        #endregion //Properties

        #region Constructors

        static TimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePicker),
                new FrameworkPropertyMetadata(typeof(TimePicker)));
        }

        public TimePicker()
        {
            DateTimeFormatInfo = DateTimeFormatInfo.GetInstance(CultureInfo.CurrentCulture);
            Keyboard.AddKeyDownHandler(this, OnKeyDown);
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, OnMouseDownOutsideCapturedElement);
        }

        #endregion //Constructors

        #region Event Handlers

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsOpen)
            {
                if (KeyboardUtilities.IsKeyModifyingPopupState(e))
                {
                    IsOpen = true;
                    // TimeListBox_Loaded() will focus on ListBoxItem.
                    e.Handled = true;
                }
            }
            else
            {
                if (KeyboardUtilities.IsKeyModifyingPopupState(e))
                {
                    CloseTimePicker(true);
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    CloseTimePicker(true);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    Value = _initialValue;
                    CloseTimePicker(true);
                    e.Handled = true;
                }
            }
        }

        private void OnMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            CloseTimePicker(false);
        }

        private void TimeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedTimeListItem = (TimeItem) e.AddedItems[0];
                var time = selectedTimeListItem.Time;
                var date = Value ?? DateTime.MinValue;

                Value = new DateTime(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds,
                    time.Milliseconds);
            }
        }

        private void TimeListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            CloseTimePicker(true);
        }

        private void Popup_Opened(object sender, EventArgs e)
        {
            if (_timeListBox is not null)
            {
                var time = Value is not null ? Value.Value.TimeOfDay : StartTimeDefaultValue;
                var nearestItem = GetNearestTimeItem(time);
                if (nearestItem is not null)
                {
                    _timeListBox.ScrollIntoView(nearestItem);
                    var listBoxItem = (ListBoxItem) _timeListBox.ItemContainerGenerator.ContainerFromItem(nearestItem);
                    if (listBoxItem is not null) listBoxItem.Focus();
                }
            }
        }

        #endregion //Event Handlers

        #region Events

        //Due to a bug in Visual Studio, you cannot create event handlers for nullable args in XAML, so I have to use object instead.
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object>), typeof(TimePicker));

        public event RoutedPropertyChangedEventHandler<object> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        #endregion //Events

        #region Methods

        private void ValidateTime(TimeSpan time)
        {
            if (time.TotalHours >= 24d)
                throw new ArgumentException("Time value cannot be greater than or equal to 24 hours.");
        }

        private void CloseTimePicker(bool isFocusOnTimePicker)
        {
            if (IsOpen)
                IsOpen = false;
            ReleaseMouseCapture();

            if (isFocusOnTimePicker)
                Focus();
        }

        public IEnumerable GenerateTimeListItemsSource()
        {
            var time = StartTime;
            var endTime = EndTime;

            if (endTime <= time)
            {
                endTime = EndTimeDefaultValue;
                time = StartTimeDefaultValue;
            }

            var timeInterval = TimeInterval;
            var timeItemList = new List<TimeItem>();

            if (timeInterval.Ticks > 0)
                while (time <= endTime)
                {
                    timeItemList.Add(CreateTimeItem(time));
                    time = time.Add(timeInterval);
                }

            return timeItemList;
        }

        private TimeItem CreateTimeItem(TimeSpan time)
        {
            return new(DateTime.MinValue.Add(time).ToString(GetTimeFormat(), CultureInfo.CurrentCulture), time);
        }

        private string GetTimeFormat()
        {
            switch (Format)
            {
                case TimeFormat.Custom:
                    return FormatString;
                case TimeFormat.LongTime:
                    return DateTimeFormatInfo.LongTimePattern;
                case TimeFormat.ShortTime:
                    return DateTimeFormatInfo.ShortTimePattern;
                default:
                    return DateTimeFormatInfo.ShortTimePattern;
            }
        }

        private void UpdateListBoxSelectedItem()
        {
            if (_timeListBox is not null)
            {
                TimeItem time = null;
                if (Value is not null)
                {
                    time = CreateTimeItem(Value.Value.TimeOfDay);
                    if (!_timeListBox.Items.Contains(time)) time = null;
                }

                _timeListBox.SelectedItem = time;
            }
        }

        private void UpdateListBoxItems()
        {
            if (_timeListBox is not null) _timeListBox.ItemsSource = GenerateTimeListItemsSource();
        }

        private TimeItem GetNearestTimeItem(TimeSpan time)
        {
            if (_timeListBox is not null)
            {
                var itemCount = _timeListBox.Items.Count;
                for (var i = 0; i < itemCount; i++)
                {
                    var timeItem = _timeListBox.Items[i] as TimeItem;
                    if (timeItem is not null)
                        if (timeItem.Time >= time)
                            return timeItem;
                }

                //They are all less than the searched time. 
                //Return the last one. (Should also be the greater one.)
                if (itemCount > 0) return _timeListBox.Items[itemCount - 1] as TimeItem;
            }

            return null;
        }

        #endregion //Methods
    }
}