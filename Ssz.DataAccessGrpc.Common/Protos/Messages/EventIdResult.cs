using Google.Protobuf.WellKnownTypes;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Common
{
    public sealed partial class EventIdResult
    {
        #region construction and destruction

        public EventIdResult(Ssz.Utils.DataAccess.EventIdResult eventIdResult)
        {
            StatusCode = eventIdResult.StatusCode;
            EventId = new EventId(eventIdResult.EventId);
        }

        #endregion
    }
}
