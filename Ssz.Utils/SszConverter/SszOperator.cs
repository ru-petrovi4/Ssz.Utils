using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils
{
    public enum SszOperator
    {
        None = 0,
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        Contains,
        NotContains,
        StartsWith,
        NotStartsWith,
        EndsWith,
        NotEndsWith,
    }

    [Flags]
    public enum SszOperatorOptions
    {
        None = 0,
        CaseSensitive = 1,        
    }
}