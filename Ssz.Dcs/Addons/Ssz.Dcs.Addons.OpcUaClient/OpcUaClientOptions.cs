using Ssz.Utils;
using System;
using System.IO;

namespace Ssz.Dcs.Addons.OpcUaClient
{
    /// <summary>
    ///     Connection options of the OPC UA client, parsed from the data access provider context params
    ///     (the addon passes its options as context params).
    /// </summary>
    public sealed class OpcUaClientOptions
    {
        public string EndpointUrl { get; set; } = @"";

        public bool UseSecurity { get; set; } = true;

        public string UserName { get; set; } = @"";

        public string Password { get; set; } = @"";

        public bool AutoAcceptServerCertificate { get; set; } = true;

        /// <summary>
        ///     Absolute path of the PKI root directory.
        /// </summary>
        public string PkiDirectory { get; set; } = @"";

        public string DefaultNamespaceUri { get; set; } = @"";

        public ushort DefaultNamespaceIndex { get; set; } = 2;

        public int PublishingIntervalMs { get; set; } = 500;

        public int SamplingIntervalMs { get; set; } = 250;

        public bool SubscribeToEvents { get; set; } = true;

        public string EventNotifierNodeId { get; set; } = @"i=2253";

        public uint SessionTimeoutMs { get; set; } = 60000;

        public static OpcUaClientOptions FromContextParams(CaseInsensitiveOrderedDictionary<string?> contextParams, string serverAddress)
        {
            var options = new OpcUaClientOptions();

            options.EndpointUrl = (contextParams.TryGetValue(OpcUaClientAddon.EndpointUrl_OptionName) ?? @"").Trim();
            if (options.EndpointUrl == @"")
                options.EndpointUrl = serverAddress.Trim();

            options.UseSecurity = new Any(contextParams.TryGetValue(OpcUaClientAddon.UseSecurity_OptionName) ?? @"true").ValueAsBoolean(false);
            options.UserName = (contextParams.TryGetValue(OpcUaClientAddon.UserName_OptionName) ?? @"").Trim();
            options.Password = contextParams.TryGetValue(OpcUaClientAddon.Password_OptionName) ?? @"";
            options.AutoAcceptServerCertificate = new Any(contextParams.TryGetValue(OpcUaClientAddon.AutoAcceptServerCertificate_OptionName) ?? @"true").ValueAsBoolean(false);

            string pkiDirectory = (contextParams.TryGetValue(OpcUaClientAddon.PkiDirectory_OptionName) ?? @"").Trim();
            if (pkiDirectory == @"")
                pkiDirectory = @"OpcUa\pki";
            options.PkiDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(pkiDirectory));

            options.DefaultNamespaceUri = (contextParams.TryGetValue(OpcUaClientAddon.DefaultNamespaceUri_OptionName) ?? @"").Trim();
            int defaultNamespaceIndex = new Any(contextParams.TryGetValue(OpcUaClientAddon.DefaultNamespaceIndex_OptionName) ?? @"2").ValueAsInt32(false);
            options.DefaultNamespaceIndex = defaultNamespaceIndex >= 0 && defaultNamespaceIndex <= UInt16.MaxValue ? (ushort)defaultNamespaceIndex : (ushort)2;

            int publishingIntervalMs = new Any(contextParams.TryGetValue(OpcUaClientAddon.PublishingIntervalMs_OptionName) ?? @"").ValueAsInt32(false);
            if (publishingIntervalMs > 0)
                options.PublishingIntervalMs = publishingIntervalMs;
            int samplingIntervalMs = new Any(contextParams.TryGetValue(OpcUaClientAddon.SamplingIntervalMs_OptionName) ?? @"").ValueAsInt32(false);
            if (samplingIntervalMs > 0)
                options.SamplingIntervalMs = samplingIntervalMs;

            options.SubscribeToEvents = new Any(contextParams.TryGetValue(OpcUaClientAddon.SubscribeToEvents_OptionName) ?? @"true").ValueAsBoolean(false);
            string eventNotifierNodeId = (contextParams.TryGetValue(OpcUaClientAddon.EventNotifierNodeId_OptionName) ?? @"").Trim();
            if (eventNotifierNodeId != @"")
                options.EventNotifierNodeId = eventNotifierNodeId;

            int sessionTimeoutMs = new Any(contextParams.TryGetValue(OpcUaClientAddon.SessionTimeoutMs_OptionName) ?? @"").ValueAsInt32(false);
            if (sessionTimeoutMs > 0)
                options.SessionTimeoutMs = (uint)sessionTimeoutMs;

            return options;
        }
    }
}
