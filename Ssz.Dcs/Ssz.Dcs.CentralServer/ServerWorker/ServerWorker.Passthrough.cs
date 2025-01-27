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
        #region public functions
        
        public override async Task<ReadOnlyMemory<byte>> PassthroughAsync(ServerContext serverContext, string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            try
            {                
                byte[] returnData;
                string systemNameToConnect = serverContext.SystemNameToConnect;
                if (systemNameToConnect == @"") // Utility context 
                {                
                    switch (passthroughName)
                    {
                        case PassthroughConstants.GetDirectoryInfo:
                            GetDirectoryInfoPassthrough(dataToSend, out returnData);
                            return returnData;
                        case PassthroughConstants.LoadFiles:
                            LoadFilesPassthrough(Encoding.UTF8.GetString(dataToSend.Span), out returnData);
                            return returnData;
                        case PassthroughConstants.GetUsers:                            
                            return await GetUsersPassthroughAsync(dataToSend);
                        case PassthroughConstants.ProcessModelingSession_RunInstructorExe:
                            ProcessModelingSession_RunInstructorExe_Passthrough(serverContext, dataToSend, out returnData);
                            return returnData;
                        default:
                            throw new RpcException(new Status(StatusCode.InvalidArgument, "Unknown passthroughName."));
                    }                
                }
                else
                {
                    switch (passthroughName)
                    {
                        case PassthroughConstants.GetAddonStatuses:
                            return await GetAddonStatusesPassthroughAsync(serverContext);
                        case PassthroughConstants.ReadConfiguration:                        
                            return await ReadConfigurationPassthroughAsync(serverContext, recipientPath, dataToSend);
                        case PassthroughConstants.WriteConfiguration:
                            await WriteConfigurationPassthroughAsync(serverContext, recipientPath, dataToSend);
                            return ReadOnlyMemory<byte>.Empty;
                        case PassthroughConstants.GetOperatorUserName:
                            return await GetOperatorUserNameAsync(serverContext);
                        case PassthroughConstants.GetOperatorRoleName:
                            return await GetOperatorRoleNameAsync(serverContext);
                        case PassthroughConstants.AddScenarioResult:
                            await AddScenarioResultPassthrough(serverContext, dataToSend);
                            return ReadOnlyMemory<byte>.Empty;
                    }

                    ObservableCollection<CentralServer.EngineSession> engineSessions = GetEngineSessions(serverContext);
                    //var tasks = new List<Task<IEnumerable<byte>?>>(dataAccessProviders.Count);
                    if (!String.IsNullOrEmpty(recipientPath))
                    {
                        string beginRecipientId;
                        string remainingRecipientId;
                        int i = recipientPath.IndexOf('/');
                        if (i >= 0)
                        {
                            beginRecipientId = recipientPath.Substring(0, i);
                            remainingRecipientId = recipientPath.Substring(i + 1);
                        }
                        else
                        {
                            beginRecipientId = recipientPath;
                            remainingRecipientId = @"";
                        }
                        CentralServer.EngineSession? engineSession = engineSessions.FirstOrDefault(es =>
                            string.Equals(
                                es.DataAccessProviderGetter_Addon.InstanceId,
                                beginRecipientId,
                                StringComparison.InvariantCultureIgnoreCase));
                        if (engineSession is not null)
                            return await engineSession.DataAccessProvider.PassthroughAsync(remainingRecipientId, passthroughName, dataToSend);
                    }

                    foreach (CentralServer.EngineSession engineSession in engineSessions)
                    {
                        Logger.LogDebug("dataAccessProvider.Passthrough passthroughName=" + passthroughName);
                        try
                        {
                            var t = engineSession.DataAccessProvider.PassthroughAsync(recipientPath, passthroughName, dataToSend);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "dataAccessProvider.Passthrough passthroughName=" + passthroughName);
                        }
                    }                    
                    return ReadOnlyMemory<byte>.Empty;
                }
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Exception during passthrough."), ex.Message);
            }
        }        

        public override string LongrunningPassthrough(ServerContext serverContext, string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            string systemNameToConnect = serverContext.SystemNameToConnect;            
            if (systemNameToConnect == @"") // Utility context 
            {
                try
                {
                    switch (passthroughName)
                    {
                        case LongrunningPassthroughConstants.ProcessModelingSession_PrepareAndRunInstructorAndEngines:
                            return ProcessModelingSession_PrepareAndRunInstructorAndEngines_LongrunningPassthrough(serverContext, dataToSend);                                                 
                        case LongrunningPassthroughConstants.ProcessModelingSession_PrepareAndRunOperatorExe:
                            return ProcessModelingSession_PrepareAndRunOperatorExe_LongrunningPassthrough(serverContext, dataToSend);
                        case LongrunningPassthroughConstants.ProcessModelingSession_RunOperatorExe:
                            return ProcessModelingSession_RunOperatorExe_LongrunningPassthrough(serverContext, dataToSend);                        
                        case LongrunningPassthroughConstants.ProcessModelingSession_SubscribeForLaunchOperatorProgress:
                            return ProcessModelingSession_SubscribeForLaunchOperatorProgress_LongrunningPassthrough(serverContext, dataToSend);
                        case LongrunningPassthroughConstants.ProcessModelingSession_DownloadChangedFiles:
                            return ProcessModelingSession_DownloadChangedFiles_LongrunningPassthrough(serverContext, dataToSend);                            
                        case LongrunningPassthroughConstants.ProcessModelingSession_UploadChangedFiles:
                            return ProcessModelingSession_UploadChangedFiles_LongrunningPassthrough(serverContext, dataToSend);                            
                        case LongrunningPassthroughConstants.SaveFiles:
                            return SaveFiles_LongrunningPassthrough(serverContext, dataToSend);                            
                        case LongrunningPassthroughConstants.DeleteFiles:
                            return DeleteFiles_LongrunningPassthrough(serverContext, dataToSend);                            
                        case LongrunningPassthroughConstants.MoveFiles:
                            return MoveFiles_LongrunningPassthrough(serverContext, dataToSend);                            
                        default:
                            throw new RpcException(new Status(StatusCode.InvalidArgument, "Unknown passthroughName."));
                    }
                }
                catch (Exception ex)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Exception during passthrough."), ex.Message);
                }
            }
            else
            {
                string jobId = Guid.NewGuid().ToString();
                LongrunningPassthrough_ProcessContext(serverContext, jobId, recipientPath, passthroughName, dataToSend);
                return jobId;
            }
        }

        public override void LongrunningPassthroughCancel(ServerContext serverContext, string jobId)
        {
        }

        #endregion

        #region private functions    

        private async void LongrunningPassthrough_ProcessContext(ServerContext serverContext, string jobId, string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            ObservableCollection<CentralServer.EngineSession> engineSessions = GetEngineSessions(serverContext);

            var statusCodeTasks = new List<Task<uint>>();

            if (!String.IsNullOrEmpty(recipientPath))
            {
                string beginRecipientPath;
                string remainingRecipientPath;
                int i = recipientPath.IndexOf('/');
                if (i >= 0)
                {
                    beginRecipientPath = recipientPath.Substring(0, i);
                    remainingRecipientPath = recipientPath.Substring(i + 1);
                }
                else
                {
                    beginRecipientPath = recipientPath;
                    remainingRecipientPath = @"";
                }
                CentralServer.EngineSession? engineSession = engineSessions.FirstOrDefault(es =>
                    string.Equals(
                        es.DataAccessProviderGetter_Addon.InstanceId,
                        beginRecipientPath,
                        StringComparison.InvariantCultureIgnoreCase));
                if (engineSession is not null)
                {
                    Logger.LogDebug("dataAccessProvider.LongrunningPassthrough passthroughName=" + passthroughName);
                    statusCodeTasks.Add(await engineSession.DataAccessProvider.LongrunningPassthroughAsync(remainingRecipientPath, passthroughName, dataToSend, null));
                }
            }
            if (statusCodeTasks.Count == 0)
                foreach (CentralServer.EngineSession engineSession in engineSessions)
                {
                    Logger.LogDebug("dataAccessProvider.LongrunningPassthrough passthroughName=" + passthroughName);
                    statusCodeTasks.Add(await engineSession.DataAccessProvider.LongrunningPassthroughAsync(recipientPath, passthroughName, dataToSend, null));
                }

            bool allSucceeded = true;
            foreach (var statusCodeTask in statusCodeTasks)
            {
                try
                {
                    if (!StatusCodes.IsGood(await statusCodeTask))
                        allSucceeded = false;
                }
                catch
                {
                    allSucceeded = false;
                }
            }
            if (!allSucceeded)
            {
                serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    ProgressLabel = Resources.ResourceManager.GetString(Properties.ResourceStrings.OperationError_ProgressLabel, serverContext.CultureInfo),
                    StatusCode = StatusCodes.BadInvalidArgument
                });
            }
            else
            {
                serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    ProgressLabel = Resources.ResourceManager.GetString(Properties.ResourceStrings.OperationCompleted_ProgressLabel, serverContext.CultureInfo),
                    StatusCode = StatusCodes.Good
                });
            }
        }

        /// <summary>
        ///     Always throws.
        /// </summary>
        /// <param name="ex"></param>        
        private void ThrowRpcException(Exception? ex)
        {            
            if (ex is not null)
            {
                Logger.LogWarning(ex, "Failed Passthrough Result");
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));                
            }
            else
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, @""));
            }            
        }
        
        private void GetDirectoryInfoPassthrough(ReadOnlyMemory<byte> dataToSend, out byte[] returnData)
        {
            try
            {
                var request = new GetDirectoryInfoRequest();
                SerializationHelper.SetOwnedData(request, dataToSend);
                DsFilesStoreDirectory dsFilesStoreDirectory = DsFilesStoreHelper.CreateDsFilesStoreDirectoryObject(FilesStoreDirectoryInfo,
                    request.PathRelativeToRootDirectory, request.FilesAndDirectoriesIncludeLevel);                
                returnData = SerializationHelper.GetOwnedData(dsFilesStoreDirectory);
            }
            catch (Exception ex)
            {
                returnData = new byte[0];
                ThrowRpcException(ex);
            }
        }

        /// <summary>
        ///     pathRelativeToRootCollection paths relative to the root of the Files Store.
        /// </summary>
        /// <param name="invariantPathRelativeToRootDirectoryCollection"></param>
        /// <param name="returnData"></param>
        /// <returns></returns>
        private void LoadFilesPassthrough(string invariantPathRelativeToRootDirectoryCollection, out byte[] returnData)
        {
            var reply = new LoadFilesReply();
            foreach (var invariantPathRelativeToRootDirectoryNullable in CsvHelper.ParseCsvLine(",", invariantPathRelativeToRootDirectoryCollection))
            {
                var invariantPathRelativeToRootDirectory = invariantPathRelativeToRootDirectoryNullable ?? @"";
                var fileInfo = new FileInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, invariantPathRelativeToRootDirectory.Replace('/', Path.DirectorySeparatorChar)));

                if (!FileSystemHelper.IsSubPathOf(fileInfo.Directory!.FullName, FilesStoreDirectoryInfo.FullName))
                    throw new Exception("Access to file destination denied.");

                try
                {
                    reply.DsFilesStoreFileDatasCollection.Add(
                        new DsFilesStoreFileData
                        {
                            InvariantPathRelativeToRootDirectory = invariantPathRelativeToRootDirectory,
                            LastModified = fileInfo.LastWriteTimeUtc,
                            FileData = File.ReadAllBytes(fileInfo.FullName)
                        });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, Properties.Resources.FileReadingError + ": " + fileInfo.Name);                    
                }
            }
            returnData = SerializationHelper.GetOwnedData(reply);
        }

        private string SaveFiles_LongrunningPassthrough(ServerContext serverContext, ReadOnlyMemory<byte> dataToSend)
        {
            string jobId = Guid.NewGuid().ToString();

            SaveFiles_LongrunningPassthroughAsync(serverContext, jobId, dataToSend);

            return jobId;
        }

        private async void SaveFiles_LongrunningPassthroughAsync(ServerContext serverContext, string jobId, ReadOnlyMemory<byte> dataToSend)
        {            
            try
            {
                var request = new SaveFilesRequest();
                SerializationHelper.SetOwnedData(request, dataToSend);
                foreach (DsFilesStoreFileData dsFilesStoreFileData in request.DatasCollection)
                {
                    string fileFullName = Path.Combine(FilesStoreDirectoryInfo.FullName, dsFilesStoreFileData.PathRelativeToRootDirectory);
                    // Creates all directories and subdirectories in the specified path unless they already exist.
                    DirectoryInfo destinationDirectoryInfo = Directory.CreateDirectory(Path.GetDirectoryName(fileFullName)!);

                    if (!FileSystemHelper.IsSubPathOf(destinationDirectoryInfo.FullName, FilesStoreDirectoryInfo.FullName))
                        throw new Exception("Access to file destination denied.");

                    // If the file to be deleted does not exist, no exception is thrown.
                    File.Delete(fileFullName); // For 'a' to 'A' changes in files names to work.
                    //     Asynchronously creates a new file, writes the specified byte array to the file,
                    //     and then closes the file. If the target file already exists, it is overwritten.
                    await File.WriteAllBytesAsync(fileFullName, dsFilesStoreFileData.FileData);
                    File.SetLastWriteTimeUtc(fileFullName, dsFilesStoreFileData.LastModified.UtcDateTime);
                }

                serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    ProgressLabel = Resources.ResourceManager.GetString(Ssz.Dcs.CentralServer.Properties.ResourceStrings.OperationCompleted_ProgressLabel, serverContext.CultureInfo),
                    StatusCode = StatusCodes.Good
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteFilesLongrunningPassthrough error.");
                serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    ProgressLabel = Resources.ResourceManager.GetString(Properties.ResourceStrings.OperationError_ProgressLabel, serverContext.CultureInfo),
                    StatusCode = StatusCodes.BadResourceUnavailable
                });
            }
        }

        private string DeleteFiles_LongrunningPassthrough(ServerContext serverContext, ReadOnlyMemory<byte> dataToSend)
        {
            string jobId = Guid.NewGuid().ToString();

            try
            {
                var request = CsvHelper.ParseCsvLine(@",", Encoding.UTF8.GetString(dataToSend.Span));
                foreach (int index in Enumerable.Range(0, request.Length))
                {
                    string fileFullName = Path.Combine(FilesStoreDirectoryInfo.FullName, (request[index] ?? @"").Replace('/', Path.DirectorySeparatorChar));
                    try
                    {
                        // If the file to be deleted does not exist, no exception is thrown.
                        File.Delete(fileFullName);
                    }
                    catch
                    {
                    }                    
                }

                serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    ProgressLabel = Resources.ResourceManager.GetString(Properties.ResourceStrings.OperationCompleted_ProgressLabel, serverContext.CultureInfo),
                    StatusCode = StatusCodes.Good
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteFilesLongrunningPassthrough error.");
                serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    ProgressLabel = Resources.ResourceManager.GetString(Properties.ResourceStrings.OperationError_ProgressLabel, serverContext.CultureInfo),
                    StatusCode = StatusCodes.BadResourceUnavailable
                });
            }

            return jobId;
        }

        private string MoveFiles_LongrunningPassthrough(ServerContext serverContext, ReadOnlyMemory<byte> dataToSend)
        {
            string jobId = Guid.NewGuid().ToString();

            try
            {
                var request = CsvHelper.ParseCsvLine(@",", Encoding.UTF8.GetString(dataToSend.Span));                
                foreach (int index in Enumerable.Range(0, request.Length / 2))
                {
                    string sourceFileFullName = Path.Combine(FilesStoreDirectoryInfo.FullName, (request[2 * index] ?? @"").Replace('/', Path.DirectorySeparatorChar));
                    string destFileFullName = Path.Combine(FilesStoreDirectoryInfo.FullName, (request[2 * index + 1] ?? @"").Replace('/', Path.DirectorySeparatorChar));
                    File.Move(sourceFileFullName, destFileFullName, true);
                }

                serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    ProgressLabel = Resources.ResourceManager.GetString(Properties.ResourceStrings.OperationCompleted_ProgressLabel, serverContext.CultureInfo),
                    StatusCode = StatusCodes.Good
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteFilesLongrunningPassthrough error.");
                serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    ProgressLabel = Resources.ResourceManager.GetString(Properties.ResourceStrings.OperationError_ProgressLabel, serverContext.CultureInfo),
                    StatusCode = StatusCodes.BadResourceUnavailable
                });
            }

            return jobId;
        }

        //private PassthroughResult PassthroughOnDbAddOperator(byte[] data)
        //{
        //    var arg = new DbEditOperatorArg();
        //    using (var reader = new SerializationReader(data))
        //    {
        //        arg.DeserializeOwnedData(reader, null);
        //    }

        //    bool added = false;

        //    if (UseCtcmLmsDb)
        //    {
        //        if (UseOpCompDb) OpCompDbHelper.SyncWithCtcmLmsDb();

        //        try
        //        {
        //            using (var dbContext = new CtcmLmsDbContext())
        //            {
        //                var operatorSameUserName =
        //                        dbContext.Operators.FirstOrDefault(s => s.UserName == arg.Operator.UserName);
        //                if (operatorSameUserName is null)
        //                {
        //                    dbContext.Operators.Add(arg.Operator);

        //                    dbContext.SaveChanges();

        //                    added = true;
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return NewFailedPassthroughResult(ex);
        //        }

        //        if (UseOpCompDb) OpCompDbHelper.SyncWithCtcmLmsDb();
        //    }            

        //    return NewSucceededPassthroughResult(new DbEditOperatorResult
        //    {
        //        Suceeded = added
        //    });
        //}

        //private PassthroughResult PassthroughOnDbUpdateOperator(byte[] data)
        //{
        //    var arg = new DbEditOperatorArg();
        //    using (var reader = new SerializationReader(data))
        //    {
        //        arg.DeserializeOwnedData(reader, null);
        //    }

        //    bool updated = false;

        //    if (UseCtcmLmsDb)
        //    {
        //        if (UseOpCompDb) OpCompDbHelper.SyncWithCtcmLmsDb();

        //        try
        //        {
        //            using (var dbContext = new CtcmLmsDbContext())
        //            {
        //                var operatorSameUserName =
        //                        dbContext.Operators.FirstOrDefault(s => s.UserName == arg.Operator.UserName);                        
        //                if (operatorSameUserName is null)
        //                {
        //                    Operator operatorSameId = dbContext.Operators.Find(arg.Operator.Id);
        //                    if (operatorSameId is not null)
        //                    {
        //                        operatorSameId.WindowsUserName = arg.Operator.WindowsUserName;
        //                        operatorSameId.UserName = arg.Operator.UserName;

        //                        dbContext.SaveChanges();

        //                        updated = true;
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return NewFailedPassthroughResult(ex);
        //        }

        //        if (UseOpCompDb) OpCompDbHelper.SyncWithCtcmLmsDb();
        //    }            

        //    return NewSucceededPassthroughResult(new DbEditOperatorResult
        //    {
        //        Suceeded = updated
        //    });
        //}

        //private PassthroughResult PassthroughOnDbDeleteOperator(byte[] data)
        //{
        //    var arg = new DbEditOperatorArg();
        //    using (var reader = new SerializationReader(data))
        //    {
        //        arg.DeserializeOwnedData(reader, null);
        //    }

        //    bool deleted = false;

        //    if (UseCtcmLmsDb)
        //    {
        //        if (UseOpCompDb) OpCompDbHelper.SyncWithCtcmLmsDb();

        //        try
        //        {
        //            using (var dbContext = new CtcmLmsDbContext())
        //            {
        //                var operatorSameUserName =
        //                        dbContext.Operators.FirstOrDefault(s => s.UserName == arg.Operator.UserName);
        //                if (operatorSameUserName is not null)
        //                {
        //                    dbContext.OpCompOperators.ToArray(); // Load for EF can update FK OperatorId to null

        //                    dbContext.Operators.Remove(operatorSameUserName);

        //                    dbContext.SaveChanges();

        //                    deleted = true;
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return NewFailedPassthroughResult(ex);
        //        }

        //        if (UseOpCompDb) OpCompDbHelper.SyncWithCtcmLmsDb();
        //    }

        //    return NewSucceededPassthroughResult(new DbEditOperatorResult
        //    {
        //        Suceeded = deleted
        //    });
        //}

        //private PassthroughResult PassthroughOnDbGetOperators(byte[] data)
        //{
        //    var arg = new DbGetOperatorsArg();
        //    using (var reader = new SerializationReader(data))
        //    {
        //        arg.DeserializeOwnedData(reader, null);
        //    }

        //    if (UseCtcmLmsDb)
        //    {
        //        if (UseOpCompDb) OpCompDbHelper.SyncWithCtcmLmsDb();

        //        try
        //        {
        //            using (var dbContext = new CtcmLmsDbContext())
        //            {
        //                return NewSucceededPassthroughResult(new DbGetOperatorsResult
        //                {
        //                    Operators = dbContext.Operators.ToList()
        //                });
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return NewFailedPassthroughResult(ex);
        //        }
        //    }

        //    return NewSucceededPassthroughResult(new DbGetOperatorsResult
        //    {
        //        Operators = new List<Operator>()
        //    });
        //}

        //private PassthroughResult PassthroughOnRuntimeAddOperator(Context context, byte[] data)
        //{
        //    var arg = new RuntimeAddOperatorArg();
        //    using (var reader = new SerializationReader(data))
        //    {
        //        arg.DeserializeOwnedData(reader, null);
        //    }

        //    bool loginResult;
        //    try
        //    {
        //        if (UseCtcmLmsDb)
        //        {
        //            using (var dbContext = new CtcmLmsDbContext())
        //            {
        //                Operator operator =
        //                        dbContext.Set<Operator>().FirstOrDefault(s => s.UserName == arg.UserName); // StringComparison.InvariantCultureIgnoreCase Not working with SQLite
        //                if (operator is null)
        //                {
        //                    loginResult = false;
        //                }
        //                else
        //                {
        //                    loginResult = true;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            loginResult = true;
        //        }
        //        if (loginResult)
        //        {
        //            AddAwaitingOperator(context.Id, arg.SessionId, arg.WindowsUserName, context.WorkstationNameWithoutArgs, arg.UserName, arg.ModelName, arg.WpfHmiProjectDesc);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return NewFailedPassthroughResult(ex);
        //    }

        //    return NewSucceededPassthroughResult(new RuntimeAddOperatorResult
        //    {
        //        Succeeded = loginResult
        //    });
        //}

        //private PassthroughResult PassthroughOnRuntimeChangeStateOperators(byte[] data)
        //{
        //    var arg = new RuntimeChangeStateOperatorsArg();
        //    using (var reader = new SerializationReader(data))
        //    {
        //        arg.DeserializeOwnedData(reader, null);
        //    }

        //    bool result = false;
        //    try
        //    {
        //        foreach (var operatorSessionId in arg.OperatorSessionIdsListToConnect)
        //        {
        //            ConnectOperator(operatorSessionId, arg.XiSystemAndHostNameToConnect);
        //        }

        //        result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return NewFailedPassthroughResult(ex);
        //    }

        //    return NewSucceededPassthroughResult(new RuntimeChangeStateOperatorsResult
        //    {
        //        Succeeded = result
        //    });
        //}

        #endregion
    }
}

//}
//else
//{
//    var parts = CsvHelper.ParseCsvLine(",", args);
//    if (parts.Length < 4)
//    {
//        return NewFailedPassthroughResult(null, out returnData);
//    }

//    string processModelName = parts[0] ?? "";
//    DsFilesStoreDirectoryType binDsFilesStoreDirectoryType = new Any(parts[1]).ValueAs<DsFilesStoreDirectoryType>(false);
//    DsFilesStoreDirectoryType dataDsFilesStoreDirectoryType = new Any(parts[2]).ValueAs<DsFilesStoreDirectoryType>(false);
//    string pathRelativeToDataDirectory = parts[3] ?? "";

//    DsFilesStoreDirectory rootDsFilesStoreDirectory;
//    try
//    {
//        rootDsFilesStoreDirectory = GetRootDsFilesStoreDirectoryCached(processModelName,
//            binDsFilesStoreDirectoryType, dataDsFilesStoreDirectoryType, pathRelativeToDataDirectory);
//    }
//    catch (Exception ex)
//    {
//        return NewFailedPassthroughResult(ex, out returnData);
//    }

//    return NewSucceededPassthroughResult(rootDsFilesStoreDirectory, out returnData);
//}