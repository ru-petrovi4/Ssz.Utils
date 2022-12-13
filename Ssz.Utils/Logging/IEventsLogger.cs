using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Logging
{
    public interface IEventsLogger : IUserFriendlyLogger
    {
    }

    /// <summary>
    ///     ILogger extensions for structured logging using typed signatures.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        ///     Extension method that creates a structured logging record
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="user"></param>
        /// <param name="sourceAddress"></param>
        /// <param name="eventId"></param>
        /// <param name="succeeded"></param>
        /// <param name="eventDesc"></param>
        /// <param name="args"></param>
        public static void LogStructured(this ILogger logger,
            string user,
            string sourceAddress,
            int eventId,
            bool succeeded,
            string eventDesc,
            params object?[] args)
        {
            logger.Log(LogLevel.Information, new EventId(eventId, eventId.ToString()), Ssz.Utils.CsvHelper.FormatForCsv(user, sourceAddress, succeeded, String.Format(eventDesc, args)));
        }
    }
}