using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class Disposable : IDisposable
    {
        public void Dispose()
        {            
        }

        public static Disposable Empty { get; } = new();
    }
}
