using System;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    public class TrendPoint
    {
        #region construction and destruction

        /// <summary>
        ///     timestamp is local time
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="value"></param>
        public TrendPoint(DateTime timestamp, double value)
        {
            Timestamp = timestamp;
            Value = value;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Local time.
        /// </summary>
        public DateTime Timestamp { get; private set; }

        public double Value { get; private set; }

        #endregion
    }
}