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

namespace Ssz.Dcs.Addons.ExperionEventsJournalFilesImporter
{
    internal class ExperionEventsJournalFiles_DataAccessProvider : FromAddon_DataAccessProviderBase
    {
        #region construction and destruction

        public ExperionEventsJournalFiles_DataAccessProvider(ExperionEventsJournalFilesImporterAddon addon, ILogger<ExperionEventsJournalFiles_DataAccessProvider> logger, IUserFriendlyLogger? userFriendlyLogger = null) :
            base(addon, new LoggersSet(logger, userFriendlyLogger))
        {            
        }

        #endregion

        #region public functions

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

            var previousWorkingTask = _workingTask;
            _workingTask = Task.Factory.StartNew(() =>
            {
                if (previousWorkingTask is not null)
                    previousWorkingTask.Wait();
                WorkingTaskMainAsync(cancellationToken).Wait();
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        ///     Tou can call Dispose() instead of this method.
        ///     Closes without waiting working thread exit.
        /// </summary>
        public override void Close()
        {
            if (!IsInitialized) return;

            base.Close();

            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        ///     Tou can call DisposeAsync() instead of this method.
        ///     Closes WITH waiting working thread exit.
        /// </summary>
        public override async Task CloseAsync()
        {
            Close();

            if (_workingTask is not null)
                await _workingTask;
        }        

        #endregion        

        #region private functions

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

            string? journalFilesPath = Addon.OptionsSubstitutedThreadSafe.TryGetValue(ExperionEventsJournalFilesImporterAddon.JournalFiles_Path_OptionName);
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

            string? rptFiles_EncodingString = Addon.OptionsSubstitutedThreadSafe.TryGetValue(ExperionEventsJournalFilesImporterAddon.RptFiles_Encoding_OptionName);
            Encoding defaultRptFiles_Encoding;
            try
            {
                defaultRptFiles_Encoding = Encoding.GetEncoding(rptFiles_EncodingString!);
            }
            catch
            {
                defaultRptFiles_Encoding = Encoding.UTF8;
            }            

            string? scanPeriodSecondsString = Addon.OptionsSubstitutedThreadSafe.TryGetValue(ExperionEventsJournalFilesImporterAddon.ScanPeriodSeconds_OptionName);
            if (String.IsNullOrEmpty(scanPeriodSecondsString))
                return;
            double scanPeriodSeconds = new Any(scanPeriodSecondsString).ValueAsDouble(false);
            if (scanPeriodSeconds > 0.0 && nowUtc - LastScanTimeUtc >= TimeSpan.FromSeconds(scanPeriodSeconds))
            {
                var eventMessagesCollection = new EventMessagesCollection();

                var journalFiles_Type = ((ExperionEventsJournalFilesImporterAddon)Addon).OptionsSubstitutedThreadSafe.TryGetValue(
                        ExperionEventsJournalFilesImporterAddon.JournalFiles_Type_OptionName) ?? @"";
                switch (journalFiles_Type.ToUpperInvariant())
                {
                    case @"RPT":
                        try
                        {
                            foreach (FileInfo fi in journalFilesDirectoryInfo.GetFiles())
                            {
                                if (!fi.Name.StartsWith(@"rpt", StringComparison.InvariantCultureIgnoreCase) ||
                                        fi.Name.EndsWith(@".htm", StringComparison.InvariantCultureIgnoreCase))
                                    continue;
                                try
                                {
                                    using var fileNameScope = LoggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScope, fi.Name));

                                    using (Stream stream = fi.OpenRead())
                                    {
                                        await ExperionEventsJournalRptFileHelper.ProcessFileAsync(stream,
                                            defaultRptFiles_Encoding,
                                            maxProcessedTimeUtc,
                                            Addon.OptionsSubstitutedThreadSafe,
                                            eventMessagesCollection.EventMessages,
                                            LoggersSet,
                                            cancellationToken);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LoggersSet.Logger.LogError(ex, "File processing error: " + fi.FullName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggersSet.Logger.LogError(ex, "Directory processing error: " + journalFilesDirectoryInfo.FullName);
                        }
                        break;
                    case @"CSV":
                        try
                        {
                            foreach (FileInfo fi in journalFilesDirectoryInfo.GetFiles())
                            {
                                if (!fi.Name.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase))
                                    continue;
                                try
                                {
                                    using var fileNameScope = LoggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScope, fi.Name));

                                    using (Stream stream = fi.OpenRead())
                                    {
                                        await ExperionEventsJournalCsvFileHelper.ProcessFileAsync(stream,
                                            Encoding.UTF8,
                                            maxProcessedTimeUtc,
                                            Addon.OptionsSubstitutedThreadSafe,
                                            eventMessagesCollection.EventMessages,
                                            LoggersSet,
                                            cancellationToken);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LoggersSet.Logger.LogError(ex, "File processing error: " + fi.FullName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggersSet.Logger.LogError(ex, "Directory processing error: " + journalFilesDirectoryInfo.FullName);
                        }
                        break;
                    case @"HTM":
                        try
                        {   
                            foreach (FileInfo fi in journalFilesDirectoryInfo.GetFiles())
                            {
                                if (!fi.Name.EndsWith(@".htm", StringComparison.InvariantCultureIgnoreCase))
                                    continue;
                                try
                                {
                                    using var fileNameScope = LoggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScope, fi.Name));

                                    using (Stream stream = fi.OpenRead())
                                    {
                                        await ExperionEventsJournalHtmFileHelper.ProcessFileAsync(stream,
                                            Encoding.UTF8,
                                            maxProcessedTimeUtc,
                                            Addon.OptionsSubstitutedThreadSafe,
                                            eventMessagesCollection.EventMessages,
                                            LoggersSet,
                                            cancellationToken);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LoggersSet.Logger.LogError(ex, "File processing error: " + fi.FullName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggersSet.Logger.LogError(ex, "Directory processing error: " + journalFilesDirectoryInfo.FullName);
                        }
                        break;
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

            string? journalFilesDeleteScanPeriodSecondsString = Addon.OptionsSubstitutedThreadSafe.TryGetValue(ExperionEventsJournalFilesImporterAddon.JournalFilesDeleteScanPeriodSeconds_OptionName);
            if (String.IsNullOrEmpty(journalFilesDeleteScanPeriodSecondsString))
                return;
            double journalFilesDeleteScanPeriodSeconds = new Any(journalFilesDeleteScanPeriodSecondsString).ValueAsDouble(false);
            if (journalFilesDeleteScanPeriodSeconds > 0.0 && nowUtc - LastJournalFilesDeleteScanTimeUtc >= TimeSpan.FromSeconds(journalFilesDeleteScanPeriodSeconds))
            {
                var journalFiles_Type = ((ExperionEventsJournalFilesImporterAddon)Addon).OptionsSubstitutedThreadSafe.TryGetValue(
                        ExperionEventsJournalFilesImporterAddon.JournalFiles_Type_OptionName) ?? @"";
                switch (journalFiles_Type.ToUpperInvariant())
                {
                    case @"RPT":
                        try
                        {
                            foreach (FileInfo fi in journalFilesDirectoryInfo.GetFiles())
                            {
                                if (!fi.Name.StartsWith(@"rpt", StringComparison.InvariantCultureIgnoreCase) ||
                                        fi.Name.EndsWith(@".htm", StringComparison.InvariantCultureIgnoreCase))
                                    continue;
                                try
                                {
                                    using var fileNameScope = LoggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScope, fi.Name));

                                    var eventMessagesCollection = new EventMessagesCollection();

                                    using (Stream stream = fi.OpenRead())
                                    {
                                        await ExperionEventsJournalRptFileHelper.ProcessFileAsync(stream,
                                            defaultRptFiles_Encoding,
                                            DateTime.MinValue,
                                            Addon.OptionsSubstitutedThreadSafe,
                                            eventMessagesCollection.EventMessages,
                                            LoggersSet,
                                            cancellationToken);
                                    }

                                    var maxEventMessage = eventMessagesCollection.EventMessages.OrderByDescending(em => em.OccurrenceTimeUtc).FirstOrDefault();

                                    if (maxEventMessage is not null && maxEventMessage.OccurrenceTimeUtc <= maxProcessedTimeUtc)
                                        fi.Delete();
                                }
                                catch (Exception ex)
                                {
                                    LoggersSet.Logger.LogError(ex, "File processing error: " + fi.FullName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggersSet.Logger.LogError(ex, "Directory processing error: " + journalFilesDirectoryInfo.FullName);
                        }
                        break;
                    case @"CSV":
                        try
                        {
                            foreach (FileInfo fi in journalFilesDirectoryInfo.GetFiles())
                            {
                                if (!fi.Name.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase))
                                    continue;
                                try
                                {
                                    using var fileNameScope = LoggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScope, fi.Name));

                                    var eventMessagesCollection = new EventMessagesCollection();

                                    using (Stream stream = fi.OpenRead())
                                    {
                                        await ExperionEventsJournalCsvFileHelper.ProcessFileAsync(stream,
                                            Encoding.UTF8,
                                            DateTime.MinValue,
                                            Addon.OptionsSubstitutedThreadSafe,
                                            eventMessagesCollection.EventMessages,
                                            LoggersSet,
                                            cancellationToken);
                                    }

                                    var maxEventMessage = eventMessagesCollection.EventMessages.OrderByDescending(em => em.OccurrenceTimeUtc).FirstOrDefault();

                                    if (maxEventMessage is not null && maxEventMessage.OccurrenceTimeUtc <= maxProcessedTimeUtc)
                                        fi.Delete();
                                }
                                catch (Exception ex)
                                {
                                    LoggersSet.Logger.LogError(ex, "File processing error: " + fi.FullName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggersSet.Logger.LogError(ex, "Directory processing error: " + journalFilesDirectoryInfo.FullName);
                        }
                        break;
                    case @"HTM":
                        try
                        {
                            Encoding defaultEncoding = Encoding.UTF8;
                            LoggersSet.Logger.LogDebug("Default encoding for files: " + defaultEncoding.EncodingName);

                            foreach (FileInfo fi in journalFilesDirectoryInfo.GetFiles())
                            {
                                if (!fi.Name.EndsWith(@".htm", StringComparison.InvariantCultureIgnoreCase))
                                    continue;
                                try
                                {
                                    using var fileNameScope = LoggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScope, fi.Name));

                                    var eventMessagesCollection = new EventMessagesCollection();

                                    using (Stream stream = fi.OpenRead())
                                    {
                                        await ExperionEventsJournalHtmFileHelper.ProcessFileAsync(stream,
                                            Encoding.UTF8,
                                            DateTime.MinValue,
                                            Addon.OptionsSubstitutedThreadSafe,
                                            eventMessagesCollection.EventMessages,
                                            LoggersSet,
                                            cancellationToken);
                                    }

                                    var maxEventMessage = eventMessagesCollection.EventMessages.OrderByDescending(em => em.OccurrenceTimeUtc).FirstOrDefault();

                                    if (maxEventMessage is not null && maxEventMessage.OccurrenceTimeUtc <= maxProcessedTimeUtc)
                                        fi.Delete();
                                }
                                catch (Exception ex)
                                {
                                    LoggersSet.Logger.LogError(ex, "File processing error: " + fi.FullName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggersSet.Logger.LogError(ex, "Directory processing error: " + journalFilesDirectoryInfo.FullName);
                        }
                        break;
                }                

                LastJournalFilesDeleteScanTimeUtc = nowUtc;
            }
        }    

        #endregion

        #region private fields        

        private Task? _workingTask;

        private CancellationTokenSource? _cancellationTokenSource;        

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
