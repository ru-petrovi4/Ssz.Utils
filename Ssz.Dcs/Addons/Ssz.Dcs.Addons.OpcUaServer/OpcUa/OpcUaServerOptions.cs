using System;
using System.Collections.Generic;

namespace Ssz.Dcs.Addons.OpcUaServer.OpcUa
{
    /// <summary>
    ///     Options of one OPC UA server instance, read from addon options.
    /// </summary>
    public sealed class OpcUaServerOptions
    {
        public string EndpointUrl { get; set; } = @"opc.tcp://localhost:4840/SszDcsCentralServer";

        public string SystemNameToConnect { get; set; } = @"";

        /// <summary>
        ///     OPC UA namespace URI of the nodes exposed by this server.
        /// </summary>
        public string NamespaceUri { get; set; } = @"";

        /// <summary>
        ///     Display name of the root folder under the Objects folder.
        /// </summary>
        public string RootFolderName { get; set; } = @"";

        public List<string> SecurityPolicies { get; } = new();

        public bool AllowAnonymous { get; set; } = true;

        public string UserName { get; set; } = @"";

        public string Password { get; set; } = @"";

        public bool AutoAcceptClientCertificates { get; set; } = true;

        /// <summary>
        ///     Absolute path of the PKI root directory.
        /// </summary>
        public string PkiDirectory { get; set; } = @"";

        public TimeSpan IdleUnsubscribeTimeout { get; set; } = TimeSpan.FromSeconds(120);

        /// <summary>
        ///     Nodes defined in nodes.csv of the addon configuration directory.
        /// </summary>
        public List<StaticNodeInfo> StaticNodes { get; } = new();
    }

    public sealed class StaticNodeInfo
    {
        public string ElementId { get; set; } = @"";

        /// <summary>
        ///     Folder path, segments separated by '/'. Empty - root folder.
        /// </summary>
        public string Path { get; set; } = @"";

        public string DisplayName { get; set; } = @"";

        public string Description { get; set; } = @"";
    }
}
