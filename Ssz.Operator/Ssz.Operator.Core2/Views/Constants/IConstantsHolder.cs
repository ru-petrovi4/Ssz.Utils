using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.Constants
{
    public interface IConstantsHolder
    {
        void FindConstants(HashSet<string> constants);
    }
}