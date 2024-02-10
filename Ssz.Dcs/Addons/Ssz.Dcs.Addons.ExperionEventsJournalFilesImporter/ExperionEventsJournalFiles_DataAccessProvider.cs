using Ssz.Dcs.CentralServer.Common;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Ssz.Dcs.Addons.ExperionEventsJournalFilesImporter
{
    internal class ExperionEventsJournalFiles_DataAccessProvider : DataAccessProviderBase
    {
        #region construction and destruction

        public ExperionEventsJournalFiles_DataAccessProvider(ExperionEventsJournalFilesImporterAddon addon, ILogger<ExperionEventsJournalFiles_DataAccessProvider> logger, IUserFriendlyLogger? userFriendlyLogger = null) :
            base(new LoggersSet(logger, userFriendlyLogger))
        {
            Addon = addon;
        }

        #endregion

        #region public functions

        public override DateTime LastFailedConnectionDateTimeUtc => _lastFailedConnectionDateTimeUtc;

        public override DateTime LastSuccessfulConnectionDateTimeUtc => _lastSuccessfulConnectionDateTimeUtc;

        public override event EventHandler<EventMessagesCallbackEventArgs> EventMessagesCallback = delegate { };

        public DateTime LastScanTimeUtc { get; protected set; }

        public DateTime LastJournalFilesDeleteScanTimeUtc { get; protected set; }

        /// <summary>
        ///     You can set DataAccessProviderOptions.ElementValueListCallbackIsEnabled = false and invoke PollElementValuesChangesAsync(...) manually.
        /// </summary>
        /// <param name="elementIdsMap"></param>
        /// <param name="serverAddress"></param>
        /// <param name="clientApplicationName"></param>
        /// <param name="clientWorkstationName"></param>
        /// <param name="systemNameToConnect"></param>
        /// <param name="contextParams"></param>
        /// <param name="options"></param>
        /// <param name="callbackDispatcher"></param>
        public override void Initialize(ElementIdsMap? elementIdsMap,            
            string serverAddress,
            string clientApplicationName,
            string clientWorkstationName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string?> contextParams,
            DataAccessProviderOptions options,
            IDispatcher? callbackDispatcher)
        {
            base.Initialize(elementIdsMap,                
                serverAddress,
                clientApplicationName, 
                clientWorkstationName, 
                systemNameToConnect, 
                contextParams,
                options,
                callbackDispatcher);

            if (CallbackDispatcher is null || !Options.EventListCallbackIsEnabled) 
                return;            

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            
            _workingTask = Task.Factory.StartNew(async ct =>
            {                
                await WorkingTaskMainAsync(cancellationToken);
            }, TaskCreationOptions.LongRunning);
        }        

        /// <summary>
        ///     Tou can call DisposeAsync() instead of this method.
        ///     Closes WITH waiting working thread exit.
        /// </summary>
        public override async Task CloseAsync()
        {
            if (!IsInitialized) 
                return;            

            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            if (_workingTask is not null)
                await _workingTask;

            await base.CloseAsync();
        }

        /// <summary>
        ///     Throws if any errors.
        /// </summary>
        /// <param name="recipientPath"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        public override Task<ReadOnlyMemory<byte>> PassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            switch (passthroughName)
            {
                case PassthroughConstants.SetAddonVariables:
                    var nameValuesCollectionString = Encoding.UTF8.GetString(dataToSend.Span);
                    var nameValuesCollection = NameValueCollectionHelper.Parse(nameValuesCollectionString);
                    Addon.CsvDb.SetData(AddonBase.VariablesCsvFileName, nameValuesCollection.Select(kvp => new string?[] { kvp.Key, kvp.Value }));
                    Addon.CsvDb.SaveData();
                    break;
            }

            return Task.FromResult<ReadOnlyMemory<byte>>(ReadOnlyMemory<byte>.Empty);
        }

        #endregion

        #region private functions

        private AddonBase Addon { get; }

        private async Task WorkingTaskMainAsync(CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new();
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await Task.Delay(1000);
                if (cancellationToken.IsCancellationRequested) break;

                var nowUtc = DateTime.UtcNow;
                
                if (LoggersSet.WrapperUserFriendlyLogger.IsEnabled(LogLevel.Trace))
                    stopwatch.Restart();

                await DoWorkAsync(nowUtc, cancellationToken);

                if (LoggersSet.WrapperUserFriendlyLogger.IsEnabled(LogLevel.Trace))
                {
                    stopwatch.Stop();
                    LoggersSet.WrapperUserFriendlyLogger.LogTrace("DoWorkAsync, ElapsedMilliseconds: " + stopwatch.ElapsedMilliseconds);                    
                }
            }
        }

        /// <summary>
        ///     On loop in working thread.
        /// </summary>
        /// <param name="nowUtc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) 
                return;

            string? journalFilesPath = Addon.OptionsSubstituted.TryGetValue(ExperionEventsJournalFilesImporterAddon.JournalFiles_Path_OptionName);
            if (String.IsNullOrEmpty(journalFilesPath))
                return;

            //NetworkCredential networkCredential = new NetworkCredential { Domain = "domain", UserName = "user", Password = "pass" };
            //CredentialCache credentialCache = new();
            //credentialCache.Add(new Uri(journalFilesPath), "Basic", networkCredential); 

            DirectoryInfo journalFilesDirectoryInfo;
            try
            {
                journalFilesDirectoryInfo = new DirectoryInfo(journalFilesPath);
            }
            catch
            {
                return;
            }
            if (!journalFilesDirectoryInfo.Exists)
            {
                IsConnected = false;
                return;
            }
            IsConnected = true;

            string? maxProcessedTimeUtcString = Addon.CsvDb.GetValue(
                        AddonBase.VariablesCsvFileName, ExperionEventsJournalFilesImporterAddon.MaxProcessedTimeUtc_VariableName, 1);
            DateTime maxProcessedTimeUtc;
            if (String.IsNullOrEmpty(maxProcessedTimeUtcString))
                maxProcessedTimeUtc = DateTime.MinValue;
            else
                maxProcessedTimeUtc = DateTimeHelper.GetDateTimeUtc(maxProcessedTimeUtcString);

            string? rptFiles_EncodingString = Addon.OptionsSubstituted.TryGetValue(ExperionEventsJournalFilesImporterAddon.RptFiles_Encoding_OptionName);
            Encoding defaultRptFiles_Encoding;
            try
            {
                defaultRptFiles_Encoding = Encoding.GetEncoding(rptFiles_EncodingString!);
            }
            catch
            {
                defaultRptFiles_Encoding = Encoding.UTF8;
            }            

            string? scanPeriodSecondsString = Addon.OptionsSubstituted.TryGetValue(ExperionEventsJournalFilesImporterAddon.ScanPeriodSeconds_OptionName);
            if (String.IsNullOrEmpty(scanPeriodSecondsString))
                return;
            double scanPeriodSeconds = new Any(scanPeriodSecondsString).ValueAsDouble(false);
            if (scanPeriodSeconds > 0.0 && nowUtc - LastScanTimeUtc >= TimeSpan.FromSeconds(scanPeriodSeconds))
            {
                var eventMessagesCollection = new EventMessagesCollection();

                var journalFiles_Type = ((ExperionEventsJournalFilesImporterAddon)Addon).OptionsSubstituted.TryGetValue(
                        ExperionEventsJournalFilesImporterAddon.JournalFiles_Type_OptionName) ?? @"";

                try
                {                    
                    foreach (FileInfo fi in journalFilesDirectoryInfo.GetFiles())
                    {
                        if (fi.LastWriteTimeUtc < maxProcessedTimeUtc)
                            continue;

                        if (fi.Name.EndsWith(@".zip", StringComparison.InvariantCultureIgnoreCase))
                        {
                            using (var stream = fi.OpenRead())
                            using (var zipArchive = ZipArchiveHelper.GetZipArchiveForRead(stream))
                            {                                
                                foreach (var entry in zipArchive.Entries)
                                {
                                    if (entry.LastWriteTime.UtcDateTime < maxProcessedTimeUtc)
                                        continue;

                                    await ProcessFileAsync(entry,
                                                null,
                                                entry.Name,
                                                journalFiles_Type,
                                                defaultRptFiles_Encoding,
                                                maxProcessedTimeUtc,
                                                Addon.OptionsSubstituted,
                                                eventMessagesCollection.EventMessages,
                                                LoggersSet,
                                                cancellationToken);                                    
                                }
                            }
                        }
                        else
                        {
                            await ProcessFileAsync(null,
                                                fi,
                                                fi.Name,
                                                journalFiles_Type,
                                                defaultRptFiles_Encoding,
                                                maxProcessedTimeUtc,
                                                Addon.OptionsSubstituted,
                                                eventMessagesCollection.EventMessages,
                                                LoggersSet,
                                                cancellationToken);
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, "Directory processing error: " + journalFilesDirectoryInfo.FullName);
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (eventMessagesCollection.EventMessages.Count > 0)
                {
                    var сallbackDispatcher = CallbackDispatcher;
                    if (сallbackDispatcher is not null)
                    {
                        ElementIdsMap?.AddCommonFieldsToEventMessagesCollection(eventMessagesCollection);
                        try
                        {
                            сallbackDispatcher.BeginInvoke(ct =>
                            {
                                EventMessagesCallback(this, new EventMessagesCallbackEventArgs { EventMessagesCollection = eventMessagesCollection });
                            });
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                LastScanTimeUtc = nowUtc;
            }

            string? journalFilesDeleteScanPeriodSecondsString = Addon.OptionsSubstituted.TryGetValue(ExperionEventsJournalFilesImporterAddon.JournalFilesDeleteScanPeriodSeconds_OptionName);
            if (String.IsNullOrEmpty(journalFilesDeleteScanPeriodSecondsString))
                return;
            double journalFilesDeleteScanPeriodSeconds = new Any(journalFilesDeleteScanPeriodSecondsString).ValueAsDouble(false);
            if (journalFilesDeleteScanPeriodSeconds > 0.0 && nowUtc - LastJournalFilesDeleteScanTimeUtc >= TimeSpan.FromSeconds(journalFilesDeleteScanPeriodSeconds))
            {
                List<FileInfo> filesToDelete = new();

                var journalFiles_Type = ((ExperionEventsJournalFilesImporterAddon)Addon).OptionsSubstituted.TryGetValue(
                        ExperionEventsJournalFilesImporterAddon.JournalFiles_Type_OptionName) ?? @"";                

                try
                {
                    foreach (FileInfo fi in journalFilesDirectoryInfo.GetFiles())
                    {
                        if (fi.LastWriteTimeUtc < maxProcessedTimeUtc)
                        {
                            filesToDelete.Add(fi);
                            continue;
                        }

                        var eventMessagesCollection = new EventMessagesCollection();

                        if (fi.Name.EndsWith(@".zip", StringComparison.InvariantCultureIgnoreCase))
                        {
                            bool allOldFiles = true;

                            using (var stream = fi.OpenRead())
                            using (var zipArchive = ZipArchiveHelper.GetZipArchiveForRead(stream))
                            {
                                foreach (var entry in zipArchive.Entries)
                                {
                                    if (entry.LastWriteTime.UtcDateTime < maxProcessedTimeUtc)
                                        continue;

                                    allOldFiles = false;

                                    await ProcessFileAsync(entry,
                                                null,
                                                entry.Name,
                                                journalFiles_Type,
                                                defaultRptFiles_Encoding,
                                                maxProcessedTimeUtc,
                                                Addon.OptionsSubstituted,
                                                eventMessagesCollection.EventMessages,
                                                LoggersSet,
                                                cancellationToken);
                                }
                            }

                            if (allOldFiles)
                            {
                                filesToDelete.Add(fi);
                            }
                            else
                            {
                                var maxEventMessage = eventMessagesCollection.EventMessages.OrderByDescending(em => em.OccurrenceTimeUtc).FirstOrDefault();

                                if (maxEventMessage is not null && maxEventMessage.OccurrenceTimeUtc <= maxProcessedTimeUtc)
                                    filesToDelete.Add(fi);
                            }                           
                        }
                        else
                        {
                            await ProcessFileAsync(null,
                                                fi,
                                                fi.Name,
                                                journalFiles_Type,
                                                defaultRptFiles_Encoding,
                                                maxProcessedTimeUtc,
                                                Addon.OptionsSubstituted,
                                                eventMessagesCollection.EventMessages,
                                                LoggersSet,
                                                cancellationToken);

                            var maxEventMessage = eventMessagesCollection.EventMessages.OrderByDescending(em => em.OccurrenceTimeUtc).FirstOrDefault();

                            if (maxEventMessage is not null && maxEventMessage.OccurrenceTimeUtc <= maxProcessedTimeUtc)
                                filesToDelete.Add(fi);
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, "Directory processing error: " + journalFilesDirectoryInfo.FullName);
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                foreach (var fileToDelete in filesToDelete)
                {
                    try
                    {
                        fileToDelete.Delete();
                    }
                    catch
                    {
                    }
                }          

                LastJournalFilesDeleteScanTimeUtc = nowUtc;
            }
        }

        #endregion

        #region private functions

        private static async Task ProcessFileAsync(ZipArchiveEntry? entry,
            FileInfo? fi,
            string fileName,
            string journalFiles_Type,
            Encoding defaultRptFiles_Encoding,
            DateTime maxProcessedTimeUtc,
            CaseInsensitiveDictionary<string?> options,
            List<EventMessage> eventMessages,
            ILoggersSet loggersSet,
            CancellationToken cancellationToken)
        {
            switch (journalFiles_Type.ToUpperInvariant())
            {
                case @"RPT":
                    if (!fileName.StartsWith(@"rpt", StringComparison.InvariantCultureIgnoreCase) ||
                                    fileName.EndsWith(@".htm", StringComparison.InvariantCultureIgnoreCase))
                        return;
                    try
                    {
                        using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScope, fileName));

                        using (Stream? stream = GetStream(entry, fi))
                        {
                            if (stream is not null)
                            {
                                await ExperionEventsJournalRptFileHelper.ProcessFileAsync(stream,
                                    defaultRptFiles_Encoding,
                                    maxProcessedTimeUtc,
                                    options,
                                    eventMessages,
                                    loggersSet,
                                    cancellationToken);
                            }                            
                        }
                    }
                    catch (Exception ex)
                    {
                        loggersSet.Logger.LogError(ex, "File processing error: " + fileName);
                    }
                    break;
                case @"CSV":
                    if (!fileName.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase))
                        return;
                    try
                    {
                        using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScope, fileName));

                        using (Stream? stream = GetStream(entry, fi))
                        {
                            if (stream is not null)
                            {
                                await ExperionEventsJournalCsvFileHelper.ProcessFileAsync(stream,
                                    Encoding.UTF8,
                                    maxProcessedTimeUtc,
                                    options,
                                    eventMessages,
                                    loggersSet,
                                    cancellationToken);
                            }                            
                        }
                    }
                    catch (Exception ex)
                    {
                        loggersSet.Logger.LogError(ex, "File processing error: " + fileName);
                    }
                    break;
                case @"HTM":
                    if (!fileName.EndsWith(@".htm", StringComparison.InvariantCultureIgnoreCase))
                        return;
                    try
                    {
                        using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScope, fileName));

                        using (Stream? stream = GetStream(entry, fi))
                        {
                            if (stream is not null)
                            {
                                await ExperionEventsJournalHtmFileHelper.ProcessFileAsync(stream,
                                    Encoding.UTF8,
                                    maxProcessedTimeUtc,
                                    options,
                                    eventMessages,
                                    loggersSet,
                                    cancellationToken);
                            }                            
                        }
                    }
                    catch (Exception ex)
                    {
                        loggersSet.Logger.LogError(ex, "File processing error: " + fileName);
                    }
                    break;
            }
        }

        private static Stream? GetStream(ZipArchiveEntry? entry, FileInfo? fi)
        {
            if (entry is not null)
                return entry.Open();

            if (fi is not null)
                return fi.OpenRead();

            throw new InvalidOperationException();
        }

        #endregion

        #region private fields      

        private DateTime _lastFailedConnectionDateTimeUtc;

        private DateTime _lastSuccessfulConnectionDateTimeUtc;

        private Task<Task>? _workingTask;

        private CancellationTokenSource? _cancellationTokenSource;

        //private HashSet<(string, DateTime)>? _processedFiles = new();

        #endregion
    }
}


//List<(string, string?)> list = new() 
//{ 
//    (@"TextMessageVersion", @"1"),
//    (@"SourceAddonName", ExperionEventsJournalFilesImporterAddon.AddonName),
//    (@"AddonVersion", @"1")
//};
//foreach (var ci in columnInfos)
//{                                
//    list.Add((ci.Name, line.Substring(ci.StartIndex, ci.Length).TrimEnd()));                                
//}

//bool elementValueListCallbackIsEnabled;
//bool eventListCallbackIsEnabled;
//try
//{
//    elementValueListCallbackIsEnabled = ElementValueListCallbackIsEnabled;
//    eventListCallbackIsEnabled = EventListCallbackIsEnabled;
//}
//catch
//{
//    return;
//}
