using System;

namespace Ssz.Operator.Core.ControlsCommon.Trends.ZoomLevels
{
    public class ZoomLevel<T> where T : ZoomLevel<T>
    {
        #region construction and destruction

        public ZoomLevel(Func<T?>? previous = null, Func<T?>? next = null)
        {
            _previous = previous ?? (() => null);
            _next = next ?? (() => null);
        }

        #endregion

        #region public functions

        public T? Next => _next();

        public T? Previous => _previous();

        public bool IsMinimum => Previous is null;

        public bool IsMaximum => Next is null;

        #endregion

        #region private fields

        private readonly Func<T?> _previous;
        private readonly Func<T?> _next;

        #endregion
    }
}