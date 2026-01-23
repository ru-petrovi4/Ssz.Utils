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
using System.Threading;
using System.Threading.Tasks;
using JsonApiDotNetCore.Services;

namespace Ssz.Dcs.CentralServer
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
            (DataAccessClient_ServerAddress_OptionName, Properties.Resources.ServerAddress_Option, @""),
            (DataAccessClient_SystemNameToConnect_OptionName, Properties.Resources.SystemNameToConnect_Option, @"DCS"),
            (DataAccessClient_ContextParams_OptionName, Properties.Resources.ContextParams_Option, @""),            
            (DataAccessClient_DangerousAcceptAnyServerCertificate_OptionName, Properties.Resources.DangerousAcceptAnyServerCertificate_Option, @"true"),
        };
        
        public override void Initialize(CancellationToken cancellationToken)
        {
            IsAddonsPassthroughSupported = true;

            string serverAddress = OptionsSubstituted.TryGetValue(DataAccessClient_ServerAddress_OptionName) ?? @"";
            string systemNameToConnect = OptionsSubstituted.TryGetValue(DataAccessClient_SystemNameToConnect_OptionName) ?? @"";
            CaseInsensitiveOrderedDictionary<string?> contextParams = NameValueCollectionHelper.Parse(OptionsSubstituted.TryGetValue(DataAccessClient_ContextParams_OptionName));

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

            dataAccessProvider.ValueSubscriptionsUpdated += (s, e) =>
            {
                if (dataAccessProvider.IsConnected)
                    LastWorkTimeUtc = DateTime.UtcNow;
            };
            dataAccessProvider.EventMessagesCallback += (s, e) =>
            {
                if (dataAccessProvider.IsConnected)
                    LastWorkTimeUtc = DateTime.UtcNow;
            };

            bool dangerousAcceptAnyServerCertificatete = new Any(OptionsSubstituted.TryGetValue(DataAccessClient_DangerousAcceptAnyServerCertificate_OptionName)).ValueAsBoolean(false);
            
            var currentServerAddress = ConfigurationHelper.GetValue<string>(Configuration, @"Kestrel:Endpoints:HttpsDefaultCert:Url", @"")
                .Replace(@"*", @"localhost");
            if (String.Equals(serverAddress, currentServerAddress, StringComparison.InvariantCultureIgnoreCase))
                serverAddress = @"";

            dataAccessProvider.Initialize(elementIdsMap,                
                serverAddress,
                @"Ssz.Dcs.Addons.DataAccessClient",
                Environment.MachineName,
                systemNameToConnect,
                contextParams,
                new DataAccessProviderOptions
                {
                    DangerousAcceptAnyServerCertificate = dangerousAcceptAnyServerCertificatete
                },
                Dispatcher!);

            CsvDb.CsvFileChanged += (sender, args) =>
            {
                if (args.CsvFileName == @"" ||
                        String.Equals(args.CsvFileName, ElementIdsMap.StandardMapFileName, StringComparison.InvariantCultureIgnoreCase) ||
                        String.Equals(args.CsvFileName, ElementIdsMap.StandardTagsFileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    Dispatcher!.BeginInvokeEx(async ct =>
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

            base.Initialize(cancellationToken);
        }

        public override void Close()
        {
            if (DataAccessProvider is not null)
            {
                var t = DataAccessProvider.CloseAsync();
                DataAccessProvider = null;
            }

            base.Close();
        }

        #endregion
    }
}