using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ssz.DataAccessGrpc.Client.Managers;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.DataAccessGrpc.Common;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System.Threading.Tasks;
using System.IO;
using Ssz.Utils.Serialization;
using Google.Protobuf;
using CommunityToolkit.HighPerformance;

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
        public async Task DefineListAsync(CaseInsensitiveOrderedDictionary<string>? listParams)
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
        public async Task<ElementValuesJournal[]> ReadElementValuesJournalsAsync(DateTime firstTimestamp, DateTime secondTimestamp,
            uint numValuesPerAlias, Ssz.Utils.DataAccess.TypeId? calculation, CaseInsensitiveOrderedDictionary<string?>? params_, uint[] serverAliases)
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

        public IEnumerable<ClientElementValuesJournalListItem> ListItems
        {
            get { return ListItemsManager.ToArray(); }
        }

        #endregion        
    }
}