﻿using Ssz.Dcs.CentralServer.Common;
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
    /// <summary>
    ///     For event files with no extension
    /// </summary>
    public static class ExperionEventsJournalRptFileHelper
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
                        if (line.Length < 10 || line.StartsWith(@"P"))
                            continue;
                        if (line.StartsWith(@"A---------------") && prevLine is not null)
                        {
                            isTable = true;
                            int endIndex = prevLine.Length;                                
                            foreach (string f in prevLine.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries).Reverse())
                            {
                                int startIndex = prevLine.LastIndexOf(f, endIndex);
                                string name;
                                if (startIndex == 0)
                                {
                                    startIndex = 1;
                                    name = f.Substring(1);
                                }
                                else
                                {
                                    name = f;
                                }
                                columnInfos.Add(new ColumnInfo
                                {
                                    StartIndex = startIndex,
                                    Length = endIndex - startIndex,
                                    Name = name
                                });
                                endIndex = startIndex - 1;
                            }
                            columnInfos.Reverse();
                        }
                    }
                    else
                    {
                        if (line.Length < 10 || line.StartsWith(@"P"))
                        {
                            isTable = false;
                            columnInfos.Clear();
                            continue;
                        }

                        var c0 = columnInfos[0];
                        string dateTimeString = line.Substring(c0.StartIndex, c0.Length).TrimEnd();
                        if (String.IsNullOrEmpty(dateTimeString))
                            continue;
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
