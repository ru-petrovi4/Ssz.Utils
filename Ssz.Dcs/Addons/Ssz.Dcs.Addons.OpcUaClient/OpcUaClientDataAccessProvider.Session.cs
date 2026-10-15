using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SszStatusCodes = Ssz.Utils.StatusCodes;

namespace Ssz.Dcs.Addons.OpcUaClient
{
    /// <summary>
    ///     OPC UA session, subscription and monitored items. Everything here runs in the working task,
    ///     except the notification callbacks of the stack, which only read the nodes under _nodesSyncRoot.
    /// </summary>
    public partial class OpcUaClientDataAccessProvider
    {
        #region private functions

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            OpcUaClientOptions options = _options ?? throw new InvalidOperationException();

            _telemetry ??= new SszTelemetryContext(_loggerFactory);
            _applicationConfiguration ??= await CreateApplicationConfigurationAsync(options, _telemetry, cancellationToken);

            EndpointDescription? endpointDescription = await CoreClientUtils.SelectEndpointAsync(_applicationConfiguration, options.EndpointUrl, options.UseSecurity, _telemetry, cancellationToken);
            if (endpointDescription is null)
                throw new InvalidOperationException("No suitable endpoint found: " + options.EndpointUrl);
            var configuredEndpoint = new ConfiguredEndpoint(null, endpointDescription, EndpointConfiguration.Create(_applicationConfiguration));

            IUserIdentity identity = options.UserName == @""
                ? new UserIdentity()
                : new UserIdentity(options.UserName, Encoding.UTF8.GetBytes(options.Password));

            _sessionFactory ??= new DefaultSessionFactory(_telemetry);
            ISession session = await _sessionFactory.CreateAsync(_applicationConfiguration, configuredEndpoint, false, ClientApplicationName, options.SessionTimeoutMs, identity, null, cancellationToken);
            session.KeepAlive += OnSessionKeepAlive;
            session.SessionClosing += OnSessionClosing;
            session.DeleteSubscriptionsOnClose = true;

            _session = session;
            Volatile.Write(ref _sessionFailed, false);

            ResolveDefaultNamespaceIndex(session, options);

            await CreateSubscriptionAsync(session, options, cancellationToken);

            IsConnectedEventWaitHandle.Set();
            IDispatcher? callbackDispatcher = CallbackDispatcher;
            if (callbackDispatcher is not null)
            {
                try
                {
                    callbackDispatcher.BeginInvoke(ct => { IsConnected = true; });
                }
                catch (Exception)
                {
                }
            }

            LoggersSet.Logger.LogInformation("OPC UA client connected to {EndpointUrl}; security: {SecurityMode} {SecurityPolicy}; user: {User}",
                endpointDescription.EndpointUrl, endpointDescription.SecurityMode, endpointDescription.SecurityPolicyUri, options.UserName == @"" ? "Anonymous" : options.UserName);
        }

        private async Task DisconnectAsync(bool notifySubscribers)
        {
            IsConnectedEventWaitHandle.Reset();

            IDispatcher? callbackDispatcher = CallbackDispatcher;
            if (callbackDispatcher is not null)
            {
                try
                {
                    callbackDispatcher.BeginInvoke(ct => { IsConnected = false; });
                }
                catch (Exception)
                {
                }
            }

            ISession? session = _session;
            _session = null;
            _subscription = null;
            _eventMonitoredItem = null;
            _eventMonitoredItemFailed = false;
            _defaultNamespaceIndex = null;

            object[] clientObjs;
            lock (_nodesSyncRoot)
            {
                foreach (OpcUaNode node in _nodes.Values)
                {
                    node.MonitoredItem = null;
                    node.NodeId = null;
                    node.Failed = false;
                    node.HasValue = false;
                }
                _nodesByClientHandle.Clear();
                _monitoredItemsToRemove.Clear();
                clientObjs = _nodesByClientObj.Keys.ToArray();
            }

            if (session is not null)
            {
                try
                {
                    session.KeepAlive -= OnSessionKeepAlive;
                    session.SessionClosing -= OnSessionClosing;
                    await session.CloseAsync(5000, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogDebug(ex, "OPC UA session close error.");
                }
                finally
                {
                    try
                    {
                        session.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }

                LoggersSet.Logger.LogInformation("OPC UA client disconnected from {EndpointUrl}", _options?.EndpointUrl);
            }

            if (notifySubscribers && clientObjs.Length > 0 && callbackDispatcher is not null)
            {
                try
                {
                    callbackDispatcher.BeginInvoke(ct =>
                    {
                        foreach (object clientObj in clientObjs)
                        {
                            ((IValueSubscription)clientObj).Update(new ValueStatusTimestamp { StatusCode = SszStatusCodes.Uncertain });
                        }
                        RaiseValueSubscriptionsUpdated();
                    });
                }
                catch (Exception)
                {
                }
            }
        }

        private void OnSessionKeepAlive(ISession session, KeepAliveEventArgs e)
        {
            if (ServiceResult.IsBad(e.Status))
            {
                LoggersSet.Logger.LogWarning("OPC UA keep alive failed: {Status}", e.Status);
                Volatile.Write(ref _sessionFailed, true);
            }
        }

        private void OnSessionClosing(object? sender, EventArgs e)
        {
            Volatile.Write(ref _sessionFailed, true);
        }

        private static async Task<ApplicationConfiguration> CreateApplicationConfigurationAsync(OpcUaClientOptions options, ITelemetryContext telemetry, CancellationToken cancellationToken)
        {
            string hostName = Opc.Ua.Utils.GetHostName();
            string applicationName = @"Ssz.Dcs.CentralServer OPC UA Client";
            string pkiDirectory = options.PkiDirectory;
            Directory.CreateDirectory(pkiDirectory);

            var configuration = new ApplicationConfiguration(telemetry)
            {
                ApplicationName = applicationName,
                ApplicationUri = Opc.Ua.Utils.Format(@"urn:{0}:Ssz.Dcs.CentralServer:OpcUaClient", hostName),
                ProductUri = @"urn:ssz:Ssz.Dcs.CentralServer:OpcUaClient",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiDirectory, @"own"),
                        SubjectName = Opc.Ua.Utils.Format(@"CN=Ssz.Dcs.OpcUaClient, O=Ssz, DC={0}", hostName)
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
                    AutoAcceptUntrustedCertificates = options.AutoAcceptServerCertificate,
                    AddAppCertToTrustedStore = false,
                    MinimumCertificateKeySize = 2048,
                    RejectSHA1SignedCertificates = true,
                    SendCertificateChain = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 60000,
                    MaxStringLength = 4 * 1024 * 1024,
                    MaxByteStringLength = 4 * 1024 * 1024,
                    MaxArrayLength = 1024 * 1024,
                    MaxMessageSize = 16 * 1024 * 1024,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                },
                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = (int)options.SessionTimeoutMs,
                    MinSubscriptionLifetime = 10000
                }
            };

            await configuration.ValidateAsync(ApplicationType.Client, cancellationToken);

            var application = new ApplicationInstance(configuration, telemetry)
            {
                ApplicationName = applicationName,
                ApplicationType = ApplicationType.Client
            };
            bool certificateOk = await application.CheckApplicationInstanceCertificatesAsync(true, null, cancellationToken);
            if (!certificateOk)
                throw new InvalidOperationException("Application instance certificate is invalid.");

            return configuration;
        }

        private void ResolveDefaultNamespaceIndex(ISession session, OpcUaClientOptions options)
        {
            if (options.DefaultNamespaceUri != @"")
            {
                int index = session.NamespaceUris.GetIndex(options.DefaultNamespaceUri);
                if (index >= 0)
                {
                    _defaultNamespaceIndex = (ushort)index;
                    return;
                }
                LoggersSet.Logger.LogWarning("OPC UA namespace '{NamespaceUri}' is not found on the server, namespace index {NamespaceIndex} is used.", options.DefaultNamespaceUri, options.DefaultNamespaceIndex);
            }
            _defaultNamespaceIndex = options.DefaultNamespaceIndex;
        }

        /// <summary>
        ///     Element id to NodeId. Full NodeId strings (ns=2;s=..., i=..., nsu=...;s=...) are parsed,
        ///     anything else is a string identifier in the default namespace.
        /// </summary>
        private NodeId? ResolveNodeId(string id)
        {
            ISession? session = _session;
            if (session is null)
                return null;

            id = id.Trim();
            if (id == @"")
                return null;

            if (LooksLikeNodeId(id))
            {
                try
                {
                    if (id.StartsWith(@"nsu=", StringComparison.InvariantCultureIgnoreCase))
                        return ExpandedNodeId.ToNodeId(ExpandedNodeId.Parse(id), session.NamespaceUris);
                    return NodeId.Parse(id);
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogWarning(ex, "Invalid OPC UA NodeId: {Id}", id);
                    return null;
                }
            }

            return new NodeId(id, _defaultNamespaceIndex ?? _options?.DefaultNamespaceIndex ?? 2);
        }

        private static bool LooksLikeNodeId(string id)
        {
            return id.StartsWith(@"ns=", StringComparison.InvariantCultureIgnoreCase) ||
                id.StartsWith(@"nsu=", StringComparison.InvariantCultureIgnoreCase) ||
                id.StartsWith(@"i=", StringComparison.InvariantCultureIgnoreCase) ||
                id.StartsWith(@"s=", StringComparison.InvariantCultureIgnoreCase) ||
                id.StartsWith(@"g=", StringComparison.InvariantCultureIgnoreCase) ||
                id.StartsWith(@"b=", StringComparison.InvariantCultureIgnoreCase);
        }

        private async Task CreateSubscriptionAsync(ISession session, OpcUaClientOptions options, CancellationToken cancellationToken)
        {
            ITelemetryContext telemetry = _telemetry ?? throw new InvalidOperationException();
            var subscription = new Subscription(telemetry, new SubscriptionOptions
            {
                DisplayName = ClientApplicationName,
                PublishingInterval = options.PublishingIntervalMs,
                KeepAliveCount = 10,
                LifetimeCount = 1000,
                MaxNotificationsPerPublish = 0,
                PublishingEnabled = true,
                TimestampsToReturn = TimestampsToReturn.Both,
                DisableMonitoredItemCache = true
            });
            subscription.FastDataChangeCallback = OnFastDataChange;
            subscription.FastEventCallback = OnFastEvent;

            session.AddSubscription(subscription);
            await subscription.CreateAsync(cancellationToken);

            _subscription = subscription;

            lock (_nodesSyncRoot)
            {
                foreach (OpcUaNode node in _nodes.Values)
                {
                    node.MonitoredItem = null;
                    node.NodeId = null;
                    node.Failed = false;
                }
            }
        }

        /// <summary>
        ///     Working task. Creates monitored items for new nodes, removes items of removed nodes, creates the event item.
        /// </summary>
        private async Task ApplySubscriptionChangesAsync(CancellationToken cancellationToken)
        {
            ISession? session = _session;
            Subscription? subscription = _subscription;
            OpcUaClientOptions? options = _options;
            ITelemetryContext? telemetry = _telemetry;
            if (session is null || subscription is null || options is null || telemetry is null)
                return;

            var nodesToAdd = new List<OpcUaNode>();
            var itemsToAdd = new List<MonitoredItem>();
            var itemsToRemove = new List<MonitoredItem>();
            var failedNodes = new List<(OpcUaNode Node, uint StatusCode)>();

            lock (_nodesSyncRoot)
            {
                itemsToRemove.AddRange(_monitoredItemsToRemove);
                _monitoredItemsToRemove.Clear();

                foreach (OpcUaNode node in _nodes.Values)
                {
                    if (node.MonitoredItem is not null || node.Failed)
                        continue;

                    NodeId? nodeId = ResolveNodeId(node.Id);
                    if (nodeId is null)
                    {
                        node.Failed = true;
                        failedNodes.Add((node, SszStatusCodes.BadNodeIdInvalid));
                        continue;
                    }

                    var monitoredItem = new MonitoredItem(telemetry, new MonitoredItemOptions
                    {
                        DisplayName = node.Id,
                        StartNodeId = nodeId,
                        AttributeId = Attributes.Value,
                        MonitoringMode = MonitoringMode.Reporting,
                        SamplingInterval = options.SamplingIntervalMs,
                        QueueSize = 1,
                        DiscardOldest = true
                    });
                    node.NodeId = nodeId;
                    node.MonitoredItem = monitoredItem;
                    _nodesByClientHandle[monitoredItem.ClientHandle] = node;
                    nodesToAdd.Add(node);
                    itemsToAdd.Add(monitoredItem);
                }
            }

            bool createEventItem = options.SubscribeToEvents && _eventMonitoredItem is null && !_eventMonitoredItemFailed;
            if (createEventItem)
            {
                NodeId? eventNotifierNodeId = ResolveNodeId(options.EventNotifierNodeId);
                if (eventNotifierNodeId is null)
                {
                    LoggersSet.Logger.LogWarning("Invalid event notifier NodeId: {NodeId}; events are not subscribed.", options.EventNotifierNodeId);
                    createEventItem = false;
                }
                else
                {
                    _eventMonitoredItem = new MonitoredItem(telemetry, new MonitoredItemOptions
                    {
                        DisplayName = @"Events",
                        StartNodeId = eventNotifierNodeId,
                        AttributeId = Attributes.EventNotifier,
                        NodeClass = NodeClass.Object,
                        MonitoringMode = MonitoringMode.Reporting,
                        SamplingInterval = 0,
                        QueueSize = 1000,
                        DiscardOldest = true,
                        Filter = CreateEventFilter()
                    });
                    itemsToAdd.Add(_eventMonitoredItem);
                }
            }

            NotifyFailedNodes(failedNodes);

            if (itemsToAdd.Count == 0 && itemsToRemove.Count == 0)
                return;

            if (itemsToRemove.Count > 0)
                subscription.RemoveItems(itemsToRemove);
            if (itemsToAdd.Count > 0)
                subscription.AddItems(itemsToAdd);

            await subscription.ApplyChangesAsync(cancellationToken);

            failedNodes.Clear();
            lock (_nodesSyncRoot)
            {
                foreach (OpcUaNode node in nodesToAdd)
                {
                    MonitoredItem? monitoredItem = node.MonitoredItem;
                    if (monitoredItem is null)
                        continue;
                    ServiceResult? error = monitoredItem.Status?.Error;
                    if (monitoredItem.Created && (error is null || ServiceResult.IsGood(error)))
                        continue;

                    uint statusCode = error is not null && ServiceResult.IsBad(error) ? error.Code : SszStatusCodes.BadNodeIdUnknown;
                    LoggersSet.Logger.LogWarning("OPC UA monitored item creation failed. Id: {Id}, NodeId: {NodeId}, status: {Status}", node.Id, node.NodeId, error);
                    subscription.RemoveItem(monitoredItem);
                    _nodesByClientHandle.TryRemove(monitoredItem.ClientHandle, out _);
                    node.MonitoredItem = null;
                    node.Failed = true;
                    failedNodes.Add((node, statusCode));
                }
            }
            NotifyFailedNodes(failedNodes);

            if (createEventItem && _eventMonitoredItem is not null)
            {
                ServiceResult? error = _eventMonitoredItem.Status?.Error;
                if (!_eventMonitoredItem.Created || (error is not null && ServiceResult.IsBad(error)))
                {
                    LoggersSet.Logger.LogWarning("OPC UA event monitored item creation failed. NodeId: {NodeId}, status: {Status}", options.EventNotifierNodeId, error);
                    subscription.RemoveItem(_eventMonitoredItem);
                    _eventMonitoredItem = null;
                    // Not retried until reconnect.
                    _eventMonitoredItemFailed = true;
                }
                else
                {
                    try
                    {
                        await subscription.ConditionRefreshAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        LoggersSet.Logger.LogDebug(ex, "OPC UA ConditionRefresh error.");
                    }
                }
            }
        }

        private void NotifyFailedNodes(List<(OpcUaNode Node, uint StatusCode)> failedNodes)
        {
            if (failedNodes.Count == 0)
                return;

            var updates = new List<(object[] ClientObjs, ValueStatusTimestamp ValueStatusTimestamp)>();
            lock (_nodesSyncRoot)
            {
                foreach (var (node, statusCode) in failedNodes)
                {
                    var vst = new ValueStatusTimestamp { StatusCode = statusCode, TimestampUtc = DateTime.UtcNow };
                    node.LastValueStatusTimestamp = vst;
                    node.HasValue = true;
                    updates.Add((node.ClientObjs.ToArray(), vst));
                }
            }
            DispatchUpdates(updates);
        }

        /// <summary>
        ///     Working task.
        /// </summary>
        private void AddClientObj(string id, object clientObj)
        {
            ValueStatusTimestamp? knownValue = null;
            lock (_nodesSyncRoot)
            {
                if (_nodesByClientObj.ContainsKey(clientObj))
                    return;

                if (!_nodes.TryGetValue(id, out OpcUaNode? node))
                {
                    node = new OpcUaNode(id);
                    _nodes[id] = node;
                }
                node.ClientObjs.Add(clientObj);
                _nodesByClientObj[clientObj] = node;
                if (node.HasValue)
                    knownValue = node.LastValueStatusTimestamp;
            }

            if (knownValue is not null)
                DispatchUpdates(new List<(object[], ValueStatusTimestamp)> { (new[] { clientObj }, knownValue.Value) });
        }

        /// <summary>
        ///     Working task.
        /// </summary>
        private void RemoveClientObj(object clientObj)
        {
            lock (_nodesSyncRoot)
            {
                if (!_nodesByClientObj.Remove(clientObj, out OpcUaNode? node))
                    return;
                node.ClientObjs.Remove(clientObj);
                if (node.ClientObjs.Count > 0)
                    return;
                _nodes.Remove(node.Id);
                if (node.MonitoredItem is not null)
                {
                    _nodesByClientHandle.TryRemove(node.MonitoredItem.ClientHandle, out _);
                    _monitoredItemsToRemove.Add(node.MonitoredItem);
                    node.MonitoredItem = null;
                }
            }
        }

        /// <summary>
        ///     Stack thread.
        /// </summary>
        private void OnFastDataChange(Subscription subscription, DataChangeNotification notification, IList<string> stringTable)
        {
            if (notification.MonitoredItems is null || notification.MonitoredItems.Count == 0)
                return;

            var updates = new List<(object[] ClientObjs, ValueStatusTimestamp ValueStatusTimestamp)>(notification.MonitoredItems.Count);
            lock (_nodesSyncRoot)
            {
                foreach (MonitoredItemNotification monitoredItemNotification in notification.MonitoredItems)
                {
                    if (!_nodesByClientHandle.TryGetValue(monitoredItemNotification.ClientHandle, out OpcUaNode? node))
                        continue;

                    DataValue? dataValue = monitoredItemNotification.Value;
                    ValueStatusTimestamp vst = OpcUaValueHelper.ToValueStatusTimestamp(dataValue);
                    node.LastValueStatusTimestamp = vst;
                    node.HasValue = true;
                    if (dataValue is not null)
                    {
                        TypeInfo? typeInfo = dataValue.WrappedValue.TypeInfo;
                        if (typeInfo is not null && typeInfo.BuiltInType != BuiltInType.Null && typeInfo.ValueRank == ValueRanks.Scalar)
                            node.BuiltInType = typeInfo.BuiltInType;
                    }
                    updates.Add((node.ClientObjs.ToArray(), vst));
                }
            }

            DispatchUpdates(updates);
        }

        private void DispatchUpdates(List<(object[] ClientObjs, ValueStatusTimestamp ValueStatusTimestamp)> updates)
        {
            if (updates.Count == 0)
                return;

            IDispatcher? callbackDispatcher = CallbackDispatcher;
            if (callbackDispatcher is null)
                return;

            try
            {
                callbackDispatcher.BeginInvoke(ct =>
                {
                    foreach (var (clientObjs, vst) in updates)
                    {
                        foreach (object clientObj in clientObjs)
                        {
                            try
                            {
                                ((IValueSubscription)clientObj).Update(vst);
                            }
                            catch (Exception ex)
                            {
                                LoggersSet.Logger.LogDebug(ex, "ValueSubscription.Update(...) exception.");
                            }
                        }
                    }
                    RaiseValueSubscriptionsUpdated();
                });
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     Working task. Returns failed client objects and their results.
        /// </summary>
        private async Task<(object[] FailedClientObjs, ResultInfo[] ResultInfos)> WriteNodesAsync(object[] clientObjs, ValueStatusTimestamp[] valueStatusTimestamps, CancellationToken cancellationToken)
        {
            ISession? session = _session;
            if (session is null)
                return (clientObjs, Enumerable.Repeat(new ResultInfo { StatusCode = SszStatusCodes.BadServerNotConnected }, clientObjs.Length).ToArray());

            var failedClientObjs = new List<object>();
            var resultInfos = new List<ResultInfo>();
            var nodesToWrite = new WriteValueCollection();
            var nodesToWriteClientObjs = new List<object>();

            for (int i = 0; i < clientObjs.Length; i++)
            {
                object clientObj = clientObjs[i];
                OpcUaNode? node;
                lock (_nodesSyncRoot)
                {
                    _nodesByClientObj.TryGetValue(clientObj, out node);
                }
                NodeId? nodeId = node?.NodeId ?? (node is not null ? ResolveNodeId(node.Id) : null);
                if (node is null || nodeId is null)
                {
                    failedClientObjs.Add(clientObj);
                    resultInfos.Add(new ResultInfo { StatusCode = SszStatusCodes.BadNodeIdUnknown });
                    continue;
                }

                object? value = await ConvertForWriteAsync(session, node, nodeId, valueStatusTimestamps[i].Value, cancellationToken);
                nodesToWrite.Add(new WriteValue
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant(value))
                });
                nodesToWriteClientObjs.Add(clientObj);
            }

            if (nodesToWrite.Count > 0)
            {
                try
                {
                    WriteResponse response = await session.WriteAsync(null, nodesToWrite, cancellationToken);
                    for (int i = 0; i < nodesToWriteClientObjs.Count; i++)
                    {
                        StatusCode statusCode = i < response.Results.Count ? response.Results[i] : Opc.Ua.StatusCodes.BadUnexpectedError;
                        if (StatusCode.IsGood(statusCode))
                            continue;
                        LoggersSet.Logger.LogWarning("OPC UA write failed. NodeId: {NodeId}, status: {Status}", nodesToWrite[i].NodeId, statusCode);
                        failedClientObjs.Add(nodesToWriteClientObjs[i]);
                        resultInfos.Add(new ResultInfo { StatusCode = statusCode.Code, Info = OpcUaValueHelper.ToSymbolicId(statusCode.Code) });
                    }
                }
                catch when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogWarning(ex, "OPC UA write error.");
                    MarkSessionFailedIfNeeded(ex);
                    foreach (object clientObj in nodesToWriteClientObjs)
                    {
                        failedClientObjs.Add(clientObj);
                        resultInfos.Add(new ResultInfo { StatusCode = SszStatusCodes.BadCommunicationError, Info = ex.Message });
                    }
                }
            }

            return (failedClientObjs.ToArray(), resultInfos.ToArray());
        }

        /// <summary>
        ///     Servers reject writes of a wrong built-in type, so the value is cast to the type of the variable
        ///     (known from the last received value or read from the DataType attribute).
        /// </summary>
        private async Task<object?> ConvertForWriteAsync(ISession session, OpcUaNode node, NodeId nodeId, Any value, CancellationToken cancellationToken)
        {
            BuiltInType? builtInType;
            lock (_nodesSyncRoot)
            {
                builtInType = node.BuiltInType;
            }

            if (builtInType is null)
            {
                try
                {
                    ReadResponse response = await session.ReadAsync(null, 0, TimestampsToReturn.Neither,
                        new ReadValueIdCollection { new ReadValueId { NodeId = nodeId, AttributeId = Attributes.DataType } }, cancellationToken);
                    if (response.Results.Count > 0 && response.Results[0].Value is NodeId dataTypeId)
                    {
                        builtInType = TypeInfo.GetBuiltInType(dataTypeId, session.TypeTree);
                        lock (_nodesSyncRoot)
                        {
                            node.BuiltInType = builtInType;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogDebug(ex, "OPC UA DataType read error. NodeId: {NodeId}", nodeId);
                }
            }

            return OpcUaValueHelper.ToOpcUaValue(value, builtInType);
        }

        private void MarkSessionFailedIfNeeded(Exception ex)
        {
            if (ex is ServiceResultException serviceResultException)
            {
                switch (serviceResultException.StatusCode)
                {
                    case Opc.Ua.StatusCodes.BadSessionIdInvalid:
                    case Opc.Ua.StatusCodes.BadSessionClosed:
                    case Opc.Ua.StatusCodes.BadSessionNotActivated:
                    case Opc.Ua.StatusCodes.BadSecureChannelIdInvalid:
                    case Opc.Ua.StatusCodes.BadSecureChannelClosed:
                    case Opc.Ua.StatusCodes.BadConnectionClosed:
                    case Opc.Ua.StatusCodes.BadServerNotConnected:
                    case Opc.Ua.StatusCodes.BadNotConnected:
                    case Opc.Ua.StatusCodes.BadCommunicationError:
                    case Opc.Ua.StatusCodes.BadTimeout:
                        Volatile.Write(ref _sessionFailed, true);
                        break;
                }
            }
        }

        #endregion

        #region private fields

        private ITelemetryContext? _telemetry;

        private ApplicationConfiguration? _applicationConfiguration;

        private DefaultSessionFactory? _sessionFactory;

        private ISession? _session;

        private bool _sessionFailed;

        private Subscription? _subscription;

        private MonitoredItem? _eventMonitoredItem;

        private bool _eventMonitoredItemFailed;

        private ushort? _defaultNamespaceIndex;

        private readonly object _nodesSyncRoot = new();

        /// <summary>
        ///     [Id on the server, Node]
        /// </summary>
        private readonly Dictionary<string, OpcUaNode> _nodes = new();

        private readonly Dictionary<object, OpcUaNode> _nodesByClientObj = new(ReferenceEqualityComparer<object>.Default);

        private readonly ConcurrentDictionary<uint, OpcUaNode> _nodesByClientHandle = new();

        private readonly List<MonitoredItem> _monitoredItemsToRemove = new();

        #endregion

        /// <summary>
        ///     One variable on the OPC UA server, shared by all client objects subscribed to the same id.
        /// </summary>
        private sealed class OpcUaNode
        {
            public OpcUaNode(string id)
            {
                Id = id;
            }

            public string Id { get; }

            public NodeId? NodeId { get; set; }

            public MonitoredItem? MonitoredItem { get; set; }

            /// <summary>
            ///     Monitored item creation failed, not retried until reconnect.
            /// </summary>
            public bool Failed { get; set; }

            public HashSet<object> ClientObjs { get; } = new(ReferenceEqualityComparer<object>.Default);

            public bool HasValue { get; set; }

            public ValueStatusTimestamp LastValueStatusTimestamp { get; set; }

            public BuiltInType? BuiltInType { get; set; }
        }
    }
}
