using Ssz.Dcs.CentralServer.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Logging;
using Ssz.Xi.Client;
using System;
using System.ComponentModel.Composition;

namespace Ssz.Dcs.Addons.DataAccessClient
{
    [Export(typeof(AddonBase))]
    public class DataAccessClientAddon : DataAccessProviderGetter_AddonBase
    {
        #region public functions

        public static readonly Guid AddonGuid = new Guid(@"0BF42EE1-FBA4-46BE-922C-E3FC360CEF15");

        public static readonly string AddonIdentifier = @"DataAccessClient";

        public override Guid Guid => AddonGuid;

        public override string Identifier => AddonIdentifier;

        public override string Desc => Properties.Resources.DataAccessClientAddon_Desc;

        public override string Version => "1.0";

        public override bool IsMultiInstance => true;

        public override (string, string, string)[] OptionsInfo => new (string, string, string)[]
        {
            (DataAccessClient_ServerAddress_OptionName, Properties.Resources.ServerAddress_Option, @"http://localhost:60080/SimcodePlatServer/ServerDiscovery"),
            (DataAccessClient_SystemNameToConnect_OptionName, Properties.Resources.SystemNameToConnect_Option, @""),
            (DataAccessClient_ContextParams_OptionName, Properties.Resources.ContextParams_Option, @""),
            (DataAccessClient_SystemNameToConnect_ToDisplay_OptionName, Properties.Resources.SystemNameToConnect_ToDisplay_Option, @"OPC NET Server"),
        };

        /// <summary>
        ///     Creates initialized IDataAccessProvider or throws. 
        ///     Addon must be initialized.
        /// </summary>
        /// <returns></returns>
        public override void InitializeDataAccessProvider(IDispatcher callbackDispatcher)
        {
            if (!IsInitialized)
                throw new InvalidOperationException();

            IsAddonsPassthroughSupported = true;

            string serverAddress = OptionsSubstitutedThreadSafe.TryGetValue(DataAccessClient_ServerAddress_OptionName) ?? @"";
            string systemNameToConnect = OptionsSubstitutedThreadSafe.TryGetValue(DataAccessClient_SystemNameToConnect_OptionName) ?? @"";
            CaseInsensitiveDictionary<string?> contextParams = NameValueCollectionHelper.Parse(OptionsSubstitutedThreadSafe.TryGetValue(DataAccessClient_ContextParams_OptionName));

            IDataAccessProvider dataAccessProvider;
            if (serverAddress.EndsWith("/ServerDiscovery", StringComparison.InvariantCultureIgnoreCase) ||
                serverAddress.EndsWith("/ServerDiscovery/", StringComparison.InvariantCultureIgnoreCase))
            {
                dataAccessProvider = ActivatorUtilities.CreateInstance<XiDataAccessProvider>(ServiceProvider);
            }
            else
            {
                dataAccessProvider = ActivatorUtilities.CreateInstance<GrpcDataAccessProvider>(ServiceProvider);
            }

            var elementIdsMap = ActivatorUtilities.CreateInstance<ElementIdsMap>(ServiceProvider);
            elementIdsMap.Initialize(CsvDb.GetData(ElementIdsMap.StandardMapFileName), CsvDb.GetData(ElementIdsMap.StandardTagsFileName), CsvDb);

            var options = new DataAccessProviderOptions { DangerousAcceptAnyServerCertificate = false };
#if DEBUG
            options.DangerousAcceptAnyServerCertificate = true;
#endif

            dataAccessProvider.Initialize(elementIdsMap,                
                serverAddress,
                @"Ssz.Dcs.Addons.DataAccessClient",
                Environment.MachineName,
                systemNameToConnect,
                contextParams,
                options,
                callbackDispatcher);

            CsvDb.CsvFileChanged += (sender, args) =>
            {
                if (args.CsvFileName == @"" ||
                        String.Equals(args.CsvFileName, ElementIdsMap.StandardMapFileName, StringComparison.InvariantCultureIgnoreCase) ||
                        String.Equals(args.CsvFileName, ElementIdsMap.StandardTagsFileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    callbackDispatcher.BeginInvokeEx(async ct =>
                    {
                        if (dataAccessProvider.IsInitialized)
                        {
                            elementIdsMap.Initialize(CsvDb.GetData(ElementIdsMap.StandardMapFileName), CsvDb.GetData(ElementIdsMap.StandardTagsFileName), CsvDb);
                            // If not initialized then does nothing.
                            await dataAccessProvider.ReInitializeAsync();
                        }                        
                    });                    
                };            
            };            

            DataAccessProvider = dataAccessProvider;
        }        

        #endregion
    }
}