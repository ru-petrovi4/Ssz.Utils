using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Reflection;
using System.Text;

namespace Ssz.Dcs.Addons.OpcUaServer.OpcUa
{
    /// <summary>
    ///     OPC UA server with a single Ssz.Dcs.CentralServer node manager.
    /// </summary>
    internal sealed class SszOpcUaServer : StandardServer
    {
        #region construction and destruction

        public SszOpcUaServer(DcsNodeManagerContext nodeManagerContext)
        {
            _nodeManagerContext = nodeManagerContext;
            _logger = nodeManagerContext.LoggerFactory.CreateLogger<SszOpcUaServer>();
        }

        #endregion

        #region public functions

        public DcsNodeManager? NodeManager { get; private set; }

        #endregion

        #region protected functions

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            NodeManager = new DcsNodeManager(server, configuration, _nodeManagerContext);

            return new MasterNodeManager(server, configuration, null, new INodeManager[] { NodeManager });
        }

        protected override ServerProperties LoadServerProperties()
        {
            Assembly assembly = typeof(SszOpcUaServer).Assembly;
            string version = assembly.GetName().Version?.ToString() ?? @"1.0.0.0";

            return new ServerProperties
            {
                ManufacturerName = @"Ssz",
                ProductName = @"Ssz.Dcs.CentralServer OPC UA Server",
                ProductUri = ProductUri,
                SoftwareVersion = version,
                BuildNumber = version,
                BuildDate = DateTime.UtcNow
            };
        }

        protected override void OnServerStarted(IServerInternal server)
        {
            base.OnServerStarted(server);

            server.SessionManager.ImpersonateUser += OnImpersonateUser;
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Validates user identity tokens of activated sessions.
        /// </summary>
        private void OnImpersonateUser(ISession session, ImpersonateEventArgs args)
        {
            OpcUaServerOptions options = _nodeManagerContext.Options;

            if (args.NewIdentity is UserNameIdentityToken userNameToken)
            {
                string password = userNameToken.DecryptedPassword is byte[] passwordBytes
                    ? Encoding.UTF8.GetString(passwordBytes)
                    : @"";

                if (options.UserName == @"" ||
                    !String.Equals(userNameToken.UserName, options.UserName, StringComparison.InvariantCultureIgnoreCase) ||
                    password != options.Password)
                {
                    _logger.LogWarning("OPC UA session user name/password rejected. User name: {UserName}", userNameToken.UserName);
                    args.IdentityValidationError = ServiceResult.Create(StatusCodes.BadUserAccessDenied, "Invalid user name or password.");
                    return;
                }

                args.Identity = new UserIdentity(userNameToken);
                return;
            }

            if (args.NewIdentity is null || args.NewIdentity is AnonymousIdentityToken)
            {
                if (!options.AllowAnonymous)
                {
                    _logger.LogWarning("OPC UA anonymous session rejected.");
                    args.IdentityValidationError = ServiceResult.Create(StatusCodes.BadIdentityTokenRejected, "Anonymous access is not allowed.");
                    return;
                }

                args.Identity = new UserIdentity();
                return;
            }

            args.IdentityValidationError = ServiceResult.Create(StatusCodes.BadIdentityTokenRejected, "Unsupported user identity token.");
        }

        #endregion

        #region private fields

        public const string ProductUri = @"urn:ssz:Ssz.Dcs.CentralServer:OpcUaServer";

        private readonly DcsNodeManagerContext _nodeManagerContext;

        private readonly ILogger _logger;

        #endregion
    }
}
