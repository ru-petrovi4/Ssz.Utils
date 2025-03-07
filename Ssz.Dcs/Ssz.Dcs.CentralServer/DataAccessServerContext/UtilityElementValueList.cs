using Ssz.Dcs.CentralServer.ServerListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using AliasResult = Ssz.Utils.DataAccess.AliasResult;

namespace Ssz.Dcs.CentralServer
{    
    public class UtilityElementValueList : ElementValueListBase<UtilityElementValueListItem>
    {
        #region construction and destruction

        public UtilityElementValueList(ServerWorkerBase serverWorker, ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
            : base(serverWorker, serverContext, listClientAlias, listParams)
        {
        }

        #endregion

        #region public functions

        public override void DoWork(DateTime nowUtc, CancellationToken token)
        {
            if (Disposed) return;

            if (!ListCallbackIsEnabled) return; // Callback is not Enabled.            

            if (nowUtc >= LastCallbackTime.AddMilliseconds(UpdateRateMs))
            {
                Guid dataGuid = ((ServerWorker)ServerContext.ServerWorker).UtilityDataGuid;                
                if (_dataGuid == dataGuid) return;
                _dataGuid = dataGuid;

                LastCallbackTime = nowUtc;

                ElementValuesCallbackMessage? elementValuesCallbackMessage = GetElementValuesCallbackMessage();

                if (elementValuesCallbackMessage is not null)
                {
                    ServerContext.AddCallbackMessage(elementValuesCallbackMessage);
                }
            }
        }

        #endregion

        #region protected functions

        protected override UtilityElementValueListItem OnNewElementListItem(uint clientAlias, uint serverAlias, string elementId)
        {
            return new UtilityElementValueListItem(clientAlias, serverAlias, elementId);
        }

        protected override List<AliasResult> OnAddElementListItemsToList(List<UtilityElementValueListItem> elementListItems)
        {
            var results = new List<AliasResult>();

            foreach (UtilityElementValueListItem item in elementListItems)
            {
                ((ServerWorker)ServerContext.ServerWorker).AddUtilityElementValueListItem(item, ServerContext.ClientWorkstationName);

                results.Add(new AliasResult
                {
                    StatusCode = (uint)StatusCode.OK,
                    ServerAlias = item.ServerAlias,
                    ClientAlias = item.ClientAlias
                });
            }

            return results;
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="elementListItems"></param>
        /// <returns></returns>
        protected override List<AliasResult> OnRemoveElementListItemsFromList(List<UtilityElementValueListItem> elementListItems)
        {
            List<AliasResult> results = new List<AliasResult>();

            foreach (UtilityElementValueListItem item in elementListItems)
            {
                ((ServerWorker)ServerContext.ServerWorker).RemoveUtilityElementValueListItem(item, ServerContext.ClientWorkstationName);
            }

            return results;
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        protected override Task<List<AliasResult>> OnWriteValuesAsync(List<UtilityElementValueListItem> items)
        {
            var failedAliasResults = new List<AliasResult>();

            if (items.Count == 0) 
                return Task.FromResult(failedAliasResults);

            foreach (UtilityElementValueListItem item in items)
            {
                ValueStatusTimestamp? valueStatusTimestamp = item.PendingWriteValueStatusTimestamp;
                if (valueStatusTimestamp is not null)
                {
                    var statusCode = ((ServerWorker)ServerContext.ServerWorker).WriteUtilityElementValueListItem(item, valueStatusTimestamp.Value.Value);
                    if (!StatusCodes.IsGood(statusCode))
                        failedAliasResults.Add(new AliasResult
                            {
                                StatusCode = statusCode,
                                ClientAlias = item.ClientAlias,
                                ServerAlias = item.ServerAlias,                                
                            });
                }
            }

            return Task.FromResult(failedAliasResults);
        }

        #endregion

        #region private fields

        private Guid _dataGuid = Guid.Empty;

        #endregion
    }
}
