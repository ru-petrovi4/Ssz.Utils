using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Logging
{
    public interface IUserFriendlyLogger : ILogger
    {
        string GetScopesString(string[]? excludeScopeNames = null);
    }
}
