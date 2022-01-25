// ReSharper disable once CheckNamespace
namespace Fluent
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using Fluent.Internal;

    /// <summary>
    /// Represent panel with ribbon group.
    /// It is automatically adjusting size of controls
    /// </summary>
    public class RibbonGroupsContainer : Panel, IScrollInfo
    {
        private struct MeasureCache
        {
            public MeasureCache(Size availableSize, Size desiredSize)
            {
                this.AvailableSize = availableSize;
                this.DesiredSize = desiredSize;
            }

            public Size AvailableSize { get; }

            public Size DesiredSize { get; }
        }

        private MeasureCache measureCache;

        #region Reduce Order

        /// <summary>
        /// Gets or sets reduce order of group in the ribbon panel.
        /// It must be enumerated with comma from the first to reduce to
        /// the last to reduce (use Control.Name as group name in the enum).
        /// Enclose in parentheses as (Control.Name) to reduce/enlarge
        /// scalable elements in the given group
        /// </summary>
        public string? ReduceOrder
        {
            get { return (string?)this.GetValue(ReduceOrderProperty); }
            set { this.SetValue(ReduceOrderProperty, value); }
        }

        /// <summary>Identifies the <see cref="ReduceOrder"/> dependency property.</summary>
        public static readonly DependencyProperty ReduceOrderProperty =
            DependencyProperty.Register(nameof(ReduceOrder), typeof(string), typeof(RibbonGroupsContainer), new PropertyMetadata(OnReduceOrderChanged));

        // handles ReduseOrder property changed
        private static void OnReduceOrderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbonPanel = (RibbonGroupsContainer)d;

            for (var i = ribbonPanel.reduceOrderIndex; i < ribbonPanel.reduceOrder.Length - 1; i++)
            {
                ribbonPanel.IncreaseGroupBoxSize(ribbonPanel.reduceOrder[i]);
            }

            ribbonPanel.reduceOrder = (((string?)e.NewValue) ?? string.Empty).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var newReduceOrderIndex = ribbonPanel.reduceOrder.Length - 1;
            ribbonPanel.reduceOrderIndex = newReduceOrderIndex;

            ribbonPanel.InvalidateMeasure();
            ribbonPanel.InvalidateArrange();
        }

        #endregion

        #region Fields

        private string[] reduceOrder = new string[0];
        private int reduceOrderIndex;

        #endregion

        #region Initialization

        /// <summary>
        /// Default constructor
        /// </summary>
        public RibbonGroupsContainer()
        {
            this.Focusable = false;
        }

        #endregion

        #region Layout Overridings

        /// <inheritdoc />
        protected override UIElementCollection CreateUIElementCollection(FrameworkElement logicalParent)
        {
            return new UIElementCollection(this, /*Parent as FrameworkElement*/this);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var desiredSize = this.GetChildrenDesiredSizeIntermediate();

            if (this.reduceOrder.Length == 0
                // Check cached measure to prevent "flicker"
                || (this.measureCache.AvailableSize == availableSize && this.measureCache.DesiredSize == desiredSize))
            {
                this.VerifyScrollData(availableSize.Width, desiredSize.Width);
                return desiredSize;
            }

            // If we have more available space - try to expand groups
            while (desiredSize.Width <= availableSize.Width)
            {
                var hasMoreVariants = this.reduceOrderIndex < this.reduceOrder.Length - 1;
                if (hasMoreVariants == false)
                {
                    break;
                }

                // Increase size of another item
                this.reduceOrderIndex++;
                this.IncreaseGroupBoxSize(this.reduceOrder[this.reduceOrderIndex]);

                desiredSize = this.GetChildrenDesiredSizeIntermediate();
            }

            // If not enough space - go to next variant
            while (desiredSize.Width > availableSize.Width)
            {
                var hasMoreVariants = this.reduceOrderIndex >= 0;
                if (hasMoreVariants == false)
                {
                    break;
                }

                // Decrease size of another item
                this.DecreaseGroupBoxSize(this.reduceOrder[this.reduceOrderIndex]);
                this.reduceOrderIndex--;

                desiredSize = this.GetChildrenDesiredSizeIntermediate();
            }

            // Set find values
            foreach (var item in this.InternalChildren)
            {
                var groupBox = item as RibbonGroupBox;
                if (groupBox is null)
                {
                    continue;
                }

                if (groupBox.State != groupBox.StateIntermediate
                    || groupBox.Scale != groupBox.ScaleIntermediate)
                {
                    using (groupBox.CacheResetGuard.Start())
                    {
                        groupBox.State = groupBox.StateIntermediate;
                        groupBox.Scale = groupBox.ScaleIntermediate;
                        groupBox.InvalidateLayout();
                        groupBox.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                    }
                }

                // Something wrong with cache?
                if (groupBox.DesiredSizeIntermediate != groupBox.DesiredSize)
                {
                    // Reset cache and reinvoke measure
                    groupBox.ClearCache();
                    return this.MeasureOverride(availableSize);
                }
            }

            this.measureCache = new MeasureCache(availableSize, desiredSize);

            this.VerifyScrollData(availableSize.Width, desiredSize.Width);
            return desiredSize;
        }

        private Size GetChildrenDesiredSizeIntermediate()
        {
            double width = 0;
            double height = 0;

            foreach (UIElement? child in this.InternalChildren)
            {
                var groupBox = child as RibbonGroupBox;
                if (groupBox is null)
                {
                    continue;
                }

                var desiredSize = groupBox.DesiredSizeIntermediate;
                width += desiredSize.Width;
                height = Math.Max(height, desiredSize.Height);
            }

            return new Size(width, height);
        }

        // Increase size of the item
        private void IncreaseGroupBoxSize(string name)
        {
            var groupBox = this.FindGroup(name);
            var scale = name.StartsWith("(", StringComparison.OrdinalIgnoreCase);

            if (groupBox is null)
            {
                return;
            }

            if (scale)
            {
                groupBox.ScaleIntermediate++;
            }
            else
            {
                if (groupBox.IsSimplified)
                {
                    groupBox.StateIntermediate = groupBox.SimplifiedStateDefinition.EnlargeState(groupBox.StateIntermediate);
                }
                else
                {
                    groupBox.StateIntermediate = groupBox.StateDefinition.EnlargeState(groupBox.StateIntermediate);
                }
            }
        }

        // Decrease size of the item
        private void DecreaseGroupBoxSize(string name)
        {
            var groupBox = this.FindGroup(name);
            var scale = name.StartsWith("(", StringComparison.OrdinalIgnoreCase);

            if (groupBox is null)
            {
                return;
            }

            if (scale)
            {
                groupBox.ScaleIntermediate--;
            }
            else
            {
                if (groupBox.IsSimplified)
                {
                    groupBox.StateIntermediate = groupBox.SimplifiedStateDefinition.ReduceState(groupBox.StateIntermediate);
                }
                else
                {
                    groupBox.StateIntermediate = groupBox.StateDefinition.ReduceState(groupBox.StateIntermediate);
                }
            }
        }

        private RibbonGroupBox? FindGroup(string name)
        {
            if (name.StartsWith("(", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(1, name.Length - 2);
            }

            foreach (FrameworkElement? child in this.InternalChildren)
            {
                if (child?.Name == name)
                {
                    return child as RibbonGroupBox;
                }
            }

            return null;
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            var finalRect = new Rect(finalSize)
            {
                X = -this.HorizontalOffset
            };

            foreach (UIElement? item in this.InternalChildren)
            {
                if (item is null)
                {
                    continue;
                }

                finalRect.Width = item.DesiredSize.Width;
                finalRect.Height = Math.Max(finalSize.Height, item.DesiredSize.Height);
                item.Arrange(finalRect);
                finalRect.X += item.DesiredSize.Width;
            }

            return finalSize;
        }

        #endregion

        #region IScrollInfo Members

        /// <inheritdoc />
        public ScrollViewer? ScrollOwner
        {
            get { return this.ScrollData.ScrollOwner; }
            set { this.ScrollData.ScrollOwner = value; }
        }

        /// <inheritdoc />
        public void SetHorizontalOffset(double offset)
        {
            var newValue = CoerceOffset(ValidateInputOffset(offset, nameof(this.HorizontalOffset)), this.ScrollData.ExtentWidth, this.ScrollData.ViewportWidth);

            if (DoubleUtil.AreClose(this.ScrollData.OffsetX, newValue) == false)
            {
                this.ScrollData.OffsetX = newValue;
                this.InvalidateArrange();
            }
        }

        /// <inheritdoc />
        public double ExtentWidth
        {
            get { return this.ScrollData.ExtentWidth; }
        }

        /// <inheritdoc />
        public double HorizontalOffset
        {
            get { return this.ScrollData.OffsetX; }
        }

        /// <inheritdoc />
        public double ViewportWidth
        {
            get { return this.ScrollData.ViewportWidth; }
        }

        /// <inheritdoc />
        public void LineLeft()
        {
            this.SetHorizontalOffset(this.HorizontalOffset - 16.0);
        }

        /// <inheritdoc />
        public void LineRight()
        {
            this.SetHorizontalOffset(this.HorizontalOffset + 16.0);
        }

        /// <inheritdoc />
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            // We can only work on visuals that are us or children.
            // An empty rect has no size or position.  We can't meaningfully use it.
            if (rectangle.IsEmpty
                || visual is null
                || ReferenceEquals(visual, this)
                || !this.IsAncestorOf(visual))
            {
                return Rect.Empty;
            }

            // Compute the child's rect relative to (0,0) in our coordinate space.
            var childTransform = visual.TransformToAncestor(this);

            rectangle = childTransform.TransformBounds(rectangle);

            // Initialize the viewport
            var viewport = new Rect(this.HorizontalOffset, rectangle.Top, this.ViewportWidth, rectangle.Height);
            rectangle.X += viewport.X;

            // Compute the offsets required to minimally scroll the child maximally into view.
            var minX = ComputeScrollOffsetWithMinimalScroll(viewport.Left, viewport.Right, rectangle.Left, rectangle.Right);

            // We have computed the scrolling offsets; scroll to them.
            this.SetHorizontalOffset(minX);

            // Compute the visible rectangle of the child relative to the viewport.
            viewport.X = minX;
            rectangle.Intersect(viewport);

            rectangle.X -= viewport.X;

            // Return the rectangle
            return rectangle;
        }

        private static double ComputeScrollOffsetWithMinimalScroll(
            double topView,
            double bottomView,
            double topChild,
            double bottomChild)
        {
            // # CHILD POSITION       CHILD SIZE      SCROLL      REMEDY
            // 1 Above viewport       <= viewport     Down        Align top edge of child & viewport
            // 2 Above viewport       > viewport      Down        Align bottom edge of child & viewport
            // 3 Below viewport       <= viewport     Up          Align bottom edge of child & viewport
            // 4 Below viewport       > viewport      Up          Align top edge of child & viewport
            // 5 Entirely within viewport             NA          No scroll.
            // 6 Spanning viewport                    NA          No scroll.
            //
            // Note: "Above viewport" = childTop above viewportTop, childBottom above viewportBottom
            //       "Below viewport" = childTop below viewportTop, childBottom below viewportBottom
            // These child thus may overlap with the viewport, but will scroll the same direction
            /*bool fAbove = DoubleUtil.LessThan(topChild, topView) && DoubleUtil.LessThan(bottomChild, bottomView);
            bool fBelow = DoubleUtil.GreaterThan(bottomChild, bottomView) && DoubleUtil.GreaterThan(topChild, topView);*/
            var fAbove = (topChild < topView) && (bottomChild < bottomView);
            var fBelow = (bottomChild > bottomView) && (topChild > topView);
            var fLarger = bottomChild - topChild > bottomView - topView;

            // Handle Cases:  1 & 4 above
            if ((fAbove && !fLarger)
               || (fBelow && fLarger))
            {
                return topChild;
            }

            // Handle Cases: 2 & 3 above
            if (fAbove || fBelow)
            {
                return bottomChild - (bottomView - topView);
            }

            // Handle cases: 5 & 6 above.
            return topView;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void MouseWheelDown()
        {
        }

        /// <inheritdoc />
        public void MouseWheelLeft()
        {
            this.SetHorizontalOffset(this.HorizontalOffset - 16);
        }

        /// <inheritdoc />
        public void MouseWheelRight()
        {
            this.SetHorizontalOffset(this.HorizontalOffset + 16);
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void MouseWheelUp()
        {
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void LineDown()
        {
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void LineUp()
        {
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void PageDown()
        {
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void PageLeft()
        {
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void PageRight()
        {
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void PageUp()
        {
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public void SetVerticalOffset(double offset)
        {
        }

        /// <inheritdoc />
        public bool CanVerticallyScroll
        {
            get { return false; }
            set { }
        }

        /// <inheritdoc />
        public bool CanHorizontallyScroll
        {
            get { return true; }
            set { }
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public double ExtentHeight
        {
            get { return 0.0; }
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public double VerticalOffset
        {
            get { return 0.0; }
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public double ViewportHeight
        {
            get { return 0.0; }
        }

        // Gets scroll data info
        private ScrollData ScrollData
        {
            get
            {
                return this.scrollData ?? (this.scrollData = new ScrollData());
            }
        }

        // Scroll data info
        private ScrollData? scrollData;

        // Validates input offset
        private static double ValidateInputOffset(double offset, string parameterName)
        {
            if (double.IsNaN(offset))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return Math.Max(0.0, offset);
        }

        // Verifies scrolling data using the passed viewport and extent as newly computed values.
        // Checks the X/Y offset and coerces them into the range [0, Extent - ViewportSize]
        // If extent, viewport, or the newly coerced offsets are different than the existing offset,
        //   cachces are updated and InvalidateScrollInfo() is called.
        private void VerifyScrollData(double viewportWidth, double extentWidth)
        {
            var isValid = true;

            if (double.IsInfinity(viewportWidth))
            {
                viewportWidth = extentWidth;
            }

            var offsetX = CoerceOffset(this.ScrollData.OffsetX, extentWidth, viewportWidth);

            isValid &= DoubleUtil.AreClose(viewportWidth, this.ScrollData.ViewportWidth);
            isValid &= DoubleUtil.AreClose(extentWidth, this.ScrollData.ExtentWidth);
            isValid &= DoubleUtil.AreClose(this.ScrollData.OffsetX, offsetX);

            this.ScrollData.ViewportWidth = viewportWidth;
            this.ScrollData.ExtentWidth = extentWidth;
            this.ScrollData.OffsetX = offsetX;

            if (isValid == false)
            {
                this.ScrollOwner?.InvalidateScrollInfo();
            }
        }

        // Returns an offset coerced into the [0, Extent - Viewport] range.
        private static double CoerceOffset(double offset, double extent, double viewport)
        {
            if (offset > extent - viewport)
            {
                offset = extent - viewport;
            }

            if (offset < 0)
            {
                offset = 0;
            }

            return offset;
        }

        #endregion

        // We have to reset the reduce order to it's initial value, clear all caches we keep here and invalidate measure/arrange
#pragma warning disable CA1801 // Review unused parameters
        internal void GroupBoxCacheClearedAndStateAndScaleResetted(RibbonGroupBox ribbonGroupBox)
#pragma warning restore CA1801 // Review unused parameters
        {
            var ribbonPanel = this;

            var newReduceOrderIndex = ribbonPanel.reduceOrder.Length - 1;
            ribbonPanel.reduceOrderIndex = newReduceOrderIndex;

            this.measureCache = default;

            foreach (var item in this.InternalChildren)
            {
                var groupBox = item as RibbonGroupBox;
                if (groupBox is null)
                {
                    continue;
                }

                groupBox.TryClearCacheAndResetStateAndScale();
            }

            ribbonPanel.InvalidateMeasure();
            ribbonPanel.InvalidateArrange();
        }
    }
}