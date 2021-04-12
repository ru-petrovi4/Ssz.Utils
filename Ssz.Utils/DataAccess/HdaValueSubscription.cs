using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public class HdaValueSubscription : IDisposable
    {
        #region construction and destruction
        
        public HdaValueSubscription(IDataAccessProvider dataProvider, string id)
        {
            _dataProvider = dataProvider;
            Id = id;            

            _dataProvider.HdaAddItem(Id, this);
        }

        public void Dispose()
        {
            _dataProvider.HdaRemoveItem(this);
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
