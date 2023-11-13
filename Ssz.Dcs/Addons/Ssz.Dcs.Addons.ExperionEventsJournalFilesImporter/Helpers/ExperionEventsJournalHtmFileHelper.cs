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
    public static class ExperionEventsJournalHtmFileHelper
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

            try
            {
                using StreamReader reader = CharsetDetectorHelper.GetStreamReader(stream, defaultEncoding);                

                bool isTable = false;
                List<ColumnInfo> columnInfos = new();

                string? line = null;
                while (!cancellationToken.IsCancellationRequested)
                {
                    string? prevLine = line;
                    line = reader.ReadLine();
                    if (line is null)
                        break;

                    if (!isTable)
                    {
                        if (line.Length < 10)
                            continue;
                        if (line.Contains(@"---------------") && prevLine is not null)
                        {
                            isTable = true;
                            int endIndex = prevLine.Length;
                            foreach (var f in prevLine.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries).Reverse())
                            {
                                int startIndex = prevLine.LastIndexOf(f, endIndex);
                                columnInfos.Add(new ColumnInfo
                                {
                                    StartIndex = startIndex,
                                    Length = endIndex - startIndex,
                                    Name = f
                                });
                                endIndex = startIndex - 1;
                            }
                            columnInfos.Reverse();
                        }
                    }
                    else
                    {
                        if (line.Length <= "                               -End of Report-".Length)
                        {
                            isTable = false;
                            columnInfos.Clear();
                            continue;
                        }

                        var c0 = columnInfos[0];
                        string dateTimeString = line.Substring(c0.StartIndex, c0.Length).TrimEnd();
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

                        eventMessage.Fields = new CaseInsensitiveDictionary<string?>(
                            columnInfos.Select(ci => new KeyValuePair<string, string?>(ci.Name, (string?)line.Substring(ci.StartIndex, ci.Length).TrimEnd())));
                        eventMessages.Add(eventMessage);
                    }
                }                
            }
            catch //(Exception ex)
            {
                //logger.LogError(ex, Properties.Resources.CsvHelper_CsvFileReadingError);
            }            

            return Task.CompletedTask;
        }

        private struct ColumnInfo
        {
            public int StartIndex;
            public int Length;
            public string Name;
        }
    }
}
