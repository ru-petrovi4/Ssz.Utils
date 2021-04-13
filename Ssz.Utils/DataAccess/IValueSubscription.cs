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
        void Update(ValueStatusTimestamp valueStatusTimestamp);

        /// <summary>
        ///     Property to use internally in ModelDataProvider. You need to
        ///     implement this field, but you shouldn't change its value.
        /// </summary>
        object? Obj { get; set; }
    }
}
