﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="VectorSeries.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a series that can be bound to a collection of VectorItems representing a vector field.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Axes;

    /// <summary>
    /// Represents a series that can be bound to a collection of <see cref="VectorItem"/>.
    /// </summary>
    public class VectorSeries : XYAxisSeries
    {
        /// <summary>
        /// The items originating from the items source.
        /// </summary>
        private List<VectorItem> actualItems;

        /// <summary>
        /// Specifies if the <see cref="actualItems" /> list can be modified.
        /// </summary>
        private bool ownsActualItems;

        /// <summary>
        /// The default color.
        /// </summary>
        private OxyColor defaultColor;

        /// <summary>
        /// The default line style.
        /// </summary>
        private LineStyle defaultLineStyle;

        /// <summary>
        /// The default tracker format string
        /// </summary>
        public new const string DefaultTrackerFormatString = "{0}\n{1}: {2}\n{3}: {4}\n{5}: {6}\nΔ{1}: {7}\nΔ{3}: {8}";

        /// <summary>
        /// The default color-axis title
        /// </summary>
        private const string DefaultColorAxisTitle = "Value";

        /// <summary>
        /// Initializes a new instance of the <see cref="HeatMapSeries" /> class.
        /// </summary>
        public VectorSeries()
        {
            this.Color = OxyColors.Automatic;
            this.MinimumSegmentLength = 2;
            this.StrokeThickness = 2;
            this.LineStyle = LineStyle.Solid;
            this.LineJoin = LineJoin.Miter;

            this.ArrowHeadLength = 3;
            this.ArrowHeadWidth = 2;
            this.ArrowHeadPosition = 1;
            this.ArrowVeeness = 0;
            this.ArrowStartPosition = 0;
            this.ArrowLabelPosition = 0;

            this.TrackerFormatString = DefaultTrackerFormatString;
            this.LabelFormatString = "0.00";
            this.LabelFontSize = 0;
        }

        /// <summary>
        /// Gets or sets the color of the arrow.
        /// </summary>
        public OxyColor Color { get; set; }

        /// <summary>
        /// Gets the minimum value of the dataset.
        /// </summary>
        public double MinValue { get; private set; }

        /// <summary>
        /// Gets the maximum value of the dataset.
        /// </summary>
        public double MaxValue { get; private set; }

        /// <summary>
        /// Gets or sets the length of the arrows heads (relative to the stroke thickness) (the default value is 10).
        /// </summary>
        /// <value>The length of the arrows heads.</value>
        public double ArrowHeadLength { get; set; }

        /// <summary>
        /// Gets or sets the width of the arrows heads (relative to the stroke thickness) (the default value is 3).
        /// </summary>
        /// <value>The width of the arrows heads.</value>
        public double ArrowHeadWidth { get; set; }

        /// <summary>
        /// Gets or sets the position of the arrow heads (relative to the end of the vector) (the default value is 1).
        /// </summary>
        /// <value>The position of the arrow heads.</value>
        /// <remarks>
        /// A value of 0 means that heads will be start at the end of the vector, effectively extending it in screen space.
        /// A value of 1 means that heads will terminate at the end of the vector.
        /// </remarks>
        public double ArrowHeadPosition { get; set; }

        /// <summary>
        /// Gets or sets the line join type.
        /// </summary>
        /// <value>The line join type.</value>
        public LineJoin LineJoin { get; set; }

        /// <summary>
        /// Gets or sets the line style.
        /// </summary>
        /// <value>The line style.</value>
        public LineStyle LineStyle { get; set; }

        /// <summary>
        /// Gets or sets the start point of the arrow.
        /// </summary>
        /// <remarks>This property is overridden by the ArrowDirection property, if set.</remarks>
        public DataPoint StartPoint { get; set; }

        /// <summary>
        /// Gets or sets the stroke thickness (the default value is 2).
        /// </summary>
        /// <value>The stroke thickness.</value>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Gets the actual line style.
        /// </summary>
        /// <value>The actual line style.</value>
        protected LineStyle ActualLineStyle
        {
            get
            {
                return this.LineStyle != LineStyle.Automatic ? this.LineStyle : this.defaultLineStyle;
            }
        }

        /// <summary>
        /// Gets or sets the minimum length of an interpolated line segment.
        /// Increasing this number will increase performance,
        /// but make the curve less accurate. The default is <c>2</c>.
        /// </summary>
        /// <value>The minimum length of the segment.</value>
        public double MinimumSegmentLength { get; set; }

        /// <summary>
        /// Gets or sets the 'veeness' of the arrow head (relative to thickness) (the default value is 0).
        /// </summary>
        /// <value>The 'veeness'.</value>
        public double ArrowVeeness { get; set; }

        /// <summary>
        /// Gets the start position of the arrows for each vector relative to the length of the vector (the default value is 0).
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates the arrow will start at the <see cref="VectorItem.Origin"/>.
        /// A value of 1 indicates the arrow will terminate at the <see cref="VectorItem.Origin"/>.
        /// Values other than 0 and 1 may produce arrows that do not pass through the <see cref="VectorItem.Origin"/> on non-linear axes.
        /// </remarks>
        public double ArrowStartPosition { get; set; }

        /// <summary>
        /// Gets the psoitions of the label for each vector along the drawn arrow (the default value is 0).
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates the label will be positioned at the start of the vector arrow.
        /// A value of 1 indicates the label will be positioned at the end of the vector arrow.
        /// </remarks>
        public double ArrowLabelPosition { get; set; }

        /// <summary>
        /// Gets or sets the color axis.
        /// </summary>
        /// <value>The color axis.</value>
        public IColorAxis ColorAxis { get; protected set; }

        /// <summary>
        /// Gets or sets the color axis key.
        /// </summary>
        /// <value>The color axis key.</value>
        public string ColorAxisKey { get; set; }

        /// <summary>
        /// Gets or sets the format string for the cell labels. The default value is <c>0.00</c>.
        /// </summary>
        /// <value>The format string.</value>
        /// <remarks>The label format string is only used when <see cref="LabelFontSize" /> is greater than 0.</remarks>
        public string LabelFormatString { get; set; }

        /// <summary>
        /// Gets or sets the font size of the labels. The default value is <c>0</c> (labels not visible).
        /// </summary>
        /// <value>The font size relative to the cell height.</value>
        public double LabelFontSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tracker can interpolate points.
        /// </summary>
        public bool CanTrackerInterpolatePoints { get; set; }

        /// <summary>
        /// Gets or sets the delegate used to map from <see cref="ItemsSeries.ItemsSource" /> to <see cref="VectorItem" />. The default is <c>null</c>.
        /// </summary>
        /// <value>The mapping.</value>
        /// <remarks>Example: series1.Mapping = item => new VectorItem(new DataPoint((MyType)item).Time1, ((MyType)item).Value1), new DataPoint((MyType)item).Time2, ((MyType)item).Value2));</remarks>
        public Func<object, VectorItem> Mapping { get; set; }

        /// <summary>
        /// Gets the list of Vectors.
        /// </summary>
        /// <value>A list of <see cref="VectorItem" />. This list is used if <see cref="ItemsSeries.ItemsSource" /> is not set.</value>
        public IList<VectorItem> Items { get; } = new List<VectorItem>();

        /// <summary>
        /// Gets the list of Vectors that should be rendered.
        /// </summary>
        /// <value>A list of <see cref="VectorItem" />.</value>
        protected IList<VectorItem> ActualItems => this.ItemsSource != null ? this.actualItems : this.Items;

        /// <summary>
        /// Renders the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        public override void Render(IRenderContext rc)
        {
            var actualRects = this.ActualItems;

            this.VerifyAxes();

            this.RenderVectors(rc, actualRects);
        }

        /// <summary>
        /// Updates the data.
        /// </summary>
        protected internal override void UpdateData()
        {
            if (this.ItemsSource == null)
            {
                return;
            }

            this.UpdateActualItems();
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <param name="i">The index of the item.</param>
        /// <returns>The item of the index.</returns>
        protected override object GetItem(int i)
        {
            var items = this.ActualItems;
            if (this.ItemsSource == null && items != null && i < items.Count)
            {
                return items[i];
            }

            return base.GetItem(i);
        }

        /// <summary>
        /// Clears or creates the <see cref="actualItems"/> list.
        /// </summary>
        private void ClearActualItems()
        {
            if (!this.ownsActualItems || this.actualItems == null)
            {
                this.actualItems = new List<VectorItem>();
            }
            else
            {
                this.actualItems.Clear();
            }

            this.ownsActualItems = true;
        }

        /// <summary>
        /// Updates the points from the <see cref="ItemsSeries.ItemsSource" />.
        /// </summary>
        private void UpdateActualItems()
        {
            // Use the Mapping property to generate the points
            if (this.Mapping != null)
            {
                this.ClearActualItems();
                foreach (var item in this.ItemsSource)
                {
                    this.actualItems.Add(this.Mapping(item));
                }

                return;
            }

            var sourceAsListOfDataRects = this.ItemsSource as List<VectorItem>;
            if (sourceAsListOfDataRects != null)
            {
                this.actualItems = sourceAsListOfDataRects;
                this.ownsActualItems = false;
                return;
            }

            this.ClearActualItems();

            var sourceAsEnumerableDataRects = this.ItemsSource as IEnumerable<VectorItem>;
            if (sourceAsEnumerableDataRects != null)
            {
                this.actualItems.AddRange(sourceAsEnumerableDataRects);
            }
        }

        /// <summary>
        /// Renders the points as line, broken line and markers.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="items">The Items to render.</param>
        protected void RenderVectors(IRenderContext rc, IEnumerable<VectorItem> items)
        {
            int i = 0;
            foreach (var item in items)
            {
                OxyColor vectorColor;
                if (this.ColorAxis != null && (this.Color.IsUndefined() || this.Color.IsAutomatic()))
                {
                    vectorColor = this.ColorAxis.GetColor(item.Value);
                }
                else
                {
                    vectorColor = this.Color.GetActualColor(this.defaultColor);
                }

                vectorColor = this.GetSelectableColor(vectorColor, i);

                var vector = item.Direction;
                var origin = item.Origin - vector * this.ArrowStartPosition;
                var textOrigin = origin + vector * this.ArrowLabelPosition;

                this.DrawVector(
                    rc,
                    origin,
                    vector,
                    vectorColor
                    );

                if (this.LabelFontSize > 0)
                {
                    rc.DrawText(
                        this.Transform(textOrigin), 
                        item.Value.ToString(this.LabelFormatString), 
                        this.ActualTextColor, 
                        this.ActualFont, 
                        this.LabelFontSize, 
                        this.ActualFontWeight, 
                        0, 
                        HorizontalAlignment.Center, 
                        VerticalAlignment.Middle);
                }

                i++;
            }
        }

        private void DrawVector(IRenderContext rc, DataPoint point, DataVector vector, OxyColor color)
        {
            var points = new List<DataPoint>() { point, point + vector };
            var screenPoints = new List<ScreenPoint>();
            RenderingExtensions.TransformAndInterpolateLines(this, points, screenPoints, this.MinimumSegmentLength);

            if (screenPoints.Count >= 2)
            {
                this.DrawArrow(rc, screenPoints, screenPoints[screenPoints.Count - 1] - screenPoints[screenPoints.Count - 2], color);
            }
        }

        /// <summary>
        /// Draws an arrow. May modify the <paramref name="points"/>.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="points">The points along the arrow to draw, which will be modified.</param>
        /// <param name="direction">The direction in which the arrow head should face</param>
        /// <param name="color">The color of the arrow.</param>
        private void DrawArrow(IRenderContext rc, IList<ScreenPoint> points, ScreenVector direction, OxyColor color)
        {
            // draws a line with an arrowhead glued on the tip (the arrowhead does not point to the end point)

            var d = direction;
            d.Normalize();
            var n = new ScreenVector(d.Y, -d.X);

            var actualHeadLength = this.ArrowHeadLength * this.StrokeThickness;
            var actualHeadWidth = this.ArrowHeadWidth * this.StrokeThickness;

            var endPoint = points.Last() - d * (actualHeadLength * this.ArrowHeadPosition);

            var veeness = d * this.ArrowVeeness * this.StrokeThickness;
            var p1 = endPoint + (d * actualHeadLength);
            var p2 = endPoint + (n * actualHeadWidth) - veeness;
            var p3 = endPoint - (n * actualHeadWidth) - veeness;

            var lineStyle = this.ActualLineStyle;
            var dashArray = lineStyle.GetDashArray();

            if (this.ArrowHeadPosition > 0 && this.ArrowHeadPosition <= 1)
            {
                // TODO: may see rendering artefacts from this on non-linear lines
                // crop elements from points which would introduce on the head, and re-include end-point if necessary
                var cropDistanceSquared = actualHeadLength * this.ArrowHeadPosition * actualHeadLength * this.ArrowHeadPosition;
                for (int i = points.Count - 1; i >= 0; i--)
                {
                    if (points[i].DistanceToSquared(p1) <= cropDistanceSquared)
                        points.RemoveAt(i);
                }

                if (points.Count > 0)
                {
                    points.Add(endPoint);
                }
            }

            if (this.StrokeThickness > 0 && lineStyle != LineStyle.None)
            {
                rc.DrawLine(
                    points,
                    color,
                    this.StrokeThickness,
                    this.EdgeRenderingMode,
                    dashArray,
                    this.LineJoin);

                rc.DrawPolygon(
                    new[] { p3, p1, p2, endPoint },
                    color,
                    OxyColors.Undefined,
                    0,
                    this.EdgeRenderingMode);
            }
        }

        /// <summary>
        /// Gets the point on the series that is nearest the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">Interpolate the series if this flag is set to <c>true</c>.</param>
        /// <returns>A TrackerHitResult for the current hit.</returns>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            var p = this.InverseTransform(point);

            var colorAxis = this.ColorAxis as Axis;
            var colorAxisTitle = colorAxis?.Title ?? DefaultColorAxisTitle;

            if (this.ActualItems != null && this.ActualItems.Count > 0)
            {
                var item = Utilities.Helpers.ArgMin(this.ActualItems, i => this.Transform(i.Origin).DistanceToSquared(point));
                p = item.Origin;

                return new TrackerHitResult
                {
                    Series = this,
                    DataPoint = p,
                    Position = this.Transform(p),
                    Item = null,
                    Index = -1,
                    Text = StringHelper.Format(
                    this.ActualCulture,
                    this.TrackerFormatString,
                    item,
                    this.Title,
                    this.XAxis.Title ?? DefaultXAxisTitle,
                    this.XAxis.GetValue(p.X),
                    this.YAxis.Title ?? DefaultYAxisTitle,
                    this.YAxis.GetValue(p.Y),
                    colorAxisTitle,
                    item.Value,
                    item.Direction.X,
                    item.Direction.Y)
                };
            }

            // if no vectors, return null
            return null;
        }

        /// <summary>
        /// Ensures that the axes of the series is defined.
        /// </summary>
        protected internal override void EnsureAxes()
        {
            base.EnsureAxes();

            this.ColorAxis = this.ColorAxisKey != null ?
                             this.PlotModel.GetAxis(this.ColorAxisKey) as IColorAxis :
                             this.PlotModel.DefaultColorAxis as IColorAxis;
        }

        /// <summary>
        /// Sets default values from the plot model.
        /// </summary>
        protected internal override void SetDefaultValues()
        {
            if (this.Color.IsAutomatic() && this.ColorAxis == null)
            {
                this.defaultLineStyle = this.PlotModel.GetDefaultLineStyle();
                this.defaultColor = this.PlotModel.GetDefaultColor();
            }
        }

        /// <summary>
        /// Updates the maximum and minimum values of the series for the x and y dimensions only.
        /// </summary>
        protected internal void UpdateMaxMinXY()
        {
            if (this.ActualItems != null && this.ActualItems.Count > 0)
            {
                this.MinX = Math.Min(this.ActualItems.Min(r => r.Origin.X - r.Direction.X * this.ArrowStartPosition), this.ActualItems.Min(r => r.Origin.X - r.Direction.X * (this.ArrowStartPosition - 1)));
                this.MaxX = Math.Max(this.ActualItems.Max(r => r.Origin.X - r.Direction.X * this.ArrowStartPosition), this.ActualItems.Max(r => r.Origin.X - r.Direction.X * (this.ArrowStartPosition - 1)));
                this.MinY = Math.Min(this.ActualItems.Min(r => r.Origin.Y - r.Direction.Y * this.ArrowStartPosition), this.ActualItems.Min(r => r.Origin.Y - r.Direction.Y * (this.ArrowStartPosition - 1)));
                this.MaxY = Math.Max(this.ActualItems.Max(r => r.Origin.Y - r.Direction.Y * this.ArrowStartPosition), this.ActualItems.Max(r => r.Origin.Y - r.Direction.Y * (this.ArrowStartPosition - 1)));
            }
        }

        /// <summary>
        /// Updates the maximum and minimum values of the series.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();

            var allDataPoints = new List<DataPoint>();
            allDataPoints.AddRange(this.ActualItems.Select(item => item.Origin - item.Direction * this.ArrowStartPosition));
            allDataPoints.AddRange(this.ActualItems.Select(item => item.Origin + item.Direction * (1 - this.ArrowStartPosition)));
            this.InternalUpdateMaxMin(allDataPoints);

            this.UpdateMaxMinXY();

            if (this.ActualItems != null && this.ActualItems.Count > 0)
            {
                this.MinValue = this.ActualItems.Min(r => r.Value);
                this.MaxValue = this.ActualItems.Max(r => r.Value);
            }
        }

        /// <summary>
        /// Updates the axes to include the max and min of this series.
        /// </summary>
        protected internal override void UpdateAxisMaxMin()
        {
            base.UpdateAxisMaxMin();
            var colorAxis = this.ColorAxis as Axis;
            if (colorAxis != null)
            {
                colorAxis.Include(this.MinValue);
                colorAxis.Include(this.MaxValue);
            }
        }
    }
}
