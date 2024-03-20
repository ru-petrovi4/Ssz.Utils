using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Common.Passthrough;
using Ssz.Dcs.CentralServer.Common.EntityFramework;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Ssz.DataAccessGrpc.ServerBase.Properties;
using System.Collections.ObjectModel;
using Ssz.Utils.Addons;

namespace Ssz.Dcs.CentralServer
{    
    public partial class ServerWorker : ServerWorkerBase
    {
        #region private functions

        private async Task<byte[]> GetAddonStatusesPassthroughAsync(ServerContext serverContext)
        {
            var dcsCentralServerAddon = _addonsManager.Addons.OfType<DcsCentralServerAddon>().Single();

            AddonStatuses addonStatuses = _addonsManager.GetAddonStatuses();
            foreach (var addonStatus in addonStatuses.AddonStatusesCollection)
            {
                addonStatus.SourcePath = @"";
                addonStatus.SourceId = dcsCentralServerAddon.ServiceId;
                addonStatus.SourceIdToDisplay = dcsCentralServerAddon.Desc;
            }

            try
            {
                ObservableCollection<EngineSession> engineSessions = GetEngineSessions(serverContext);

                if (engineSessions.Count > 0)
                {
                    var taskInfos = new List<(Task<ReadOnlyMemory<byte>>, EngineSession)>(engineSessions.Count);

                    foreach (EngineSession engineSession in engineSessions)
                    {
                        if (engineSession.DataAccessProviderGetter_Addon.IsAddonsPassthroughSupported)
                        {
                            Logger.LogDebug("dataAccessProvider.Passthrough passthroughName=GetAddonStatuses");
                            taskInfos.Add((engineSession.DataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetAddonStatuses, new byte[0]), engineSession));
                        }                            
                    }

                    foreach (var taskInfo in taskInfos)
                    {
                        var task = taskInfo.Item1;
                        EngineSession engineSession = taskInfo.Item2;                        
                        try
                        {
                            var replyAddonStatuses = new AddonStatuses();
                            SerializationHelper.SetOwnedData(replyAddonStatuses, await task);

                            foreach (var addonStatus in replyAddonStatuses.AddonStatusesCollection)
                            {
                                if (String.IsNullOrEmpty(addonStatus.SourcePath))
                                    addonStatus.SourcePath = engineSession.DataAccessProviderGetter_Addon.InstanceId;
                                else
                                    addonStatus.SourcePath = engineSession.DataAccessProviderGetter_Addon.InstanceId + "/" + addonStatus.SourcePath;                                
                            }

                            addonStatuses.AddonStatusesCollection.AddRange(replyAddonStatuses.AddonStatusesCollection);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "dataAccessProvider.Passthrough passthroughName=GetAddonStatuses");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ThrowRpcException(ex);
            }

            return SerializationHelper.GetOwnedData(addonStatuses);
        }

        private async Task<byte[]> ReadConfigurationPassthroughAsync(ServerContext serverContext, string recipientPath, ReadOnlyMemory<byte> dataToSend)
        {
            ObservableCollection<EngineSession> engineSessions = GetEngineSessions(serverContext);
            ConfigurationFiles configurationFiles;
            var dcsCentralServerAddon = _addonsManager.Addons.OfType<DcsCentralServerAddon>().Single();

            if (dataToSend.Length == 0)
            {
                configurationFiles = _addonsManager.ReadConfiguration(null);
                foreach (var configurationFile in configurationFiles.ConfigurationFilesCollection)
                {
                    configurationFile.SourcePath = @"";
                    configurationFile.SourceId = dcsCentralServerAddon.ServiceId;
                    configurationFile.SourceIdToDisplay = dcsCentralServerAddon.Desc;
                }

                try
                {
                    if (engineSessions.Count > 0)
                    {
                        var taskInfos = new List<(Task<ReadOnlyMemory<byte>>, EngineSession)>(engineSessions.Count);

                        foreach (EngineSession engineSession in engineSessions)
                        {
                            if (engineSession.DataAccessProviderGetter_Addon.IsAddonsPassthroughSupported)
                            {
                                Logger.LogDebug("dataAccessProvider.Passthrough passthroughName=ReadConfiguration");
                                taskInfos.Add((engineSession.DataAccessProvider.PassthroughAsync(@"", PassthroughConstants.ReadConfiguration, new byte[0]),
                                    engineSession));
                            }
                        }

                        foreach (var taskInfo in taskInfos)
                        {
                            var task = taskInfo.Item1;
                            EngineSession engineSession = taskInfo.Item2;                            
                            try
                            {
                                var replyConfigurationFiles = new ConfigurationFiles();
                                SerializationHelper.SetOwnedData(replyConfigurationFiles, await task);

                                foreach (var configurationFile in replyConfigurationFiles.ConfigurationFilesCollection)
                                {
                                    if (String.IsNullOrEmpty(configurationFile.SourcePath))
                                        configurationFile.SourcePath = engineSession.DataAccessProviderGetter_Addon.InstanceId;
                                    else
                                        configurationFile.SourcePath = engineSession.DataAccessProviderGetter_Addon.InstanceId + "/" + configurationFile.SourcePath;                                    
                                }

                                configurationFiles.ConfigurationFilesCollection.AddRange(replyConfigurationFiles.ConfigurationFilesCollection);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "dataAccessProvider.Passthrough passthroughName=ReadConfiguration");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ThrowRpcException(ex);
                }
            }
            else
            {                
                string? pathRelativeToRootDirectory = Encoding.UTF8.GetString(dataToSend.Span);
                          
                if (String.IsNullOrEmpty(recipientPath))
                {
                    configurationFiles = _addonsManager.ReadConfiguration(pathRelativeToRootDirectory);
                    foreach (var configurationFile in configurationFiles.ConfigurationFilesCollection)
                    {
                        configurationFile.SourcePath = @"";
                        configurationFile.SourceId = dcsCentralServerAddon.ServiceId;
                        configurationFile.SourceIdToDisplay = dcsCentralServerAddon.Desc;
                    }
                }
                else
                {
                    configurationFiles = new ConfigurationFiles();

                    string beginSourcePath;
                    string remainingSourcePath;
                    int i = recipientPath.IndexOf('/');
                    if (i >= 0)
                    {
                        beginSourcePath = recipientPath.Substring(0, i);
                        remainingSourcePath = recipientPath.Substring(i + 1);
                    }
                    else
                    {
                        beginSourcePath = recipientPath;
                        remainingSourcePath = @"";
                    }
                    EngineSession? engineSession = engineSessions.FirstOrDefault(es =>
                        es.DataAccessProviderGetter_Addon.IsAddonsPassthroughSupported &&
                        String.Equals(
                            es.DataAccessProviderGetter_Addon.InstanceId,
                            beginSourcePath,
                            StringComparison.InvariantCultureIgnoreCase));
                    if (engineSession is not null)
                    {
                        var task = engineSession.DataAccessProvider.PassthroughAsync(remainingSourcePath, PassthroughConstants.ReadConfiguration,
                            Encoding.UTF8.GetBytes(pathRelativeToRootDirectory));

                        try
                        {
                            var replyConfigurationFiles = new ConfigurationFiles();
                            SerializationHelper.SetOwnedData(replyConfigurationFiles, await task);

                            foreach (var configurationFile in replyConfigurationFiles.ConfigurationFilesCollection)
                            {
                                if (String.IsNullOrEmpty(configurationFile.SourcePath))
                                    configurationFile.SourcePath = engineSession.DataAccessProviderGetter_Addon.InstanceId;
                                else
                                    configurationFile.SourcePath = engineSession.DataAccessProviderGetter_Addon.InstanceId + "/" + configurationFile.SourcePath;                                
                            }

                            configurationFiles.ConfigurationFilesCollection.AddRange(replyConfigurationFiles.ConfigurationFilesCollection);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "dataAccessProvider.Passthrough passthroughName=ReadConfiguration");
                        }
                    }
                }
            }
            return SerializationHelper.GetOwnedData(configurationFiles);
        }

        private async Task WriteConfigurationPassthroughAsync(ServerContext serverContext, string recipientPath, ReadOnlyMemory<byte> dataToSend)
        {
            try
            {
                var allConfigurationFiles = new ConfigurationFiles();
                SerializationHelper.SetOwnedData(allConfigurationFiles, dataToSend);

                ObservableCollection<EngineSession> engineSessions = GetEngineSessions(serverContext);

                var tasks = new List<Task<ReadOnlyMemory<byte>>>(engineSessions.Count);

                foreach (var group in allConfigurationFiles.ConfigurationFilesCollection.GroupBy(ccf => ccf.SourcePath))
                {
                    string sourcePath = group.Key;
                    var configurationFiles = new ConfigurationFiles();
                    configurationFiles.ConfigurationFilesCollection.AddRange(group);
                    if (String.IsNullOrEmpty(sourcePath))
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            _addonsManager.WriteConfigurationThreadSafe(configurationFiles);
                            return ReadOnlyMemory<byte>.Empty;
                        }));
                    }
                    else
                    {
                        string beginSourcePath;
                        string remainingSourcePath;
                        int i = sourcePath.IndexOf('/');
                        if (i >= 0)
                        {
                            beginSourcePath = sourcePath.Substring(0, i);
                            remainingSourcePath = sourcePath.Substring(i + 1);
                        }
                        else
                        {
                            beginSourcePath = sourcePath;
                            remainingSourcePath = @"";
                        }
                        foreach (var configurationFile in configurationFiles.ConfigurationFilesCollection)
                        {
                            configurationFile.SourcePath = remainingSourcePath;
                        }
                        EngineSession? engineSession = engineSessions.FirstOrDefault(es =>
                            es.DataAccessProviderGetter_Addon.IsAddonsPassthroughSupported &&
                            String.Equals(
                                es.DataAccessProviderGetter_Addon.InstanceId,
                                beginSourcePath,
                                StringComparison.InvariantCultureIgnoreCase));
                        if (engineSession is not null)
                        {
                            tasks.Add(engineSession.DataAccessProvider.PassthroughAsync(@"", PassthroughConstants.WriteConfiguration,
                                SerializationHelper.GetOwnedData(configurationFiles)));
                        }
                    }

                    foreach (var task in tasks)
                    {
                        try
                        {
                            await task;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "dataAccessProvider.Passthrough passthroughName=WriteConfiguration");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {                
                ThrowRpcException(ex);
            }
        }        

        #endregion
    }
}
