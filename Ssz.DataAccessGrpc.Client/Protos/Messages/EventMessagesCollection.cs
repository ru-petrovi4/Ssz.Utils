using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    internal sealed partial class EventMessagesCollection
    {
        #region public functions

        public void CombineWith(EventMessagesCollection nextEventMessagesCollection)
        {
            Guid = nextEventMessagesCollection.Guid;
            NextCollectionGuid = nextEventMessagesCollection.NextCollectionGuid;

            EventMessages.Add(nextEventMessagesCollection.EventMessages);
        }

        #endregion
    }
}
