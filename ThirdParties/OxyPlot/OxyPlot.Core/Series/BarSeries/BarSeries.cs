﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BarSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a series for clustered or stacked bar charts.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Series
{
    using OxyPlot.Axes;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a series for clustered or stacked bar charts.
    /// </summary>
    public class BarSeries : BarSeriesBase<BarItem>, IStackableSeries
    {
        /// <summary>
        /// The default tracker format string
        /// </summary>
        public new const string DefaultTrackerFormatString = "{0}\n{1}: {2}";

        /// <summary>
        /// The default fill color.
        /// </summary>
        private OxyColor defaultFillColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="BarSeries" /> class.
        /// </summary>
        public BarSeries()
        {
            this.FillColor = OxyColors.Automatic;
            this.NegativeFillColor = OxyColors.Undefined;
            this.TrackerFormatString = DefaultTrackerFormatString;
            this.LabelMargin = 2;
            this.LabelAngle = 0;
            this.StackGroup = string.Empty;
            this.StrokeThickness = 0;
            this.BaseValue = 0;
            this.BaseLine = double.NaN;
            this.ActualBaseLine = double.NaN;
        }

        /// <summary>
        /// Gets or sets the base value. Default value is 0.
        /// </summary>
        /// <value>The base value.</value>
        public double BaseValue { get; set; }

        /// <summary>
        /// Gets or sets the base value.
        /// </summary>
        /// <value>The base value.</value>
        public double BaseLine { get; set; }

        /// <summary>
        /// Gets or sets the actual base line.
        /// </summary>
        /// <returns>The actual base line.</returns>
        public double ActualBaseLine { get; protected set; }

        /// <summary>
        /// Gets the actual fill color.
        /// </summary>
        /// <value>The actual color.</value>
        public OxyColor ActualFillColor => this.FillColor.GetActualColor(this.defaultFillColor);

        /// <summary>
        /// Gets or sets the color field.
        /// </summary>
        public string ColorField { get; set; }

        /// <summary>
        /// Gets or sets the color of the interior of the bars.
        /// </summary>
        /// <value>The color.</value>
        public OxyColor FillColor { get; set; }

        /// <inheritdoc/>
        public bool IsStacked { get; set; }

        /// <inheritdoc/>
        public bool OverlapsStack { get; set; }

        /// <summary>
        /// Gets or sets the label format string.
        /// </summary>
        public string LabelFormatString { get; set; }

        /// <summary>
        /// Gets or sets the color of the interior of the bars when the value is negative.
        /// </summary>
        /// <value>The color.</value>
        public OxyColor NegativeFillColor { get; set; }

        /// <inheritdoc/>
        public string StackGroup { get; set; }

        /// <summary>
        /// Gets or sets the value field.
        /// </summary>
        public string ValueField { get; set; }

        /// <summary>
        /// Gets or sets the actual rectangles for the bars.
        /// </summary>
        protected IList<OxyRect> ActualBarRectangles { get; set; }

        /// <inheritdoc/>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            if (this.ActualBarRectangles == null || this.ValidItems.Count == 0)
            {
                return null;
            }

            var i = 0;
            foreach (var rectangle in this.ActualBarRectangles)
            {
                if (rectangle.Contains(point))
                {
                    // get the item corresponding to this bar/column rectangle
                    var item = this.ValidItems[i];
                    var categoryIndex = item.GetCategoryIndex(i);
                    var dp = new DataPoint(categoryIndex, this.ValidItems[i].Value);

                    // get the item that the bar/column is bound to, or the item from the Items collection
                    var boundItem = this.GetItem(this.ValidItemsIndexInversion[i]);

                    return new TrackerHitResult
                    {
                        Series = this,
                        DataPoint = dp,
                        Position = point,
                        Item = boundItem,
                        Index = i,
                        Text = this.GetTrackerText(item, boundItem, categoryIndex)
                    };
                }

                i++;
            }

            return null;
        }

        /// <inheritdoc/>
        public override void RenderLegend(IRenderContext rc, OxyRect legendBox)
        {
            var xmid = (legendBox.Left + legendBox.Right) / 2;
            var ymid = (legendBox.Top + legendBox.Bottom) / 2;
            var height = (legendBox.Bottom - legendBox.Top) * 0.8;
            var width = height;
            rc.DrawRectangle(
                new OxyRect(xmid - (0.5 * width), ymid - (0.5 * height), width, height),
                this.GetSelectableColor(this.ActualFillColor),
                this.StrokeColor,
                this.StrokeThickness,
                this.EdgeRenderingMode);
        }

        /// <inheritdoc/>
        protected internal override void SetDefaultValues()
        {
            if (this.FillColor.IsAutomatic())
            {
                this.defaultFillColor = this.PlotModel.GetDefaultColor();
            }
        }

        /// <summary>
        /// Updates the axes to include the max and min of this series.
        /// </summary>
        protected internal override void UpdateAxisMaxMin()
        {
            base.UpdateAxisMaxMin();

            this.ComputeActualBaseLine();
            this.XAxis.Include(this.ActualBaseLine);
        }

        /// <summary>
        /// Computes the actual base value..
        /// </summary>
        protected void ComputeActualBaseLine()
        {
            if (double.IsNaN(this.BaseLine))
            {
                if (this.XAxis.IsLogarithmic())
                {
                    var lowestPositiveValue = this.ActualItems == null ? 1 : this.ActualItems.Select(p => p.Value).Where(v => v > 0).MinOrDefault(1);
                    this.ActualBaseLine = Math.Max(lowestPositiveValue / 10.0, this.BaseValue);
                }
                else
                {
                    this.ActualBaseLine = 0;
                }
            }
            else
            {
                this.ActualBaseLine = this.BaseLine;
            }
        }

        /// <inheritdoc/>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();

            if (this.ValidItems.Count == 0)
            {
                return;
            }

            var categoryAxis = this.GetCategoryAxis();

            double minValue = double.MaxValue, maxValue = double.MinValue;
            if (this.IsStacked)
            {
                var labels = this.GetCategoryAxis().ActualLabels;
                for (var i = 0; i < labels.Count; i++)
                {
                    var j = 0;
                    var values = this.ValidItems.Where(item => item.GetCategoryIndex(j++) == i)
                        .Select(item => item.Value).Concat(new[] { 0d }).ToList();
                    var minTemp = values.Where(v => v <= 0).Sum();
                    var maxTemp = values.Where(v => v >= 0).Sum();

                    var stackIndex = this.Manager.GetStackIndex(this.StackGroup);
                    var stackedMinValue = this.Manager.GetCurrentMinValue(stackIndex, i);
                    if (!double.IsNaN(stackedMinValue))
                    {
                        minTemp += stackedMinValue;
                    }

                    this.Manager.SetCurrentMinValue(stackIndex, i, minTemp);

                    var stackedMaxValue = this.Manager.GetCurrentMaxValue(stackIndex, i);
                    if (!this.OverlapsStack && !double.IsNaN(stackedMaxValue))
                    {
                        maxTemp += stackedMaxValue;
                    }

                    this.Manager.SetCurrentMaxValue(stackIndex, i, maxTemp);

                    minValue = Math.Min(minValue, minTemp + this.BaseValue);
                    maxValue = Math.Max(maxValue, maxTemp + this.BaseValue);
                }
            }
            else
            {
                var values = this.ValidItems.Select(item => item.Value).Concat(new[] { 0d }).ToList();
                minValue = values.Min();
                maxValue = values.Max();
                if (this.BaseValue < minValue)
                {
                    minValue = this.BaseValue;
                }

                if (this.BaseValue > maxValue)
                {
                    maxValue = this.BaseValue;
                }
            }

            this.MinX = minValue;
            this.MaxX = maxValue;
        }

        /// <inheritdoc/>
        protected virtual string GetTrackerText(BarItem barItem, object item, int categoryIndex)
        {
            var categoryAxis = this.GetCategoryAxis();
            var valueAxis = this.XAxis;

            return StringHelper.Format(
                this.ActualCulture,
                this.TrackerFormatString,
                item,
                this.Title,
                categoryAxis.FormatValue(categoryIndex),
                valueAxis.GetValue(barItem.Value));
        }

        /// <inheritdoc/>
        protected override bool IsValid(BarItem item)
        {
            return this.XAxis.IsValidValue(item.Value);
        }

        /// <summary>
        /// Renders the bar/column item.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="barValue">The end value of the bar.</param>
        /// <param name="categoryValue">The category value.</param>
        /// <param name="actualBarWidth">The actual width of the bar.</param>
        /// <param name="item">The item.</param>
        /// <param name="rect">The rectangle of the bar.</param>
        protected virtual void RenderItem(
            IRenderContext rc,
            double barValue,
            double categoryValue,
            double actualBarWidth,
            BarItem item,
            OxyRect rect)
        {
            // Get the color of the item
            var actualFillColor = item.Color;
            if (actualFillColor.IsAutomatic())
            {
                actualFillColor = this.ActualFillColor;
                if (item.Value < 0 && !this.NegativeFillColor.IsUndefined())
                {
                    actualFillColor = this.NegativeFillColor;
                }
            }

            rc.DrawRectangle(
                rect,
                this.GetSelectableFillColor(actualFillColor),
                this.StrokeColor,
                this.StrokeThickness,
                this.EdgeRenderingMode.GetActual(EdgeRenderingMode.PreferSharpness));
        }

        /// <inheritdoc/>
        public override void Render(IRenderContext rc)
        {
            this.ActualBarRectangles = new List<OxyRect>();

            if (this.ValidItems.Count == 0)
            {
                return;
            }

            var actualBarWidth = this.GetActualBarWidth();
            var stackIndex = this.IsStacked ? this.Manager.GetStackIndex(this.StackGroup) : 0;
            for (var i = 0; i < this.ValidItems.Count; i++)
            {
                var item = this.ValidItems[i];
                var categoryIndex = this.ValidItems[i].GetCategoryIndex(i);

                var value = item.Value;

                // Get base- and topValue
                var baseValue = double.NaN;
                if (this.IsStacked && !this.OverlapsStack)
                {
                    baseValue = this.Manager.GetCurrentBaseValue(stackIndex, categoryIndex, value < 0);
                }

                if (double.IsNaN(baseValue))
                {
                    baseValue = this.BaseValue;
                }

                var topValue = this.IsStacked ? baseValue + value : value;

                if (this.YAxis.IsLogarithmic() && !this.YAxis.IsValidValue(topValue))
                {
                    continue;
                }

                // Calculate offset
                double categoryValue;
                if (this.IsStacked)
                {
                    categoryValue = this.Manager.GetCategoryValue(categoryIndex, stackIndex, actualBarWidth);
                }
                else
                {
                    categoryValue = categoryIndex - 0.5 + this.Manager.GetCurrentBarOffset(categoryIndex);
                }

                if (this.IsStacked)
                {
                    this.Manager.SetCurrentBaseValue(stackIndex, categoryIndex, value < 0, topValue);
                }

                var clampBase = this.XAxis.IsLogarithmic() && !this.XAxis.IsValidValue(baseValue);
                var p1 = this.Transform(clampBase ? this.XAxis.ClipMinimum : baseValue, categoryValue);
                var p2 = this.Transform(topValue, categoryValue + actualBarWidth);

                var rectangle = new OxyRect(p1, p2);

                this.ActualBarRectangles.Add(rectangle);

                this.RenderItem(rc, topValue, categoryValue, actualBarWidth, item, rectangle);

                if (this.LabelFormatString != null)
                {
                    this.RenderLabel(
                        rc,
                        item,
                        baseValue,
                        topValue,
                        categoryValue,
                        categoryValue + actualBarWidth,
                        this.LabelFormatString);
                }

                if (!this.IsStacked)
                {
                    this.Manager.IncreaseCurrentBarOffset(categoryIndex, actualBarWidth);
                }
            }
        }

        /// <inheritdoc/>
        protected override bool UpdateFromDataFields()
        {
            if (this.ValueField == null)
            {
                return false;
            }

            // Using reflection to add items by value and color (optional)
            var filler = new ListBuilder<BarItem>();
            filler.Add(this.ValueField, double.NaN);
            filler.Add(this.ColorField, OxyColors.Automatic);
            filler.Fill(
                this.ItemsSourceItems,
                this.ItemsSource,
                args => new BarItem(Convert.ToDouble(args[0])) { Color = (OxyColor)args[1] });

            return true;
        }
    }
}
