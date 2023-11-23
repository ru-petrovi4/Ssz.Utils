using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ssz.DataAccessGrpc.Client.Managers;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client.ClientLists
{
    /// <summary>
    /// 
    /// </summary>
    internal class ClientElementValuesJournalList : ClientElementListBase<ClientElementValuesJournalListItem>
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="listParams"></param>
        public ClientElementValuesJournalList(ClientContext context)
            : base(context)
        {
            ListType = (uint)StandardListType.ElementValuesJournalList;            
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listParams"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task DefineListAsync(CaseInsensitiveDictionary<string>? listParams)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            await Context.DefineListAsync(this, listParams);
        }

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

        /// <summary>
        ///     Returns failed items only.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public override async Task<IEnumerable<ClientElementValuesJournalListItem>?> CommitAddItemsAsync()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return await CommitAddItemsInternalAsync();
        }

        public override async Task<IEnumerable<ClientElementValuesJournalListItem>?> CommitRemoveItemsAsync()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return await CommitRemoveItemsInternalAsync();
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
        public async Task<ValueStatusTimestamp[][]> ReadElementValuesJournalsAsync(DateTime firstTimestamp, DateTime secondTimestamp,
            uint numValuesPerAlias, Ssz.Utils.DataAccess.TypeId? calculation, CaseInsensitiveDictionary<string?>? params_, uint[] serverAliases)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValuesJournalList.");

            return await Context.ReadElementValuesJournalsAsync(this,
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

                    for (int index = 0; index < elementValuesJournal.DoubleStatusCodes.Count; index++)
                    {
                        valuesList.Add(new ValueStatusTimestamp
                        {
                            Value = new Any(elementValuesJournal.DoubleValues[index]),
                            StatusCode = elementValuesJournal.DoubleStatusCodes[index],
                            TimestampUtc = elementValuesJournal.DoubleTimestamps[index].ToDateTime()
                        }
                        );
                    }
                    for (int index = 0; index < elementValuesJournal.UintStatusCodes.Count; index++)
                    {
                        valuesList.Add(new ValueStatusTimestamp
                        {
                            Value = new Any(elementValuesJournal.UintValues[index]),
                            StatusCode = elementValuesJournal.UintStatusCodes[index],
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