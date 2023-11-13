using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    [Flags]
    public enum InstructorAccessFlags
    {
        CanReadSavesOfOtherUsers = 0x1
    }
}
