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

        /// <summary>
        ///     Gets new instance of DataAccessProviderGetter Addon, not listed in Addons.csv
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="serverAddress"></param>
        /// <param name="systemNameToConnect"></param>
        /// <param name="contextParams"></param>
        /// <param name="addonDispatcher"></param>
        /// <returns></returns>
        private static DataAccessProviderGetter_AddonBase GetNewInitializedDataAccessProviderAddon(IServiceProvider serviceProvider, string serverAddress, string systemNameToConnect, CaseInsensitiveDictionary<string?> contextParams, IDispatcher addonDispatcher)
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

        private class ControlEngine_TrainingEngineSession : EngineSession
        {
            #region construction and destruction

            public ControlEngine_TrainingEngineSession(string engineSessionId, DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon) :
                base(engineSessionId, dataAccessProviderGetter_Addon)
            {
            }

            #endregion

            #region public functions            

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