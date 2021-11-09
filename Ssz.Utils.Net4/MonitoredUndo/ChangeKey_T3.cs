using System.Globalization;

namespace Ssz.Utils.MonitoredUndo
{
    /// <summary>
    ///     Used to uniquely identify a change that has a 3-part "key".
    /// </summary>
    public class ChangeKey<T1, T2, T3>
    {
        #region construction and destruction

        public ChangeKey(T1 item1, T2 item2, T3 item3)
        {
            _mOne = item1;
            _mTwo = item2;
            _mThree = item3;
        }

        #endregion

        #region public functions

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            var tuple = obj as ChangeKey<T1, T2, T3>;
            if (tuple is null)
            {
                return false;
            }
            if (Equals(_mOne, tuple._mOne))
            {
                if (Equals(_mTwo, tuple._mTwo))
                {
                    return Equals(_mThree, tuple._mThree);
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return CombineHashCodes(_mOne.GetHashCode(), CombineHashCodes(_mTwo.GetHashCode(), _mThree.GetHashCode()));
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Tuple of '{0}', '{1}', '{2}'", _mOne, _mTwo, _mThree);
        }

        public T1 Item1
        {
            get { return _mOne; }
        }

        public T2 Item2
        {
            get { return _mTwo; }
        }

        public T3 Item3
        {
            get { return _mThree; }
        }

        #endregion

        #region internal functions

        internal static int CombineHashCodes(int h1, int h2)
        {
            return ((h1 << 5) + h1) ^ h2;
        }

        #endregion

        #region private fields

        private readonly T1 _mOne;
        private readonly T2 _mTwo;
        private readonly T3 _mThree;

        #endregion
    }
}