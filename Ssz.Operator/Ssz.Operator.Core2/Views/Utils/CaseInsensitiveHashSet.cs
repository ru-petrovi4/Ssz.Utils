#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.Utils
{    

    public class CaseInsensitiveHashSet : HashSet<string>
    {
        #region construction and destruction

        public CaseInsensitiveHashSet() : base(StringComparer.InvariantCultureIgnoreCase)
        {
        }

        #endregion
    }
}
