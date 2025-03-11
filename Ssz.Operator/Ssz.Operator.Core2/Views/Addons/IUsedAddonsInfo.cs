using System;
using System.Collections.Generic;

namespace Ssz.Operator.Core.Addons
{
    public interface IUsedAddonsInfo
    {
        IEnumerable<Guid> GetUsedAddonGuids();
    }
}