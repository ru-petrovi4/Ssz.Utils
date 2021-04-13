using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class EventMessagesCollection
    {
        #region public functions

        public void Add(EventMessagesCollection eventMessagesCollection)
        {
            Guid = eventMessagesCollection.Guid;
            NextCollectionGuid = eventMessagesCollection.NextCollectionGuid;

            EventMessages.Add(eventMessagesCollection.EventMessages);
        }

        #endregion
    }
}
