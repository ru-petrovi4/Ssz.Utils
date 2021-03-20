using System.Collections.Generic;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.ListItems
{
    public class ServerElementValueJournalListItem : ServerElementListItemBase
    {
        #region construction and destruction

        public ElementValueJournalListItem(uint clientAlias, uint serverAlias)
            : base(clientAlias, serverAlias)
        {
        }

        #endregion

        #region public functions

        public void UpdateDictionary(JournalDataValues dataValues)
        {
            JournalDataValues journalDataValues = null;
            if (_journalDataValues.TryGetValue(dataValues.Calculation, out journalDataValues))
                _journalDataValues.Remove(dataValues.Calculation);
            _journalDataValues.Add(dataValues.Calculation, dataValues);
        }

        #endregion

        #region protected functions

        protected Dictionary<TypeId, JournalDataValues> _journalDataValues = new Dictionary<TypeId, JournalDataValues>();

        #endregion
    }
}