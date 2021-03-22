using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Client.ClientListItems
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientElementValueJournalListItem : ClientElementListItemBase
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates an DataGrpc Data Object using its client alias and Instance Id.
        /// </summary>        
        /// <param name="elementId"> The InstanceId used by the server to identify the data object. </param>
        public ClientElementValueJournalListItem(string elementId)
            : base(elementId)
        {
        }

        #endregion
    }
}
