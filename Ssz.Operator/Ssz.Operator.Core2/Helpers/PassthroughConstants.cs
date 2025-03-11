using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public static class PassthroughConstants
    {
        /// <summary>
        ///     Utility Context (systemNameToConnect is String.Empty).
        ///     Request: GetDirectoryInfoRequest
        ///     Reply: DsFilesStoreDirectory
        /// </summary>
        public const string GetDirectoryInfo = @"GetDirectoryInfo";

        /// <summary>
        ///     Utility Context (systemNameToConnect is String.Empty).
        ///     Request: UTF8 CSV string with files paths relative to the root of the Files Store
        ///     Reply: LoadFilesReply
        /// </summary>
        public const string LoadFiles = @"LoadFiles";

        /// <summary>
        ///     Utility Context (systemNameToConnect is String.Empty).
        ///     Request: empty.
        ///     Reply: GetUsersReply
        /// </summary>
        public const string GetUsers = @"GetUsers";

        /// <summary>
        ///     Utility DataAccessProvider (systemNameToConnect is String.Empty).        
        ///     Request: UTF8 string with ProcessModelingSessionId,programpath,commandline. 
        /// </summary>
        public const string ProcessModelingSession_RunInstructorExe = @"ProcessModelingSession.RunInstructorExe";        

        /// <summary>
        ///     Utility Context (systemNameToConnect is String.Empty).
        ///     Request: ScenarioResult bytes.
        ///     Reply: empty
        /// </summary>
        public const string AddScenarioResult = @"AddScenarioResult";

        /// <summary>
        ///     Process Context (systemNameToConnect is not String.Empty).
        ///     Request: UTF8 Options/Values string.
        ///     Reply: empty
        /// </summary>
        public const string SetAddonVariables = @"SetAddonVariables";

        /// <summary>
        ///     Process Context (systemNameToConnect is not String.Empty).
        ///     Request: empty
        ///     Reply: AddonStatuses
        /// </summary>
        public const string GetAddonStatuses = @"GetAddonStatuses";

        /// <summary>
        ///     Process Context (systemNameToConnect is not String.Empty).
        ///     Request: empty
        ///     Reply: UTF8 String
        /// </summary>
        public const string GetOperatorUserName = @"GetOperatorUserName";

        /// <summary>
        ///     Process Context (systemNameToConnect is not String.Empty).
        ///     Request: empty
        ///     Reply: UTF8 String
        /// </summary>
        public const string GetOperatorRoleName = @"GetOperatorRoleName";

        /// <summary>
        ///     Process Context (systemNameToConnect is not String.Empty).
        ///     Request: empty
        ///     Reply: ConfigurationCsvFiles
        /// </summary>
        public const string ReadConfiguration = @"ReadConfiguration";

        /// <summary>
        ///     Process Context (systemNameToConnect is not String.Empty).
        ///     Request: ConfigurationCsvFiles        
        /// </summary>
        public const string WriteConfiguration = @"WriteConfiguration";        

        /// <summary>
        ///     Process Context DataAccessProvider (systemNameToConnect is not String.Empty).
        /// </summary>
        public const string Shutdown = @"Shutdown";
    }

    public static class LongrunningPassthroughConstants
    {
        /// <summary>
        ///     Saves files to server store.
        ///     Utility DataAccessProvider (systemNameToConnect is String.Empty).
        ///     Request: SaveFilesRequest      
        /// </summary>
        public const string SaveFiles = @"SaveFiles";

        /// <summary>
        ///     Delete files on server store.
        ///     Utility DataAccessProvider (systemNameToConnect is String.Empty).
        ///     Request: UTF8 CSV string with files paths relative to the root of the Files Store 
        /// </summary>
        public const string DeleteFiles = @"DeleteFiles";        

        /// <summary>
        ///     Move files on servre store.
        ///     Utility DataAccessProvider (systemNameToConnect is String.Empty).
        ///     Request: UTF8 CSV string with old and new files paths relative to the root of the Files Store 
        /// </summary>
        public const string MoveFiles = @"MoveFiles";

        /// <summary>
        ///     Downloads necesary files.
        ///     Utility DataAccessProvider (systemNameToConnect is String.Empty).        
        ///     Request: UTF8 string with ProcessModelingSessionId. 
        /// </summary>
        public const string ProcessModelingSession_PrepareAndRunInstructorAndEngines = @"ProcessModelingSession.PrepareAndRunInstructorAndEngines";        

        /// <summary>
        ///     Utility DataAccessProvider (systemNameToConnect is String.Empty).        
        ///     Request: UTF8 string with OperatorSessionId. 
        /// </summary>
        public const string ProcessModelingSession_PrepareAndRunOperatorExe = @"ProcessModelingSession.PrepareAndRunOperatorExe";

        /// <summary>
        ///     Utility DataAccessProvider (systemNameToConnect is String.Empty).        
        ///     Request: UTF8 string with OperatorSessionId,BinDirectoryInfo.FullName,DataDirectoryInfo.FullName. 
        /// </summary>
        public const string ProcessModelingSession_RunOperatorExe = @"ProcessModelingSession.RunOperatorExe";

        /// <summary>
        ///     Utility DataAccessProvider (systemNameToConnect is String.Empty).        
        ///     Request: UTF8 string with OperatorSessionId. 
        /// </summary>
        public const string ProcessModelingSession_SubscribeForLaunchOperatorProgress = @"ProcessModelingSession.SubscribeForLaunchOperatorProgress";

        /// <summary>
        ///     !!!Warning!!! Not downolads child directories!!!
        ///     Downloads changed files from server store.
        ///     Utility DataAccessProvider (systemNameToConnect is String.Empty).
        ///     Request: UTF8 string with directory paths relative to the root of the Files Store. 
        /// </summary>
        public const string ProcessModelingSession_DownloadChangedFiles = @"ProcessModelingSession.DownloadChangedFiles";

        /// <summary>
        ///     !!!Warning!!! Not Uploads child directories!!!
        ///     Uploads changed files to server store.
        ///     Utility DataAccessProvider (systemNameToConnect is String.Empty).
        ///     Request: UTF8 string with directory paths relative to the root of the Files Store. 
        /// </summary>
        public const string ProcessModelingSession_UploadChangedFiles = @"ProcessModelingSession.UploadChangedFiles";

        /// <summary>
        ///     Process DataAccessProvider (systemNameToConnect is not String.Empty).
        ///     Request: UTF8 string with path relative to Process Directory, withount '\' at the beginning, without file extension at the end.
        /// </summary>
        public const string LoadStateFile = @"LoadStateFile";

        /// <summary>
        ///     Process DataAccessProvider (systemNameToConnect is not String.Empty).
        ///     Request: UTF8 string with path relative to Process Directory, withount '\' at the beginning, without file extension at the end.
        /// </summary>
        public const string SaveStateFile = @"SaveStateFile";

        /// <summary>
        ///     Process DataAccessProvider (systemNameToConnect is not String.Empty).
        ///     Request: UTF8 string with number of seconds.
        /// </summary>
        public const string Step = @"Step";        
    }
}


//public const string PlatInstructor_SlaveStepComplete = @"SlaveStepComplete"; // args: string, uint step       