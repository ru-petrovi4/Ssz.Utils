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
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.ServerListItems;
using System.Linq;
using System.Text;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : DataAccessServerWorkerBase
    {
        #region public functions
        
        public Guid UtilityDataGuid { get; set; } = Guid.Empty;

        public void AddUtilityElementValueListItem(UtilityElementValueListItem listItem, string clientWorkstationName)
        {
            string id = GetUtilityItemsKey(listItem.ElementId, clientWorkstationName);            
            if (!_utilityItems.TryGetValue(id, out UtilityItem? utilityItem))
            {
                utilityItem = new UtilityItem(listItem.ElementId, clientWorkstationName);
                _utilityItems.Add(id, utilityItem);                

                _utilityItemsDoWorkNeeded = true;
            }
            utilityItem.UtilityElementValueListItemsCollection.Add(listItem);
            listItem.IsReadable = true;
            listItem.IsWritable = false;
            listItem.UpdateValueStatusTimestamp(utilityItem.ValueStatusTimestamp);
        }
        
        public void RemoveUtilityElementValueListItem(UtilityElementValueListItem listItem, string clientWorkstationName)
        {
            string id = GetUtilityItemsKey(listItem.ElementId, clientWorkstationName);
            if (!_utilityItems.TryGetValue(id, out UtilityItem? utilityItem))
            {
                return;
            }
            utilityItem.UtilityElementValueListItemsCollection.Remove(listItem);
            if (utilityItem.UtilityElementValueListItemsCollection.Count == 0)
            {
                _utilityItems.Remove(id);
            }
        }

        /// <summary>
        ///     Reruns Status Code <see cref="Ssz.Utils.StatusCodes"/>
        /// </summary>
        /// <param name="listItem"></param>
        /// <param name="value"></param>
        public uint WriteUtilityElementValueListItem(UtilityElementValueListItem listItem, Any value)
        {
            //string elementId = listItem.ElementId;

            //if (!_utilityItems.TryGetValue(elementId, out UtilityItem? utilityItem))
            //{
            //    utilityItem = new UtilityItem(elementId);
            //    _utilityItems.Add(elementId, utilityItem);                
            //}
            //utilityItem.UpdateValue(value.ValueAsString(false), DateTime.UtcNow);

            //_utilityItemsDoWorkNeeded = true;

            return StatusCodes.Good;
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
            if (_utilityItemsDoWorkNeeded)
            {
                _utilityItemsDoWorkNeeded = false;

                DoWorkOperatorsUtilityItems(nowUtc, cancellationToken);

                DoWorkCentralServerUtilityItems(nowUtc, cancellationToken);

                DoWorkCentralServersUtilityItems(nowUtc, cancellationToken);

                UtilityDataGuid = Guid.NewGuid();
            }
        }                

        private void DoWorkOperatorsUtilityItems(DateTime nowUtc, CancellationToken cancellationToken)
        {
            UtilityItem[] processModelingSessionOperatorsUtilityItems = _utilityItems.Values
                    .Where(mi => mi.ElementId.StartsWith(DataAccessConstants.UtilityItem_Operators + @"[", StringComparison.InvariantCultureIgnoreCase) && mi.ElementId.EndsWith("]")).ToArray();

            foreach (var g in processModelingSessionOperatorsUtilityItems.GroupBy(i => i.ElementId))
            {
                string utilityItemValue = @"";

                string elementId = g.Key;
                int i1 = elementId.IndexOf('[');
                int i2 = elementId.IndexOf(']', i1);
                string processModelingSessionId = elementId.Substring(i1 + 1, i2 - i1 - 1);
                ProcessModelingSession? processModelingSession = _processModelingSessionsCollection.TryGetValue(processModelingSessionId);
                if (processModelingSession is not null)
                {
                    foreach (var operatorSession in OperatorSessionsCollection.Values)
                    {
                        if (String.IsNullOrEmpty(operatorSession.OperatorUserName))
                            continue;

                        if (operatorSession.ProcessModelingSession is null)
                        {
                            if (!operatorSession.ProcessModelNames.Contains(@"*", StringComparer.InvariantCultureIgnoreCase) &&
                                    !operatorSession.ProcessModelNames.Contains(processModelingSession.ProcessModelName, StringComparer.InvariantCultureIgnoreCase))
                                continue;
                        }
                        else
                        {
                            if (!String.Equals(operatorSession.ProcessModelingSession.ProcessModelingSessionId, processModelingSessionId, StringComparison.InvariantCultureIgnoreCase))
                                continue;
                        }

                        var operatorValues = new List<object?>
                            {
                                operatorSession.OperatorWorkstationName,
                                operatorSession.OperatorWorkstationName,
                                operatorSession.WindowsUserName,
                                operatorSession.WindowsUserNameToDisplay,
                                operatorSession.OperatorSessionId,
                                operatorSession.OperatorUserName,
                                operatorSession.DsProject_PathRelativeToDataDirectory,
                                operatorSession.OperatorPlay_AdditionalCommandLine == @"" ? null : operatorSession.OperatorPlay_AdditionalCommandLine,
                                operatorSession.OperatorRoleId,
                                operatorSession.OperatorRoleName,
                                operatorSession.OperatorSessionStatus
                            };
                        utilityItemValue += CsvHelper.FormatForCsv(",", operatorValues) + Environment.NewLine;
                    }
                }

                foreach (UtilityItem processModelingSessionOperatorsUtilityItem in g)
                {
                    processModelingSessionOperatorsUtilityItem.UpdateValue(utilityItemValue, nowUtc);
                }
            }
        }

        private void DoWorkCentralServerUtilityItems(DateTime nowUtc, CancellationToken cancellationToken)
        {
            CaseInsensitiveOrderedDictionary<List<string?>> clientsCsvFileData = 
                _addonsManager.Addons.OfType<DcsCentralServerAddon>().Single().CsvDb.GetData(DcsCentralServerAddon.ClientsCsvFileName);

            UtilityItem[] centralServerUtilityItems = _utilityItems.Values
                    .Where(mi => String.Equals(mi.ElementId, DataAccessConstants.UtilityItem_CentralServer, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            if (_additionalCentralServerInfosCollection.Count == 0)
            {
                string utilityItemValue = @"*"; // Single this server
                foreach (var centralServerUtilityItem in centralServerUtilityItems)
                {
                    centralServerUtilityItem.UpdateValue(utilityItemValue, nowUtc);
                }
            }
            else
            {
                foreach (var i in _additionalCentralServerInfosCollection.Values)
                {
                    i.ClientWorkstationsGroups.Clear();
                }

                foreach (var centralServerUtilityItem in centralServerUtilityItems)
                {
                    string centralServerUtilityItem_Value = centralServerUtilityItem.ValueStatusTimestamp.Value.ValueAsString(false);
                    AdditionalCentralServerInfo? additionalCentralServerInfo = _additionalCentralServerInfosCollection.Values
                            .FirstOrDefault(i => String.Equals(i.ServerAddress, centralServerUtilityItem_Value, StringComparison.InvariantCultureIgnoreCase));
                    if (additionalCentralServerInfo is not null)
                        additionalCentralServerInfo!.ClientWorkstationsGroups.Add(GetClientWorkstationsGroup(centralServerUtilityItem.ClientWorkstationName, clientsCsvFileData));
                }

                foreach (var centralServerUtilityItem in centralServerUtilityItems)
                {
                    string centralServerUtilityItem_Value = centralServerUtilityItem.ValueStatusTimestamp.Value.ValueAsString(false);
                    AdditionalCentralServerInfo? additionalCentralServerInfo = _additionalCentralServerInfosCollection.Values
                            .FirstOrDefault(i => String.Equals(i.ServerAddress, centralServerUtilityItem_Value, StringComparison.InvariantCultureIgnoreCase));
                    if (additionalCentralServerInfo is null)
                    {
                        var clientWorkstationsGroup = GetClientWorkstationsGroup(centralServerUtilityItem.ClientWorkstationName, clientsCsvFileData);

                        // Find group
                        foreach (var i in _additionalCentralServerInfosCollection.Values)
                        {
                            if (i.ClientWorkstationsGroups.Contains(clientWorkstationsGroup))
                            {
                                additionalCentralServerInfo = i;
                                break;
                            }
                        }

                        if (additionalCentralServerInfo is null)
                        {
                            // Find best additionalCentralServerInfo
                            int minClientWorkstationsCount = Int32.MaxValue;
                            foreach (var i in _additionalCentralServerInfosCollection.Values)
                            {
                                if (i.ClientWorkstationsGroups.Count < minClientWorkstationsCount)
                                {
                                    additionalCentralServerInfo = i;
                                    minClientWorkstationsCount = i.ClientWorkstationsGroups.Count;
                                }
                            }
                        }

                        additionalCentralServerInfo!.ClientWorkstationsGroups.Add(clientWorkstationsGroup);
                        centralServerUtilityItem.UpdateValue(additionalCentralServerInfo.ServerAddress, nowUtc);
                    }
                }
            }
        }

        private string GetClientWorkstationsGroup(string clientWorkstationName, CaseInsensitiveOrderedDictionary<List<string?>> clientsCsvFileData)
        {            
            string clientWorkstationsGroup = @"default";
            foreach (var kvp in clientsCsvFileData)
            {
                if (kvp.Value.Skip(1).Any(v => String.Equals(v, clientWorkstationName, StringComparison.InvariantCultureIgnoreCase)))
                    clientWorkstationsGroup = kvp.Key;
            }
            return clientWorkstationsGroup;
        }

        private void DoWorkCentralServersUtilityItems(DateTime nowUtc, CancellationToken cancellationToken)
        {
            UtilityItem[] centralServersUtilityItems = _utilityItems.Values
                    .Where(mi => String.Equals(mi.ElementId, DataAccessConstants.UtilityItem_CentralServers, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            string utilityItemValue;
            if (_additionalCentralServerInfosCollection.Count == 0)
                utilityItemValue = @"*"; // Single this server  
            else
                utilityItemValue = CsvHelper.FormatForCsv(@",", _additionalCentralServerInfosCollection.Select(kvp => kvp.Value.ServerAddress));
            foreach (var centralServersUtilityItem in centralServersUtilityItems)
            {
                centralServersUtilityItem.UpdateValue(utilityItemValue, nowUtc);
            }
        }

        private string GetUtilityItemsKey(string elementId, string clientWorkstationName)
        {
            if (String.Equals(elementId, DataAccessConstants.UtilityItem_CentralServer, StringComparison.InvariantCultureIgnoreCase))
                return elementId + "@" + clientWorkstationName;
            
            return elementId;
        }

        #endregion

        #region private fields       

        private volatile bool _utilityItemsDoWorkNeeded;

        /// <summary>
        ///     [id, UtilityItem]
        /// </summary>
        private readonly CaseInsensitiveOrderedDictionary<UtilityItem> _utilityItems = new(256);        

        #endregion

        private class UtilityItem
        {
            #region construction and destruction

            public UtilityItem(string elementId, string clientWorkstationName)
            {
                ElementId = elementId;
                ClientWorkstationName = clientWorkstationName;
            }

            #endregion

            #region public functions

            public string ElementId { get; }

            public string ClientWorkstationName { get; }

            public List<UtilityElementValueListItem> UtilityElementValueListItemsCollection { get; } = new();

            public ValueStatusTimestamp ValueStatusTimestamp { get { return _valueStatusTimestamp; } }            

            /// <summary>
            ///     Checks for value change.
            /// </summary>
            /// <param name="value"></param>
            /// <param name="nowUtc"></param>
            public void UpdateValue(string value, DateTime nowUtc)
            {
                bool updated = false;
                if (StatusCodes.IsUncertain(_valueStatusTimestamp.StatusCode))
                {
                    _valueStatusTimestamp = new ValueStatusTimestamp(new Any(value), StatusCodes.Good, nowUtc);
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

            private ValueStatusTimestamp _valueStatusTimestamp = new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain };

            #endregion
        }
    }
}

// string utilityItemValue = ConfigurationHelper.GetValue<string>(_configuration, @"Kestrel:Endpoints:HttpsDefaultCert:Url", @"");