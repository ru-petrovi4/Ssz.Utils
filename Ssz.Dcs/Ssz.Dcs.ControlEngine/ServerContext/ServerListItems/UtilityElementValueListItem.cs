using Ssz.Dcs.ControlEngine.ServerListItems;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine.ServerListItems
{
    public class UtilityElementValueListItem : ElementValueListItemBase
    {
        #region construction and destruction

        public UtilityElementValueListItem(uint clientAlias, uint serverAlias, string elementId)
            : base(clientAlias, serverAlias, elementId)
        {
        }        

        #endregion
    }
}