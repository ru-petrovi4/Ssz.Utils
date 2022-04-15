using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Net4
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
