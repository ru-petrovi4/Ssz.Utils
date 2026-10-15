using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Configuration;
using Ssz.Utils.DataAccess;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.Addons.OpcUaServer.OpcUa
{
    /// <summary>
    ///     Builds the OPC UA application configuration in code (no XML config file) and controls the server lifetime.
    /// </summary>
    internal sealed class OpcUaServerHost
    {
        #region construction and destruction

        public OpcUaServerHost(OpcUaServerOptions options, IDataAccessProvider dataAccessProvider, Ssz.Utils.IDispatcher dispatcher, ILoggerFactory loggerFactory)
        {
            _options = options;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<OpcUaServerHost>();
            _nodeManagerContext = new DcsNodeManagerContext
            {
                Options = options,
                DataAccessProvider = dataAccessProvider,
                Dispatcher = dispatcher,
                LoggerFactory = loggerFactory
            };
        }

        #endregion

        #region public functions

        public bool IsRunning { get; private set; }

        public string? LastError { get; private set; }

        public DcsNodeManager? NodeManager => _server?.NodeManager;

        public async Task StartAsync()
        {
            try
            {
                var telemetry = new SszTelemetryContext(_loggerFactory);

                ApplicationConfiguration configuration = BuildConfiguration(telemetry);
                await configuration.ValidateAsync(ApplicationType.Server, CancellationToken.None);

                var application = new ApplicationInstance(configuration, telemetry)
                {
                    ApplicationName = configuration.ApplicationName,
                    ApplicationType = ApplicationType.Server
                };

                bool certificateOk = await application.CheckApplicationInstanceCertificatesAsync(true, null, CancellationToken.None);
                if (!certificateOk)
                    throw new InvalidOperationException("Application instance certificate is invalid.");

                var server = new SszOpcUaServer(_nodeManagerContext);
                await application.StartAsync(server);

                _application = application;
                _server = server;
                IsRunning = true;
                LastError = null;

                _logger.LogInformation("OPC UA server started. Endpoints: {Endpoints}",
                    String.Join(@"; ", server.GetEndpoints().Select(e => e.EndpointUrl).Distinct()));
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _logger.LogError(ex, "OPC UA server start failed. Endpoint URL: {EndpointUrl}", _options.EndpointUrl);
            }
        }

        /// <summary>
        ///     Must be called in the addon dispatcher.
        /// </summary>
        public void Stop()
        {
            IsRunning = false;

            try
            {
                _server?.NodeManager?.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OPC UA node manager shutdown error.");
            }

            ApplicationInstance? application = _application;
            _application = null;
            _server = null;
            if (application is not null)
            {
                try
                {
                    application.StopAsync().AsTask().Wait(StopTimeout);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OPC UA server stop error.");
                }
            }

            _logger.LogInformation("OPC UA server stopped.");
        }

        #endregion

        #region private functions

        private ApplicationConfiguration BuildConfiguration(ITelemetryContext telemetry)
        {
            string hostName = Opc.Ua.Utils.GetHostName();
            string applicationName = @"Ssz.Dcs.CentralServer OPC UA Server";
            string pkiDirectory = _options.PkiDirectory;
            Directory.CreateDirectory(pkiDirectory);

            var configuration = new ApplicationConfiguration(telemetry)
            {
                ApplicationName = applicationName,
                ApplicationUri = Opc.Ua.Utils.Format(@"urn:{0}:Ssz.Dcs.CentralServer:OpcUaServer:{1}", hostName, Uri.EscapeDataString(_options.SystemNameToConnect)),
                ProductUri = SszOpcUaServer.ProductUri,
                ApplicationType = ApplicationType.Server,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiDirectory, @"own"),
                        SubjectName = Opc.Ua.Utils.Format(@"CN=Ssz.Dcs.CentralServer, O=Ssz, DC={0}", hostName)
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiDirectory, @"issuer")
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiDirectory, @"trusted")
                    },
                    RejectedCertificateStore = new CertificateStoreIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiDirectory, @"rejected")
                    },
                    AutoAcceptUntrustedCertificates = _options.AutoAcceptClientCertificates,
                    AddAppCertToTrustedStore = false,
                    MinimumCertificateKeySize = 2048,
                    RejectSHA1SignedCertificates = true,
                    SendCertificateChain = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 120000,
                    MaxStringLength = 4 * 1024 * 1024,
                    MaxByteStringLength = 4 * 1024 * 1024,
                    MaxArrayLength = 1024 * 1024,
                    MaxMessageSize = 16 * 1024 * 1024,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                },
                ServerConfiguration = new ServerConfiguration
                {
                    BaseAddresses = new StringCollection { _options.EndpointUrl },
                    SecurityPolicies = BuildSecurityPolicies(),
                    UserTokenPolicies = BuildUserTokenPolicies(),
                    DiagnosticsEnabled = false,
                    MaxSessionCount = 100,
                    MinSessionTimeout = 10000,
                    MaxSessionTimeout = 3600000,
                    MaxBrowseContinuationPoints = 10,
                    MaxQueryContinuationPoints = 10,
                    MaxHistoryContinuationPoints = 100,
                    MaxRequestAge = 600000,
                    MinPublishingInterval = 100,
                    MaxPublishingInterval = 3600000,
                    PublishingResolution = 50,
                    MaxSubscriptionLifetime = 3600000,
                    MaxMessageQueueSize = 100,
                    MaxNotificationQueueSize = 1000,
                    MaxNotificationsPerPublish = 10000,
                    MinMetadataSamplingInterval = 1000,
                    MaxRegistrationInterval = 0,
                    MaxEventQueueSize = 10000,
                    MaxSubscriptionCount = 1000,
                    MaxPublishRequestCount = 20,
                    MinSubscriptionLifetime = 10000,
                    ServerCapabilities = new StringCollection { @"DA", @"AC", @"HD", @"HE" }
                }
            };

            return configuration;
        }

        private ServerSecurityPolicyCollection BuildSecurityPolicies()
        {
            var result = new ServerSecurityPolicyCollection();

            foreach (string policyName in _options.SecurityPolicies)
            {
                if (String.Equals(policyName, @"None", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Add(new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None });
                    continue;
                }

                string? policyUri = policyName switch
                {
                    _ when String.Equals(policyName, @"Basic256Sha256", StringComparison.InvariantCultureIgnoreCase) => SecurityPolicies.Basic256Sha256,
                    _ when String.Equals(policyName, @"Aes128_Sha256_RsaOaep", StringComparison.InvariantCultureIgnoreCase) => SecurityPolicies.Aes128_Sha256_RsaOaep,
                    _ when String.Equals(policyName, @"Aes256_Sha256_RsaPss", StringComparison.InvariantCultureIgnoreCase) => SecurityPolicies.Aes256_Sha256_RsaPss,
                    _ when String.Equals(policyName, @"Basic256", StringComparison.InvariantCultureIgnoreCase) => SecurityPolicies.Basic256,
                    _ when String.Equals(policyName, @"Basic128Rsa15", StringComparison.InvariantCultureIgnoreCase) => SecurityPolicies.Basic128Rsa15,
                    _ => null
                };

                if (policyUri is null)
                {
                    _logger.LogWarning("Unknown OPC UA security policy ignored: {PolicyName}", policyName);
                    continue;
                }

                result.Add(new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.Sign, SecurityPolicyUri = policyUri });
                result.Add(new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.SignAndEncrypt, SecurityPolicyUri = policyUri });
            }

            if (result.Count == 0)
            {
                result.Add(new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None });
                result.Add(new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.SignAndEncrypt, SecurityPolicyUri = SecurityPolicies.Basic256Sha256 });
            }

            return result;
        }

        private UserTokenPolicyCollection BuildUserTokenPolicies()
        {
            var result = new UserTokenPolicyCollection();

            if (_options.AllowAnonymous)
                result.Add(new UserTokenPolicy(UserTokenType.Anonymous));

            if (_options.UserName != @"")
                result.Add(new UserTokenPolicy(UserTokenType.UserName));

            if (result.Count == 0)
            {
                _logger.LogWarning("Neither anonymous access nor user name is configured for the OPC UA server; anonymous access is enabled.");
                result.Add(new UserTokenPolicy(UserTokenType.Anonymous));
            }

            return result;
        }

        #endregion

        #region private fields

        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(15);

        private readonly OpcUaServerOptions _options;

        private readonly ILoggerFactory _loggerFactory;

        private readonly ILogger _logger;

        private readonly DcsNodeManagerContext _nodeManagerContext;

        private ApplicationInstance? _application;

        private SszOpcUaServer? _server;

        #endregion
    }
}
