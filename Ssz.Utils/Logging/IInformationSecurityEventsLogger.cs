using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Logging
{
    public interface IInformationSecurityEventsLogger : IUserFriendlyLogger
    {        
    }

    public class NullInformationSecurityEventsLoggers : IInformationSecurityEventsLogger
    {
        public static readonly NullInformationSecurityEventsLoggers Instance = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public string GetScopesString(string[]? excludeScopeNames = null)
        {
            return @"";
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {            
        }
    }

    public static class InformationSecurityEventsConstants
    {
        public const string UserScopeName = @"User";
        public const string SourceIpAddressScopeName = @"SourceIpAddress";
        public const string SourceHostScopeName = @"SourceHost";
        public const string SeverityScopeName = @"Severity";
        public const string SucceededScopeName = @"Succeeded";
        public const string EventNameScopeName = @"EventName";
        public const string EventSubjectScopeName = @"EventSubject";
        public const string EventObjectScopeName = @"EventObject";
        public const string EventAdditionalFieldsScopeName = @"EventAdditionalFields";
    }
    
    public static class InformationSecurityEventsLoggerExtensions
    {
        /// <summary>
        ///     Extension method that creates a structured logging record.
        ///     All fields must have an InvariantCulture and can be processed programmatically, except eventDesc and eventDescArgs
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="user"></param>
        /// <param name="sourceIpAddress"></param>
        /// <param name="sourceHost"></param>
        /// <param name="eventId">Event Category</param>
        /// <param name="severity"></param>
        /// <param name="succeeded"></param>
        /// <param name="eventName"></param>
        /// <param name="eventSubject"></param>
        /// <param name="eventObject"></param>
        /// <param name="eventAdditionalFields"></param>
        /// <param name="eventDesc"></param>
        /// <param name="eventDescArgs"></param>
        public static void InformationSecurityEvent(this ILogger logger,
            string user,
            string sourceIpAddress,
            string sourceHost,
            int eventId,
            int severity,
            bool succeeded,
            string eventName,
            string eventSubject,
            string eventObject,
            string eventAdditionalFields,
            string eventDesc,
            params object?[] eventDescArgs)
        {
            using var scope = logger.BeginScope(new (string, object?)[] {
                (InformationSecurityEventsConstants.UserScopeName, user),
                (InformationSecurityEventsConstants.SourceIpAddressScopeName, sourceIpAddress),
                (InformationSecurityEventsConstants.SourceHostScopeName, sourceHost),
                (InformationSecurityEventsConstants.SeverityScopeName, severity),
                (InformationSecurityEventsConstants.SucceededScopeName, succeeded),
                (InformationSecurityEventsConstants.EventNameScopeName, eventName),
                (InformationSecurityEventsConstants.EventSubjectScopeName, eventSubject),
                (InformationSecurityEventsConstants.EventObjectScopeName, eventObject),
                (InformationSecurityEventsConstants.EventAdditionalFieldsScopeName, eventAdditionalFields)
            });
            logger.Log(LogLevel.Information, new EventId(eventId, eventId.ToString()), eventDesc, eventDescArgs);
        }
    }
}