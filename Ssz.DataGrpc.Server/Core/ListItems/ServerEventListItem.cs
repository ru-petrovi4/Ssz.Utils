using Ssz.Utils;
using Xi.Common.Support;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.ListItems
{
    public class ServerEventListItem
    {
        #region public functions

        public static EventListItem New()
        {
            return ObjectsCache<EventListItem>.GetObject();
        }

        /// <summary>
        ///     This property is provides the data type used to transported the value.
        /// </summary>
        public override TransportDataType ValueTransportTypeKey
        {
            get { return TransportDataType.EventMessage; }
        }

        public EventMessage EventMessage { get; set; }

        void ICacheObject.Initialize()
        {
            ClientAlias = 0;
            ServerAlias = 0;
            EntryQueued = false;
            StatusCode = 0;
            EventMessage = null;
        }

        void ICacheObject.Close()
        {            
        }

        public void Delete()
        {
            ObjectsCache<EventListItem>.ReturnObject(this);
        }

        #endregion
    }
}