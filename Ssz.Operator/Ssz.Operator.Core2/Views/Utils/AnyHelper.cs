#nullable disable

using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.Utils
{
    public static class ObsoleteAnyHelper
    {
        public static T ConvertTo<T>(object value, bool stringIsLocalized, string stringFormat = null)
        {
            return (new Any(value)).ValueAs<T>(stringIsLocalized, stringFormat);
        }

        public static object ConvertTo(object value, Type toType, bool stringIsLocalized, string stringFormat = null)
        {
            return (new Any(value)).ValueAs(toType, stringIsLocalized, stringFormat);
        }        
    }
}
