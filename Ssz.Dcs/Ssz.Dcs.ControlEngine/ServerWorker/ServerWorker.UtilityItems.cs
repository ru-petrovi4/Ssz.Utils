using System;
using System.Collections.Generic;
using System.Threading;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Ssz.Dcs.ControlEngine.ServerListItems;
using System.Linq;
using System.Text;

namespace Ssz.Dcs.ControlEngine
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region public functions      
        
        //public event Action<string?, Ssz.Utils.DataAccess.EventMessage>? UtilityEventMessageNotification;

        public Guid UtilityDataGuid { get; set; } = Guid.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="utilityElementValueListItem"></param>
        public void AddUtilityElementValueListItem(UtilityElementValueListItem utilityElementValueListItem)
        {
            string elementId = utilityElementValueListItem.ElementId;
            
            if (!_utilityItems.TryGetValue(elementId, out UtilityItem? utilityItem))
            {
                utilityItem = new UtilityItem(elementId);
                _utilityItems.Add(elementId, utilityItem);                

                _utilityItemsProcessingNeeded = true;
            }
            utilityItem.UtilityElementValueListItemsCollection.Add(utilityElementValueListItem);
            utilityElementValueListItem.UpdateValueStatusTimestamp(utilityItem.ValueStatusTimestamp);
        }

        /// <summary>
        ///     valueSubscription is not null.
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public void RemoveUtilityElementValueListItem(UtilityElementValueListItem utilityElementValueListItem)
        {
            string elementId = utilityElementValueListItem.ElementId;
            if (!_utilityItems.TryGetValue(elementId, out UtilityItem? utilityItem))
            {
                return;
            }
            utilityItem.UtilityElementValueListItemsCollection.Remove(utilityElementValueListItem);
            if (utilityItem.UtilityElementValueListItemsCollection.Count == 0)
            {
                _utilityItems.Remove(elementId);
            }
        }

        #endregion

        #region private function

        /// <summary>
        ///     On loop in working thread.
        /// </summary>
        /// <param name="nowUtc"></param>
        /// <param name="cancellationToken"></param>
        private void DoWorkUtilityItems(DateTime nowUtc, CancellationToken cancellationToken)
        {
            if (!_utilityItemsProcessingNeeded) return;
            _utilityItemsProcessingNeeded = false;            

            CalculateUtilityItems(nowUtc, cancellationToken);

            UtilityDataGuid = Guid.NewGuid();            
        }                

        private void CalculateUtilityItems(DateTime nowUtc, CancellationToken cancellationToken)
        {            
        }

        #endregion

        #region private fields       

        private volatile bool _utilityItemsProcessingNeeded;

        private readonly CaseInsensitiveDictionary<UtilityItem> _utilityItems = new(256);        

        #endregion

        private class UtilityItem
        {
            #region construction and destruction

            public UtilityItem(string id)
            {
                Id = id;
            }

            #endregion

            #region public functions

            public string Id { get; }

            public List<UtilityElementValueListItem> UtilityElementValueListItemsCollection { get; } = new();

            public ValueStatusTimestamp ValueStatusTimestamp { get { return _valueStatusTimestamp; } }

            public void UpdateValue(string value, DateTime nowUtc)
            {
                bool updated = false;
                if (ValueStatusCodes.IsUnknown(_valueStatusTimestamp.ValueStatusCode))
                {
                    _valueStatusTimestamp = new ValueStatusTimestamp(new Any(value), ValueStatusCodes.Good, nowUtc);
                    updated = true;
                }
                else
                {
                    if (_valueStatusTimestamp.Value.ValueAsString(false) != value)
                    {
                        _valueStatusTimestamp.Value.Set(value);
                        _valueStatusTimestamp.TimestampUtc = nowUtc;
                        updated = true;
                    }
                }
                if (updated)
                {
                    foreach (UtilityElementValueListItem utilityElementValueListItem in UtilityElementValueListItemsCollection)
                    {
                        utilityElementValueListItem.UpdateValueStatusTimestamp(_valueStatusTimestamp);
                    }
                }    
            }

            #endregion

            #region private fields

            private ValueStatusTimestamp _valueStatusTimestamp = new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.Unknown };

            #endregion
        }
    }
}