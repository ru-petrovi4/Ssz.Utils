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

        void Update(AddItemResult addItemResult);

        void Update(ValueStatusTimestamp valueStatusTimestamp);
    }

    public class AddItemResult
    {
        public ResultInfo ResultInfo = null!;

        public TypeId? DataTypeId;

        public bool IsReadable;

        public bool IsWritable;

        /// <summary>
        ///     Unspecified Unknown AddItemResult.
        /// </summary>
        public static readonly AddItemResult UnknownAddItemResult = new AddItemResult { ResultInfo = new ResultInfo { StatusCode = JobStatusCodes.Unknown } };

        /// <summary>
        ///     Unspecified InvalidArgument AddItemResult.
        /// </summary>
        public static readonly AddItemResult InvalidArgumentAddItemResult = new AddItemResult { ResultInfo = new ResultInfo { StatusCode = JobStatusCodes.InvalidArgument } };
    }
}
