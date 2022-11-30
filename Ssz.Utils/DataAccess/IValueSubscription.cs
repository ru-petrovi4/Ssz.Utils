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
        string MappedElementIdOrConst { get; set; }

        TypeId? DataTypeId { get; set; }

        bool? IsReadable { get; set; }

        bool? IsWritable { get; set; }

        void Update(ValueStatusTimestamp valueStatusTimestamp);
    }
}
