using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ssz.Utils.Addons;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Ssz.Utils.Logging
{
    public class LoggersSet : ILoggersSet
    {
        #region construction and destruction

        /// <summary>
        ///     Creates WrapperUserFriendlyLogger that writes to Logger and UserFriendlyLogger.
        ///     if userFriendlyLogger is null, uses NullLogger
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userFriendlyLogger"></param>
        public LoggersSet(ILogger logger, IUserFriendlyLogger? userFriendlyLogger)
        {
            Logger = logger;
            if (userFriendlyLogger is null)
            {
                UserFriendlyLogger = new WrapperUserFriendlyLogger(NullLogger.Instance);
                WrapperUserFriendlyLogger = new WrapperUserFriendlyLogger(logger);
            }
            else
            {
                UserFriendlyLogger = userFriendlyLogger;
                WrapperUserFriendlyLogger = new WrapperUserFriendlyLogger(logger, userFriendlyLogger);
            }
        }

        #endregion

        #region public functions        

        ILogger ILoggersSet.Logger => Logger;

        public static ILoggersSet Empty { get; } = new LoggersSet(NullLogger.Instance, null);

        public ILogger Logger { get; }

        /// <summary>
        ///     Messages are localized. Priority is Information, Error, Warning.
        ///     Can be changed at any time.
        /// </summary>
        public IUserFriendlyLogger UserFriendlyLogger { get; private set; }

        /// <summary>
        ///     Writes to Logger and UserFriendlyLogger
        /// </summary>
        public IUserFriendlyLogger WrapperUserFriendlyLogger { get; private set; }

        public void SetUserFriendlyLogger(IUserFriendlyLogger? userFriendlyLogger)
        {
            if (userFriendlyLogger is null)
            {
                UserFriendlyLogger = new WrapperUserFriendlyLogger(NullLogger.Instance);
                WrapperUserFriendlyLogger = new WrapperUserFriendlyLogger(Logger);
            }
            else
            {
                UserFriendlyLogger = userFriendlyLogger;
                WrapperUserFriendlyLogger = new WrapperUserFriendlyLogger(Logger, userFriendlyLogger);
            }
        }

        #endregion
    }

    public class LoggersSet<TCategoryName> : ILoggersSet
    {
        #region construction and destruction

        /// <summary>
        ///     Creates WrapperUserFriendlyLogger that writes to Logger and UserFriendlyLogger.
        ///     if userFriendlyLogger is null, uses NullLogger
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userFriendlyLogger"></param>
        public LoggersSet(ILogger<TCategoryName> logger, IUserFriendlyLogger? userFriendlyLogger)
        {
            Logger = logger;
            if (userFriendlyLogger is null)
            {
                UserFriendlyLogger = new WrapperUserFriendlyLogger(NullLogger.Instance);
                WrapperUserFriendlyLogger = new WrapperUserFriendlyLogger(logger);
            }
            else
            {
                UserFriendlyLogger = userFriendlyLogger;
                WrapperUserFriendlyLogger = new WrapperUserFriendlyLogger(logger, userFriendlyLogger);
            }
        }

        #endregion

        #region public functions        

        ILogger ILoggersSet.Logger => Logger;

        public static ILoggersSet Empty { get; } = new LoggersSet<object>(NullLogger<object>.Instance, null);

        public ILogger<TCategoryName> Logger { get; }

        /// <summary>
        ///     Messages are localized. Priority is Information, Error, Warning.
        ///     Can be changed at any time.
        /// </summary>
        public IUserFriendlyLogger UserFriendlyLogger { get; private set; }

        /// <summary>
        ///     Writes to Logger and UserFriendlyLogger
        /// </summary>
        public IUserFriendlyLogger WrapperUserFriendlyLogger { get; private set; }

        public void SetUserFriendlyLogger(IUserFriendlyLogger? userFriendlyLogger)
        {
            if (userFriendlyLogger is null)
            {
                UserFriendlyLogger = new WrapperUserFriendlyLogger(NullLogger.Instance);
                WrapperUserFriendlyLogger = new WrapperUserFriendlyLogger(Logger);
            }
            else
            {
                UserFriendlyLogger = userFriendlyLogger;
                WrapperUserFriendlyLogger = new WrapperUserFriendlyLogger(Logger, userFriendlyLogger);
            }
        }

        #endregion
    }
}
