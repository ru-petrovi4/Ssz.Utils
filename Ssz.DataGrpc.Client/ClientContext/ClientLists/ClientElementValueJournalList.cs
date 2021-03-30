using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Common;
using Ssz.Utils;
using Ssz.Utils.DataSource;

namespace Ssz.DataGrpc.Client.ClientLists
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientElementValueJournalList : ClientElementListBase<ClientElementValueJournalListItem>
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="listParams"></param>
        public ClientElementValueJournalList(ClientContext context, CaseInsensitiveDictionary<string>? listParams)
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
        public override ClientElementValueJournalListItem PrepareAddItem(string elementId)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueJournalList.");

            var dataJournalListItem = new ClientElementValueJournalListItem(elementId);
            dataJournalListItem.ClientAlias = ListItemsManager.Add(dataJournalListItem);
            dataJournalListItem.IsInClientList = true;
            dataJournalListItem.PreparedForAdd = true;
            return dataJournalListItem;
        }

        public override IEnumerable<ClientElementValueJournalListItem>? CommitAddItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return CommitAddItemsInternal();
        }

        public override IEnumerable<ClientElementValueJournalListItem>? CommitRemoveItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return CommitRemoveItemsInternal();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstTimeStamp"></param>
        /// <param name="secondTimeStamp"></param>
        /// <param name="numValuesPerDataObject"></param>
        /// <param name="valueStatusTimestampSetCollection"></param>
        public ValueStatusTimestamp[][] ReadElementValueJournals(DateTime firstTimeStamp, DateTime secondTimeStamp,
            uint numValuesPerDataObject, Ssz.Utils.DataSource.TypeId calculation, uint[] serverAliases)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueJournalList.");

            return Context.ReadElementValueJournals(this,
                firstTimeStamp,
                secondTimeStamp,
                numValuesPerDataObject,
                calculation,
                serverAliases);
        }

        /// <summary>
        ///     Returns ElementValueJournals or null, if waiting next message.
        /// </summary>
        /// <param name="elementValueJournalsCollection"></param>
        /// <returns></returns>
        public ValueStatusTimestamp[][]? OnReadElementValueJournal(ElementValueJournalsCollection elementValueJournalsCollection)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueJournalList.");

            if (elementValueJournalsCollection.Guid != @"" && _incompleteElementValueJournalsCollectionCollection.Count > 0)
            {
                var beginElementValueJournalsCollection = _incompleteElementValueJournalsCollectionCollection.TryGetValue(elementValueJournalsCollection.Guid);
                if (beginElementValueJournalsCollection != null)
                {
                    _incompleteElementValueJournalsCollectionCollection.Remove(elementValueJournalsCollection.Guid);
                    beginElementValueJournalsCollection.Add(elementValueJournalsCollection);
                    elementValueJournalsCollection = beginElementValueJournalsCollection;
                }
            }

            if (elementValueJournalsCollection.NextCollectionGuid != @"")
            {
                _incompleteElementValueJournalsCollectionCollection[elementValueJournalsCollection.NextCollectionGuid] = elementValueJournalsCollection;

                return null;
            }
            else
            {
                var list = new List<ValueStatusTimestamp[]>();

                foreach (ElementValueJournal elementValueJournal in elementValueJournalsCollection.ElementValueJournals)
                {
                    var valuesList = new List<ValueStatusTimestamp>();

                    for (int index = 0; index < elementValueJournal.DoubleStatusCodes.Count; index++)
                    {
                        valuesList.Add(new ValueStatusTimestamp
                        {
                            Value = new Any(elementValueJournal.DoubleValues[index]),
                            StatusCode = elementValueJournal.DoubleStatusCodes[index],
                            TimestampUtc = elementValueJournal.DoubleTimestamps[index].ToDateTime()
                        }
                        );
                    }
                    for (int index = 0; index < elementValueJournal.UintStatusCodes.Count; index++)
                    {
                        valuesList.Add(new ValueStatusTimestamp
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

        public IEnumerable<ClientElementValueJournalListItem> ListItems
        {
            get { return ListItemsManager.ToArray(); }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     InformationReport callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<ElementValueJournalsCollection> _incompleteElementValueJournalsCollectionCollection = new CaseInsensitiveDictionary<ElementValueJournalsCollection>();

        #endregion
    }
}