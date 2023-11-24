using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;
using Ssz.Xi.Client.Internal.Context;
using Ssz.Xi.Client.Internal.ListItems;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Lists
{
    /// <summary>
    ///     This class implements the IXiDataJournalList interface.
    /// </summary>
    internal class XiDataJournalList : XiDataAndDataJournalListBase<XiDataJournalListItem>, IXiDataJournalListProxy
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates a new data journal list for the specified context.
        /// </summary>
        /// <param name="context"> The context that owns the data journal list. </param>
        /// <param name="updateRate"> The update rate for the data journal list. </param>
        /// <param name="bufferingRate"> The BufferingRate for this data journal list. Set to 0 if not used. </param>
        /// <param name="filterSet"> The FilterSet for this data journal list. Set to null if not used. </param>
        internal XiDataJournalList(XiContext context, uint updateRate, uint bufferingRate, FilterSet? filterSet)
            : base(context)
        {
            StandardListType = StandardListType.DataJournalList;
            ListAttributes = Context.DefineList(this, updateRate, bufferingRate, filterSet);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method is used to create and add a new data object to a Journal Data List.  The new data
        ///     object is created using its InstanceId.
        /// </summary>
        /// <param name="instanceId"> The InstanceId of the data object to create and add. </param>
        /// <returns> Returns the newly created data object. </returns>
        public IXiDataJournalListItem PrepareAddItem(InstanceId instanceId)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataJournalList.");

            var dataJournalListItem = new XiDataJournalListItem(0, instanceId);
            dataJournalListItem.ClientAlias = ListItemsManager.Add(dataJournalListItem);
            dataJournalListItem.PreparedForAdd = true;
            return dataJournalListItem;
        }

        public IEnumerable<IXiDataJournalListItem>? CommitAddItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            return CommitAddItemsInternal();
        }

        public IEnumerable<IXiDataJournalListItem>? CommitRemoveItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            return CommitRemoveItemsInternal();
        }

        /*
        /// <summary>
        ///   <para> This method is used to read the historical values that fall between a start and end time for one or more data objects within a specific data journal list. </para>
        /// </summary>
        /// <param name="firstTimestamp"> The filter that specifies the first or beginning (of returned list) timestamp for values to be returned. Valid operands include the Timestamp (UTC) and OpcHdaTimestampStr constants defined by the FilterOperand class. The FilterOperand Operator is used to determine if the returned data should include data values the occur exactly at the first or second time stamp. If the equals operator is specified then values that occur at the first and second time stamp will be included in the sample set. Any other operator will not include first or second time stamped values. </param>
        /// <param name="secondTimestamp"> The filter that specifies the second or ending (of returned list) timestamp for values to be returned. Valid operands include the Timestamp (UTC) and OpcHdaTimestampStr constants defined by the FilterOperand class. The FilterOperand Operator is not used. </param>
        /// <param name="numValuesPerDataObject"> The maximum number of values to be returned for each data object. </param>
        /// <param name="xiDataJournalListItem"> The list of data objects whose historical values are to be read. Each data object is represented by a value set that contains the values selected and returned by the server. </param>
        public JournalDataValues ReadJournalDataForTimeInterval(FilterCriterion firstTimestamp, FilterCriterion secondTimestamp,
                                                   uint numValuesPerDataObject,
                                                   IXiDataJournalListItem xiDataJournalListItem)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataJournalList.");

            var serverAliases = new List<uint> {((XiDataJournalListItem) xiDataJournalListItem).ServerAlias};

            JournalDataValues[] journalDataValuesArray = Context.ReadJournalDataForTimeInterval(ServerListId, firstTimestamp,
                                                                                          secondTimestamp,
                                                                                          numValuesPerDataObject,
                                                                                          serverAliases);

            return journalDataValuesArray.FirstOrDefault();
        }*/

        /// <summary>
        ///     <para>
        ///         This method is used to read the historical values that fall between a start and end time for one or more
        ///         data objects within a specific data journal list.
        ///     </para>
        /// </summary>
        /// <param name="firstTimestamp">
        ///     The filter that specifies the first or beginning (of returned list) timestamp for values
        ///     to be returned. Valid operands include the Timestamp (UTC) and OpcHdaTimestampStr constants defined by the
        ///     FilterOperand class. The FilterOperand Operator is used to determine if the returned data should include data
        ///     values the occur exactly at the first or second time stamp. If the equals operator is specified then values that
        ///     occur at the first and second time stamp will be included in the sample set. Any other operator will not include
        ///     first or second time stamped values.
        /// </param>
        /// <param name="secondTimestamp">
        ///     The filter that specifies the second or ending (of returned list) timestamp for values
        ///     to be returned. Valid operands include the Timestamp (UTC) and OpcHdaTimestampStr constants defined by the
        ///     FilterOperand class. The FilterOperand Operator is not used.
        /// </param>
        /// <param name="numValuesPerDataObject"> The maximum number of values to be returned for each data object. </param>
        /// <param name="xiValueStatusTimestampSetCollection">
        ///     The list of data objects whose historical values are to be read. Each data
        ///     object is represented by a value set that contains the values selected and returned by the server.
        /// </param>
        public void ReadJournalDataForTimeInterval(FilterCriterion firstTimestamp, FilterCriterion secondTimestamp,
            uint numValuesPerDataObject,
            IEnumerable<IXiDataJournalValueStatusTimestampSet>? xiValueStatusTimestampSetCollection)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataJournalList.");

            var serverAliases = new HashSet<uint>();
            if (xiValueStatusTimestampSetCollection is null)
            {
                foreach (XiDataJournalListItem item in ListItemsManager)
                {
                    serverAliases.Add(item.ServerAlias);
                }
            }
            else
            {
                foreach (IXiDataJournalValueStatusTimestampSet xiValueStatusTimestampSet in xiValueStatusTimestampSetCollection)
                {
                    if (xiValueStatusTimestampSet.CalculationTypeId.LocalId ==
                        JournalDataSampleTypes.RawDataSamples.ToString(CultureInfo.InvariantCulture))
                    {
                        serverAliases.Add(
                            ((XiDataJournalValueStatusTimestampSet) xiValueStatusTimestampSet).OwningXiDataJournalListItem
                                .ServerAlias);
                    }
                }
            }

            if (serverAliases.Count == 0) return;

            JournalDataValues[]? journalDataValuesArray = Context.ReadJournalDataForTimeInterval(ServerListId,
                firstTimestamp,
                secondTimestamp,
                numValuesPerDataObject,
                serverAliases.ToList());

            if (journalDataValuesArray is not null)
                foreach (JournalDataValues journalDataValues in journalDataValuesArray)
                {
                    XiDataJournalListItem? xiDataJournalListItem;
                    if (ListItemsManager.TryGetValue(journalDataValues.ClientAlias, out xiDataJournalListItem))
                        xiDataJournalListItem.Update(journalDataValues);
                }
        }

        /// <summary>
        ///     This method is used to read the historical values at specific times for
        ///     one or more data objects within a specific data journal list.  If no element exists
        ///     at the specified time in the data journal for an object, the server creates an
        ///     interpolated value for that time and includes it in the response as though it
        ///     actually existed in the journal.
        /// </summary>
        /// <param name="timestamps">
        ///     Identifies the timestamps of historical values to be returned for each of the requested data
        ///     objects.
        /// </param>
        /// <param name="xiValueStatusTimestampSetList">
        ///     The list of data objects whose historical values are to be read. Each data
        ///     object is represented by a value set that contains the values selected and returned by the server.
        /// </param>
        public void ReadJournalDataAtSpecificTimes(List<DateTime> timestamps,
            List<IXiDataJournalValueStatusTimestampSet> xiValueStatusTimestampSetList)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataJournalList.");

            var serverAliases = new HashSet<uint>();
            if (xiValueStatusTimestampSetList is null || xiValueStatusTimestampSetList.Count == 0)
            {
                foreach (XiDataJournalListItem item in ListItemsManager)
                {
                    serverAliases.Add(item.ServerAlias);
                }
            }
            else
            {
                foreach (IXiDataJournalValueStatusTimestampSet xiValueStatusTimestampSet in xiValueStatusTimestampSetList)
                {
                    if (xiValueStatusTimestampSet.CalculationTypeId.LocalId ==
                        JournalDataSampleTypes.AtTimeDataSamples.ToString(CultureInfo.InvariantCulture))
                    {
                        serverAliases.Add(
                            ((XiDataJournalValueStatusTimestampSet) xiValueStatusTimestampSet).OwningXiDataJournalListItem
                                .ServerAlias);
                    }
                }
            }

            JournalDataValues[]? journalDataValuesArray = Context.ReadJournalDataAtSpecificTimes(ServerListId, timestamps,
                serverAliases.ToList());
            if (journalDataValuesArray is not null)
                foreach (JournalDataValues journalDataValues in journalDataValuesArray)
                {
                    XiDataJournalListItem? xiDataJournalListItem;
                    if (ListItemsManager.TryGetValue(journalDataValues.ClientAlias, out xiDataJournalListItem))
                        xiDataJournalListItem.Update(journalDataValues);
                }
        }

        /// <summary>
        ///     This method is used to read calculated historical values (e.g. averages or
        ///     interpolations) for one or more data objects within a specific data journal list.
        ///     The time-range used to select the historical values is specified by the client.
        ///     Additionally, the client specifies a calculation period that divides that time
        ///     range into periods. The server calculates a return value for each of these periods.
        /// </summary>
        /// <param name="firstTimestamp">
        ///     The filter that specifies the inclusive earliest (oldest) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="secondTimestamp">
        ///     The filter that specifies the inclusive newest (most recent) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="calculationPeriod">
        ///     The time span used to divide the specified time range into individual periods for
        ///     which return values are calculated. The specified calculation is performed on the set of historical values of a
        ///     data object that fall within each period.
        /// </param>
        /// <param name="xiValueStatusTimestampSetList">
        ///     The list of data objects whose historical values are to be read. Each data
        ///     object is represented by a value set that contains the values calculated and returned by the server.
        /// </param>
        public void ReadCalculatedJournalData(FilterCriterion firstTimestamp, FilterCriterion secondTimestamp,
            TimeSpan calculationPeriod,
            List<IXiDataJournalValueStatusTimestampSet> xiValueStatusTimestampSetList)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataJournalList.");

            var serverAliasesAndCalculations = new List<AliasAndCalculation>();
            if (xiValueStatusTimestampSetList is null || xiValueStatusTimestampSetList.Count == 0)
            {
                foreach (XiDataJournalListItem item in ListItemsManager)
                {
                    foreach (
                        XiDataJournalValueStatusTimestampSet xiValueStatusTimestampSet in item.ValueStatusTimestampSets)
                    {
                        if (xiValueStatusTimestampSet.CalculationTypeId.LocalId !=
                            JournalDataSampleTypes.RawDataSamples.ToString(CultureInfo.InvariantCulture) &&
                            xiValueStatusTimestampSet.CalculationTypeId.LocalId !=
                            JournalDataSampleTypes.AtTimeDataSamples.ToString(CultureInfo.InvariantCulture))
                        {
                            var aliasAndCalculation = new AliasAndCalculation
                            {
                                ServerAlias =
                                    xiValueStatusTimestampSet.
                                        OwningXiDataJournalListItem.ServerAlias,
                                Calculation = new TypeId(xiValueStatusTimestampSet.CalculationTypeId)
                            };
                            serverAliasesAndCalculations.Add(aliasAndCalculation);
                        }
                    }
                }
            }
            else
            {
                foreach (IXiDataJournalValueStatusTimestampSet xiValueStatusTimestampSet in xiValueStatusTimestampSetList)
                {
                    if (xiValueStatusTimestampSet.CalculationTypeId.LocalId !=
                        JournalDataSampleTypes.RawDataSamples.ToString(CultureInfo.InvariantCulture) &&
                        xiValueStatusTimestampSet.CalculationTypeId.LocalId !=
                        JournalDataSampleTypes.AtTimeDataSamples.ToString(CultureInfo.InvariantCulture))
                    {
                        var s = xiValueStatusTimestampSet as
                                    XiDataJournalValueStatusTimestampSet;
                        if (s is null) throw new InvalidOperationException();
                        var aliasAndCalculation = new AliasAndCalculation
                        { 
                            ServerAlias =
                                s.
                                    OwningXiDataJournalListItem.ServerAlias,
                            Calculation = new TypeId(xiValueStatusTimestampSet.CalculationTypeId)
                        };
                        serverAliasesAndCalculations.Add(aliasAndCalculation);
                    }
                }
            }

            JournalDataValues[]? journalDataValuesArray = Context.ReadCalculatedJournalData(ServerListId, firstTimestamp,
                secondTimestamp,
                calculationPeriod,
                serverAliasesAndCalculations);
            if (journalDataValuesArray is not null)
            foreach (JournalDataValues journalDataValues in journalDataValuesArray)
            {
                XiDataJournalListItem? xiDataJournalListItem;
                if (ListItemsManager.TryGetValue(journalDataValues.ClientAlias, out xiDataJournalListItem))
                    xiDataJournalListItem.Update(journalDataValues);
            }
        }

        /// <summary>
        ///     This method is used to read changed historical values for one or more
        ///     data objects within a specific data journal list.  Changed historical
        ///     values are those that were entered into the journal and then changed
        ///     (corrected) by an operator or other user.
        /// </summary>
        /// <param name="firstTimestamp">
        ///     The filter that specifies the inclusive earliest (oldest) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="secondTimestamp">
        ///     The filter that specifies the inclusive newest (most recent) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="numValuesPerDataObject"> The maximum number of values to be returned per data object. </param>
        /// <param name="dataObjects">
        ///     The list of data objects whose historical values are to be read. Each data object may
        ///     contain zero, one, or more value sets, each of which contains changed values selected and returned by the server.
        /// </param>
        public void ReadJournalDataChanges(FilterCriterion firstTimestamp, FilterCriterion secondTimestamp,
            uint numValuesPerDataObject, List<XiDataJournalListItem> dataObjects)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataJournalList.");

            var serverAliases = new List<uint>();
            var dataObjectDict = new Dictionary<uint, XiDataJournalListItem>();
            if (null == dataObjects || 0 >= dataObjects.Count)
            {
                foreach (XiDataJournalListItem dataObjectInList in ListItemsManager)
                {
                    if (!dataObjectDict.ContainsKey(dataObjectInList.ClientAlias))
                    {
                        serverAliases.Add(dataObjectInList.ServerAlias);
                        dataObjectDict.Add(dataObjectInList.ClientAlias, dataObjectInList);
                    }
                }
            }
            else
            {
                foreach (XiDataJournalListItem dataObject in dataObjects)
                {
                    if (!dataObjectDict.ContainsKey(dataObject.ClientAlias))
                    {
                        serverAliases.Add(dataObject.ServerAlias);
                        dataObjectDict.Add(dataObject.ClientAlias, dataObject);
                    }
                }
            }
            JournalDataChangedValues[]? valuesFromServer = Context.ReadJournalDataChanges(ServerListId, firstTimestamp,
                secondTimestamp,
                numValuesPerDataObject,
                serverAliases);
            if (valuesFromServer is not null)
                foreach (JournalDataChangedValues changedValues in valuesFromServer)
                {
                    XiDataJournalListItem dataObjectInDict = dataObjectDict[changedValues.ClientAlias];
                    dataObjectInDict.SetDataChanges(changedValues);
                }
        }

        /// <summary>
        ///     This method reads the properties associated with a historized data object.
        /// </summary>
        /// <param name="firstTimestamp">
        ///     The filter that specifies the inclusive earliest (oldest) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="secondTimestamp">
        ///     The filter that specifies the inclusive newest (most recent) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="dataObject"> The data object whose property values are to be read. </param>
        /// <param name="propertiesToRead">
        ///     The TypeIds of the properties to read. Each property is identified by its property
        ///     type.
        /// </param>
        public void ReadJournalDataProperties(FilterCriterion firstTimestamp, FilterCriterion secondTimestamp,
            XiDataJournalListItem dataObject, List<TypeId> propertiesToRead)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataJournalList.");

            JournalDataPropertyValue[]? propValueArray = Context.ReadJournalDataProperties(ServerListId, firstTimestamp,
                secondTimestamp,
                dataObject.ServerAlias,
                propertiesToRead);
            dataObject.SetPropertyValues(propValueArray);
        }

        public IEnumerable<IXiDataJournalListItem> ListItems
        {
            get { return ListItemsManager.ToArray(); }
        }

        #endregion
    }
}