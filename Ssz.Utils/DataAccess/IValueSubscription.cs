using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public interface IValueSubscription
    {        
        void Update(string mappedElementIdOrConst);        

        void Update(ValueStatusTimestamp valueStatusTimestamp);
    }
}
