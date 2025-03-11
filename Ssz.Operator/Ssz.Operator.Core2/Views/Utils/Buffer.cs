#nullable disable

using System.Collections.Generic;

namespace Ssz.Operator.Core.Utils
{
    public class Buffer<T> : List<T>
    {
        #region construction and destruction

        public Buffer()
        {
        }

        public Buffer(int capacity)
            : base(capacity)
        {
        }

        #endregion

        #region public functions

        public void ClearAndSetCapacity(int capacity)
        {
            Clear();
            if (capacity > Capacity) Capacity = capacity;
        }

        #endregion
    }
}