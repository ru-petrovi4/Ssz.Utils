using Ssz.Dcs.CentralServer.ServerListItems;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.ServerListItems
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



//#region public functions

//public void UpdateIfChanged(Any value)
//{
//    if (ValueStatusTimestamp.StatusCode == StatusCodes.Unknown)
//    {
//        ValueStatusTimestamp.Value = value;
//        ValueStatusTimestamp.StatusCode = StatusCodes.Good;
//        ValueStatusTimestamp.TimestampUtc = DateTime.UtcNow;
//        Changed = true;
//    }
//    else
//    {
//        if (value != ValueStatusTimestamp.Value)
//        {
//            ValueStatusTimestamp.Value = value;
//            ValueStatusTimestamp.StatusCode = StatusCodes.Good;
//            ValueStatusTimestamp.TimestampUtc = DateTime.UtcNow;
//            Changed = true;
//        }
//    }
//}

//#endregion
