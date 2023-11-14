using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Threading;


namespace Ssz.Dcs.CentralServer.ServerListItems
{    
    public class ElementValueListItemBase : ElementListItemBase
    {
        #region construction and destruction

        public ElementValueListItemBase(uint clientAlias, uint serverAlias, string elementId)
            : base(clientAlias, serverAlias, elementId)
        {
        }

        #endregion

        #region public functions

        public bool Changed { get; set; }

        public ValueStatusTimestamp ValueStatusTimestamp { get; private set; } = new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.Uncertain };

        public bool? IsReadable { get; set; }

        public bool? IsWritable { get; set; }

        /// <summary>
        ///     Does not check for value change.
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        public void UpdateValueStatusTimestamp(ValueStatusTimestamp valueStatusTimestamp)
        {
            if (ValueStatusCodes.IsUncertain(valueStatusTimestamp.ValueStatusCode))
            {
                if (!ValueStatusCodes.IsUncertain(ValueStatusTimestamp.ValueStatusCode))
                {
                    ValueStatusTimestamp = valueStatusTimestamp;
                    Changed = true;
                }
            }
            else
            {
                ValueStatusTimestamp = valueStatusTimestamp;
                Changed = true;
            }            
        }

        public void Touch()
        {
            if (ValueStatusCodes.IsUncertain(ValueStatusTimestamp.ValueStatusCode))
                return;
            Changed = true;
        }

        public ValueStatusTimestamp? PendingWriteValueStatusTimestamp { get; set; }

        #endregion
    }
}