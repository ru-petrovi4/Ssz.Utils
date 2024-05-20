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
        string ElementId { get; }

        void Update(string mappedElementIdOrConst);        

        void Update(ValueStatusTimestamp valueStatusTimestamp);
    }
}
