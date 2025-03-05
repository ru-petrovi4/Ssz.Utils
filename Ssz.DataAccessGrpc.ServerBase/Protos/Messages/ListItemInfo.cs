using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public sealed partial class ListItemInfo
    {
        #region public functions

        public Ssz.Utils.DataAccess.ListItemInfo ToListItemInfoMessage()
        {
            var listItemInfo = new Ssz.Utils.DataAccess.ListItemInfo();
            listItemInfo.ElementId = ElementId;
            listItemInfo.ClientAlias = ClientAlias;            
            return listItemInfo;
        }

        #endregion
    }
}
