using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public static class DataAccessConstants
    {
        public const string CentralServer_ClientWindowsService_ClientApplicationName = @"Ssz.Dcs.CentralServer_ClientWindowsService";

        public const string Launcher_ClientApplicationName = @"Simcode.DeltaSim.Launcher";

        public const string Instructor_ClientApplicationName = @"Simcode.DeltaSim.Instructor";

        public const string Operator_ClientApplicationName = @"Simcode.DeltaSim.Operator.Play";

        public const string ControlEngine_ClientApplicationName = @"Ssz.Dcs.ControlEngine";

        public const string CentralServer_ClientApplicationName = @"Ssz.Dcs.CentralServer";

        public const string UtilityItem_Operators = @"Operators";

        public const string UtilityItem_CentralServer = @"CentralServer";

        public const string UtilityItem_CentralServers = @"CentralServers";

        public const string ParamName_ConnectionToMain = @"ConnectionToMain";

        public const string ParamName_AdditionalCentralServerAddress = @"AdditionalCentralServerAddress";

        public const string ParamName_ControlEngineServerAddress = @"ControlEngineServerAddress";

        public const string ParamName_Instructor_ProcessModelNames = @"Instructor_ProcessModelNames";

        public const string ParamName_Operator_ProcessModelNames = @"Operator_ProcessModelNames";

        public const string ParamName_Engine_ProcessModelNames = @"Engine_ProcessModelNames";

        public const string ParamName_RunningControlEnginesCount = @"RunningControlEnginesCount";

        public const string ParamName_AllCentralServers = @"AllCentralServers";        

        public const string ParamName_EngineSessionId = @"EngineSessionId";

        public static readonly TimeSpan UnrecoverableTimeout = TimeSpan.FromSeconds(180);

        public static readonly TimeSpan OperationCleanupTimeout = TimeSpan.FromDays(1);

        public const string Dcs_SystemName = "DCS";

        public const string DefaultProcessModelingSessionId = @"MODEL";

        public const string PlatformXiProcessModelingSessionId = @"PLATFORM";

        public const string UsoXiProcessModelingSessionId = @"USO";        
    }
}
