using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public class ValueJournalSubscription : IDisposable
    {
        #region construction and destruction
        
        public ValueJournalSubscription(IDataAccessProvider dataProvider, string id)
        {
            _dataProvider = dataProvider;
            Id = id;            

            _dataProvider.JournalAddItem(Id, this);
        }

        public void Dispose()
        {
            _dataProvider.JournalRemoveItem(this);
        }

        #endregion

        #region public functions
        
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; private set; }

        #endregion

        #region private fields

        private readonly IDataAccessProvider _dataProvider;        

        #endregion
    }
}
