using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Logging
{
    public interface ILoggersSet
    {
        ILogger Logger { get; }

        /// <summary>
        ///     Messages are localized. Priority is Information, Error, Warning.
        ///     Can be changed at any time.
        /// </summary>
        IUserFriendlyLogger UserFriendlyLogger { get; }

        /// <summary>
        ///     Writes to Logger and UserFriendlyLogger
        /// </summary>
        IUserFriendlyLogger LoggerAndUserFriendlyLogger { get; }

        void SetUserFriendlyLogger(IUserFriendlyLogger? userFriendlyLogger);
    }
}
