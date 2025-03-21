﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents an axis presenting <see cref="System.DateTime" /> values.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Axes
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Represents an axis presenting <see cref="System.DateTime" /> values.
    /// </summary>
    /// <remarks>The actual numeric values on the axis are days since 1900/01/01.
    /// Use the static ToDouble and ToDateTime to convert numeric values to and from DateTimes.
    /// The StringFormat value can be used to force formatting of the axis values
    /// <code>"yyyy-MM-dd"</code> shows date
    /// <code>"w"</code> or <code>"ww"</code> shows week number
    /// <code>"h:mm"</code> shows hours and minutes</remarks>
    public class DateTimeAxis : LinearAxis
    {
        /// <summary>
        /// The default precision that is used for rounding DateTime values. 1ms is used to emulate the behavior of .NET 6 and earlier.
        /// </summary>
        public static readonly TimeSpan DefaultPrecision = TimeSpan.FromMilliseconds(1);

        /// <summary>
        /// The time origin.
        /// </summary>
        /// <remarks>This gives the same numeric date values as Excel</remarks>
        private static readonly DateTime TimeOrigin = new DateTime(1899, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// The maximum day value
        /// </summary>
        private static readonly double MaxDayValue = (DateTime.MaxValue - TimeOrigin).TotalDays;

        /// <summary>
        /// The minimum day value
        /// </summary>
        private static readonly double MinDayValue = (DateTime.MinValue - TimeOrigin).TotalDays;

        /// <summary>
        /// The actual interval type.
        /// </summary>
        private DateTimeIntervalType actualIntervalType;

        /// <summary>
        /// The actual minor interval type.
        /// </summary>
        private DateTimeIntervalType actualMinorIntervalType;

        /// <summary>
        /// Initializes a new instance of the <see cref = "DateTimeAxis" /> class.
        /// </summary>
        public DateTimeAxis()
        {
            this.Position = AxisPosition.Bottom;
            this.IntervalType = DateTimeIntervalType.Auto;
            this.FirstDayOfWeek = DayOfWeek.Monday;
            this.CalendarWeekRule = CalendarWeekRule.FirstFourDayWeek;
            this.DateTimePrecision = DefaultPrecision;
        }

        /// <summary>
        /// Gets or sets CalendarWeekRule.
        /// </summary>
        public CalendarWeekRule CalendarWeekRule { get; set; }

        /// <summary>
        /// Gets or sets FirstDayOfWeek.
        /// </summary>
        public DayOfWeek FirstDayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets IntervalType.
        /// </summary>
        public DateTimeIntervalType IntervalType { get; set; }

        /// <summary>
        /// Gets or sets MinorIntervalType.
        /// </summary>
        public DateTimeIntervalType MinorIntervalType { get; set; }

        /// <summary>
        /// Gets or sets the precision that is used for DateTime values internally. Limiting the precision avoids 'unexpected' tick labels, e.g.
        /// '11:59' for a value of 11:59.99999. The default value is 1 Millisecond.
        /// </summary>
        /// <remarks>For .NET 6 and below, the minimum precision is 1 ms. Using a DateTimePrecision smaller than 1 ms will not result in increased precision.</remarks>
        public TimeSpan DateTimePrecision { get; set; }

        /// <summary>
        /// Gets or sets the time zone (used when formatting date/time values).
        /// </summary>
        /// <value>The time zone info.</value>
        /// <remarks>No date/time conversion will be performed if this property is <c>null</c>.</remarks>
        public TimeZoneInfo TimeZone { get; set; }

        /// <summary>
        /// Creates a data point.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>A data point.</returns>
        public static DataPoint CreateDataPoint(DateTime x, double y)
        {
            return new DataPoint(ToDouble(x), y);
        }

        /// <summary>
        /// Creates a data point.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>A data point.</returns>
        public static DataPoint CreateDataPoint(DateTime x, DateTime y)
        {
            return new DataPoint(ToDouble(x), ToDouble(y));
        }

        /// <summary>
        /// Creates a data point.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>A data point.</returns>
        public static DataPoint CreateDataPoint(double x, DateTime y)
        {
            return new DataPoint(x, ToDouble(y));
        }

        /// <summary>
        /// Converts a numeric representation of the date (number of days after the time origin) to a DateTime structure, using a precision of 1 Millisecond.
        /// </summary>
        /// <param name="value">The number of days after the time origin.</param>
        /// <returns>A <see cref="DateTime" /> structure. Ticks = 0 if the value is invalid.</returns>
        [Obsolete("Use ConvertToDateTime(double value) or ToDateTime(double value, TimeSpan precision) instead.")]
        public static DateTime ToDateTime(double value)
        {
            return ToDateTime(value, DefaultPrecision);
        }

        /// <summary>
        /// Converts a numeric representation of the date (number of days after the time origin) to a DateTime structure.
        /// </summary>
        /// <param name="value">The number of days after the time origin.</param>
        /// <param name="precision">The precision that is used for the conversion. The DateTime value is rounded to the next integer multiple of this value.</param>
        /// <returns>A <see cref="DateTime" /> structure. Ticks = 0 if the value is invalid.</returns>
        public static DateTime ToDateTime(double value, TimeSpan precision)
        {
            if (double.IsNaN(value) || value < MinDayValue || value > MaxDayValue)
            {
                return new DateTime();
            }

            var preliminaryDateTime = TimeOrigin.AddDays(value - 1);

            var precisionIntervals = preliminaryDateTime.Ticks / precision.Ticks;
            var remainderTicks = preliminaryDateTime.Ticks % precision.Ticks;

            if (remainderTicks >= precision.Ticks / 2)
            {
                precisionIntervals += 1;
            }

            return new DateTime(precisionIntervals * precision.Ticks, preliminaryDateTime.Kind);
        }

        /// <summary>
        /// Converts a DateTime to days after the time origin.
        /// </summary>
        /// <param name="value">The date/time structure.</param>
        /// <returns>The number of days after the time origin.</returns>
        public static double ToDouble(DateTime value)
        {
            var span = value - TimeOrigin;
            return span.TotalDays + 1;
        }

        /// <summary>
        /// Converts a numeric representation of the date (number of days after the time origin) to a DateTime structure, using the precision specified by <see cref="DateTimePrecision" />.
        /// </summary>
        /// <param name="value">The number of days after the time origin.</param>
        /// <returns>A <see cref="DateTime" /> structure. Ticks = 0 if the value is invalid.</returns>
        public DateTime ConvertToDateTime(double value)
        {
            return ToDateTime(value, this.DateTimePrecision);
        }

        /// <summary>
        /// Gets the tick values.
        /// </summary>
        /// <param name="majorLabelValues">The major label values.</param>
        /// <param name="majorTickValues">The major tick values.</param>
        /// <param name="minorTickValues">The minor tick values.</param>
        public override void GetTickValues(
            out IList<double> majorLabelValues, out IList<double> majorTickValues, out IList<double> minorTickValues)
        {
            minorTickValues = this.CreateDateTimeTickValues(
                this.ClipMinimum, this.ClipMaximum, this.ActualMinorStep, this.actualMinorIntervalType);
            majorTickValues = this.CreateDateTimeTickValues(
                this.ClipMinimum, this.ClipMaximum, this.ActualMajorStep, this.actualIntervalType);
            majorLabelValues = majorTickValues;

            minorTickValues = AxisUtilities.FilterRedundantMinorTicks(majorTickValues, minorTickValues);
        }

        /// <summary>
        /// Gets the value from an axis coordinate, converts from double to the correct data type if necessary.
        /// e.g. DateTimeAxis returns the DateTime and CategoryAxis returns category strings.
        /// </summary>
        /// <param name="x">The coordinate.</param>
        /// <returns>The value.</returns>
        public override object GetValue(double x)
        {
            var time = this.ConvertToDateTime(x);

            if (this.TimeZone != null)
            {
                time = TimeZoneInfo.ConvertTime(time, this.TimeZone);
            }

            return time;
        }

        /// <summary>
        /// Updates the intervals.
        /// </summary>
        /// <param name="plotArea">The plot area.</param>
        internal override void UpdateIntervals(OxyRect plotArea)
        {
            base.UpdateIntervals(plotArea);
            switch (this.actualIntervalType)
            {
                case DateTimeIntervalType.Years:
                    this.ActualMinorStep = 31;
                    this.actualMinorIntervalType = DateTimeIntervalType.Years;
                    if (this.StringFormat == null)
                    {
                        this.ActualStringFormat = "yyyy";
                    }

                    break;
                case DateTimeIntervalType.Months:
                    this.actualMinorIntervalType = DateTimeIntervalType.Months;
                    if (this.StringFormat == null)
                    {
                        this.ActualStringFormat = "yyyy-MM-dd";
                    }

                    break;
                case DateTimeIntervalType.Weeks:
                    this.actualMinorIntervalType = DateTimeIntervalType.Days;
                    this.ActualMajorStep = 7;
                    this.ActualMinorStep = 1;
                    if (this.StringFormat == null)
                    {
                        this.ActualStringFormat = "yyyy/ww";
                    }

                    break;
                case DateTimeIntervalType.Days:
                    this.ActualMinorStep = this.ActualMajorStep;
                    if (this.StringFormat == null)
                    {
                        this.ActualStringFormat = "yyyy-MM-dd";
                    }

                    break;
                case DateTimeIntervalType.Hours:
                    this.ActualMinorStep = this.ActualMajorStep;
                    if (this.StringFormat == null)
                    {
                        this.ActualStringFormat = "HH:mm";
                    }

                    break;
                case DateTimeIntervalType.Minutes:
                    this.ActualMinorStep = this.ActualMajorStep;
                    if (this.StringFormat == null)
                    {
                        this.ActualStringFormat = "HH:mm";
                    }

                    break;
                case DateTimeIntervalType.Seconds:
                    this.ActualMinorStep = this.ActualMajorStep;
                    if (this.StringFormat == null)
                    {
                        this.ActualStringFormat = "HH:mm:ss";
                    }

                    break;
                    
                    
                    
                case DateTimeIntervalType.Milliseconds:
                    this.ActualMinorStep = this.ActualMajorStep;
                    if (this.ActualStringFormat == null)
                    {
                        this.ActualStringFormat = "HH:mm:ss.fff";
                    }

                    break;
                    
                case DateTimeIntervalType.Manual:
                    break;
                case DateTimeIntervalType.Auto:
                    break;
            }
        }

        /// <summary>
        /// Gets the default string format.
        /// </summary>
        /// <returns>
        /// The format string.
        /// </returns>
        protected override string GetDefaultStringFormat()
        {
            return null;
        }

        /// <summary>
        /// Formats the value to be used on the axis.
        /// </summary>
        /// <param name="x">The value to format.</param>
        /// <returns>The formatted value.</returns>
        protected override string FormatValueOverride(double x)
        {
            // convert the double value to a DateTime
            var time = this.ConvertToDateTime(x);

            // If a time zone is specified, convert the time
            if (this.TimeZone != null)
            {
                time = TimeZoneInfo.ConvertTime(time, this.TimeZone);
            }

            string fmt = this.ActualStringFormat;
            if (fmt == null)
            {
                return time.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
            }

            int week = this.GetWeek(time);
            fmt = fmt.Replace("ww", week.ToString("00"));
            fmt = fmt.Replace("w", week.ToString(CultureInfo.InvariantCulture));
            fmt = string.Concat("{0:", fmt, "}");
            return string.Format(this.ActualCulture, fmt, time);
        }

        /// <inheritdoc/>
        protected override double CalculateActualInterval(double availableSize, double maxIntervalSize, double minIntervalCount, double maxIntervalCount)
        {
            const double Year = 365.25;
            const double Month = 30.5;
            const double Week = 7;
            const double Day = 1.0;
            const double Hour = Day / 24;
            const double Minute = Hour / 60;
            const double Second = Minute / 60;
            const double MilliSecond = Second / 1000;

            double range = Math.Abs(this.ClipMinimum - this.ClipMaximum);

            var goodIntervals = new[]
                                    {   MilliSecond, 2 * MilliSecond, 10 * MilliSecond, 100 * MilliSecond,
                                        Second, 2 * Second, 5 * Second, 10 * Second, 30 * Second, Minute, 2 * Minute,
                                        5 * Minute, 10 * Minute, 30 * Minute, Hour, 4 * Hour, 8 * Hour, 12 * Hour, Day,
                                        2 * Day, 5 * Day, Week, 2 * Week, Month, 2 * Month, 3 * Month, 4 * Month,
                                        6 * Month, Year
                                    };

            double interval = goodIntervals[0];

            // bound min/max interval counts
            minIntervalCount = Math.Max(minIntervalCount, 0);
            maxIntervalCount = Math.Min(maxIntervalCount, Math.Max((int)(availableSize / maxIntervalSize), 2));

            while (true)
            {
                if (range / interval < maxIntervalCount)
                {
                    break;
                }

                double nextInterval = goodIntervals.FirstOrDefault(i => i > interval);
                if (Math.Abs(nextInterval) <= 0)
                {
                    nextInterval = interval * 2;
                }

                if (range / nextInterval < minIntervalCount)
                {
                    break;
                }

                interval = nextInterval;
            }

            this.actualIntervalType = this.IntervalType;
            this.actualMinorIntervalType = this.MinorIntervalType;

            if (this.IntervalType == DateTimeIntervalType.Auto)
            {
                this.actualIntervalType = DateTimeIntervalType.Milliseconds;

                if (interval >= 1.0 / 24 / 60 / 60)
                {
                    this.actualIntervalType = DateTimeIntervalType.Seconds;
                }
                    
                if (interval >= 1.0 / 24 / 60)
                {
                    this.actualIntervalType = DateTimeIntervalType.Minutes;
                }

                if (interval >= 1.0 / 24)
                {
                    this.actualIntervalType = DateTimeIntervalType.Hours;
                }

                if (interval >= 1)
                {
                    this.actualIntervalType = DateTimeIntervalType.Days;
                }

                if (interval >= 30)
                {
                    this.actualIntervalType = DateTimeIntervalType.Months;
                }

                if (range >= 365.25)
                {
                    this.actualIntervalType = DateTimeIntervalType.Years;
                }
            }

            if (this.actualIntervalType == DateTimeIntervalType.Months)
            {
                double monthsRange = range / 30.5;
                interval = this.CalculateActualInterval(availableSize, maxIntervalSize, monthsRange, this.MinimumMajorIntervalCount, this.MaximumMajorIntervalCount);
            }

            if (this.actualIntervalType == DateTimeIntervalType.Years)
            {
                double yearsRange = range / 365.25;
                interval = this.CalculateActualInterval(availableSize, maxIntervalSize, yearsRange, this.MinimumMajorIntervalCount, this.MaximumMajorIntervalCount);
            }

            if (this.actualMinorIntervalType == DateTimeIntervalType.Auto)
            {
                switch (this.actualIntervalType)
                {
                    case DateTimeIntervalType.Years:
                        this.actualMinorIntervalType = DateTimeIntervalType.Months;
                        break;
                    case DateTimeIntervalType.Months:
                        this.actualMinorIntervalType = DateTimeIntervalType.Days;
                        break;
                    case DateTimeIntervalType.Weeks:
                        this.actualMinorIntervalType = DateTimeIntervalType.Days;
                        break;
                    case DateTimeIntervalType.Days:
                        this.actualMinorIntervalType = DateTimeIntervalType.Hours;
                        break;
                    case DateTimeIntervalType.Hours:
                        this.actualMinorIntervalType = DateTimeIntervalType.Minutes;
                        break;
                    default:
                        this.actualMinorIntervalType = DateTimeIntervalType.Days;
                        break;
                }
            }

            return interval;
        }

        /// <summary>
        /// Creates the date tick values.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <param name="step">The step.</param>
        /// <param name="intervalType">Type of the interval.</param>
        /// <returns>Date tick values.</returns>
        private IList<double> CreateDateTickValues(
            double min, double max, double step, DateTimeIntervalType intervalType)
        {
            var values = new Collection<double>();
            var start = this.ConvertToDateTime(min);
            if (start.Ticks == 0)
            {
                // Invalid start time
                return values;
            }

            switch (intervalType)
            {
                case DateTimeIntervalType.Weeks:

                    // make sure the first tick is at the 1st day of a week
                    start = start.AddDays(-(int)start.DayOfWeek + (int)this.FirstDayOfWeek);
                    break;
                case DateTimeIntervalType.Months:

                    // make sure the first tick is at the 1st of a month
                    start = new DateTime(start.Year, start.Month, 1);
                    break;
                case DateTimeIntervalType.Years:

                    // make sure the first tick is at Jan 1st
                    start = new DateTime(start.Year, 1, 1);
                    break;
            }

            // Adds a tick to the end time to make sure the end DateTime is included.
            var end = this.ConvertToDateTime(max).AddTicks(1);
            if (end.Ticks == 0)
            {
                // Invalid end time
                return values;
            }

            var current = start;
            double eps = step * 1e-3;
            var minDateTime = this.ConvertToDateTime(min - eps);
            var maxDateTime = this.ConvertToDateTime(max + eps);

            if (minDateTime.Ticks == 0 || maxDateTime.Ticks == 0)
            {
                // Invalid min/max time
                return values;
            }

            while (current < end)
            {
                if (current > minDateTime && current < maxDateTime)
                {
                    values.Add(ToDouble(current));
                }

                try
                {
                    switch (intervalType)
                    {
                        case DateTimeIntervalType.Months:
                            current = current.AddMonths((int)Math.Ceiling(step));
                            break;
                        case DateTimeIntervalType.Years:
                            current = current.AddYears((int)Math.Ceiling(step));
                            break;
                        default:
                            current = current.AddDays(step);
                            break;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // AddMonths/AddYears/AddDays can throw an exception
                    // We could test this by comparing to MaxDayValue/MinDayValue, but it is easier to catch the exception...
                    break;
                }
            }

            return values;
        }

        /// <summary>
        /// Creates <see cref="DateTime" /> tick values.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="intervalType">The interval type.</param>
        /// <returns>A list of <see cref="DateTime" /> tick values.</returns>
        private IList<double> CreateDateTimeTickValues(
            double min, double max, double interval, DateTimeIntervalType intervalType)
        {
            // If the step size is more than 7 days (e.g. months or years) we use a specialized tick generation method that adds tick values with uneven spacing...
            if (intervalType > DateTimeIntervalType.Days)
            {
                return this.CreateDateTickValues(min, max, interval, intervalType);
            }

            // For shorter step sizes we use the method from Axis
            return this.CreateTickValues(min, max, interval);
        }

        /// <summary>
        /// Gets the week number for the specified date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>The week number for the current culture.</returns>
        private int GetWeek(DateTime date)
        {
            return this.ActualCulture.Calendar.GetWeekOfYear(date, this.CalendarWeekRule, this.FirstDayOfWeek);
        }
    }
}
