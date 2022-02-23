using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal class __Error
    {
        internal static void EndOfFile()
        {
            throw new InvalidOperationException();
        }
    }
}
