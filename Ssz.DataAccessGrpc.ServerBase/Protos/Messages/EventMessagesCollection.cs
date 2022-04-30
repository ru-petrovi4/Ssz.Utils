using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public sealed partial class EventMessagesCollection
    {
        #region public functions

        public List<EventMessagesCollection> SplitForCorrectGrpcMessageSize()
        {
            if (EventMessages.Count < MaxEventMessagesCount)
            {
                return new List<EventMessagesCollection> { this };
            }

            var result = new List<EventMessagesCollection>();

            int index = 0;
            EventMessagesCollection? prevEventMessagesCollection = null;
            while (index < EventMessages.Count)
            {
                var eventMessagesCollection = new EventMessagesCollection();
                if (prevEventMessagesCollection is not null)
                {
                    string guid = System.Guid.NewGuid().ToString();
                    prevEventMessagesCollection.NextCollectionGuid = guid;
                    eventMessagesCollection.Guid = guid;
                }

                eventMessagesCollection.EventMessages.AddRange(EventMessages.Skip(index).Take(MaxEventMessagesCount));

                result.Add(eventMessagesCollection);
                index += MaxEventMessagesCount;
                prevEventMessagesCollection = eventMessagesCollection;
            }

            return result;
        }

        #endregion

        #region private fields

        private const int MaxEventMessagesCount = 1024;

        #endregion 
    }
}
