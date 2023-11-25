using System;
using System.Collections.Generic;
using System.Threading;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.ServerListItems;
using System.Linq;
using System.Text;
using Ssz.Dcs.CentralServer.Common.EntityFramework;
using Ssz.Dcs.CentralServer.Common.Passthrough;
using Ssz.Utils.Serialization;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region public functions      

        public void InsertUser(string dataAccessUtilityContextId, string userName, string personnelNumber, string windowsUserName)
        {
            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    if (dbContext.IsConfigured)
                    {
                        dbContext.Users.Add(new User
                        {
                            UserName = userName,
                            PersonnelNumber = personnelNumber,
                            WindowsUserName = windowsUserName,
                        });
                        dbContext.SaveChanges();
                    }  
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Insert user failed. " + userName);
                throw new RpcException(new Status(StatusCode.InvalidArgument, Properties.Resources.InsertUserError), ex.Message);
            }
        }

        public void UpdateUser(string dataAccessUtilityContextId, string userName, string newUserName, string newPersonnelNumber, string newWindowsUserName)
        {
            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    if (dbContext.IsConfigured)
                    {
                        var user = dbContext.Users.FirstOrDefault(u => u.UserName == userName);
                        if (user is null)
                        {
                            Logger.LogError("Delete user failed. " + userName);
                            throw new RpcException(new Status(StatusCode.InvalidArgument, Properties.Resources.DeleteUserError));
                        }
                        user.UserName = newUserName;
                        user.PersonnelNumber = newPersonnelNumber;
                        user.WindowsUserName = newWindowsUserName;
                        dbContext.SaveChanges();
                    }                    
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Insert user failed.");
                throw new RpcException(new Status(StatusCode.InvalidArgument, Properties.Resources.UpdateUserError), ex.Message);
            }

            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                try
                {
                    var rootDsFilesStoreDirectory = DsFilesStoreHelper.CreateDsFilesStoreDirectoryObject(FilesStoreDirectoryInfo, @"", 1);
                    foreach (DsFilesStoreDescriptor dsFileStoreDescriptor in DsFilesStoreHelper.GetDsFilesStoreDescriptors(rootDsFilesStoreDirectory))
                    {
                        var dsFilesStoreItem = DsFilesStoreHelper.FindDsFilesStoreItem(rootDsFilesStoreDirectory, dsFileStoreDescriptor.RelativeToDescriptorFileOrDirectoryPath);
                        if (dsFilesStoreItem is null) continue;
                        UpdateUserNameInSavesDirectory(dsFilesStoreItem.DsFilesStoreDirectory.Name, userName, newUserName);
                    }
                }
                catch
                {
                }
            });                       
        }

        public void DeleteUser(string dataAccessUtilityContextId, string userName)
        {
            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    if (dbContext.IsConfigured)
                    {
                        var user = dbContext.Users.FirstOrDefault(u => u.UserName == userName);
                        if (user is null)
                        {
                            Logger.LogError("Delete user failed. " + userName);
                            throw new RpcException(new Status(StatusCode.InvalidArgument, Properties.Resources.DeleteUserError));
                        }
                        dbContext.Users.Remove(user);
                        dbContext.SaveChanges();
                    }                    
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Insert user failed.");
                throw new RpcException(new Status(StatusCode.InvalidArgument, Properties.Resources.DeleteUserError), ex.Message);
            }
        }

        #endregion

        #region private function

        private void GetUsersPassthrough(out byte[] returnData)
        {
            returnData = new byte[0];
            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    if (dbContext.IsConfigured)
                    {
                        var reply = new GetUsersReply
                        {
                            UsersCollection = dbContext.Users.ToList()
                        };
                        returnData = SerializationHelper.GetOwnedData(reply);
                    }                    
                }
            }
            catch (Exception ex)
            {                
                ThrowRpcException(ex);
            }
        }

        private async Task AddScenarioResultPassthrough(ServerContext serverContext, byte[] dataToSend)
        {
            try
            {
                var scenarioResult = new ScenarioResult();
                SerializationHelper.SetOwnedData(scenarioResult, dataToSend);

                ProcessModelingSession? processModelingSession = GetProcessModelingSessionOrNull(serverContext.SystemNameToConnect);
                if (processModelingSession is not null &&
                    processModelingSession.DbEnity_ProcessModelingSessionId is not null) // Process context with no operatorSessionId.
                {
                    var operatorSessionsIds = OperatorSessionsCollection
                        .Where(os => os.Value.ProcessModelingSession == processModelingSession)
                        .Select(os => os.Value.DbEnity_OperatorSessionId)
                        .ToArray();

                    using (var dbContext = _dbContextFactory.CreateDbContext())
                    {
                        if (dbContext.IsConfigured)
                        {
                            scenarioResult.ProcessModelingSessionId = processModelingSession.DbEnity_ProcessModelingSessionId.Value;
                            scenarioResult.OperatorSessions.AddRange(dbContext.OperatorSessions.Where(os => operatorSessionsIds.Contains(os.Id)));

                            dbContext.ScenarioResults.Add(scenarioResult);

                            await dbContext.SaveChangesAsync();
                        }                        
                    }
                }                    
            }
            catch (Exception ex)
            {                
                ThrowRpcException(ex);
            }
        }        

        private void UpdateUserNameInSavesDirectory(string processModelName, string userName, string newUserName)
        {
            string userDirectoryName = DsFilesStoreHelper.GetDsFilesStoreDirectoryName(userName);
            try
            {
                var directoryInfo = new DirectoryInfo(Path.Combine(
                    FilesStoreDirectoryInfo.FullName, processModelName, DsFilesStoreConstants.SavesDirectoryNameUpper));
                if (!directoryInfo.Exists) return;
                foreach (var userDirectoryInfo in directoryInfo.EnumerateDirectories().ToArray())
                {
                    if (String.Equals(userDirectoryInfo.Name, userDirectoryName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        FileSystemHelper.MoveDirectory(userDirectoryInfo.FullName, Path.Combine(
                                FilesStoreDirectoryInfo.FullName, processModelName, DsFilesStoreConstants.SavesDirectoryNameUpper,
                                DsFilesStoreHelper.GetDsFilesStoreDirectoryName(newUserName)));
                        break;
                    }                    
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Rename directory failed: " + userName);
            }            
        }

        #endregion
    }
}