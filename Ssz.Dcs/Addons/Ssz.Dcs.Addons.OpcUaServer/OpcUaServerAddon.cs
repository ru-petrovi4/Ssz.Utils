using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Dcs.Addons.OpcUaServer.OpcUa;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.Addons.OpcUaServer
{
    /// <summary>
    ///     Exposes one Ssz.Dcs.CentralServer system (DCS or a process modeling session) as an OPC UA server:
    ///     tags as variables (data access, subscriptions, writes), events and alarms (alarms and conditions, acknowledge),
    ///     values and events journals (historical access).
    ///     Connects to the central server in-process through the standard data access provider.
    /// </summary>
    [Export(typeof(AddonBase))]
    public class OpcUaServerAddon : AddonBase
    {
        #region public functions

        public static readonly string EndpointUrl_OptionName = @"%(OpcUaServer_EndpointUrl)";

        public static readonly string SystemNameToConnect_OptionName = @"%(OpcUaServer_SystemNameToConnect)";

        public static readonly string ContextParams_OptionName = @"%(OpcUaServer_ContextParams)";

        public static readonly string SecurityPolicies_OptionName = @"%(OpcUaServer_SecurityPolicies)";

        public static readonly string AllowAnonymous_OptionName = @"%(OpcUaServer_AllowAnonymous)";

        public static readonly string UserName_OptionName = @"%(OpcUaServer_UserName)";

        public static readonly string Password_OptionName = @"%(OpcUaServer_Password)";

        public static readonly string AutoAcceptClientCertificates_OptionName = @"%(OpcUaServer_AutoAcceptClientCertificates)";

        public static readonly string PkiDirectory_OptionName = @"%(OpcUaServer_PkiDirectory)";

        public static readonly string IdleUnsubscribeSeconds_OptionName = @"%(OpcUaServer_IdleUnsubscribeSeconds)";

        /// <summary>
        ///     Nodes definition file in the addon configuration directory.
        ///     Columns: ElementId, Folder path (segments separated by '/'), Display name, Description.
        /// </summary>
        public const string NodesCsvFileName = @"nodes.csv";

        /// <summary>
        ///     Root folder name when the utility context (empty system name) is exposed.
        /// </summary>
        public const string UtilityRootFolderName = @"Utility";

        public static readonly Guid AddonGuid = new Guid(@"3E1B0C58-9B6D-4F3A-8C2E-5A7D1F4B9E21");

        public static readonly string AddonIdentifier = @"OpcUaServer";

        public override Guid Guid => AddonGuid;

        public override string Identifier => AddonIdentifier;

        public override string Desc => Properties.Resources.OpcUaServerAddon_Desc;

        public override string Version => "1.0";

        public override bool IsMultiInstance => true;

        public override (string, string, string)[] OptionsInfo => new (string, string, string)[]
        {
            (EndpointUrl_OptionName, Properties.Resources.EndpointUrl_Option, @"opc.tcp://localhost:4840/SszDcsCentralServer"),
            (SystemNameToConnect_OptionName, Properties.Resources.SystemNameToConnect_Option, DataAccessConstants.Dcs_SystemName),
            (ContextParams_OptionName, Properties.Resources.ContextParams_Option, @""),
            (SecurityPolicies_OptionName, Properties.Resources.SecurityPolicies_Option, @"None,Basic256Sha256,Aes128_Sha256_RsaOaep,Aes256_Sha256_RsaPss"),
            (AllowAnonymous_OptionName, Properties.Resources.AllowAnonymous_Option, @"true"),
            (UserName_OptionName, Properties.Resources.UserName_Option, @""),
            (Password_OptionName, Properties.Resources.Password_Option, @""),
            (AutoAcceptClientCertificates_OptionName, Properties.Resources.AutoAcceptClientCertificates_Option, @"true"),
            (PkiDirectory_OptionName, Properties.Resources.PkiDirectory_Option, @"OpcUa\pki"),
            (IdleUnsubscribeSeconds_OptionName, Properties.Resources.IdleUnsubscribeSeconds_Option, @"120"),
        };

        /// <summary>
        ///     Not null after Initialize(...), closes when the addon closes.
        /// </summary>
        public IDataAccessProvider? DataAccessProvider { get; private set; }

        public override void Initialize(CancellationToken cancellationToken)
        {
            _logger = ServiceProvider.GetRequiredService<ILogger<OpcUaServerAddon>>();

            try
            {
                OpcUaServerOptions options = ReadOptions();

                IDataAccessProvider dataAccessProvider = CreateDataAccessProvider(options);
                DataAccessProvider = dataAccessProvider;

                _serverHost = new OpcUaServerHost(options, dataAccessProvider, Dispatcher!, ServiceProvider.GetRequiredService<ILoggerFactory>());
                _ = _serverHost.StartAsync();
            }
            catch (Exception ex)
            {
                _initializeError = ex.Message;
                _logger.LogError(ex, "OPC UA server addon initialization error.");
            }

            base.Initialize(cancellationToken);
        }

        public override void Close()
        {
            base.Close();

            OpcUaServerHost? serverHost = _serverHost;
            _serverHost = null;
            if (serverHost is not null)
            {
                try
                {
                    serverHost.Stop();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "OPC UA server stop error.");
                }
            }

            IDataAccessProvider? dataAccessProvider = DataAccessProvider;
            DataAccessProvider = null;
            if (dataAccessProvider is not null)
            {
                var t = dataAccessProvider.CloseAsync();
            }
        }

        public override Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            OpcUaServerHost? serverHost = _serverHost;
            if (serverHost is not null && serverHost.IsRunning)
            {
                if (nowUtc - _lastCleanupUtc >= CleanupPeriod)
                {
                    _lastCleanupUtc = nowUtc;
                    try
                    {
                        serverHost.NodeManager?.Cleanup(nowUtc);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "OPC UA node manager cleanup error.");
                    }
                }

                if (DataAccessProvider?.IsConnected == true)
                    LastWorkTimeUtc = nowUtc;
            }

            return Task.CompletedTask;
        }

        public override Task<AddonStatus> GetAddonStatusAsync()
        {
            AddonStatus addonStatus = new()
            {
                AddonGuid = Guid,
                AddonIdentifier = Identifier,
                AddonDesc = Desc,
                AddonInstanceId = InstanceId,
                LastWorkTimeUtc = LastWorkTimeUtc
            };

            OpcUaServerHost? serverHost = _serverHost;
            if (!IsInitialized || serverHost is null)
            {
                addonStatus.StateCode = _initializeError is null ? AddonStateCodes.STATE_INITIALIZING : AddonStateCodes.STATE_NOT_OPERATIONAL;
                addonStatus.Label = _initializeError is null ? Properties.Resources.Status_ServerStarting : Properties.Resources.Status_ServerFailed + @": " + _initializeError;
            }
            else if (serverHost.LastError is not null)
            {
                addonStatus.StateCode = AddonStateCodes.STATE_NOT_OPERATIONAL;
                addonStatus.Label = Properties.Resources.Status_ServerFailed + @": " + serverHost.LastError;
            }
            else if (!serverHost.IsRunning)
            {
                addonStatus.StateCode = AddonStateCodes.STATE_INITIALIZING;
                addonStatus.Label = Properties.Resources.Status_ServerStarting;
            }
            else if (DataAccessProvider is null || !DataAccessProvider.IsConnected)
            {
                addonStatus.StateCode = AddonStateCodes.STATE_NOT_OPERATIONAL;
                addonStatus.Label = Properties.Resources.Status_DataAccessProviderIsNotConnected;
            }
            else
            {
                addonStatus.StateCode = AddonStateCodes.STATE_OPERATIONAL;
                addonStatus.Label = Properties.Resources.Status_Operational;
            }

            return Task.FromResult(addonStatus);
        }

        #endregion

        #region private functions

        private OpcUaServerOptions ReadOptions()
        {
            var options = new OpcUaServerOptions();

            string endpointUrl = (OptionsSubstituted.TryGetValue(EndpointUrl_OptionName) ?? @"").Trim();
            if (endpointUrl != @"")
                options.EndpointUrl = endpointUrl;

            // Empty system name is the utility context of the central server.
            string systemNameToConnect = (OptionsSubstituted.TryGetValue(SystemNameToConnect_OptionName) ?? @"").Trim();
            options.SystemNameToConnect = systemNameToConnect;
            options.RootFolderName = systemNameToConnect == @"" ? UtilityRootFolderName : systemNameToConnect;
            options.NamespaceUri = @"urn:ssz:Ssz.Dcs.CentralServer:" + Uri.EscapeDataString(options.RootFolderName);

            options.SecurityPolicies.AddRange(
                CsvHelper.ParseCsvLine(@",", OptionsSubstituted.TryGetValue(SecurityPolicies_OptionName))
                    .Select(p => (p ?? @"").Trim())
                    .Where(p => p != @""));

            options.AllowAnonymous = new Any(OptionsSubstituted.TryGetValue(AllowAnonymous_OptionName) ?? @"true").ValueAsBoolean(false);
            options.UserName = (OptionsSubstituted.TryGetValue(UserName_OptionName) ?? @"").Trim();
            options.Password = OptionsSubstituted.TryGetValue(Password_OptionName) ?? @"";
            options.AutoAcceptClientCertificates = new Any(OptionsSubstituted.TryGetValue(AutoAcceptClientCertificates_OptionName) ?? @"true").ValueAsBoolean(false);

            string pkiDirectory = (OptionsSubstituted.TryGetValue(PkiDirectory_OptionName) ?? @"").Trim();
            if (pkiDirectory == @"")
                pkiDirectory = @"OpcUa\pki";
            pkiDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(pkiDirectory));
            // Every addon instance is a separate OPC UA application with its own certificate.
            if (!String.IsNullOrEmpty(InstanceId))
                pkiDirectory = Path.Combine(pkiDirectory, InstanceId);
            options.PkiDirectory = pkiDirectory;

            int idleUnsubscribeSeconds = new Any(OptionsSubstituted.TryGetValue(IdleUnsubscribeSeconds_OptionName) ?? @"").ValueAsInt32(false);
            if (idleUnsubscribeSeconds <= 0)
                idleUnsubscribeSeconds = 120;
            options.IdleUnsubscribeTimeout = TimeSpan.FromSeconds(idleUnsubscribeSeconds);

            foreach (var kvp in CsvDb.GetData(NodesCsvFileName))
            {
                if (kvp.Key.StartsWith('!'))
                    continue; // Header line

                string elementId = (CsvDb.GetValue(kvp.Value, 0) ?? @"").Trim();
                if (elementId == @"")
                    continue;

                options.StaticNodes.Add(new StaticNodeInfo
                {
                    ElementId = elementId,
                    Path = CsvDb.GetValue(kvp.Value, 1) ?? @"",
                    DisplayName = CsvDb.GetValue(kvp.Value, 2) ?? @"",
                    Description = CsvDb.GetValue(kvp.Value, 3) ?? @""
                });
            }

            return options;
        }

        /// <summary>
        ///     The addon is a regular client of the central server, connected in-process.
        /// </summary>
        private IDataAccessProvider CreateDataAccessProvider(OpcUaServerOptions options)
        {
            CaseInsensitiveOrderedDictionary<string?> contextParams = NameValueCollectionHelper.Parse(OptionsSubstituted.TryGetValue(ContextParams_OptionName));

            var dataAccessProvider = ActivatorUtilities.CreateInstance<GrpcDataAccessProvider>(ServiceProvider);

            var elementIdsMap = ActivatorUtilities.CreateInstance<ElementIdsMap>(ServiceProvider);
            elementIdsMap.Initialize(CsvDb.GetData(ElementIdsMap.StandardMapFileName), CsvDb.GetData(ElementIdsMap.StandardTagsFileName), CsvDb);

            dataAccessProvider.ValueSubscriptionsUpdated += (s, e) =>
            {
                if (dataAccessProvider.IsConnected)
                    LastWorkTimeUtc = DateTime.UtcNow;
            };

            // In-process connection to the central server, when hosted by it.
            DataAccessServerWorkerBase? localDataAccessServerWorker = ServiceProvider.GetService<DataAccessServerWorkerBase>();

            string serverAddress = ConfigurationHelper.GetValue<string>(Configuration, @"Kestrel:Endpoints:HttpsDefaultCert:Url", @"")
                .Replace(@"*", @"localhost");
            if (serverAddress == @"")
                serverAddress = @"https://localhost:60060";

            dataAccessProvider.Initialize(elementIdsMap,
                serverAddress,
                @"Ssz.Dcs.Addons.OpcUaServer",
                ConfigurationHelper.GetThisWorkstationName(Configuration),
                options.SystemNameToConnect,
                contextParams,
                new DataAccessProviderOptions
                {
                    LocalDataAccessServerWorker = localDataAccessServerWorker,
                    DangerousAcceptAnyServerCertificate = true
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
                }
            };

            return dataAccessProvider;
        }

        #endregion

        #region private fields

        private static readonly TimeSpan CleanupPeriod = TimeSpan.FromSeconds(10);

        private ILogger? _logger;

        private OpcUaServerHost? _serverHost;

        private string? _initializeError;

        private DateTime _lastCleanupUtc;

        #endregion
    }
}
