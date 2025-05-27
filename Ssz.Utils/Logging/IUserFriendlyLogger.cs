using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Logging
{
    public interface IUserFriendlyLogger : ILogger
    {
        string GetScopesString(string[]? excludeScopeNames = null);

        void ClearStatistics();

        Dictionary<LogLevel, int> GetStatistics();

        int GetStatistics(LogLevel logLevel, bool count_LogLevel_GreaterThanOrEqualTo = false);
    }
}
