using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay.ViewportRestrictions;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    public class HWZoomRestriction : IViewportRestriction
    {
        #region private fields

        private TimeSpan _interval;

        #endregion

        #region construction and destruction

        public HWZoomRestriction()
        {
            Interval = TimeSpan.FromMinutes(5);
        }

        #endregion

        #region public functions

        public DateTime? TimeMin;
        public double ValueMin, ValueMax;

        public TimeSpan Interval
        {
            get => _interval;
            set
            {
                if (_interval == value) return;
                _interval = value;
                Changed.Raise(this);
            }
        }

        public ScrollBar? HorizontalScrollBar { get; set; }
        public ScrollBar? VerticalScrollBar { get; set; }
        public double ValueRange { get; set; }
        public event EventHandler Changed = delegate { };

        public Rect Apply(Rect oldRect, Rect askedRect, Viewport2D viewport)
        {
            var chartPlotter = (ChartPlotter) viewport.Plotter;
            var horizontalAxis = (DateTimeAxis) chartPlotter.HorizontalAxis;

            var resultRect = new Rect
            {
                Y = 0,
                Height = 1,
                Width = horizontalAxis.ConvertToDouble(new DateTime(Interval.Ticks))
            };
            if (HorizontalScrollBar is not null && VerticalScrollBar is not null)
            {
                if (TimeMin.HasValue)
                    resultRect.X =
                        horizontalAxis.ConvertToDouble(TimeMin.Value +
                                                       TimeSpan.FromTicks((long) HorizontalScrollBar.Value));

                if (ValueMax > ValueMin)
                {
                    resultRect.Y = (ValueMin +
                                    (ValueMax - ValueMin) *
                                    (VerticalScrollBar.Maximum - VerticalScrollBar.Value - VerticalScrollBar.Minimum) /
                                    (VerticalScrollBar.Maximum - VerticalScrollBar.Minimum)) /
                                   (ValueMax - ValueMin);
                    resultRect.Height = ValueRange / (ValueMax - ValueMin);
                }
            }

            return resultRect;
        }

        #endregion
    }
}