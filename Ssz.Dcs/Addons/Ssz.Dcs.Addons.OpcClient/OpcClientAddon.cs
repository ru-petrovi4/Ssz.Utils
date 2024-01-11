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

        public static readonly string OpcDa_Host_OptionName = @"%(OpcDa_Host)";

        public static readonly string OpcAe_Host_OptionName = @"%(OpcAe_Host)";

        public static readonly string OpcHda_Host_OptionName = @"%(OpcHda_Host)";

        public static readonly string UsoHda_Host_OptionName = @"%(UsoHda_Host)";

        public static readonly string OpcDa_ProgId_OptionName = @"%(OpcDa_ProgId)";

        public static readonly string OpcAe_ProgId_OptionName = @"%(OpcAe_ProgId)";

        public static readonly string OpcHda_ProgId_OptionName = @"%(OpcHda_ProgId)";

        public static readonly string UsoHda_ProgId_OptionName = @"%(UsoHda_ProgId)";

        public static readonly Guid AddonGuid = new Guid(@"D963D4F1-C7BC-4964-8003-EBE582F7336A");

        public static readonly string AddonIdentifier = @"OpcClient";

        public override Guid Guid => AddonGuid;

        public override string Identifier => AddonIdentifier;

        public override string Desc => Properties.Resources.OpcClientAddon_Desc;

        public override string Version => "1.0";

        public override bool IsMultiInstance => true;

        public override (string, string, string)[] OptionsInfo => new (string, string, string)[]
        {
            (OpcDa_Host_OptionName, Properties.Resources.OpcDa_Host_Option, @"localhost"),
            (OpcAe_Host_OptionName, Properties.Resources.OpcAe_Host_Option, @"localhost"),
            (OpcHda_Host_OptionName, Properties.Resources.OpcHda_Host_Option, @"localhost"),
            (UsoHda_Host_OptionName, Properties.Resources.UsoHda_Host_Option, @"localhost"),
            (OpcDa_ProgId_OptionName, Properties.Resources.OpcDa_ProgId_Option, @"Uso.OpcDAServer"),
            (OpcAe_ProgId_OptionName, Properties.Resources.OpcAe_ProgId_Option, @"Uso.OpcAEServer"),
            (OpcHda_ProgId_OptionName, Properties.Resources.OpcHda_ProgId_Option, @"Uso.OpcHdaServer"),
            (UsoHda_ProgId_OptionName, Properties.Resources.UsoHda_ProgId_Option, @"Uso.OpcHdaServer"),
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

            //string serverAddress = OptionsSubstituted.TryGetValue(OpcClient_ServerAddress_OptionName) ?? @"";
            //string systemNameToConnect = OptionsSubstituted.TryGetValue(OpcClient_SystemNameToConnect_OptionName) ?? @"";
            //CaseInsensitiveDictionary<string?> contextParams = NameValueCollectionHelper.Parse(OptionsSubstituted.TryGetValue(OpcClient_ContextParams_OptionName));

            IDataAccessProvider dataAccessProvider = ActivatorUtilities.CreateInstance<OpcClientDataAccessProvider>(ServiceProvider);            

            var elementIdsMap = ActivatorUtilities.CreateInstance<ElementIdsMap>(ServiceProvider);
            elementIdsMap.Initialize(CsvDb.GetData(ElementIdsMap.StandardMapFileName), CsvDb.GetData(ElementIdsMap.StandardTagsFileName), CsvDb);

            dataAccessProvider.Initialize(elementIdsMap,
                @"",
                @"Ssz.Dcs.Addons.OpcClient",
                Environment.MachineName,
                @"",
                new CaseInsensitiveDictionary<string?>
                {
                    { OpcDa_Host_OptionName, OptionsSubstituted.TryGetValue(OpcDa_Host_OptionName) },
                    { OpcAe_Host_OptionName, OptionsSubstituted.TryGetValue(OpcAe_Host_OptionName) },
                    { OpcHda_Host_OptionName, OptionsSubstituted.TryGetValue(OpcHda_Host_OptionName) },
                    { UsoHda_Host_OptionName, OptionsSubstituted.TryGetValue(UsoHda_Host_OptionName) },
                    { OpcDa_ProgId_OptionName, OptionsSubstituted.TryGetValue(OpcDa_ProgId_OptionName) },
                    { OpcAe_ProgId_OptionName, OptionsSubstituted.TryGetValue(OpcAe_ProgId_OptionName) },
                    { OpcHda_ProgId_OptionName, OptionsSubstituted.TryGetValue(OpcHda_ProgId_OptionName) },
                    { UsoHda_ProgId_OptionName, OptionsSubstituted.TryGetValue(UsoHda_ProgId_OptionName) },
                },
                new DataAccessProviderOptions(),
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