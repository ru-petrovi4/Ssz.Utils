using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public class ValuesJournalSubscription : IDisposable
    {
        #region construction and destruction
        
        public ValuesJournalSubscription(IDataAccessProvider dataAccessProvider, string id)
        {
            DataAccessProvider = dataAccessProvider;
            Id = id;            

            DataAccessProvider.JournalAddItem(Id, this);
        }

        public void Dispose()
        {
            DataAccessProvider.JournalRemoveItem(this);
        }

        #endregion

        #region public functions
        
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; private set; }

        public IDataAccessProvider DataAccessProvider { get; private set; }

        #endregion
    }
}
