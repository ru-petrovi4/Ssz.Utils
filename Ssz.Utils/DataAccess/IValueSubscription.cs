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

        AddItemResult? AddItemResult { get; set; }

        void Update(ValueStatusTimestamp valueStatusTimestamp);
    }

    public class AddItemResult
    {
        /// <summary>
        ///     See Ssz.Utils.JobStatusCodes
        /// </summary>
        public uint AddItemJobStatusCode;

        public TypeId? DataTypeId;

        public bool IsReadable;

        public bool IsWritable;
    }
}
