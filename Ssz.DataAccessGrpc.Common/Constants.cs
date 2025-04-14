using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Common
{
    public static class Constants
    {
        /// <summary>
        ///     Size in bytes.
        /// </summary>
        public const int MaxReplyObjectSize = 1024 * 1024;

        public const int MaxEventMessagesCount = 1024;        
    }
}
