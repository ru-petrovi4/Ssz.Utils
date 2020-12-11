using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public class BytesArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        #region public functions

        /// <summary>
        /// 
        /// </summary>
        public static BytesArrayEqualityComparer Instance = new BytesArrayEqualityComparer();

        /// <summary>        
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(byte[] obj)
        {
            return 0;
        }

        #endregion
    }
}
