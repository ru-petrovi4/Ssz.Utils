using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Serialization
{
    public class UnknownObject
    {
        public string TypeString { get; set; } = null!;

        public byte[] Data { get; set; } = null!;
    }
}
