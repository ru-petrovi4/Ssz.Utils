using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.DataGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;

namespace Ssz.DataGrpc.Client.ClientLists
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientElementValuesJournalList : ClientElementListBase<ClientElementValuesJournalListItem>
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="listParams"></param>
        public ClientElementValuesJournalList(ClientContext context, CaseInsensitiveDictionary<string>? listParams)
            : base(context)
        {
            ListType = (uint)StandardListType.ElementValuesJournalList;
            Context.DefineList(this, listParams);
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementId"></param>
        /// <returns></returns>
        public override ClientElementValuesJournalListItem PrepareAddItem(string elementId)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValuesJournalList.");

            var dataJournalListItem = new ClientElementValuesJournalListItem(elementId);
            dataJournalListItem.ClientAlias = ListItemsManager.Add(dataJournalListItem);
            dataJournalListItem.IsInClientList = true;
            dataJournalListItem.PreparedForAdd = true;
            return dataJournalListItem;
        }

        public override IEnumerable<ClientElementValuesJournalListItem>? CommitAddItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return CommitAddItemsInternal();
        }

        public override IEnumerable<ClientElementValuesJournalListItem>? CommitRemoveItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return CommitRemoveItemsInternal();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstTimestamp"></param>
        /// <param name="secondTimestamp"></param>
        /// <param name="numValuesPerAlias"></param>
        /// <param name="calculation"></param>
        /// <param name="params_"></param>
        /// <param name="serverAliases"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public ValueStatusTimestamp[][] ReadElementValuesJournals(DateTime firstTimestamp, DateTime secondTimestamp,
            uint numValuesPerAlias, Ssz.Utils.DataAccess.TypeId? calculation, CaseInsensitiveDictionary<string>? params_, uint[] serverAliases)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValuesJournalList.");

            return Context.ReadElementValuesJournals(this,
                firstTimestamp,
                secondTimestamp,
                numValuesPerAlias,
                calculation,
                params_,
                serverAliases);
        }

        /// <summary>
        ///     Returns ElementValuesJournals or null, if waiting next message.
        /// </summary>
        /// <param name="elementValuesJournalsCollection"></param>
        /// <returns></returns>
        public ValueStatusTimestamp[][]? OnReadElementValuesJournals(ElementValuesJournalsCollection elementValuesJournalsCollection)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValuesJournalList.");

            if (!String.IsNullOrEmpty(elementValuesJournalsCollection.Guid) && _incompleteElementValuesJournalsCollectionCollection.Count > 0)
            {
                var beginElementValuesJournalsCollection = _incompleteElementValuesJournalsCollectionCollection.TryGetValue(elementValuesJournalsCollection.Guid);
                if (beginElementValuesJournalsCollection is not null)
                {
                    _incompleteElementValuesJournalsCollectionCollection.Remove(elementValuesJournalsCollection.Guid);
                    beginElementValuesJournalsCollection.CombineWith(elementValuesJournalsCollection);
                    elementValuesJournalsCollection = beginElementValuesJournalsCollection;
                }
            }

            if (!String.IsNullOrEmpty(elementValuesJournalsCollection.NextCollectionGuid))
            {
                _incompleteElementValuesJournalsCollectionCollection[elementValuesJournalsCollection.NextCollectionGuid] = elementValuesJournalsCollection;

                return null;
            }
            else
            {
                var list = new List<ValueStatusTimestamp[]>();

                foreach (ElementValuesJournal elementValuesJournal in elementValuesJournalsCollection.ElementValuesJournals)
                {
                    var valuesList = new List<ValueStatusTimestamp>();

                    for (int index = 0; index < elementValuesJournal.DoubleValueStatusCodes.Count; index++)
                    {
                        valuesList.Add(new ValueStatusTimestamp
                        {
                            Value = new Any(elementValuesJournal.DoubleValues[index]),
                            ValueStatusCode = elementValuesJournal.DoubleValueStatusCodes[index],
                            TimestampUtc = elementValuesJournal.DoubleTimestamps[index].ToDateTime()
                        }
                        );
                    }
                    for (int index = 0; index < elementValuesJournal.UintValueStatusCodes.Count; index++)
                    {
                        valuesList.Add(new ValueStatusTimestamp
                        {
                            Value = new Any(elementValuesJournal.UintValues[index]),
                            ValueStatusCode = elementValuesJournal.UintValueStatusCodes[index],
                            TimestampUtc = elementValuesJournal.UintTimestamps[index].ToDateTime()
                        }
                        );
                    }

                    list.Add(valuesList.ToArray());
                }

                return list.ToArray();
            }
        }

        public IEnumerable<ClientElementValuesJournalListItem> ListItems
        {
            get { return ListItemsManager.ToArray(); }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     ElementValuesCallback callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<ElementValuesJournalsCollection> _incompleteElementValuesJournalsCollectionCollection = new();

        #endregion
    }
}