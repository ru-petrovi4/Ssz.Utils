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
    public partial class ServerWorker : ServerWorkerBase
    {
        #region public functions
        
        public Guid UtilityDataGuid { get; set; } = Guid.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listItem"></param>
        public void AddUtilityElementValueListItem(UtilityElementValueListItem listItem)
        {
            string elementId = listItem.ElementId;
            
            if (!_utilityItems.TryGetValue(elementId, out UtilityItem? utilityItem))
            {
                utilityItem = new UtilityItem(elementId);
                _utilityItems.Add(elementId, utilityItem);                

                _utilityItemsDoWorkNeeded = true;
            }
            utilityItem.UtilityElementValueListItemsCollection.Add(listItem);
            listItem.IsReadable = true;
            listItem.IsWritable = false;
            listItem.UpdateValueStatusTimestamp(utilityItem.ValueStatusTimestamp);
        }

        /// <summary>
        ///     valueSubscription is not null.
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public void RemoveUtilityElementValueListItem(UtilityElementValueListItem listItem)
        {
            string elementId = listItem.ElementId;
            if (!_utilityItems.TryGetValue(elementId, out UtilityItem? utilityItem))
            {
                return;
            }
            utilityItem.UtilityElementValueListItemsCollection.Remove(listItem);
            if (utilityItem.UtilityElementValueListItemsCollection.Count == 0)
            {
                _utilityItems.Remove(elementId);
            }
        }

        /// <summary>
        ///     Reruns Status Code <see cref="Ssz.Utils.JobStatusCodes"/>
        /// </summary>
        /// <param name="listItem"></param>
        /// <param name="value"></param>
        public uint WriteUtilityElementValueListItem(UtilityElementValueListItem listItem, Any value)
        {
            string elementId = listItem.ElementId;

            if (!_utilityItems.TryGetValue(elementId, out UtilityItem? utilityItem))
            {
                utilityItem = new UtilityItem(elementId);
                _utilityItems.Add(elementId, utilityItem);                
            }
            utilityItem.UpdateValue(value.ValueAsString(false), DateTime.UtcNow);

            _utilityItemsDoWorkNeeded = true;

            return JobStatusCodes.OK;
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

                //CalculateFilesStoreUtilityItem(nowUtc, cancellationToken);

                UtilityDataGuid = Guid.NewGuid();
            }
        }                

        private void DoWorkOperatorsUtilityItems(DateTime nowUtc, CancellationToken cancellationToken)
        {
            UtilityItem[] processModelingSessionOperatorsUtilityItems = _utilityItems.Values
                    .Where(mi => mi.Id.StartsWith(DataAccessConstants.Operators_UtilityItem + @"[", StringComparison.InvariantCultureIgnoreCase) && mi.Id.EndsWith("]")).ToArray();

            foreach (UtilityItem processModelingSessionOperatorsUtilityItem in processModelingSessionOperatorsUtilityItems)
            {
                string utilityItemValue = @"";
                
                string id = processModelingSessionOperatorsUtilityItem.Id;
                int i1 = id.IndexOf('[');
                int i2 = id.IndexOf(']', i1);
                string processModelingSessionId = id.Substring(i1 + 1, i2 - i1 - 1);
                ProcessModelingSession? processModelingSession = _processModelingSessionsCollection.TryGetValue(processModelingSessionId);
                if (processModelingSession is not null)
                {                    
                    foreach (var operatorWorkstationName in _operatorWorkstationNamesCollection)
                    {
                        bool isWorkstationForProcess = false;
                        string operatorWorkstationNameToDisplay = @"";
                        var values = _csvDb.GetValues(WorkstationsCsvFileName, operatorWorkstationName);
                        if (values is null || values.Count < 3 || values.Skip(2).All(v => String.IsNullOrEmpty(v)))
                        {
                            isWorkstationForProcess = true;
                            if (values is not null && values.Count >= 2)
                                operatorWorkstationNameToDisplay = values[1] ?? @"";                         
                        }
                        else
                        {
                            var processModelName = processModelingSession.ProcessModelName;
                            if (values.Skip(2).Any(v => String.Equals(v, processModelName, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                isWorkstationForProcess = true;
                                operatorWorkstationNameToDisplay = values[1] ?? @"";
                            }
                        }

                        if (isWorkstationForProcess)
                        {
                            var operatorSessions = OperatorSessionsCollection.Values.Where(ts => 
                                (ts.ProcessModelingSession is null || String.Equals(ts.ProcessModelingSession.ProcessModelingSessionId, processModelingSessionId, StringComparison.InvariantCultureIgnoreCase)) &&
                                String.Equals(ts.OperatorWorkstationName, operatorWorkstationName, StringComparison.InvariantCultureIgnoreCase)).ToArray();

                            foreach (var operatorSession in operatorSessions)
                            {
                                if (String.IsNullOrEmpty(operatorSession.OperatorUserName))
                                    continue;

                                var operatorValues = new List<object?>();
                                operatorValues.Add(operatorWorkstationName);
                                operatorValues.Add(operatorWorkstationNameToDisplay);
                                operatorValues.Add(operatorSession.WindowsUserName);
                                operatorValues.Add(operatorSession.WindowsUserNameToDisplay);
                                operatorValues.Add(operatorSession.OperatorSessionId);
                                operatorValues.Add(operatorSession.OperatorUserName);
                                operatorValues.Add(operatorSession.DsProject_PathRelativeToDataDirectory);
                                operatorValues.Add(operatorSession.OperatorPlay_AdditionalCommandLine == @"" ? null : operatorSession.OperatorPlay_AdditionalCommandLine);
                                operatorValues.Add(operatorSession.OperatorRoleId);
                                operatorValues.Add(operatorSession.OperatorRoleName);
                                operatorValues.Add(operatorSession.OperatorSessionStatus);
                                utilityItemValue += CsvHelper.FormatForCsv(",", operatorValues) + Environment.NewLine;
                            }
                            //if (operatorSessions.Length > 0)
                            //{
                                
                            //}
                            //else
                            //{
                            //    var operatorValues = new List<object?>();
                            //    operatorValues.Add(operatorWorkstationName);
                            //    operatorValues.Add(operatorWorkstationNameToDisplay);
                            //    operatorValues.Add(null);
                            //    operatorValues.Add(null);
                            //    operatorValues.Add(null);
                            //    operatorValues.Add(null);
                            //    operatorValues.Add(null);
                            //    operatorValues.Add(null);
                            //    operatorValues.Add(null);
                            //    operatorValues.Add(null);
                            //    operatorValues.Add(OperatorSessionConstants.OperatorReadyToInitiateSession);
                            //    utilityItemValue += CsvHelper.FormatForCsv(",", operatorValues) + Environment.NewLine;
                            //}
                        }
                    }
                }

                processModelingSessionOperatorsUtilityItem.UpdateValue(utilityItemValue, nowUtc);
            }
        }

        #endregion

        #region private fields       

        private volatile bool _utilityItemsDoWorkNeeded;

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

            /// <summary>
            ///     Checks for value change.
            /// </summary>
            /// <param name="value"></param>
            /// <param name="nowUtc"></param>
            public void UpdateValue(string value, DateTime nowUtc)
            {
                bool updated = false;
                if (ValueStatusCodes.IsUncertain(_valueStatusTimestamp.ValueStatusCode))
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

            private ValueStatusTimestamp _valueStatusTimestamp = new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.Uncertain };

            #endregion
        }
    }
}