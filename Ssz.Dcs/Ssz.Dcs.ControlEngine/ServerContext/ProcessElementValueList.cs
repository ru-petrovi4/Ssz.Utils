using Ssz.Dcs.ControlEngine.ServerListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Ssz.Dcs.ControlEngine
{
    /// <summary>
    ///     The Data List class is used to represent a list of current process data values.
    ///     The data values held by Items list represents current process values with a status
    ///     and a time stamp.
    /// </summary>
    public class ProcessElementValueList : ElementValueListBase<ProcessElementValueListItem>
    {
        #region construction and destruction

        public ProcessElementValueList(ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
            : base(serverContext, listClientAlias, listParams)
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
                var device = ((ServerWorker)ServerContext.ServerWorker).Device;
                string dataGuid;
                if (device is null)
                    dataGuid = @"";
                else
                    dataGuid = device.DataGuid.ToString();
                
                if (_dataGuid == dataGuid) return;
                _dataGuid = dataGuid;

                if (device is null)
                {
                    foreach (ProcessElementValueListItem item in ListItemsManager)
                    {
                        if (item.Connection is not null)
                        {
                            item.Connection.Dispose();
                            item.Connection = null;
                        }
                        item.UpdateValueStatusTimestamp(new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain });
                    }
                }
                else
                {
                    foreach (ProcessElementValueListItem item in ListItemsManager)
                    {
                        if (item.InvalidElementId) continue;                        
                        if (item.Connection is null)
                        {
                            PrepareConnection(item, device, nowUtc);                            
                        }
                        if (item.Connection is not null)
                        {
                            item.UpdateValueStatusTimestamp(new ValueStatusTimestamp(item.Connection.GetValue(), StatusCodes.Good, nowUtc));
                        }                        
                    }
                }                

                LastCallbackTime = nowUtc;

                ServerContext.ElementValuesCallbackMessage? elementValuesCallbackMessage = GetElementValuesCallbackMessage();

                if (elementValuesCallbackMessage is not null)
                {
                    ServerContext.AddCallbackMessage(elementValuesCallbackMessage);                    
                }
            }
        }        

        public override void TouchList()
        {
            _dataGuid = null;

            base.TouchList();
        }

        /// <summary>
        ///     Force to send all data.
        /// </summary>
        public override void Reset()
        {
            _dataGuid = null;

            foreach (ProcessElementValueListItem item in ListItemsManager)
            {
                item.Reset();
            }

            base.Reset();
        }

        #endregion

        #region protected functions

        protected override ProcessElementValueListItem OnNewElementListItem(uint clientAlias, uint serverAlias, string elementId)
        {
            return new ProcessElementValueListItem(clientAlias, serverAlias, elementId);
        }

        protected override Task<List<AliasResult>> OnAddElementListItemsToListAsync(List<ProcessElementValueListItem> elementListItems)
        {
            var results = new List<AliasResult>();

            if (elementListItems.Count == 0) 
                return Task.FromResult(results);

            foreach (ProcessElementValueListItem item in elementListItems)
            {
                results.Add(new AliasResult
                {
                    StatusCode = StatusCodes.Good,
                    ServerAlias = item.ServerAlias,
                    ClientAlias = item.ClientAlias
                });
            }

            _dataGuid = null;

            return Task.FromResult(results);
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="changedListItems"></param>
        /// <returns></returns>
        protected override async Task<List<AliasResult>> OnWriteValuesAsync(List<ProcessElementValueListItem> changedListItems)
        {
            var resultsList = new List<AliasResult>();

            if (changedListItems.Count == 0)
                return resultsList;

            var device = ((ServerWorker)ServerContext.ServerWorker).Device;

            if (device is null)
            {
                foreach (ProcessElementValueListItem item in changedListItems)
                {
                    resultsList.Add(new AliasResult
                        {
                            StatusCode = StatusCodes.Uncertain,
                            ServerAlias = item.ServerAlias,
                            ClientAlias = item.ClientAlias
                        });
                }

                return resultsList;
            }

            DateTime nowUtc = DateTime.UtcNow;

            bool valueIsSet = false;
            foreach (ProcessElementValueListItem item in changedListItems)
            {
                if (item.InvalidElementId) continue;
                if (item.Connection is null)
                {
                    PrepareConnection(item, device, nowUtc);
                }
                if (item.Connection is not null)
                {
                    ValueStatusTimestamp? valueStatusTimestamp = item.PendingWriteValueStatusTimestamp;
                    if (valueStatusTimestamp is not null)
                    {
                        var resultInfo = await item.Connection.SetValueAsync(valueStatusTimestamp.Value.Value);
                        if (!StatusCodes.IsGood(resultInfo.StatusCode))
                            resultsList.Add(new AliasResult
                                {
                                    StatusCode = resultInfo.StatusCode,
                                    Info = resultInfo.Info,
                                    Label = resultInfo.Label,
                                    Details = resultInfo.Details,
                                    ServerAlias = item.ServerAlias,
                                    ClientAlias = item.ClientAlias
                                });
                        valueIsSet = true;
                    }
                }
            }

            if (valueIsSet)
                device.DataGuid = Guid.NewGuid();

            return resultsList;
        }

        #endregion

        #region private fields

        private void PrepareConnection(ProcessElementValueListItem item, DsDevice device, DateTime nowUtc)
        {
            item.InvalidElementId = true;
            var i = item.ElementId.IndexOf('.');
            if (i > 0)
            {
                var block = device.ModulesTempRuntimeData.ChildDsBlocksDictionary.TryGetValue(item.ElementId.Substring(0, i));
                if (block is not null)
                {
                    item.Connection = DsConnectionsFactory.CreateRefConnection(item.ElementId, block.ParentModule, block.ParentComponentDsBlock);
                    item.Connection.GetValue();
                    if (item.Connection.DsBlockIndexInModule == IndexConstants.DsBlockIndexInModule_IncorrectDsBlockFullTagName)
                    {
                        item.Connection.Dispose();
                        item.Connection = null;
                    }
                    else
                    {
                        item.InvalidElementId = false;
                    }
                }
            }
            if (item.InvalidElementId)
            {
                item.UpdateValueStatusTimestamp(new ValueStatusTimestamp { StatusCode = StatusCodes.BadNodeIdUnknown });
            }
        }

        #endregion        

        #region private fields

        private string? _dataGuid;

        #endregion
    }
}
