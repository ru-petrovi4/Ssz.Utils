using System;

namespace Ssz.Operator.Core.ControlsCommon.Trends.ZoomLevels
{
    public class ValueZoomLevel : ZoomLevel<ValueZoomLevel>
    {
        #region private fields

        private readonly double _scaleCoefficient;

        #endregion

        #region construction and destruction

        public ValueZoomLevel(
            double scaleCoefficient,
            Func<ValueZoomLevel?>? previous = null,
            Func<ValueZoomLevel?>? next = null) :
            base(previous, next)
        {
            _scaleCoefficient = scaleCoefficient;
        }

        #endregion

        #region public functions

        public static readonly ValueZoomLevel One = new(0.1, next: () => Two);
        public static readonly ValueZoomLevel Two = new(0.2, () => One, () => Three);
        public static readonly ValueZoomLevel Three = new(0.5, () => Two, () => Four);
        public static readonly ValueZoomLevel Four = new(1.1, () => Three, () => Five);
        public static readonly ValueZoomLevel Five = new(2, () => Four);

        public static ValueZoomLevel Minimum = One;
        public static ValueZoomLevel Maximum = Five;

        public double VisibleRange(double minScale, double maxScale)
        {
            return (maxScale - minScale) * _scaleCoefficient;
        }

        #endregion
    }
}