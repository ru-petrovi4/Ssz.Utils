using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Properties;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region public functions      

        public event Action<string?, Ssz.Utils.DataAccess.EventMessage>? UtilityEventMessageNotification;

        public event Action<ServerContext, Ssz.Utils.DataAccess.EventMessage>? ProcessEventMessageNotification;

        #endregion

        #region private functions

        private void Generate_PrepareAndRunInstructorExe_SystemEvent(            
            string targetWorkstationName,
            ProcessModelingSession processModelingSession)
        {
            Action<string?, Ssz.Utils.DataAccess.EventMessage>? utilityEventMessageNotification = UtilityEventMessageNotification;
            if (utilityEventMessageNotification is null) return;            

            var eventMessage = new Ssz.Utils.DataAccess.EventMessage(new Ssz.Utils.DataAccess.EventId
            {
                Conditions = new List<Ssz.Utils.DataAccess.TypeId> { EventMessageConstants.PrepareAndRunInstructorExe_TypeId }
            });

            eventMessage.EventType = EventType.SystemEvent;
            eventMessage.OccurrenceTimeUtc = DateTime.UtcNow;
            eventMessage.TextMessage = CsvHelper.FormatForCsv(",", new object?[] {
                processModelingSession.LaunchEnginesJobId,
                processModelingSession.ProcessModelingSessionId,
                processModelingSession.ProcessModelName,
                processModelingSession.InstructorUserName });            

            utilityEventMessageNotification(targetWorkstationName, eventMessage);
        }
        
        /// <summary>
        ///     Downloads all necesary files from server if needed.
        /// </summary>
        /// <param name="targetWorkstationName"></param>
        /// <param name="processModelingSession"></param>
        /// <param name="binDsFilesStoreDirectoryType"></param>
        /// <param name="dataDsFilesStoreDirectoryType"></param>
        /// <param name="pathRelativeToDataDirectory"></param>
        /// <param name="instanceInfo"></param>
        private void Generate_LaunchEngine_SystemEvent(            
            string targetWorkstationName,
            ProcessModelingSession processModelingSession,            
            DsFilesStoreDirectoryType binDsFilesStoreDirectoryType,
            DsFilesStoreDirectoryType dataDsFilesStoreDirectoryType,
            string pathRelativeToDataDirectory,
            string instanceInfo)
        {
            Action<string?, Ssz.Utils.DataAccess.EventMessage>? utilityEventMessageNotification = UtilityEventMessageNotification;
            if (utilityEventMessageNotification is null) return;

            var eventMessage = new Ssz.Utils.DataAccess.EventMessage(new Ssz.Utils.DataAccess.EventId
            {
                Conditions = new List<Ssz.Utils.DataAccess.TypeId> { EventMessageConstants.PrepareAndRunEngineExe_TypeId }
            });

            eventMessage.EventType = EventType.SystemEvent;
            eventMessage.OccurrenceTimeUtc = DateTime.UtcNow;
            eventMessage.TextMessage = CsvHelper.FormatForCsv(",", new object?[] { null, processModelingSession.ProcessModelingSessionId, 
                processModelingSession.ProcessModelName,
                processModelingSession.InstructorUserName,
                new Any((uint)processModelingSession.InstructorAccessFlags).ValueAsString(false),
                binDsFilesStoreDirectoryType, dataDsFilesStoreDirectoryType, pathRelativeToDataDirectory, instanceInfo });

            utilityEventMessageNotification(targetWorkstationName, eventMessage);
        }        

        private void Generate_PrepareAndRunOperatorExe_UtilityEvent(            
            string targetWorkstationName, 
            OperatorSession operatorSession)
        {
            if (operatorSession.ProcessModelingSession is null)
                return;

            Action<string?, Ssz.Utils.DataAccess.EventMessage>? utilityEventMessageNotification = UtilityEventMessageNotification;
            if (utilityEventMessageNotification is null) return;            

            var eventMessage = new Ssz.Utils.DataAccess.EventMessage(
                new Ssz.Utils.DataAccess.EventId
                {
                    Conditions = new List<Ssz.Utils.DataAccess.TypeId> { EventMessageConstants.PrepareAndRunOperatorExe_TypeId }
                }
                );

            string operatorSessionDescription = Properties.Resources.OperatorSessionDescription_InterfaceNameToDisplay + @" " + operatorSession.Interface_NameToDisplay +
                "; " + Properties.Resources.OperatorSessionDescription_OperatorRoleName + @" " + operatorSession.OperatorRoleName;

            eventMessage.EventType = EventType.SystemEvent;
            eventMessage.OccurrenceTimeUtc = DateTime.UtcNow;
            eventMessage.TextMessage = CsvHelper.FormatForCsv(",", new[] {
                operatorSession.LaunchOperatorJobId,                 
                operatorSession.OperatorSessionId,
                operatorSession.ProcessModelingSession.ProcessModelName, 
                operatorSession.DsProject_PathRelativeToDataDirectory,                
                operatorSessionDescription
            });

            utilityEventMessageNotification(targetWorkstationName, eventMessage);
        }

        private void Generate_SetOperatorLauncherBusy_UtilityEvent(
            string targetWorkstationName,
            OperatorSession operatorSession)
        {
            if (operatorSession.ProcessModelingSession is null)
                return;

            Action<string?, Ssz.Utils.DataAccess.EventMessage>? utilityEventMessageNotification = UtilityEventMessageNotification;
            if (utilityEventMessageNotification is null) return;

            var eventMessage = new Ssz.Utils.DataAccess.EventMessage(
                new Ssz.Utils.DataAccess.EventId
                {
                    Conditions = new List<Ssz.Utils.DataAccess.TypeId> { EventMessageConstants.SetOperatorLauncherBusy_TypeId }
                }
                );

            string operatorSessionDescription = Properties.Resources.OperatorSessionDescription_InterfaceNameToDisplay + @" " + operatorSession.Interface_NameToDisplay +
                "; " + Properties.Resources.OperatorSessionDescription_OperatorRoleName + @" " + operatorSession.OperatorRoleName;

            eventMessage.EventType = EventType.SystemEvent;
            eventMessage.OccurrenceTimeUtc = DateTime.UtcNow;
            eventMessage.TextMessage = CsvHelper.FormatForCsv(",", new[] {
                operatorSession.LaunchOperatorJobId,
                operatorSession.ProcessModelingSession.ProcessModelingSessionId,
                operatorSession.OperatorSessionId });

            utilityEventMessageNotification(targetWorkstationName, eventMessage);
        }

        private void Generate_RunOperatorExe_UtilityEvent(
            string targetWorkstationName,
            OperatorSession operatorSession, 
            string binDirectoryFullName, 
            string dataDirectoryFullName,
            string centralServerAddress)
        {
            if (operatorSession.ProcessModelingSession is null)
                return;

            Action<string?, Ssz.Utils.DataAccess.EventMessage>? utilityEventMessageNotification = UtilityEventMessageNotification;
            if (utilityEventMessageNotification is null) return;

            var eventMessage = new Ssz.Utils.DataAccess.EventMessage(
                new Ssz.Utils.DataAccess.EventId
                {
                    Conditions = new List<Ssz.Utils.DataAccess.TypeId> { EventMessageConstants.RunOperatorExe_TypeId }
                }
                );

            string operatorSessionDescription = Properties.Resources.OperatorSessionDescription_InterfaceNameToDisplay + @" " + operatorSession.Interface_NameToDisplay +
                "; " + Properties.Resources.OperatorSessionDescription_OperatorRoleName + @" " + operatorSession.OperatorRoleName;

            eventMessage.EventType = EventType.SystemEvent;
            eventMessage.OccurrenceTimeUtc = DateTime.UtcNow;
            eventMessage.TextMessage = CsvHelper.FormatForCsv(",", new[] {
                operatorSession.LaunchOperatorJobId,
                operatorSession.ProcessModelingSession.ProcessModelingSessionId,
                operatorSession.OperatorSessionId,
                binDirectoryFullName,
                dataDirectoryFullName,
                centralServerAddress,
                operatorSession.DsProject_PathRelativeToDataDirectory,
                StringHelper.GetNullForEmptyString(operatorSession.OperatorPlay_AdditionalCommandLine),
                operatorSessionDescription
            });

            utilityEventMessageNotification(targetWorkstationName, eventMessage);
        }

        private void Generate_RunInstructorExe_UtilityEvent(
            string targetWorkstationName,
            ProcessModelingSession processModelingSession, string binDirectoryFullName, string arguments)
        {            
            Action<string?, Ssz.Utils.DataAccess.EventMessage>? utilityEventMessageNotification = UtilityEventMessageNotification;
            if (utilityEventMessageNotification is null) return;

            var eventMessage = new Ssz.Utils.DataAccess.EventMessage(
                new Ssz.Utils.DataAccess.EventId
                {
                    Conditions = new List<Ssz.Utils.DataAccess.TypeId> { EventMessageConstants.RunInstructorExe_TypeId }
                }
                );
            
            eventMessage.EventType = EventType.SystemEvent;
            eventMessage.OccurrenceTimeUtc = DateTime.UtcNow;
            eventMessage.TextMessage = CsvHelper.FormatForCsv(",", new[] {
                processModelingSession.ProcessModelingSessionId,
                binDirectoryFullName,
                arguments
            });

            utilityEventMessageNotification(targetWorkstationName, eventMessage);
        }

        private void Generate_DownloadChangedFiles_SystemEvent(
            string targetWorkstationName,
            string jobId,
            string directoryPathsRelativeToRootDirectory,
            bool includeSubdirectories)
        {
            Action<string?, Ssz.Utils.DataAccess.EventMessage>? utilityEventMessageNotification = UtilityEventMessageNotification;
            if (utilityEventMessageNotification is null) return;

            var eventMessage = new Ssz.Utils.DataAccess.EventMessage(new Ssz.Utils.DataAccess.EventId
            {
                Conditions = new List<Ssz.Utils.DataAccess.TypeId> { EventMessageConstants.DownloadChangedFiles_TypeId }
            });

            eventMessage.EventType = EventType.SystemEvent;
            eventMessage.OccurrenceTimeUtc = DateTime.UtcNow;
            eventMessage.TextMessage = CsvHelper.FormatForCsv(",", new object?[] {
                jobId,
                directoryPathsRelativeToRootDirectory,
                includeSubdirectories
            });

            utilityEventMessageNotification(targetWorkstationName, eventMessage);
        }

        private void Generate_UploadChangedFiles_SystemEvent(            
            string targetWorkstationName,
            string jobId,
            string directoryPathsRelativeToRootDirectory)
        {
            Action<string?, Ssz.Utils.DataAccess.EventMessage>? utilityEventMessageNotification = UtilityEventMessageNotification;
            if (utilityEventMessageNotification is null) return;

            var eventMessage = new Ssz.Utils.DataAccess.EventMessage(new Ssz.Utils.DataAccess.EventId
            {
                Conditions = new List<Ssz.Utils.DataAccess.TypeId> { EventMessageConstants.UploadChangedFiles_TypeId }
            });

            eventMessage.EventType = EventType.SystemEvent;
            eventMessage.OccurrenceTimeUtc = DateTime.UtcNow;
            eventMessage.TextMessage = CsvHelper.FormatForCsv(",", new object?[] {
                jobId,
                directoryPathsRelativeToRootDirectory });

            utilityEventMessageNotification(targetWorkstationName, eventMessage);
        }

        #endregion
    }
}
