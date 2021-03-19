using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Client.Core.Context;
using Ssz.DataGrpc.Client.Core.ListItems;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Common;
using Ssz.Utils;

namespace Ssz.DataGrpc.Client.Core.Lists
{
    /// <summary>
    /// 
    /// </summary>
    public class DataGrpcElementValueJournalList : DataGrpcElementListBase<DataGrpcElementValueJournalListItem>
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="listParams"></param>
        public DataGrpcElementValueJournalList(DataGrpcContext context, CaseInsensitiveDictionary<string>? listParams)
            : base(context)
        {
            ListType = (uint)StandardListType.ElementValueJournalList;
            Context.DefineList(this, listParams);
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementId"></param>
        /// <returns></returns>
        public override DataGrpcElementValueJournalListItem PrepareAddItem(string elementId)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueJournalList.");

            var dataJournalListItem = new DataGrpcElementValueJournalListItem(elementId);
            dataJournalListItem.ClientAlias = ListItemsManager.Add(dataJournalListItem);
            dataJournalListItem.IsInClientList = true;
            dataJournalListItem.PreparedForAdd = true;
            return dataJournalListItem;
        }

        public override IEnumerable<DataGrpcElementValueJournalListItem>? CommitAddItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueList.");

            return CommitAddItemsInternal();
        }

        public override IEnumerable<DataGrpcElementValueJournalListItem>? CommitRemoveItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueList.");

            return CommitRemoveItemsInternal();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstTimeStamp"></param>
        /// <param name="secondTimeStamp"></param>
        /// <param name="numValuesPerDataObject"></param>
        /// <param name="dataGrpcValueStatusTimestampSetCollection"></param>
        public DataGrpcValueStatusTimestamp[][] ReadElementValueJournalForTimeInterval(DateTime firstTimeStamp, DateTime secondTimeStamp,
            uint numValuesPerDataObject, TypeId calculation, uint[] serverAliases)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueJournalList.");

            return Context.ReadElementValueJournalForTimeInterval(this,
                firstTimeStamp,
                secondTimeStamp,
                numValuesPerDataObject,
                calculation,
                serverAliases);
        }

        /// <summary>
        ///     Returns ElementValueJournals or null, if waiting next message.
        /// </summary>
        /// <param name="elementValueJournalArrays"></param>
        /// <returns></returns>
        public DataGrpcValueStatusTimestamp[][]? OnReadElementValueJournal(ElementValueJournalArrays elementValueJournalArrays)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueJournalList.");

            if (elementValueJournalArrays.Guid != @"" && _incompleteElementValueJournalArraysCollection.Count > 0)
            {
                var beginElementValueJournalArrays = _incompleteElementValueJournalArraysCollection.TryGetValue(elementValueJournalArrays.Guid);
                if (beginElementValueJournalArrays != null)
                {
                    _incompleteElementValueJournalArraysCollection.Remove(elementValueJournalArrays.Guid);
                    beginElementValueJournalArrays.Add(elementValueJournalArrays);
                    elementValueJournalArrays = beginElementValueJournalArrays;
                }
            }

            if (elementValueJournalArrays.NextArraysGuid != @"")
            {
                _incompleteElementValueJournalArraysCollection[elementValueJournalArrays.NextArraysGuid] = elementValueJournalArrays;

                return null;
            }
            else
            {
                var list = new List<DataGrpcValueStatusTimestamp[]>();

                foreach (ElementValueJournal elementValueJournal in elementValueJournalArrays.ElementValueJournals)
                {
                    var valuesList = new List<DataGrpcValueStatusTimestamp>();

                    for (int index = 0; index < elementValueJournal.DoubleStatusCodes.Count; index++)
                    {
                        valuesList.Add(new DataGrpcValueStatusTimestamp
                        {
                            Value = new Any(elementValueJournal.DoubleValues[index]),
                            StatusCode = elementValueJournal.DoubleStatusCodes[index],
                            TimestampUtc = elementValueJournal.DoubleTimestamps[index].ToDateTime()
                        }
                        );
                    }
                    for (int index = 0; index < elementValueJournal.UintStatusCodes.Count; index++)
                    {
                        valuesList.Add(new DataGrpcValueStatusTimestamp
                        {
                            Value = new Any(elementValueJournal.UintValues[index]),
                            StatusCode = elementValueJournal.UintStatusCodes[index],
                            TimestampUtc = elementValueJournal.UintTimestamps[index].ToDateTime()
                        }
                        );
                    }

                    list.Add(valuesList.ToArray());
                }

                return list.ToArray();
            }
        }

        public IEnumerable<DataGrpcElementValueJournalListItem> ListItems
        {
            get { return ListItemsManager.ToArray(); }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     InformationReport callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<ElementValueJournalArrays> _incompleteElementValueJournalArraysCollection = new CaseInsensitiveDictionary<ElementValueJournalArrays>();

        #endregion
    }
}