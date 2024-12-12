using System.Collections.Generic;

namespace Ssz.Operator.Core.Constants
{
    public interface IConstantsHolder
    {
        void FindConstants(HashSet<string> constants);
    }
}