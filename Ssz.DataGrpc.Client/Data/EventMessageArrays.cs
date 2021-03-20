using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class EventMessageArrays
    {
        #region public functions

        public void Add(EventMessageArrays eventMessageArrays)
        {
            Guid = eventMessageArrays.Guid;
            NextArraysGuid = eventMessageArrays.NextArraysGuid;

            EventMessages.Add(eventMessageArrays.EventMessages);
        }

        #endregion
    }
}
