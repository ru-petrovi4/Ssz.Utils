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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class TimelinePanel : Panel, IScrollInfo
    {
        #region Members

        private List<DateElement> _visibleElements;

        #endregion //Members

        #region Constructors

        static TimelinePanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimelinePanel),
                new FrameworkPropertyMetadata(typeof(TimelinePanel)));
        }

        #endregion //Constructors

        #region Properties

        #region BeginDate Property

        public static readonly DependencyProperty BeginDateProperty = DependencyProperty.Register("BeginDate",
            typeof(DateTime), typeof(TimelinePanel),
            new FrameworkPropertyMetadata(DateTime.MinValue, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public DateTime BeginDate
        {
            get => (DateTime) GetValue(BeginDateProperty);
            set => SetValue(BeginDateProperty, value);
        }

        #endregion

        #region EndDate Property

        public static readonly DependencyProperty EndDateProperty = DependencyProperty.Register("EndDate",
            typeof(DateTime), typeof(TimelinePanel),
            new FrameworkPropertyMetadata(DateTime.MinValue, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public DateTime EndDate
        {
            get => (DateTime) GetValue(EndDateProperty);
            set => SetValue(EndDateProperty, value);
        }

        #endregion

        #region OverlapBehavior Property

        public static readonly DependencyProperty OverlapBehaviorProperty =
            DependencyProperty.Register("OverlapBehavior", typeof(OverlapBehavior), typeof(TimelinePanel),
                new FrameworkPropertyMetadata(default(OverlapBehavior),
                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        public OverlapBehavior OverlapBehavior
        {
            get => (OverlapBehavior) GetValue(OverlapBehaviorProperty);
            set => SetValue(OverlapBehaviorProperty, value);
        }

        #endregion

        #region KeepOriginalOrderForOverlap Property

        public static readonly DependencyProperty KeepOriginalOrderForOverlapProperty =
            DependencyProperty.Register("KeepOriginalOrderForOverlap", typeof(bool), typeof(TimelinePanel),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool KeepOriginalOrderForOverlap
        {
            get => (bool) GetValue(KeepOriginalOrderForOverlapProperty);
            set => SetValue(KeepOriginalOrderForOverlapProperty, value);
        }

        #endregion

        #region Orientation Property

        public static readonly DependencyProperty OrientationProperty =
            StackPanel.OrientationProperty.AddOwner(typeof(TimelinePanel),
                new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public Orientation Orientation
        {
            get => (Orientation) GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        #endregion

        #region UnitTimeSpan Property

        public static readonly DependencyProperty UnitTimeSpanProperty = DependencyProperty.Register("UnitTimeSpan",
            typeof(TimeSpan), typeof(TimelinePanel),
            new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public TimeSpan UnitTimeSpan
        {
            get => (TimeSpan) GetValue(UnitTimeSpanProperty);
            set => SetValue(UnitTimeSpanProperty, value);
        }

        #endregion

        #region UnitSize Property

        public static readonly DependencyProperty UnitSizeProperty = DependencyProperty.Register("UnitSize",
            typeof(double), typeof(TimelinePanel),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double UnitSize
        {
            get => (double) GetValue(UnitSizeProperty);
            set => SetValue(UnitSizeProperty, value);
        }

        #endregion

        #region Date Attached Property

        public static readonly DependencyProperty DateProperty = DependencyProperty.RegisterAttached("Date",
            typeof(DateTime), typeof(TimelinePanel),
            new FrameworkPropertyMetadata(DateTime.MinValue, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static DateTime GetDate(DependencyObject obj)
        {
            return (DateTime) obj.GetValue(DateProperty);
        }

        public static void SetDate(DependencyObject obj, DateTime value)
        {
            obj.SetValue(DateProperty, value);
        }

        #endregion

        #region DateEnd Attached Property

        public static readonly DependencyProperty DateEndProperty = DependencyProperty.RegisterAttached("DateEnd",
            typeof(DateTime), typeof(TimelinePanel),
            new FrameworkPropertyMetadata(DateTime.MinValue, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static DateTime GetDateEnd(DependencyObject obj)
        {
            return (DateTime) obj.GetValue(DateEndProperty);
        }

        public static void SetDateEnd(DependencyObject obj, DateTime value)
        {
            obj.SetValue(DateEndProperty, value);
        }

        #endregion

        #endregion //Properties

        #region Base Class Overrides

        protected override Size MeasureOverride(Size availableSize)
        {
            var calcBeginDate = DateTime.MaxValue;
            var calcEndDate = DateTime.MinValue;

            foreach (UIElement child in InternalChildren)
            {
                var date = GetDate(child);
                var dateEnd = GetDateEnd(child);

                if (date < calcBeginDate) calcBeginDate = date;

                if (date > calcEndDate) calcEndDate = date;

                if (dateEnd > calcEndDate) calcEndDate = dateEnd;
            }

            if (BeginDate == DateTime.MinValue) BeginDate = calcBeginDate;

            if (EndDate == DateTime.MinValue) EndDate = calcEndDate;

            foreach (UIElement child in InternalChildren)
            {
                var date = GetDate(child);
                var dateEnd = GetDateEnd(child);

                var childSize = availableSize;

                if (dateEnd > DateTime.MinValue && dateEnd > date)
                {
                    if (Orientation == Orientation.Horizontal)
                    {
                        if (UnitTimeSpan != TimeSpan.Zero && UnitSize > 0)
                        {
                            var size = (dateEnd.Ticks - date.Ticks) / (double) UnitTimeSpan.Ticks;

                            childSize.Width = size * UnitSize;
                        }
                        else if (!double.IsPositiveInfinity(availableSize.Width))
                        {
                            // width is DateEnd - Date
                            childSize.Width = CalculateTimelineOffset(dateEnd, availableSize.Width) -
                                              CalculateTimelineOffset(date, availableSize.Width);
                        }
                    }
                    else
                    {
                        if (UnitTimeSpan != TimeSpan.Zero && UnitSize > 0)
                        {
                            var size = (dateEnd.Ticks - date.Ticks) / (double) UnitTimeSpan.Ticks;

                            childSize.Height = size * UnitSize;
                        }
                        else if (!double.IsPositiveInfinity(availableSize.Height))
                        {
                            // height is DateEnd - Date
                            childSize.Height = CalculateTimelineOffset(dateEnd, availableSize.Height) -
                                               CalculateTimelineOffset(date, availableSize.Height);
                        }
                    }
                }

                child.Measure(childSize);
            }

            var newAvailableSize = new Size(availableSize.Width, availableSize.Height);

            if (UnitTimeSpan != TimeSpan.Zero && UnitSize > 0)
            {
                var size = (EndDate.Ticks - BeginDate.Ticks) / (double) UnitTimeSpan.Ticks;

                if (Orientation == Orientation.Horizontal)
                    newAvailableSize.Width = size * UnitSize;
                else
                    newAvailableSize.Height = size * UnitSize;
            }

            var desiredSize = new Size();

            if (Orientation == Orientation.Vertical && double.IsPositiveInfinity(newAvailableSize.Height) ||
                Orientation == Orientation.Horizontal && double.IsPositiveInfinity(newAvailableSize.Width))
            {
                // Our panel cannot layout items when we have positive infinity in the layout direction
                // so defer to arrange pass.
                _visibleElements = null;
            }
            else
            {
                LayoutItems(InternalChildren, newAvailableSize);

                var desiredRect = new Rect();

                foreach (var child in _visibleElements) desiredRect.Union(child.PlacementRectangle);

                if (Orientation == Orientation.Horizontal)
                {
                    desiredSize.Width = newAvailableSize.Width;
                    desiredSize.Height = desiredRect.Size.Height;
                }
                else
                {
                    desiredSize.Width = desiredRect.Size.Width;
                    desiredSize.Height = newAvailableSize.Height;
                }
            }

            if (IsScrolling)
            {
                var viewport = new Size(availableSize.Width, availableSize.Height);
                var extent = new Size(desiredSize.Width, desiredSize.Height);
                var offset = new Vector(Math.Max(0, Math.Min(_offset.X, extent.Width - viewport.Width)),
                    Math.Max(0, Math.Min(_offset.Y, extent.Height - viewport.Height)));

                SetScrollingData(viewport, extent, offset);

                desiredSize.Width = Math.Min(desiredSize.Width, availableSize.Width);
                desiredSize.Height = Math.Min(desiredSize.Height, availableSize.Height);

                _physicalViewport = availableSize;
            }

            return desiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var finalRect = new Rect();

            if (_visibleElements is null) LayoutItems(InternalChildren, finalSize);

            foreach (var child in _visibleElements)
            {
                if (IsScrolling)
                {
                    var placement = new Rect(child.PlacementRectangle.Location, child.PlacementRectangle.Size);
                    placement.Offset(-_offset);

                    child.Element.Arrange(placement);
                }
                else
                {
                    child.Element.Arrange(child.PlacementRectangle);
                }

                finalRect.Union(child.PlacementRectangle);
            }

            Size renderSize;

            if (Orientation == Orientation.Horizontal)
                renderSize = new Size(finalSize.Width, finalRect.Size.Height);
            else
                renderSize = new Size(finalRect.Size.Width, finalSize.Height);

            return renderSize;
        }

        #endregion //Base Class Overrides

        #region Methods

        private void ResetScrollInfo()
        {
            _offset = new Vector();
            _physicalViewport = _viewport = _extent = new Size(0, 0);
        }

        private void SetScrollingData(Size viewport, Size extent, Vector offset)
        {
            _offset = offset;

            if (!AreVirtuallyEqual(viewport, _viewport) ||
                !AreVirtuallyEqual(extent, _extent) ||
                !AreVirtuallyEqual(offset, _computedOffset))
            {
                _viewport = viewport;
                _extent = extent;
                _offset = offset;

                OnScrollChange();
            }
        }

        private double ValidateInputOffset(double offset, string parameterName)
        {
            if (double.IsNaN(offset))
                throw new ArgumentOutOfRangeException(parameterName);

            return Math.Max(0d, offset);
        }

        private void OnScrollChange()
        {
            if (ScrollOwner is not null) ScrollOwner.InvalidateScrollInfo();
        }

        private double CalculateTimelineOffset(DateTime d, double widthFinal)
        {
            double offset;

            var tickRange = EndDate.Ticks - BeginDate.Ticks;
            var tickOffset = d.Ticks - BeginDate.Ticks;

            if (UnitTimeSpan != TimeSpan.Zero && UnitSize > 0)
            {
                offset = tickOffset / (double) UnitTimeSpan.Ticks * UnitSize;
            }
            else
            {
                if (tickRange > 0)
                    offset = tickOffset / (double) tickRange * widthFinal;
                else
                    offset = 0;
            }

            return offset;
        }

        private static int CompareElementsByLeft(DateElement a, DateElement b)
        {
            return a.PlacementRectangle.Left.CompareTo(b.PlacementRectangle.Left);
        }

        private static int CompareElementsByTop(DateElement a, DateElement b)
        {
            return a.PlacementRectangle.Top.CompareTo(b.PlacementRectangle.Top);
        }

        private void LayoutItems(UIElementCollection children, Size availableSize)
        {
            _visibleElements = new List<DateElement>();
            var overlappingElements = new List<DateElement>();

            var index = 0;
            foreach (UIElement child in children)
            {
                if (child is null)
                    continue;

                var date = GetDate(child);
                var dateEnd = GetDateEnd(child);

                if (child.Visibility != Visibility.Collapsed)
                {
                    if (KeepOriginalOrderForOverlap)
                        _visibleElements.Add(new DateElement(child, date, dateEnd, index));
                    else
                        _visibleElements.Add(new DateElement(child, date, dateEnd));
                }

                index++;
            }

            _visibleElements.Sort();

            foreach (var child in _visibleElements)
            {
                var date = GetDate(child.Element);
                var dateEnd = GetDateEnd(child.Element);

                if (Orientation == Orientation.Vertical)
                {
                    //---------------------------------------------------------------------
                    // 
                    // Begin Layout Algorithm (Vertical Orientation)
                    //
                    //---------------------------------------------------------------------

                    // calculate the values for y (top) and height

                    // y
                    child.PlacementRectangle.Y = CalculateTimelineOffset(date, availableSize.Height);

                    // height
                    if (dateEnd > DateTime.MinValue && dateEnd > date)
                        // height is DateEnd - Date
                        child.PlacementRectangle.Height = CalculateTimelineOffset(dateEnd, availableSize.Height) -
                                                          CalculateTimelineOffset(date, availableSize.Height);
                    else
                        // height is the desired size
                        child.PlacementRectangle.Height = child.Element.DesiredSize.Height;

                    // now calcualte the values for x (left) and width based on the OverlapBehavior

                    switch (OverlapBehavior)
                    {
                        //---------------------------------------------------------------------
                        //
                        // OverlapBehavior == None (Vertical)
                        //
                        //---------------------------------------------------------------------

                        case OverlapBehavior.None:

                            #region OverlapBehavior == None (Vertical)

                            child.PlacementRectangle.X = 0;
                            child.PlacementRectangle.Width = child.Element.DesiredSize.Width;

                            break;

                        #endregion

                        //---------------------------------------------------------------------
                        //
                        // OverlapBehavior == Hide (Vertical)
                        //
                        //---------------------------------------------------------------------

                        case OverlapBehavior.Hide:

                            #region OverlapBehavior = Hide

                            overlappingElements.Clear();

                            foreach (var compare in _visibleElements)
                            {
                                if (child == compare)
                                    break;

                                var childRect = child.PlacementRectangle;
                                var compareRect = compare.PlacementRectangle;

                                if (childRect.Top >= compareRect.Top && childRect.Top < compareRect.Bottom)
                                    overlappingElements.Add(compare);
                            }

                            if (overlappingElements.Count > 0)
                            {
                                child.PlacementRectangle.X = 0;
                                child.PlacementRectangle.Y = 0;
                                child.PlacementRectangle.Width = 0;
                                child.PlacementRectangle.Height = 0;
                            }
                            else
                            {
                                child.PlacementRectangle.X = 0;
                                child.PlacementRectangle.Width = child.Element.DesiredSize.Width;
                            }

                            break;

                        #endregion

                        //---------------------------------------------------------------------
                        //
                        // OverlapBehavior == Stretch (Vertical)
                        //
                        //---------------------------------------------------------------------

                        case OverlapBehavior.Stretch:

                            #region OverlapBehavior = Stretch

                            // find the first gap at the desired vertical (Y) location (note that in doing this, we 
                            // only need to look for elements that intersect at the "top" of the item that is being
                            // placed because the _VisibleElements collection has been sorted so that the first 
                            // items come first--this means there won't be anything "below" the current item that
                            // isn't also below the current item

                            // find all overlapping elements

                            overlappingElements.Clear();

                            foreach (var compare in _visibleElements)
                            {
                                if (child == compare)
                                    break;

                                var childRect = child.PlacementRectangle;
                                var compareRect = compare.PlacementRectangle;

                                if (childRect.Top >= compareRect.Top && childRect.Top < compareRect.Bottom)
                                    overlappingElements.Add(compare);
                            }

                            // sort the elements according to their "left" value so that as we search through
                            // the list we are able to identiy gaps

                            overlappingElements.Sort(CompareElementsByLeft);

                            // initialize left and width such that the item will be stretch to fill the available 
                            // space and if there are no overlapping items, skip to the end

                            double left = 0;
                            var width = availableSize.Width;

                            if (overlappingElements.Count > 0)
                            {
                                var foundGap = false;

                                // now, look for a gap (there is a good chance that there won't be one, but if there is 
                                // then we will place the item in it)

                                for (var i = 0; i < overlappingElements.Count; i++)
                                {
                                    var r = overlappingElements[i].PlacementRectangle;

                                    // if this is the first overlapping element, then look for a gap at the beginning
                                    if (i == 0)
                                        if (r.Left > 0)
                                        {
                                            left = 0;
                                            width = r.Left;
                                            foundGap = true;
                                            break;
                                        }

                                    // if this is the last overlapping element
                                    if (i == overlappingElements.Count - 1)
                                        //left = r.Right;
                                        break;

                                    // if this is an element somewhere in the middle, then 

                                    var n = overlappingElements[i + 1].PlacementRectangle;
                                    if (n.Left - r.Right > 0)
                                    {
                                        left = r.Right;
                                        width = n.Left - r.Right;
                                        foundGap = true;
                                        break;
                                    }
                                }

                                // if we didn't find a gap, we need to make one by scooting the overlapping elements 
                                // over and then placing the item at the end

                                if (!foundGap)
                                {
                                    width = Math.Min(availableSize.Width / (overlappingElements.Count + 1),
                                        overlappingElements[0].PlacementRectangle.Width);
                                    left = 0;

                                    foreach (var e in overlappingElements)
                                    {
                                        e.PlacementRectangle.Width = width;
                                        e.PlacementRectangle.X = left;
                                        left += width;
                                    }
                                }
                            }

                            child.PlacementRectangle.X = left;

                            if (double.IsPositiveInfinity(width))
                                child.PlacementRectangle.Width = child.Element.DesiredSize.Width;
                            else
                                child.PlacementRectangle.Width = width;

                            break;

                        #endregion

                        //---------------------------------------------------------------------
                        //
                        // OverlapBehavior == Stack (Vertical)
                        //
                        //---------------------------------------------------------------------

                        case OverlapBehavior.Stack:

                            #region OverlapBehavior = Stack;

                            overlappingElements.Clear();

                            foreach (var compare in _visibleElements)
                            {
                                if (child == compare)
                                    break;

                                var childRect = child.PlacementRectangle;
                                var compareRect = compare.PlacementRectangle;

                                if (childRect.Top >= compareRect.Top && childRect.Top < compareRect.Bottom)
                                    overlappingElements.Add(compare);
                            }

                            // sort the elements according to their "left" value so that as we search through
                            // the list we are able to identiy gaps

                            overlappingElements.Sort(CompareElementsByLeft);

                            // initialize left and width values, width will always be it's desired size

                            left = 0;
                            child.PlacementRectangle.Width = child.Element.DesiredSize.Width;

                            // find the first gap that is big enough to accomodate the current item

                            for (var i = 0; i < overlappingElements.Count; i++)
                            {
                                var r = overlappingElements[i].PlacementRectangle;

                                if (i == 0)
                                    if (r.Left >= child.PlacementRectangle.Width)
                                    {
                                        left = 0;
                                        break;
                                    }

                                if (i == overlappingElements.Count - 1)
                                {
                                    left = r.Right;
                                    break;
                                }

                                var n = overlappingElements[i + 1].PlacementRectangle;
                                if (n.Left - r.Right >= child.PlacementRectangle.Width)
                                {
                                    left = r.Right;
                                    break;
                                }
                            }

                            child.PlacementRectangle.X = left;

                            break;

                        #endregion
                    }
                }
                else
                {
                    //---------------------------------------------------------------------
                    // 
                    // Begin Layout Algorithm (Horizontal Orientation)
                    //
                    //---------------------------------------------------------------------

                    // calculate the values for x (left) and width

                    // x
                    child.PlacementRectangle.X = CalculateTimelineOffset(date, availableSize.Width);

                    // width
                    if (dateEnd > DateTime.MinValue && dateEnd > date)
                        // width is DateEnd - Date
                        child.PlacementRectangle.Width = CalculateTimelineOffset(dateEnd, availableSize.Width) -
                                                         CalculateTimelineOffset(date, availableSize.Width);
                    else
                        // width is the desired size
                        child.PlacementRectangle.Width = child.Element.DesiredSize.Width;

                    // now calcualte the values for y (top) and height based on the OverlapBehavior

                    switch (OverlapBehavior)
                    {
                        //---------------------------------------------------------------------
                        //
                        // OverlapBehavior == None (Horizontal)
                        //
                        //---------------------------------------------------------------------

                        case OverlapBehavior.None:

                            #region OverlapBehavior == None (Horizontal)

                            child.PlacementRectangle.Y = 0;
                            child.PlacementRectangle.Height = child.Element.DesiredSize.Height;

                            break;

                        #endregion

                        //---------------------------------------------------------------------
                        //
                        // OverlapBehavior == Hide (Horizontal)
                        //
                        //---------------------------------------------------------------------

                        case OverlapBehavior.Hide:

                            #region OverlapBehavior = Hide (Horizontal)

                            overlappingElements.Clear();

                            foreach (var compare in _visibleElements)
                            {
                                if (child == compare)
                                    break;

                                var childRect = child.PlacementRectangle;
                                var compareRect = compare.PlacementRectangle;

                                if (childRect.Left >= compareRect.Left && childRect.Left < compareRect.Right)
                                    overlappingElements.Add(compare);
                            }

                            if (overlappingElements.Count > 0)
                            {
                                child.PlacementRectangle.X = 0;
                                child.PlacementRectangle.Y = 0;
                                child.PlacementRectangle.Width = 0;
                                child.PlacementRectangle.Height = 0;
                            }
                            else
                            {
                                child.PlacementRectangle.Y = 0;
                                child.PlacementRectangle.Height = child.Element.DesiredSize.Height;
                            }

                            break;

                        #endregion

                        //---------------------------------------------------------------------
                        //
                        // OverlapBehavior == Stretch (Horizontal)
                        //
                        //---------------------------------------------------------------------

                        case OverlapBehavior.Stretch:

                            #region OverlapBehavior = Stretch

                            // find the first gap at the desired vertical (Y) location (note that in doing this, we 
                            // only need to look for elements that intersect at the "top" of the item that is being
                            // placed because the _VisibleElements collection has been sorted so that the first 
                            // items come first--this means there won't be anything "below" the current item that
                            // isn't also below the current item

                            // find all overlapping elements

                            overlappingElements.Clear();

                            foreach (var compare in _visibleElements)
                            {
                                if (child == compare)
                                    break;

                                var childRect = child.PlacementRectangle;
                                var compareRect = compare.PlacementRectangle;

                                if (childRect.Left >= compareRect.Left && childRect.Left < compareRect.Right)
                                    overlappingElements.Add(compare);
                            }

                            // sort the elements according to their "left" value so that as we search through
                            // the list we are able to identiy gaps

                            overlappingElements.Sort(CompareElementsByTop);

                            // initialize left and width such that the item will be stretch to fill the available 
                            // space and if there are no overlapping items, skip to the end

                            double top = 0;
                            var height = availableSize.Height;

                            if (overlappingElements.Count > 0)
                            {
                                var foundGap = false;

                                // now, look for a gap (there is a good chance that there won't be one, but if there is 
                                // then we will place the item in it)

                                for (var i = 0; i < overlappingElements.Count; i++)
                                {
                                    var r = overlappingElements[i].PlacementRectangle;

                                    // if this is the first overlapping element, then look for a gap at the beginning
                                    if (i == 0)
                                        if (r.Top > 0)
                                        {
                                            top = 0;
                                            height = r.Top;
                                            foundGap = true;
                                            break;
                                        }

                                    // if this is the last overlapping element
                                    if (i == overlappingElements.Count - 1)
                                        //left = r.Right;
                                        break;

                                    // if this is an element somewhere in the middle, then 

                                    var n = overlappingElements[i + 1].PlacementRectangle;
                                    if (n.Top - r.Bottom > 0)
                                    {
                                        top = r.Bottom;
                                        height = n.Top - r.Bottom;
                                        foundGap = true;
                                        break;
                                    }
                                }

                                // if we didn't find a gap, we need to make one by scooting the overlapping elements 
                                // over and then placing the item at the end

                                if (!foundGap)
                                {
                                    height = Math.Min(availableSize.Height / (overlappingElements.Count + 1),
                                        overlappingElements[0].PlacementRectangle.Height);
                                    top = 0;

                                    foreach (var e in overlappingElements)
                                    {
                                        e.PlacementRectangle.Height = height;
                                        e.PlacementRectangle.Y = top;
                                        top += height;
                                    }
                                }
                            }

                            child.PlacementRectangle.Y = top;

                            if (double.IsPositiveInfinity(height))
                                child.PlacementRectangle.Height = child.Element.DesiredSize.Height;
                            else
                                child.PlacementRectangle.Height = height;

                            break;

                        #endregion

                        //---------------------------------------------------------------------
                        //
                        // OverlapBehavior == Stack (Horizontal)
                        //
                        //---------------------------------------------------------------------

                        case OverlapBehavior.Stack:

                            #region OverlapBehavior = Stack;

                            overlappingElements.Clear();

                            foreach (var compare in _visibleElements)
                            {
                                if (child == compare)
                                    break;

                                var childRect = child.PlacementRectangle;
                                var compareRect = compare.PlacementRectangle;

                                if (childRect.Left >= compareRect.Left && childRect.Left < compareRect.Right)
                                    overlappingElements.Add(compare);
                            }

                            // sort the elements according to their "left" value so that as we search through
                            // the list we are able to identiy gaps

                            overlappingElements.Sort(CompareElementsByTop);

                            // initialize left and width values, width will always be it's desired size

                            top = 0;
                            child.PlacementRectangle.Height = child.Element.DesiredSize.Height;

                            // find the first gap that is big enough to accomodate the current item

                            for (var i = 0; i < overlappingElements.Count; i++)
                            {
                                var r = overlappingElements[i].PlacementRectangle;

                                if (i == 0)
                                    if (r.Top >= child.PlacementRectangle.Height)
                                    {
                                        top = 0;
                                        break;
                                    }

                                if (i == overlappingElements.Count - 1)
                                {
                                    top = r.Bottom;
                                    break;
                                }

                                var n = overlappingElements[i + 1].PlacementRectangle;
                                if (n.Top - r.Bottom >= child.PlacementRectangle.Height)
                                {
                                    top = r.Bottom;
                                    break;
                                }
                            }

                            child.PlacementRectangle.Y = top;

                            break;

                        #endregion
                    }
                }
            }
        }

        private static bool AreVirtuallyEqual(double d1, double d2)
        {
            if (double.IsPositiveInfinity(d1))
                return double.IsPositiveInfinity(d2);

            if (double.IsNegativeInfinity(d1))
                return double.IsNegativeInfinity(d2);

            if (double.IsNaN(d1))
                return double.IsNaN(d2);

            var n = d1 - d2;
            var d = (Math.Abs(d1) + Math.Abs(d2) + 10) * 1.0e-15;
            return -d < n && d > n;
        }

        private static bool AreVirtuallyEqual(Size s1, Size s2)
        {
            return AreVirtuallyEqual(s1.Width, s2.Width)
                   && AreVirtuallyEqual(s1.Height, s2.Height);
        }

        private static bool AreVirtuallyEqual(Vector v1, Vector v2)
        {
            return AreVirtuallyEqual(v1.X, v2.X)
                   && AreVirtuallyEqual(v1.Y, v2.Y);
        }

        #endregion //Methods

        #region Interfaces

        #region IScrollInfo

        public bool CanHorizontallyScroll { get; set; } = false;

        public bool CanVerticallyScroll { get; set; } = false;

        public double ExtentHeight => _extent.Height;

        public double ExtentWidth => _extent.Width;

        public double HorizontalOffset => _offset.X;

        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + (Orientation == Orientation.Vertical ? 1d : 16d));
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - (Orientation == Orientation.Horizontal ? 1d : 16d));
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + (Orientation == Orientation.Horizontal ? 1d : 16d));
        }

        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - (Orientation == Orientation.Vertical ? 1d : 16d));
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return rectangle;
        }

        public void MouseWheelDown()
        {
            SetVerticalOffset(VerticalOffset +
                              SystemParameters.WheelScrollLines * (Orientation == Orientation.Vertical ? 1d : 16d));
        }

        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset - 3d * (Orientation == Orientation.Horizontal ? 1d : 16d));
        }

        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset + 3d * (Orientation == Orientation.Horizontal ? 1d : 16d));
        }

        public void MouseWheelUp()
        {
            SetVerticalOffset(VerticalOffset -
                              SystemParameters.WheelScrollLines * (Orientation == Orientation.Vertical ? 1d : 16d));
        }

        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + ViewportHeight);
        }

        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset - ViewportWidth);
        }

        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset + ViewportWidth);
        }

        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - ViewportHeight);
        }

        public ScrollViewer ScrollOwner
        {
            get => _scrollOwner;
            set
            {
                if (_scrollOwner != value)
                {
                    _scrollOwner = value;

                    ResetScrollInfo();
                }
            }
        }

        public void SetHorizontalOffset(double offset)
        {
            offset = ValidateInputOffset(offset, "HorizontalOffset");

            if (!AreVirtuallyEqual(offset, _offset.X))
            {
                _offset.X = offset;

                InvalidateMeasure();
            }
        }

        public void SetVerticalOffset(double offset)
        {
            offset = ValidateInputOffset(offset, "VerticalOffset");

            if (!AreVirtuallyEqual(offset, _offset.Y))
            {
                _offset.Y = offset;

                InvalidateMeasure();
            }
        }

        public double VerticalOffset => _offset.Y;

        public double ViewportHeight => _viewport.Height;

        public double ViewportWidth => _viewport.Width;

        private bool IsScrolling => _scrollOwner is not null;

        private readonly Vector _computedOffset = new(0d, 0d);
        private Size _extent = new(0, 0);
        private Vector _offset = new(0, 0);
        private ScrollViewer _scrollOwner;
        private Size _viewport;
        private Size _physicalViewport;

        #endregion //IScrollInfo

        #endregion //Interfaces
    }
}