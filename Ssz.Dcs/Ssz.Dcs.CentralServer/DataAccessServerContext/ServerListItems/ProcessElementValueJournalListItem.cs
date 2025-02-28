

using Ssz.Utils.DataAccess;
using System.Collections.Generic;

namespace Ssz.Dcs.CentralServer.ServerListItems
{
    /// <summary>
    ///     The DataJournalListItem class is used to hold the actual SimCore HDA Data Value.
    /// </summary>
    public class ProcessElementValuesJournalListItem : ElementListItemBase
    {
        #region construction and destruction

        public ProcessElementValuesJournalListItem(uint clientAlias, uint serverAlias, string elementId)
            : base(clientAlias, serverAlias, elementId)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                foreach (ValuesJournalSubscription valueJournalSubscription in ValuesJournalSubscriptionsCollection)
                {
                    valueJournalSubscription.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public List<ValuesJournalSubscription> ValuesJournalSubscriptionsCollection { get; } = new();

        #endregion
    }
}