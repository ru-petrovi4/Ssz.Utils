using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.DataGrpc.Server;
using Ssz.Utils.DataAccess;
using Ssz.Utils;

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
        /// <param name="firstTimestamp"></param>
        /// <param name="secondTimestamp"></param>
        /// <param name="numValuesPerDataObject"></param>
        /// <param name="valueStatusTimestampSetCollection"></param>
        public ValueStatusTimestamp[][] ReadElementValueJournals(DateTime firstTimestamp, DateTime secondTimestamp,
            uint numValuesPerDataObject, Ssz.Utils.DataAccess.TypeId calculation, uint[] serverAliases)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueJournalList.");

            return Context.ReadElementValueJournals(this,
                firstTimestamp,
                secondTimestamp,
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
                    beginElementValueJournalsCollection.CombineWith(elementValueJournalsCollection);
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

                    for (int index = 0; index < elementValueJournal.DoubleValueStatusCodes.Count; index++)
                    {
                        valuesList.Add(new ValueStatusTimestamp
                        {
                            Value = new Any(elementValueJournal.DoubleValues[index]),
                            ValueStatusCode = elementValueJournal.DoubleValueStatusCodes[index],
                            TimestampUtc = elementValueJournal.DoubleTimestamps[index].ToDateTime()
                        }
                        );
                    }
                    for (int index = 0; index < elementValueJournal.UintValueStatusCodes.Count; index++)
                    {
                        valuesList.Add(new ValueStatusTimestamp
                        {
                            Value = new Any(elementValueJournal.UintValues[index]),
                            ValueStatusCode = elementValueJournal.UintValueStatusCodes[index],
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
        ///     ElementValuesCallback callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<ElementValueJournalsCollection> _incompleteElementValueJournalsCollectionCollection = new CaseInsensitiveDictionary<ElementValueJournalsCollection>();

        #endregion
    }
}