using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;

namespace Ssz.Dcs.Addons.OpcUaClient
{
    /// <summary>
    ///     Data source addon: connects the central server to an OPC UA server (data access, alarms and conditions,
    ///     historical access). Like the OPC DA client addon, every switched on instance becomes an engine session
    ///     of the DCS system.
    /// </summary>
    [Export(typeof(AddonBase))]
    public class OpcUaClientAddon : DataAccessProviderGetter_AddonBase
    {
        #region public functions

        public static readonly string EndpointUrl_OptionName = @"%(OpcUaClient_EndpointUrl)";

        public static readonly string UseSecurity_OptionName = @"%(OpcUaClient_UseSecurity)";

        public static readonly string UserName_OptionName = @"%(OpcUaClient_UserName)";

        public static readonly string Password_OptionName = @"%(OpcUaClient_Password)";

        public static readonly string AutoAcceptServerCertificate_OptionName = @"%(OpcUaClient_AutoAcceptServerCertificate)";

        public static readonly string PkiDirectory_OptionName = @"%(OpcUaClient_PkiDirectory)";

        public static readonly string DefaultNamespaceUri_OptionName = @"%(OpcUaClient_DefaultNamespaceUri)";

        public static readonly string DefaultNamespaceIndex_OptionName = @"%(OpcUaClient_DefaultNamespaceIndex)";

        public static readonly string PublishingIntervalMs_OptionName = @"%(OpcUaClient_PublishingIntervalMs)";

        public static readonly string SamplingIntervalMs_OptionName = @"%(OpcUaClient_SamplingIntervalMs)";

        public static readonly string SubscribeToEvents_OptionName = @"%(OpcUaClient_SubscribeToEvents)";

        public static readonly string EventNotifierNodeId_OptionName = @"%(OpcUaClient_EventNotifierNodeId)";

        public static readonly string SessionTimeoutMs_OptionName = @"%(OpcUaClient_SessionTimeoutMs)";

        public static readonly Guid AddonGuid = new Guid(@"8C4D2F6B-1A3E-4B7C-9D5F-2E6A8B0C4D1F");

        public static readonly string AddonIdentifier = @"OpcUaClient";

        public override Guid Guid => AddonGuid;

        public override string Identifier => AddonIdentifier;

        public override string Desc => Properties.Resources.OpcUaClientAddon_Desc;

        public override string Version => "1.0";

        public override bool IsMultiInstance => true;

        public override (string, string, string)[] OptionsInfo => new (string, string, string)[]
        {
            (EndpointUrl_OptionName, Properties.Resources.EndpointUrl_Option, @"opc.tcp://localhost:4840"),
            (UseSecurity_OptionName, Properties.Resources.UseSecurity_Option, @"true"),
            (UserName_OptionName, Properties.Resources.UserName_Option, @""),
            (Password_OptionName, Properties.Resources.Password_Option, @""),
            (AutoAcceptServerCertificate_OptionName, Properties.Resources.AutoAcceptServerCertificate_Option, @"true"),
            (PkiDirectory_OptionName, Properties.Resources.PkiDirectory_Option, @"OpcUa\pki"),
            (DefaultNamespaceUri_OptionName, Properties.Resources.DefaultNamespaceUri_Option, @""),
            (DefaultNamespaceIndex_OptionName, Properties.Resources.DefaultNamespaceIndex_Option, @"2"),
            (PublishingIntervalMs_OptionName, Properties.Resources.PublishingIntervalMs_Option, @"500"),
            (SamplingIntervalMs_OptionName, Properties.Resources.SamplingIntervalMs_Option, @"250"),
            (SubscribeToEvents_OptionName, Properties.Resources.SubscribeToEvents_Option, @"true"),
            (EventNotifierNodeId_OptionName, Properties.Resources.EventNotifierNodeId_Option, @"i=2253"),
            (SessionTimeoutMs_OptionName, Properties.Resources.SessionTimeoutMs_Option, @"60000"),
        };

        public override void Initialize(CancellationToken cancellationToken)
        {
            IDataAccessProvider dataAccessProvider = ActivatorUtilities.CreateInstance<OpcUaClientDataAccessProvider>(ServiceProvider);

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

            var contextParams = new CaseInsensitiveOrderedDictionary<string?>();
            foreach (var optionInfo in OptionsInfo)
            {
                contextParams[optionInfo.Item1] = OptionsSubstituted.TryGetValue(optionInfo.Item1);
            }

            // Every addon instance is a separate OPC UA client application with its own certificate.
            string pkiDirectory = (contextParams.TryGetValue(PkiDirectory_OptionName) ?? @"").Trim();
            if (pkiDirectory == @"")
                pkiDirectory = @"OpcUa\pki";
            pkiDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(pkiDirectory));
            if (!String.IsNullOrEmpty(InstanceId))
                pkiDirectory = Path.Combine(pkiDirectory, InstanceId);
            contextParams[PkiDirectory_OptionName] = pkiDirectory;

            string endpointUrl = (contextParams.TryGetValue(EndpointUrl_OptionName) ?? @"").Trim();

            dataAccessProvider.Initialize(elementIdsMap,
                endpointUrl,
                @"Ssz.Dcs.Addons.OpcUaClient",
                ConfigurationHelper.GetThisWorkstationName(Configuration),
                @"",
                contextParams,
                new DataAccessProviderOptions(),
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
                }
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
