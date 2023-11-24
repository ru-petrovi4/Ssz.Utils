using Ssz.Dcs.CentralServer.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Logging;
using Ssz.Xi.Client;
using System;
using System.ComponentModel.Composition;

namespace Ssz.Dcs.Addons.OpcClient
{
    [Export(typeof(AddonBase))]
    public class OpcClientAddon : DataAccessProviderGetter_AddonBase
    {
        #region public functions

        public static readonly Guid AddonGuid = new Guid(@"D963D4F1-C7BC-4964-8003-EBE582F7336A");

        public static readonly string AddonIdentifier = @"OpcClient";

        public override Guid Guid => AddonGuid;

        public override string Identifier => AddonIdentifier;

        public override string Desc => Properties.Resources.OpcClientAddon_Desc;

        public override string Version => "1.0";

        public override bool IsMultiInstance => true;

        public override (string, string, string)[] OptionsInfo => new (string, string, string)[]
        {
            //(OpcClient_ServerAddress_OptionName, Properties.Resources.ServerAddress_Option, @"http://localhost:60080/SimcodePlatServer/ServerDiscovery"),
            //(OpcClient_SystemNameToConnect_OptionName, Properties.Resources.SystemNameToConnect_Option, @""),
            //(OpcClient_ContextParams_OptionName, Properties.Resources.ContextParams_Option, @""),
            //(OpcClient_SystemNameToConnect_ToDisplay_OptionName, Properties.Resources.SystemNameToConnect_ToDisplay_Option, @"OPC NET Server"),
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

            //string serverAddress = OptionsSubstitutedThreadSafe.TryGetValue(OpcClient_ServerAddress_OptionName) ?? @"";
            //string systemNameToConnect = OptionsSubstitutedThreadSafe.TryGetValue(OpcClient_SystemNameToConnect_OptionName) ?? @"";
            //CaseInsensitiveDictionary<string?> contextParams = NameValueCollectionHelper.Parse(OptionsSubstitutedThreadSafe.TryGetValue(OpcClient_ContextParams_OptionName));

            IDataAccessProvider dataAccessProvider = ActivatorUtilities.CreateInstance<OpcClientDataAccessProvider>(ServiceProvider);            

            var elementIdsMap = ActivatorUtilities.CreateInstance<ElementIdsMap>(ServiceProvider);
            elementIdsMap.Initialize(CsvDb.GetData(ElementIdsMap.StandardMapFileName), CsvDb.GetData(ElementIdsMap.StandardTagsFileName), CsvDb);

            dataAccessProvider.Initialize(elementIdsMap,
                @"",
                @"Ssz.Dcs.Addons.OpcClient",
                Environment.MachineName,
                @"",
                new CaseInsensitiveDictionary<string?>(),
                new DataAccessProviderOptions(),
                callbackDispatcher);

            CsvDb.CsvFileChanged += (sender, args) =>
            {
                if (args.CsvFileName == @"" ||
                        String.Equals(args.CsvFileName, ElementIdsMap.StandardMapFileName, StringComparison.InvariantCultureIgnoreCase) ||
                        String.Equals(args.CsvFileName, ElementIdsMap.StandardTagsFileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    callbackDispatcher.BeginAsyncInvoke(async ct =>
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