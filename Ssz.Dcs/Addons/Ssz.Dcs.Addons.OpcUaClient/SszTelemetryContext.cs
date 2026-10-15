using Microsoft.Extensions.Logging;
using Opc.Ua;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Ssz.Dcs.Addons.OpcUaClient
{
    /// <summary>
    ///     Routes OPC UA stack logging into the host logging infrastructure.
    /// </summary>
    internal sealed class SszTelemetryContext : ITelemetryContext
    {
        public SszTelemetryContext(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
        }

        public ILoggerFactory LoggerFactory { get; }

        public ActivitySource ActivitySource { get; } = new ActivitySource(@"Ssz.Dcs.Addons.OpcUaClient");

        public Meter CreateMeter()
        {
            return new Meter(@"Ssz.Dcs.Addons.OpcUaClient");
        }
    }
}
