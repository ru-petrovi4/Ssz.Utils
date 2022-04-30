using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client.ClientListItems
{
    /// <summary>
    /// 
    /// </summary>
    internal class ClientElementValuesJournalListItem : ClientElementListItemBase
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates an DataAccessGrpc Data Object using its client alias and Instance Id.
        /// </summary>        
        /// <param name="elementId"> The InstanceId used by the server to identify the data object. </param>
        public ClientElementValuesJournalListItem(string elementId)
            : base(elementId)
        {
        }

        #endregion
    }
}
