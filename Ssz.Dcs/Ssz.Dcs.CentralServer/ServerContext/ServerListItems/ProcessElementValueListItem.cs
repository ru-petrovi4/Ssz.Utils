using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.ServerListItems
{
    public class ProcessElementValueListItem : ElementValueListItemBase
    {
        #region construction and destruction

        public ProcessElementValueListItem(uint clientAlias, uint serverAlias, string elementId)
            : base(clientAlias, serverAlias, elementId)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            
			if (disposing)
			{
				foreach (ValueSubscription valueSubscription in ValueSubscriptionsCollection)
                {
                    valueSubscription.Dispose();
                }
			}

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public List<ValueSubscription> ValueSubscriptionsCollection { get; } = new();

        public void ValueSubscriptionsOnValueChanged()
        {
            if (ValueSubscriptionsCollection.All(vs => StatusCodes.IsBad(vs.ValueStatusTimestamp.StatusCode)))
            {
                UpdateValueStatusTimestamp(new ValueStatusTimestamp { StatusCode = StatusCodes.Bad });
            }
            else
            {
                ValueSubscription? valueSubscription = ValueSubscriptionsCollection.FirstOrDefault(vs => StatusCodes.IsGood(vs.ValueStatusTimestamp.StatusCode));
                if (valueSubscription is not null)
                {
                    UpdateValueStatusTimestamp(valueSubscription.ValueStatusTimestamp);
                }
                else
                {
                    UpdateValueStatusTimestamp(new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain });
                }
            }
        }

        public void ValueSubscriptionOnValueChanged(object? sender, ValueStatusTimestampUpdatedEventArgs args)
        {
            ValueSubscriptionsOnValueChanged();
        }

        #endregion     
    }
}



//#region private functions

//private void Update(ValueStatusTimestamp valueStatusTimestamp)
//{
//    if (ValueStatusTimestamp is null)
//    {
//        ValueStatusTimestamp = valueStatusTimestamp;
//        Changed = true;
//    }
//    else
//    {
//        if (valueStatusTimestamp != ValueStatusTimestamp)
//        {
//            ValueStatusTimestamp = valueStatusTimestamp;
//            Changed = true;
//        }
//    }
//}

//#endregion
