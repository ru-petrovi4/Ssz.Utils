using Ssz.Dcs.CentralServer.Common;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.Addons.ExperionEventsJournalFilesImporter
{
    public static class ExperionEventsJournalCsvFileHelper
    {
        public static Task ProcessFileAsync(Stream stream, 
            Encoding defaultEncoding, 
            DateTime maxProcessedTimeUtc, 
            CaseInsensitiveDictionary<string?> options,
            List<EventMessage> eventMessages,
            ILoggersSet loggersSet,
            CancellationToken cancellationToken)
        {
            string? dateTimeFormatOption = options.TryGetValue(ExperionEventsJournalFilesImporterAddon.JournalFiles_DateTimeFormat_OptionName) ??
                ExperionEventsJournalFilesImporterAddon.JournalFiles_DateTimeFormat_OptionDefaultValue;
            string? sourceTimeZoneOption = options.TryGetValue(ExperionEventsJournalFilesImporterAddon.JournalFiles_SourceTimeZone_OptionName) ??
                ExperionEventsJournalFilesImporterAddon.JournalFiles_SourceTimeZone_OptionDefaultValue;
            int sourceTimeZone = new Any(sourceTimeZoneOption).ValueAsInt32(false);
            string separator = @",";
            try
            {                             
                using StreamReader reader = CharsetDetectorHelper.GetStreamReader(stream, defaultEncoding);                

                bool isTable = false;
                string?[] columns = new string?[0];
                int eventTimeColumnIndex = -1;
                int alarmTimeColumnIndex = -1;

                string? line = null;
                while (!cancellationToken.IsCancellationRequested)
                {
                    string? prevLine = line;
                    line = reader.ReadLine();
                    if (line is null)
                        break;

                    if (!isTable)
                    {
                        if (line == @"")
                            continue;
                        if (line.StartsWith("sep="))
                        {
                            separator = line.Substring(4, line.Length - 4);
                            continue;
                        }
                        if (separator != @"" && line.Contains(separator + @"Source" + separator))
                        {
                            isTable = true;
                            columns = CsvHelper.ParseCsvLine(separator, line);
                            eventTimeColumnIndex = columns.IndexOf(c => c == @"Event Time");
                            if (eventTimeColumnIndex == -1)
                                eventTimeColumnIndex = columns.IndexOf(c => c == @"EventTime");
                            alarmTimeColumnIndex = columns.IndexOf(c => c == @"Alarm Time");
                            if (alarmTimeColumnIndex == -1)
                                alarmTimeColumnIndex = columns.IndexOf(c => c == @"AlarmTime");
                            if (eventTimeColumnIndex == -1 && alarmTimeColumnIndex == -1)
                            {
                                loggersSet.UserFriendlyLogger.LogError(Properties.Resources.EventTimeColumnNotfound, line);
                                break;
                            }
                        }
                    }
                    else
                    {
                        var parts = CsvHelper.ParseCsvLine(separator, line);
                            
                        if (eventTimeColumnIndex >= 0)
                        {
                            if (String.IsNullOrWhiteSpace(line) || eventTimeColumnIndex >= parts.Length)
                                continue;
                            string? dateTimeString = parts[eventTimeColumnIndex];
                            var (timeUtc, succeeded) = ExperionHelper.GetDateTime(dateTimeString, dateTimeFormatOption);
                            if (!succeeded)
                            {
                                loggersSet.Logger.LogError("DateTime parse error: " + dateTimeString + "; Format: " + dateTimeFormatOption);
                                continue;
                            }

                            timeUtc -= TimeSpan.FromHours(sourceTimeZone);
                            if (timeUtc <= maxProcessedTimeUtc)
                                continue;

                            var eventMessage = new EventMessage(new Ssz.Utils.DataAccess.EventId
                            {
                                Conditions = new List<TypeId> { EventMessageConstants.ExternalEvent_TypeId }
                            });

                            eventMessage.EventType = EventType.SystemEvent;
                            eventMessage.OccurrenceTimeUtc = timeUtc;

                            CaseInsensitiveDictionary<string?> nameValueCollection = new();
                            foreach (int i in Enumerable.Range(0, parts.Length))
                            {
                                if (i >= columns.Length)
                                    break;
                                if (i == eventTimeColumnIndex || String.IsNullOrEmpty(columns[i]))
                                    continue;
                                nameValueCollection.Add(columns[i] ?? @"", parts[i]);
                            }

                            eventMessage.Fields = nameValueCollection;
                            eventMessages.Add(eventMessage);
                        }
                        else if (alarmTimeColumnIndex >= 1)
                        {
                            if (String.IsNullOrWhiteSpace(line) || alarmTimeColumnIndex >= parts.Length)
                                continue;
                            string? dateTimeString = parts[alarmTimeColumnIndex];
                            var (timeUtc, succeeded) = ExperionHelper.GetDateTime(dateTimeString, dateTimeFormatOption);
                            if (!succeeded)
                            {
                                loggersSet.Logger.LogError("DateTime parse error: " + dateTimeString + "; Format: " + dateTimeFormatOption);
                                continue;
                            }
                            timeUtc -= TimeSpan.FromHours(sourceTimeZone);
                            if (timeUtc <= maxProcessedTimeUtc)
                                continue;

                            var eventMessage = new EventMessage(new Ssz.Utils.DataAccess.EventId
                            {
                                Conditions = new List<TypeId> { EventMessageConstants.ExternalEvent_TypeId }
                            });

                            eventMessage.EventType = EventType.SystemEvent;
                            eventMessage.OccurrenceTimeUtc = timeUtc;

                            CaseInsensitiveDictionary<string?> nameValueCollection = new()
                            {
                                { @"", parts[0] }
                            };
                            foreach (int i in Enumerable.Range(1, parts.Length - 1))
                            {
                                if (i == alarmTimeColumnIndex || String.IsNullOrEmpty(columns[i]))
                                    continue;
                                nameValueCollection.Add(columns[i] ?? @"", parts[i]);
                            }

                            eventMessage.Fields = nameValueCollection;
                            eventMessages.Add(eventMessage);
                        }
                    }
                }                
            }
            catch //(Exception ex)
            {
                //logger.LogError(ex, Properties.Resources.CsvHelper_CsvFileReadingError);
            }

            return Task.CompletedTask;
        }
    }
}
