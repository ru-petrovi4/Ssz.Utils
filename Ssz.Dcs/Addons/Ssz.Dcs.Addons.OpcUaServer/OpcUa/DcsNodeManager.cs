using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SszStatusCodes = Ssz.Utils.StatusCodes;
using UaStatusCodes = Opc.Ua.StatusCodes;

namespace Ssz.Dcs.Addons.OpcUaServer.OpcUa
{
    /// <summary>
    ///     Everything the node manager needs from the addon.
    /// </summary>
    internal sealed class DcsNodeManagerContext
    {
        public required OpcUaServerOptions Options { get; init; }

        public required IDataAccessProvider DataAccessProvider { get; init; }

        /// <summary>
        ///     Dispatcher of the data access provider callbacks (the addon dispatcher).
        ///     All calls to the data access provider value subscriptions are made in this dispatcher.
        /// </summary>
        public required IDispatcher Dispatcher { get; init; }

        public required ILoggerFactory LoggerFactory { get; init; }
    }

    /// <summary>
    ///     OPC UA address space of one Ssz.Dcs.CentralServer system (DCS or process modeling session).
    ///     Tags are exposed as variables (NodeId = ns=X;s=ElementId), events as OPC UA events and alarm conditions,
    ///     values and events journals as OPC UA history.
    ///     Threading: the OPC UA stack calls this class from its own threads, the data access provider calls back
    ///     in the addon dispatcher. Node state is protected by <see cref="CustomNodeManager2.Lock"/>.
    /// </summary>
    internal sealed partial class DcsNodeManager : CustomNodeManager2
    {
        #region construction and destruction

        public DcsNodeManager(IServerInternal server, ApplicationConfiguration configuration, DcsNodeManagerContext context)
            : base(server, configuration, context.LoggerFactory.CreateLogger<DcsNodeManager>(), context.Options.NamespaceUri)
        {
            _options = context.Options;
            _dataAccessProvider = context.DataAccessProvider;
            _dispatcher = context.Dispatcher;
            _logger = context.LoggerFactory.CreateLogger<DcsNodeManager>();
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Must be called in the addon dispatcher. Unsubscribes all tags from the data access provider.
        /// </summary>
        public void Shutdown()
        {
            _dataAccessProvider.EventMessagesCallback -= OnDataAccessProvider_EventMessagesCallback;

            lock (Lock)
            {
                foreach (TagNode tagNode in _tagNodes.Values.ToList())
                {
                    Unsubscribe(tagNode, false);
                }
            }
        }

        /// <summary>
        ///     Must be called in the addon dispatcher.
        ///     Unsubscribes tags that have no monitored items and were not accessed for a while.
        ///     Dynamic tags are removed from the address space.
        /// </summary>
        public void Cleanup(DateTime nowUtc)
        {
            lock (Lock)
            {
                List<TagNode> idleTagNodes = _tagNodes.Values
                    .Where(t => t.SubscribeRequested && t.MonitoredItemsCount <= 0 && nowUtc - t.LastAccessUtc > _options.IdleUnsubscribeTimeout)
                    .ToList();

                foreach (TagNode tagNode in idleTagNodes)
                {
                    Unsubscribe(tagNode, tagNode.IsDynamic);
                }
            }
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                FolderState root = CreateFolder(null, @"", _options.RootFolderName);
                root.EventNotifier = (byte)(EventNotifiers.SubscribeToEvents | EventNotifiers.HistoryRead);
                root.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                AddExternalReference(ObjectIds.ObjectsFolder, ReferenceTypeIds.Organizes, false, root.NodeId, externalReferences);
                root.AddReference(ReferenceTypeIds.HasNotifier, true, ObjectIds.Server);
                AddExternalReference(ObjectIds.Server, ReferenceTypeIds.HasNotifier, false, root.NodeId, externalReferences);
                _root = root;

                _dynamicFolder = CreateFolder(root, DynamicFolderName, DynamicFolderName);
                _alarmsFolder = CreateFolder(root, AlarmsFolderName, AlarmsFolderName);

                foreach (StaticNodeInfo staticNode in _options.StaticNodes)
                {
                    string elementId = staticNode.ElementId.Trim();
                    if (!IsValidElementId(elementId) || _tagNodes.ContainsKey(elementId))
                        continue;

                    FolderState parent = GetOrCreateFolder(staticNode.Path);
                    string displayName = String.IsNullOrWhiteSpace(staticNode.DisplayName) ? elementId : staticNode.DisplayName.Trim();
                    CreateTagNode(parent, elementId, displayName, staticNode.Description ?? @"", false);
                }

                AddPredefinedNode(SystemContext, root);
                AddRootNotifier(root);
            }

            _dataAccessProvider.EventMessagesCallback += OnDataAccessProvider_EventMessagesCallback;

            _logger.LogInformation("OPC UA address space created. System: {SystemName}, static tags: {StaticTagsCount}", _options.SystemNameToConnect, _tagNodes.Count);
        }

        public override void DeleteAddressSpace()
        {
            _dataAccessProvider.EventMessagesCallback -= OnDataAccessProvider_EventMessagesCallback;

            lock (Lock)
            {
                base.DeleteAddressSpace();
            }
        }

        /// <summary>
        ///     Subscribes the requested tags before the values are read from the node state.
        /// </summary>
        public override void Read(OperationContext context, double maxAge, IList<ReadValueId> nodesToRead, IList<DataValue> values, IList<ServiceResult> errors)
        {
            List<TagNode>? newlySubscribedTagNodes = null;

            for (int ii = 0; ii < nodesToRead.Count; ii++)
            {
                ReadValueId nodeToRead = nodesToRead[ii];
                if (nodeToRead.Processed || nodeToRead.AttributeId != Attributes.Value || !IsNodeIdInNamespace(nodeToRead.NodeId))
                    continue;

                TagNode? tagNode = GetTagNode(nodeToRead.NodeId);
                if (tagNode is null)
                    continue;

                if (EnsureSubscribed(tagNode))
                    (newlySubscribedTagNodes ??= new List<TagNode>()).Add(tagNode);
            }

            if (newlySubscribedTagNodes is not null && _dataAccessProvider.IsConnected)
                WaitForFirstValues(newlySubscribedTagNodes);

            base.Read(context, maxAge, nodesToRead, values, errors);
        }

        /// <summary>
        ///     Writes of the Value attribute of tags go to the data access provider, everything else is handled by the base class.
        /// </summary>
        public override void Write(OperationContext context, IList<WriteValue> nodesToWrite, IList<ServiceResult> errors)
        {
            List<PendingWrite>? pendingWrites = null;

            for (int ii = 0; ii < nodesToWrite.Count; ii++)
            {
                WriteValue nodeToWrite = nodesToWrite[ii];
                if (nodeToWrite.Processed || nodeToWrite.AttributeId != Attributes.Value || !IsNodeIdInNamespace(nodeToWrite.NodeId))
                    continue;

                TagNode? tagNode = GetTagNode(nodeToWrite.NodeId);
                if (tagNode is null)
                    continue;

                nodeToWrite.Processed = true;

                if (nodeToWrite.Value is null)
                {
                    errors[ii] = UaStatusCodes.BadTypeMismatch;
                    continue;
                }

                if (!String.IsNullOrEmpty(nodeToWrite.IndexRange))
                {
                    errors[ii] = UaStatusCodes.BadIndexRangeInvalid;
                    continue;
                }

                EnsureSubscribed(tagNode);
                (pendingWrites ??= new List<PendingWrite>()).Add(new PendingWrite(ii, tagNode, nodeToWrite.Value));
            }

            if (pendingWrites is not null)
            {
                ServiceResult[] results = WriteToDataAccessProvider(pendingWrites);
                for (int i = 0; i < pendingWrites.Count; i++)
                {
                    errors[pendingWrites[i].Index] = results[i];
                }
            }

            base.Write(context, nodesToWrite, errors);
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     Resolves predefined nodes and creates tag nodes on demand for NodeIds of the form ns=X;s=ElementId.
        /// </summary>
        protected override NodeHandle? GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
        {
            lock (Lock)
            {
                if (!IsNodeIdInNamespace(nodeId))
                    return null;

                NodeState? node = FindOrCreateNode(nodeId);
                if (node is null)
                    return null;

                return new NodeHandle
                {
                    NodeId = nodeId,
                    Node = node,
                    Validated = true
                };
            }
        }

        protected override void OnMonitoredItemCreated(ServerSystemContext context, NodeHandle handle, ISampledDataChangeMonitoredItem monitoredItem)
        {
            base.OnMonitoredItemCreated(context, handle, monitoredItem);

            if (handle.Node?.Handle is TagNode tagNode)
            {
                lock (Lock)
                {
                    tagNode.MonitoredItemsCount += 1;
                }
                EnsureSubscribed(tagNode);
            }
        }

        protected override void OnMonitoredItemDeleted(ServerSystemContext context, NodeHandle handle, ISampledDataChangeMonitoredItem monitoredItem)
        {
            base.OnMonitoredItemDeleted(context, handle, monitoredItem);

            if (handle.Node?.Handle is TagNode tagNode)
            {
                lock (Lock)
                {
                    tagNode.MonitoredItemsCount -= 1;
                    tagNode.LastAccessUtc = DateTime.UtcNow;
                }
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Must be called under Lock.
        /// </summary>
        private NodeState? FindOrCreateNode(NodeId nodeId)
        {
            if (PredefinedNodes.TryGetValue(nodeId, out NodeState? node))
                return node;

            if (_dynamicFolder is not null &&
                nodeId.IdType == IdType.String &&
                nodeId.Identifier is string elementId &&
                IsValidElementId(elementId))
            {
                TagNode tagNode = CreateTagNode(_dynamicFolder, elementId, elementId, @"", true);
                AddPredefinedNode(SystemContext, tagNode.Variable);
                _logger.LogDebug("Dynamic OPC UA tag node created: {ElementId}", elementId);
                return tagNode.Variable;
            }

            return null;
        }

        private TagNode? GetTagNode(NodeId nodeId)
        {
            lock (Lock)
            {
                return FindOrCreateNode(nodeId)?.Handle as TagNode;
            }
        }

        /// <summary>
        ///     Folder, element and alarm node ids are strings. Folders and alarms start with '/', element ids cannot.
        /// </summary>
        private static bool IsValidElementId(string elementId)
        {
            return !String.IsNullOrWhiteSpace(elementId) && elementId[0] != '/' && elementId.Length <= 1024;
        }

        private static string NormalizePath(string? path)
        {
            if (String.IsNullOrWhiteSpace(path))
                return @"";
            return String.Join('/', path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        /// <summary>
        ///     Must be called under Lock, before AddPredefinedNode(root).
        /// </summary>
        private FolderState GetOrCreateFolder(string? path)
        {
            path = NormalizePath(path);
            if (path == @"")
                return _root!;

            if (_folders.TryGetValue(path, out FolderState? existingFolder))
                return existingFolder;

            int i = path.LastIndexOf('/');
            FolderState parent = i < 0 ? _root! : GetOrCreateFolder(path.Substring(0, i));
            string name = i < 0 ? path : path.Substring(i + 1);
            return CreateFolder(parent, path, name);
        }

        /// <summary>
        ///     Must be called under Lock.
        /// </summary>
        private FolderState CreateFolder(FolderState? parent, string path, string name)
        {
            var folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(FolderNodeIdPrefix + path, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText(name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            parent?.AddChild(folder);
            _folders[path] = folder;
            return folder;
        }

        /// <summary>
        ///     Must be called under Lock. Does not add the node to the predefined nodes.
        /// </summary>
        private TagNode CreateTagNode(FolderState parent, string elementId, string displayName, string description, bool isDynamic)
        {
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = elementId,
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(elementId, NamespaceIndex),
                BrowseName = new QualifiedName(displayName, NamespaceIndex),
                DisplayName = new LocalizedText(displayName),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                DataType = DataTypeIds.BaseDataType,
                ValueRank = ValueRanks.Scalar,
                AccessLevel = (byte)(AccessLevels.CurrentReadOrWrite | AccessLevels.HistoryRead),
                UserAccessLevel = (byte)(AccessLevels.CurrentReadOrWrite | AccessLevels.HistoryRead),
                Historizing = true,
                MinimumSamplingInterval = 100,
                Value = null,
                StatusCode = UaStatusCodes.BadWaitingForInitialData,
                Timestamp = DateTime.UtcNow
            };
            if (description != @"")
                variable.Description = new LocalizedText(description);

            var tagNode = new TagNode(elementId, variable, isDynamic);
            variable.Handle = tagNode;

            parent.AddChild(variable);
            _tagNodes[elementId] = tagNode;

            return tagNode;
        }

        /// <summary>
        ///     Requests a value subscription for the tag in the data access provider, if not requested yet.
        ///     Returns true if the subscription was requested by this call.
        /// </summary>
        private bool EnsureSubscribed(TagNode tagNode)
        {
            lock (Lock)
            {
                tagNode.LastAccessUtc = DateTime.UtcNow;

                if (tagNode.IsDeleted || tagNode.SubscribeRequested)
                    return false;

                tagNode.SubscribeRequested = true;
                tagNode.Generation += 1;
                int generation = tagNode.Generation;
                tagNode.FirstValueTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                _dispatcher.BeginInvoke(ct =>
                {
                    lock (Lock)
                    {
                        if (tagNode.IsDeleted || tagNode.Generation != generation)
                            return;

                        tagNode.ValueSubscription = new ValueSubscription(_dataAccessProvider, tagNode.ElementId,
                            (sender, args) => OnTagValueUpdated(tagNode, generation, args.NewValueStatusTimestamp));
                    }
                });

                return true;
            }
        }

        /// <summary>
        ///     Must be called in the addon dispatcher, under Lock.
        /// </summary>
        private void Unsubscribe(TagNode tagNode, bool removeNode)
        {
            tagNode.Generation += 1;
            tagNode.SubscribeRequested = false;

            try
            {
                tagNode.ValueSubscription?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ValueSubscription.Dispose() error.");
            }
            tagNode.ValueSubscription = null;

            try
            {
                tagNode.ValuesJournalSubscription?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ValuesJournalSubscription.Dispose() error.");
            }
            tagNode.ValuesJournalSubscription = null;

            if (removeNode)
            {
                tagNode.IsDeleted = true;
                _tagNodes.Remove(tagNode.ElementId);
                tagNode.Variable.Parent?.RemoveChild(tagNode.Variable);
                DeleteNode(SystemContext, tagNode.Variable.NodeId);
            }
            else
            {
                tagNode.Variable.StatusCode = UaStatusCodes.UncertainNoCommunicationLastUsableValue;
                tagNode.Variable.Timestamp = DateTime.UtcNow;
                tagNode.Variable.ClearChangeMasks(SystemContext, false);
            }
        }

        /// <summary>
        ///     Called in the addon dispatcher when the data access provider updates the tag value.
        /// </summary>
        private void OnTagValueUpdated(TagNode tagNode, int generation, ValueStatusTimestamp vst)
        {
            lock (Lock)
            {
                if (tagNode.IsDeleted || tagNode.Generation != generation)
                    return;

                BaseDataVariableState variable = tagNode.Variable;

                object? value = OpcUaValueHelper.ToOpcUaValue(vst.Value);
                if (value is not null)
                {
                    NodeId? dataTypeId = TypeInfo.GetDataTypeId(value);
                    if (dataTypeId is not null && !NodeId.IsNull(dataTypeId) && variable.DataType != dataTypeId)
                        variable.DataType = dataTypeId;
                }

                variable.Value = value;
                variable.StatusCode = OpcUaValueHelper.ToOpcUaStatusCode(vst.StatusCode);
                variable.Timestamp = vst.TimestampUtc == default ? DateTime.UtcNow : DateTime.SpecifyKind(vst.TimestampUtc, DateTimeKind.Utc);
                variable.ClearChangeMasks(SystemContext, false);
            }

            tagNode.FirstValueTcs.TrySetResult(true);
        }

        private static void WaitForFirstValues(List<TagNode> tagNodes)
        {
            try
            {
                Task[] tasks = tagNodes.Select(t => (Task)t.FirstValueTcs.Task).ToArray();
                Task.WaitAll(tasks, FirstValueTimeout);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     Writes the values in the addon dispatcher and waits for the results.
        /// </summary>
        private ServiceResult[] WriteToDataAccessProvider(List<PendingWrite> pendingWrites)
        {
            var tcs = new TaskCompletionSource<ResultInfo[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            _dispatcher.BeginInvoke(async ct =>
            {
                try
                {
                    var tasks = new Task<ResultInfo>[pendingWrites.Count];
                    for (int i = 0; i < pendingWrites.Count; i++)
                    {
                        PendingWrite pendingWrite = pendingWrites[i];
                        ValueSubscription? valueSubscription;
                        lock (Lock)
                        {
                            valueSubscription = pendingWrite.TagNode.ValueSubscription;
                        }
                        if (valueSubscription is null)
                        {
                            tasks[i] = Task.FromResult(new ResultInfo { StatusCode = SszStatusCodes.BadServerNotConnected });
                            continue;
                        }

                        DateTime timestampUtc = pendingWrite.Value.SourceTimestamp == DateTime.MinValue
                            ? DateTime.UtcNow
                            : DateTime.SpecifyKind(pendingWrite.Value.SourceTimestamp, DateTimeKind.Utc);
                        var vst = new ValueStatusTimestamp(OpcUaValueHelper.ToAny(pendingWrite.Value.Value), SszStatusCodes.Good, timestampUtc);
                        tasks[i] = _dataAccessProvider.WriteAsync(valueSubscription, vst);
                    }

                    tcs.TrySetResult(await Task.WhenAll(tasks));
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            var results = new ServiceResult[pendingWrites.Count];
            ResultInfo[]? resultInfos = null;
            try
            {
                if (tcs.Task.Wait(WriteTimeout))
                    resultInfos = tcs.Task.Result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OPC UA write to data access provider failed.");
            }

            for (int i = 0; i < results.Length; i++)
            {
                if (resultInfos is null)
                {
                    results[i] = UaStatusCodes.BadTimeout;
                    continue;
                }

                ResultInfo resultInfo = resultInfos[i];
                if (SszStatusCodes.IsGood(resultInfo.StatusCode))
                    results[i] = ServiceResult.Good;
                else
                    results[i] = new ServiceResult((StatusCode)resultInfo.StatusCode, new LocalizedText(resultInfo.Info ?? @""));
            }

            return results;
        }

        #endregion

        #region private fields

        private const string FolderNodeIdPrefix = @"/";

        private const string DynamicFolderName = @"Dynamic";

        private const string AlarmsFolderName = @"Alarms";

        private static readonly TimeSpan FirstValueTimeout = TimeSpan.FromMilliseconds(1500);

        private static readonly TimeSpan WriteTimeout = TimeSpan.FromSeconds(30);

        private readonly OpcUaServerOptions _options;

        private readonly IDataAccessProvider _dataAccessProvider;

        private readonly IDispatcher _dispatcher;

        private readonly ILogger _logger;

        private FolderState? _root;

        private FolderState? _dynamicFolder;

        private FolderState? _alarmsFolder;

        /// <summary>
        ///     [Normalized path, Folder]
        /// </summary>
        private readonly Dictionary<string, FolderState> _folders = new();

        /// <summary>
        ///     [ElementId, TagNode]
        /// </summary>
        private readonly Dictionary<string, TagNode> _tagNodes = new();

        #endregion

        private sealed class TagNode
        {
            public TagNode(string elementId, BaseDataVariableState variable, bool isDynamic)
            {
                ElementId = elementId;
                Variable = variable;
                IsDynamic = isDynamic;
                LastAccessUtc = DateTime.UtcNow;
            }

            public string ElementId { get; }

            public BaseDataVariableState Variable { get; }

            /// <summary>
            ///     Created on demand by a client request (not from nodes.csv).
            /// </summary>
            public bool IsDynamic { get; }

            public bool IsDeleted { get; set; }

            /// <summary>
            ///     Incremented on every subscribe/unsubscribe, guards against stale callbacks.
            /// </summary>
            public int Generation { get; set; }

            public bool SubscribeRequested { get; set; }

            public ValueSubscription? ValueSubscription { get; set; }

            public ValuesJournalSubscription? ValuesJournalSubscription { get; set; }

            public int MonitoredItemsCount { get; set; }

            public DateTime LastAccessUtc { get; set; }

            public TaskCompletionSource<bool> FirstValueTcs { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private sealed class PendingWrite
        {
            public PendingWrite(int index, TagNode tagNode, DataValue value)
            {
                Index = index;
                TagNode = tagNode;
                Value = value;
            }

            public int Index { get; }

            public TagNode TagNode { get; }

            public DataValue Value { get; }
        }
    }
}
