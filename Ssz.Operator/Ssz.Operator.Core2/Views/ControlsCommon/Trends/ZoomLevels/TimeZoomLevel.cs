using System;

namespace Ssz.Operator.Core.ControlsCommon.Trends.ZoomLevels
{
    public class TimeZoomLevel : ZoomLevel<TimeZoomLevel>
    {
        #region construction and destruction

        private TimeZoomLevel(
            TimeSpan visibleRange,
            Func<TimeZoomLevel?>? previous = null,
            Func<TimeZoomLevel?>? next = null) :
            base(previous, next)
        {
            VisibleRange = visibleRange;
        }

        #endregion

        #region private fields

        #endregion

        #region public functions

        public static readonly TimeZoomLevel One = new(TimeSpan.FromMinutes(1), next: () => Two);
        public static readonly TimeZoomLevel Two = new(TimeSpan.FromSeconds(90), () => One, () => Three);
        public static readonly TimeZoomLevel Three = new(TimeSpan.FromMinutes(3), () => Two, () => Four);
        public static readonly TimeZoomLevel Four = new(TimeSpan.FromMinutes(6), () => Three, () => Five);
        public static readonly TimeZoomLevel Five = new(TimeSpan.FromMinutes(12), () => Four, () => Six);
        public static readonly TimeZoomLevel Six = new(TimeSpan.FromMinutes(24), () => Five, () => Seven);
        public static readonly TimeZoomLevel Seven = new(TimeSpan.FromMinutes(48), () => Six, () => Eight);
        public static readonly TimeZoomLevel Eight = new(TimeSpan.FromMinutes(96), () => Seven);

        public static readonly TimeZoomLevel Minimum = One;
        public static readonly TimeZoomLevel Maximum = Eight;

        public TimeSpan VisibleRange { get; }

        #endregion
    }
}