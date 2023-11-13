using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    /// <summary>
    ///     Struct
    /// </summary>
    public struct DsParam : IDisposable
    {
        #region construction and destruction        

        public void Dispose()
        {
            Connection?.Dispose();
            if (Connections is not null)
            {
                foreach (var connection in Connections)
                {
                    connection?.Dispose();
                }
            }
            Connection = null;            
        }

        #endregion

        #region public functions
        
        public Any Value;

        public DsConnectionBase? Connection;

        public Any[] Values;

        public DsConnectionBase?[] Connections;        

        /// <summary>
        ///     No count value check.
        ///     If arrays sizes equal does nothing
        /// </summary>
        /// <param name="count"></param>
        public void Resize(int count)
        {
            Array.Resize(ref Values, count);
            Array.Resize(ref Connections, count);
        }        

        #endregion
    }
}
