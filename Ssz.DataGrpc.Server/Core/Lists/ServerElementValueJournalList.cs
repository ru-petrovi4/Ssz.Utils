using Ssz.DataGrpc.Server.Core.Context;
using Ssz.DataGrpc.Server.Core.ListItems;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.Lists
{
    /// <summary>
    ///   The Data Journal List is used to represent a collection of historical 
    ///   process data values.  Each value contained by the Data Journal List
    ///   contains a collection of data values for a specified time interval.  
    ///   There are several options as to the exact nature of this collection 
    ///   of data values, the data value collection may be raw values or values 
    ///   process (calculated) according to the servers capabilities.
    /// </summary>
    public abstract class ServerElementValueJournalList :
        ElementListBase<TElementValueJournalListItemBase>
        where TElementValueJournalListItemBase : ElementValueJournalListItem
    {
        #region construction and destruction

        /// <summary>
        ///   This constructor is simply a pass through place holder.
        /// </summary>
        /// <param name = "context"></param>
        /// <param name = "clientId"></param>
        /// <param name = "updateRate"></param>
        /// <param name = "listType"></param>
        /// <param name = "listKey"></param>
        public ElementValueJournalListBase(ServerContext<ServerListRoot> context, uint clientId, uint updateRate, uint bufferingRate,
                               uint listType, uint listKey, StandardMib mib)
            : base(context, clientId, updateRate, bufferingRate, listType, listKey, mib)
        {
        }

        #endregion

        #region public functions

        public override uint OnTouchList()
        {
            return XiFaultCodes.S_OK;
        }

        #endregion
    }
}