using System;
using System.Collections.Generic;
using System.Linq;

using Ssz.Utils;
using Ssz.Utils.DataAccess;

namespace Ssz.Operator.Core.DataAccess
{
    internal class DsEventSourceArea : EventSourceArea
    {
        #region construction and destruction

        public DsEventSourceArea(string area, IDataAccessProvider dataAccessProvider) :
            base(area, dataAccessProvider)
        {
        }

        #endregion        
    }
}