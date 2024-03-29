using Ssz.Dcs.CentralServer.Common;
using Microsoft.Extensions.DependencyInjection;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region public functions

        /// <summary>
        ///     Returns DCS or ProcessModelingSession engines.
        /// </summary>
        /// <param name="serverContext"></param>
        /// <returns></returns>
        public ObservableCollection<EngineSession> GetEngineSessions(ServerContext serverContext)
        {
            string systemNameToConnect = serverContext.SystemNameToConnect;
            if (systemNameToConnect == @"")
                return new ObservableCollection<EngineSession>(); // Utility context                                                           

            if (String.Equals(systemNameToConnect, DataAccessConstants.Dcs_SystemName, StringComparison.InvariantCultureIgnoreCase))
            {
                return Dcs_EngineSessions;
            }
            else
            {
                ProcessModelingSession? processModelingSession = GetProcessModelingSessionOrNull(systemNameToConnect);
                if (processModelingSession is null)
                    return new ObservableCollection<EngineSession>(); // Unknown systemNameToConnect

                return processModelingSession.EngineSessions;
            }
        }

        #endregion

        #region private functions

        private EngineSession[] GetEngineSessions()
        {
            var result = new List<EngineSession>();

            result.AddRange(Dcs_EngineSessions);

            foreach (var processModelingSession in _processModelingSessionsCollection.Values)
            {
                result.AddRange(processModelingSession.EngineSessions);
            }

            return result.ToArray();
        }

        /// <summary>
        ///     Gets new instance of DataAccessProviderGetter Addon, not listed in Addons.csv
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="serverAddress"></param>
        /// <param name="systemNameToConnect"></param>
        /// <param name="contextParams"></param>
        /// <param name="addonDispatcher"></param>
        /// <returns></returns>
        private static DataAccessProviderGetter_AddonBase GetNewPreparedDataAccessProviderAddon(IServiceProvider serviceProvider, string serverAddress, string systemNameToConnect, CaseInsensitiveDictionary<string?> contextParams, IDispatcher addonDispatcher)
        {
            var addonsManager = serviceProvider.GetRequiredService<AddonsManager>();

            var dataAccessClient_Addon = (DataAccessProviderGetter_AddonBase)addonsManager.CreateAvailableAddon(@"DataAccessClient", @"",
                new[]
                {
                        new [] { DataAccessProviderGetter_AddonBase.DataAccessClient_ServerAddress_OptionName, serverAddress },
                        new [] { DataAccessProviderGetter_AddonBase.DataAccessClient_SystemNameToConnect_OptionName, systemNameToConnect },
                        new [] { DataAccessProviderGetter_AddonBase.DataAccessClient_ContextParams_OptionName, NameValueCollectionHelper.GetNameValueCollectionString(contextParams) }
                },
                addonDispatcher)!;

            dataAccessClient_Addon.Initialize(CancellationToken.None);            

            return dataAccessClient_Addon;
        }


        #endregion

        private class TrainingEngineSessionBase : EngineSession
        {
            #region construction and destruction

            public TrainingEngineSessionBase(DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon, string engine_TargetWorkstationName) :
                base(dataAccessProviderGetter_Addon)
            {
                Engine_TargetWorkstationName = engine_TargetWorkstationName;
            }

            public override void Dispose()
            {                
                DataAccessProviderGetter_Addon.Close();

                base.Dispose();
            }

            #endregion

            #region public functions

            public string ServerAddress => DataAccessProvider.ServerAddress;

            public string SystemNameToConnect => DataAccessProvider.SystemNameToConnect;

            public CaseInsensitiveDictionary<string?> ContextParams => DataAccessProvider.ContextParams;

            public string Engine_TargetWorkstationName { get; }

            #endregion            
        }

        private class Control_TrainingEngineSession : TrainingEngineSessionBase
        {
            #region construction and destruction

            public Control_TrainingEngineSession(DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon, string engine_TargetWorkstationName) :
                base(dataAccessProviderGetter_Addon, engine_TargetWorkstationName)
            {
            }

            #endregion

            #region public functions            

            public int PortNumber { get; set; }

            #endregion            
        }

        private class PlatInstructor_TrainingEngineSession : TrainingEngineSessionBase
        {
            #region construction and destruction

            public PlatInstructor_TrainingEngineSession(DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon, string engine_TargetWorkstationName) :
                base(dataAccessProviderGetter_Addon, engine_TargetWorkstationName)
            {
            }

            #endregion

            #region public functions

            public string SystemNameBase { get; set; } = @"";

            public string SystemNameInstance { get; set; } = @"";

            #endregion
        }        
    }
}


//private class AddonEngineSession : EngineSession
//{
//    #region construction and destruction

//    public AddonEngineSession(IServiceProvider serviceProvider, IDispatcher? callbackDispatcher, string serverAddress, string systemNameToConnect, CaseInsensitiveDictionary<string?> contextParams) :
//        base(serviceProvider, callbackDispatcher, serverAddress, systemNameToConnect, contextParams)
//    {
//    }

//    #endregion

//    #region public functions



//    #endregion
//}