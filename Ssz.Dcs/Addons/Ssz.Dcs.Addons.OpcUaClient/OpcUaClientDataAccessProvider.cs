using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.Addons.OpcUaClient
{
    /// <summary>
    ///     IDataAccessProvider over an OPC UA client session: values through a subscription, events and alarms
    ///     through an event monitored item, history through HistoryRead.
    ///     Structure follows OpcClientDataAccessProvider (OPC DA): element ids are mapped with ElementIdsMap,
    ///     constants and converters are handled here, the OPC UA session lives in the working task.
    /// </summary>
    public partial class OpcUaClientDataAccessProvider : DataAccessProviderBase, IDataAccessProvider
    {
        #region construction and destruction

        public OpcUaClientDataAccessProvider(ILogger<OpcUaClientDataAccessProvider> logger, ILoggerFactory loggerFactory, IUserFriendlyLogger? userFriendlyLogger = null) :
            base(new LoggersSet<OpcUaClientDataAccessProvider>(logger, userFriendlyLogger))
        {
            _loggerFactory = loggerFactory;
        }

        #endregion

        #region public functions

        public override DateTime LastFailedConnectionDateTimeUtc => _lastFailedConnectionDateTimeUtc;

        public override DateTime LastSuccessfulConnectionDateTimeUtc => _lastSuccessfulConnectionDateTimeUtc;

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).
        /// </summary>
        public override event EventHandler ValueSubscriptionsUpdated = delegate { };

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).
        /// </summary>
        public override event EventHandler<EventMessagesCallbackEventArgs> EventMessagesCallback = delegate { };

        public override void Initialize(ElementIdsMap? elementIdsMap,
            string serverAddress,
            string clientApplicationName,
            string clientWorkstationName,
            string systemNameToConnect,
            CaseInsensitiveOrderedDictionary<string?> contextParams,
            DataAccessProviderOptions options,
            IDispatcher? callbackDispatcher)
        {
            base.Initialize(elementIdsMap,
                serverAddress,
                clientApplicationName,
                clientWorkstationName,
                systemNameToConnect,
                contextParams,
                options,
                callbackDispatcher);

            _options = OpcUaClientOptions.FromContextParams(contextParams, serverAddress);

            lock (ConstItemsDictionary)
            {
                ConstItemsDictionary.Clear();
                if (ElementIdsMap is not null)
                {
                    foreach (var values in ElementIdsMap.Map.Values)
                    {
                        if (values.Count >= 2 && !values[0]!.Contains(ElementIdsMap.GenericTag)
                            && values.Skip(2).All(v => String.IsNullOrEmpty(v)))
                        {
                            var constAny = ElementIdsMap.TryGetConstValue(values[1]);
                            if (constAny.HasValue)
                            {
                                ConstItemsDictionary[values[0]!] = new ConstItem { Value = constAny.Value };
                            }
                        }
                    }
                }
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            _workingTask = WorkingTaskMainAsync(cancellationToken);

            foreach (ValueSubscriptionObj valueSubscriptionObj in _valueSubscriptionsCollection.Values)
            {
                valueSubscriptionObj.ValueSubscription.Update(
                    AddItem(valueSubscriptionObj));
            }
        }

        /// <summary>
        ///     You can call DisposeAsync() instead of this method.
        ///     Closes WITH waiting working thread exit.
        /// </summary>
        public override async Task CloseAsync()
        {
            if (!IsInitialized)
                return;

            foreach (ValueSubscriptionObj valueSubscriptionObj in _valueSubscriptionsCollection.Values)
            {
                valueSubscriptionObj.ChildValueSubscriptionsList = null;
                valueSubscriptionObj.Converter = null;
                valueSubscriptionObj.MapValues = null;
            }

            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            if (_workingTask is not null)
                await _workingTask;

            await base.CloseAsync();
        }

        /// <summary>
        ///     Returns id actually used for OPC UA subscription.
        ///     valueSubscription.Update() is called using сallbackDispatcher, see Initialize(..).
        /// </summary>
        public override void AddItem(string? elementId, IValueSubscription valueSubscription)
        {
            if (elementId is null || elementId == @"")
            {
                var callbackDispatcher = CallbackDispatcher;
                if (callbackDispatcher is not null)
                    try
                    {
                        callbackDispatcher.BeginInvoke(ct =>
                        {
                            valueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.BadNodeIdUnknown });
                            RaiseValueSubscriptionsUpdated();
                        });
                    }
                    catch (Exception)
                    {
                    }

                return;
            }

            var valueSubscriptionObj = new ValueSubscriptionObj(elementId, valueSubscription);
            _valueSubscriptionsCollection.Add(valueSubscription, valueSubscriptionObj);

            if (IsInitialized)
            {
                valueSubscription.Update(
                    AddItem(valueSubscriptionObj));
            }
        }

        /// <summary>
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        public override void RemoveItem(IValueSubscription valueSubscription)
        {
            if (!_valueSubscriptionsCollection.Remove(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj))
                return;

            if (IsInitialized)
            {
                RemoveItem(valueSubscriptionObj);
            }
        }

        /// <summary>
        ///     Write with mapping and conversion. The write to the OPC UA server is not awaited.
        /// </summary>
        public override Task<ResultInfo> WriteAsync(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp)
        {
            var callbackDispatcher = CallbackDispatcher;
            if (!IsInitialized || callbackDispatcher is null)
                return Task.FromResult(ResultInfo.UncertainResultInfo);

            if (!StatusCodes.IsGood(valueStatusTimestamp.StatusCode))
                return Task.FromResult(ResultInfo.UncertainResultInfo);
            var value = valueStatusTimestamp.Value;

            if (!_valueSubscriptionsCollection.TryGetValue(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj))
                return Task.FromResult(ResultInfo.UncertainResultInfo);

            if (LoggersSet.UserFriendlyLogger.IsEnabled(LogLevel.Information))
                LoggersSet.UserFriendlyLogger.LogInformation("UI TAG: \"" + valueSubscriptionObj.ElementId + "\"; Value from UI: \"" + value + "\"");

            IValueSubscription[]? constItemValueSubscriptionsArray = null;
            lock (ConstItemsDictionary)
            {
                var constItem = ConstItemsDictionary.TryGetValue(valueSubscriptionObj.ElementId);
                if (constItem is not null)
                {
                    constItem.Value = value;
                    constItemValueSubscriptionsArray = constItem.Subscribers.ToArray();
                }
            }

            if (constItemValueSubscriptionsArray is not null)
            {
                try
                {
                    callbackDispatcher.BeginInvoke(ct =>
                    {
                        foreach (var constItemValueSubscription in constItemValueSubscriptionsArray)
                        {
                            constItemValueSubscription.Update(valueStatusTimestamp);
                        }
                    });
                }
                catch (Exception)
                {
                }

                return Task.FromResult(ResultInfo.GoodResultInfo);
            }

            object?[]? resultValues = null;
            if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
            {
                SszConverter converter = valueSubscriptionObj.Converter ?? SszConverter.Empty;
                resultValues =
                    converter.ConvertBack(value.ValueAsObject(),
                        valueSubscriptionObj.ChildValueSubscriptionsList.Count,
                        null,
                        LoggersSet);
                if (resultValues.Length == 0)
                    return Task.FromResult(ResultInfo.GoodResultInfo);
            }

            var writes = new List<(object ClientObj, ValueStatusTimestamp ValueStatusTimestamp)>();
            if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
            {
                if (resultValues is null) throw new InvalidOperationException();
                for (var i = 0; i < resultValues.Length; i++)
                {
                    var resultValue = resultValues[i];
                    if (resultValue == SszConverter.DoNothing)
                        continue;
                    ChildValueSubscription childValueSubscription = valueSubscriptionObj.ChildValueSubscriptionsList[i];
                    if (childValueSubscription.IsConst)
                        continue;
                    if (LoggersSet.UserFriendlyLogger.IsEnabled(LogLevel.Information))
                        LoggersSet.UserFriendlyLogger.LogInformation("Model TAG: \"" + childValueSubscription.ElementId + "\"; Write Value to Model: \"" + new Any(resultValue) + "\"");
                    writes.Add((childValueSubscription, new ValueStatusTimestamp(new Any(resultValue), StatusCodes.Good, DateTime.UtcNow)));
                }
            }
            else
            {
                if (value.ValueAsObject() == SszConverter.DoNothing)
                    return Task.FromResult(ResultInfo.GoodResultInfo);
                if (LoggersSet.UserFriendlyLogger.IsEnabled(LogLevel.Information))
                    LoggersSet.UserFriendlyLogger.LogInformation("Model TAG: \"" +
                        (valueSubscriptionObj.MapValues is not null ? valueSubscriptionObj.MapValues[1] : valueSubscriptionObj.ElementId) +
                        "\"; Write Value to Model: \"" + value + "\"");
                writes.Add((valueSubscription, new ValueStatusTimestamp(value, StatusCodes.Good, DateTime.UtcNow)));
            }

            if (writes.Count == 0)
                return Task.FromResult(ResultInfo.GoodResultInfo);

            WorkingThreadSafeDispatcher.BeginInvoke(async ct =>
            {
                try
                {
                    await WriteNodesAsync(writes.Select(w => w.ClientObj).ToArray(), writes.Select(w => w.ValueStatusTimestamp).ToArray(), ct);
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogWarning(ex, "OPC UA write error.");
                }
            });

            return Task.FromResult(ResultInfo.GoodResultInfo);
        }

        /// <summary>
        ///     No values mapping and conversion.
        ///     Returns failed ValueSubscriptions and ResultInfos.
        ///     If connection error, all ValueSubscriptions are failed.
        /// </summary>
        public override async Task<(IValueSubscription[], ResultInfo[])> WriteAsync(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps)
        {
            var taskCompletionSource = new TaskCompletionSource<(IValueSubscription[], ResultInfo[])>(TaskCreationOptions.RunContinuationsAsynchronously);

            WorkingThreadSafeDispatcher.BeginInvoke(async ct =>
            {
                try
                {
                    var (failedClientObjs, resultInfos) = await WriteNodesAsync(valueSubscriptions.Cast<object>().ToArray(), valueStatusTimestamps, ct);
                    taskCompletionSource.TrySetResult((failedClientObjs.OfType<IValueSubscription>().ToArray(), resultInfos));
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            });

            return await taskCompletionSource.Task;
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     Dispatcher for the working task; all OPC UA session calls are made there.
        /// </summary>
        protected ThreadSafeDispatcher WorkingThreadSafeDispatcher { get; } = new();

        protected CaseInsensitiveOrderedDictionary<ConstItem> ConstItemsDictionary { get; } = new();

        protected void RaiseValueSubscriptionsUpdated()
        {
            DataGuid = Guid.NewGuid();

            ValueSubscriptionsUpdated(this, EventArgs.Empty);
        }

        #endregion

        #region private functions

        private async Task WorkingTaskMainAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(0);

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(10, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    await DoWorkAsync(DateTime.UtcNow, cancellationToken);
                }
                catch when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogWarning(ex, @"OPC UA Client WorkingTaskMain Exception");
                }
            }

            await DisconnectAsync(false);
        }

        private async Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            await WorkingThreadSafeDispatcher.InvokeActionsInQueueAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (_session is not null && Volatile.Read(ref _sessionFailed))
            {
                LoggersSet.Logger.LogWarning("OPC UA session failed, reconnecting. Endpoint: {EndpointUrl}", _options?.EndpointUrl);
                await DisconnectAsync(true);
            }

            if (_session is null)
            {
                if (IsInitialized && _options is not null && _options.EndpointUrl != @"" &&
                    nowUtc > _lastFailedConnectionDateTimeUtc + ReconnectPeriod)
                {
                    try
                    {
                        await ConnectAsync(cancellationToken);
                    }
                    catch when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _lastFailedConnectionDateTimeUtc = nowUtc;
                        LoggersSet.Logger.LogWarning(ex, "OPC UA connection failed. Endpoint: {EndpointUrl}", _options.EndpointUrl);
                        await DisconnectAsync(false);
                    }
                }
            }

            if (!IsInitialized || _session is null)
                return;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ApplySubscriptionChangesAsync(cancellationToken);
                _lastSuccessfulConnectionDateTimeUtc = nowUtc;
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LoggersSet.Logger.LogWarning(ex, "OPC UA subscription update error.");
                Volatile.Write(ref _sessionFailed, true);
            }
        }

        /// <summary>
        ///     Preconditions: must be Initialized.
        ///     Returns MappedElementIdOrConst
        /// </summary>
        private string AddItem(ValueSubscriptionObj valueSubscriptionObj)
        {
            string elementId = valueSubscriptionObj.ElementId;

            var callbackDispatcher = CallbackDispatcher;
            if (callbackDispatcher is null)
                return elementId;

            IValueSubscription valueSubscription = valueSubscriptionObj.ValueSubscription;

            if (ElementIdsMap is not null)
            {
                var constAny = ElementIdsMap.TryGetConstValue(elementId);
                if (!constAny.HasValue)
                {
                    lock (ConstItemsDictionary)
                    {
                        var constItem = ConstItemsDictionary.TryGetValue(elementId);
                        if (constItem is not null)
                        {
                            constItem.Subscribers.Add(valueSubscription);
                            constAny = constItem.Value;
                        }
                    }
                }
                if (constAny.HasValue)
                {
                    try
                    {
                        callbackDispatcher.BeginInvoke(ct =>
                        {
                            valueSubscription.Update(new ValueStatusTimestamp(constAny.Value, StatusCodes.Good, DateTime.UtcNow));
                            RaiseValueSubscriptionsUpdated();
                        });
                    }
                    catch (Exception)
                    {
                    }

                    return constAny.Value.ValueAsString(false);
                }

                valueSubscriptionObj.MapValues = ElementIdsMap.GetFromMap(elementId);

                if (valueSubscriptionObj.MapValues is not null)
                {
                    if (valueSubscriptionObj.MapValues.Skip(2).All(v => String.IsNullOrEmpty(v)))
                    {
                        constAny = ElementIdsMap.TryGetConstValue(valueSubscriptionObj.MapValues[1]);
                    }
                    else
                    {
                        var childValueSubscriptionsList = new List<ChildValueSubscription>();
                        SszConverter? converter = null;

                        for (var i = 1; i < valueSubscriptionObj.MapValues.Count; i++)
                        {
                            string v = valueSubscriptionObj.MapValues[i] ?? "";

                            int index;
                            if ((StringHelper.StartsWithIgnoreCase(v, @"READCONVERTER") ||
                                 StringHelper.StartsWithIgnoreCase(v, @"WRITECONVERTER"))
                                && (index = v.IndexOf('=')) > 0)
                            {
                                if (converter is null)
                                    converter = new SszConverter();

                                var values = v.Substring(index + 1).Split(new[] { "->" }, StringSplitOptions.None);
                                switch (v.Substring(0, index).Trim().ToUpperInvariant())
                                {
                                    case @"READCONVERTER":
                                        if (values.Length == 1)
                                            converter.Statements.Add(new SszStatement(@"true", values[0].Trim(), 0));
                                        else // values.Length > 1
                                            converter.Statements.Add(new SszStatement(values[0].Trim(),
                                                values[1].Trim(), 0));
                                        break;
                                    case @"WRITECONVERTER":
                                        if (values.Length == 1)
                                            converter.BackStatements.Add(new SszStatement(@"true", values[0].Trim(), 0));
                                        else if (values.Length == 2)
                                            converter.BackStatements.Add(new SszStatement(values[0].Trim(),
                                                values[1].Trim(), 0));
                                        else // values.Length > 2
                                            converter.BackStatements.Add(new SszStatement(
                                                values[0].Trim(),
                                                values[1].Trim(),
                                                new Any(values[2].Trim()).ValueAsInt32(false)));
                                        break;
                                }
                            }
                            else
                            {
                                if (converter is not null)
                                    continue;

                                var childValueSubscription = new ChildValueSubscription(valueSubscriptionObj, v);
                                childValueSubscriptionsList.Add(childValueSubscription);
                            }
                        }

                        if (converter is not null && converter.Statements.Count == 0 && converter.BackStatements.Count == 0)
                        {
                            converter = null;
                        }

                        if (childValueSubscriptionsList.Count > 1 || converter is not null)
                        {
                            valueSubscriptionObj.ChildValueSubscriptionsList = childValueSubscriptionsList;
                            valueSubscriptionObj.Converter = converter;
                            try
                            {
                                callbackDispatcher.BeginInvoke(ct => valueSubscriptionObj.ChildValueSubscriptionUpdated());
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }

            if (valueSubscriptionObj.MapValues is not null)
            {
                WorkingThreadSafeDispatcher.BeginInvoke(ct =>
                {
                    if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                    {
                        foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                            if (!childValueSubscription.IsConst)
                                AddClientObj(childValueSubscription.ElementId, childValueSubscription);
                    }
                    else
                    {
                        AddClientObj(valueSubscriptionObj.MapValues[1] ?? "", valueSubscription);
                    }
                });

                return valueSubscriptionObj.MapValues[1] ?? "";
            }
            else
            {
                WorkingThreadSafeDispatcher.BeginInvoke(ct =>
                {
                    AddClientObj(elementId, valueSubscription);
                });

                return elementId;
            }
        }

        /// <summary>
        ///     Preconditions: must be Initialized.
        /// </summary>
        private void RemoveItem(ValueSubscriptionObj valueSubscriptionObj)
        {
            var valueSubscription = valueSubscriptionObj.ValueSubscription;

            var constAny = ElementIdsMap.TryGetConstValue(valueSubscriptionObj.ElementId);
            if (constAny.HasValue) return;

            var disposable = valueSubscriptionObj.Converter as IDisposable;
            if (disposable is not null) disposable.Dispose();

            lock (ConstItemsDictionary)
            {
                var constItem = ConstItemsDictionary.TryGetValue(valueSubscriptionObj.ElementId);
                if (constItem is not null)
                {
                    constItem.Subscribers.Remove(valueSubscription);
                    return;
                }
            }

            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                    {
                        if (!childValueSubscription.IsConst)
                            RemoveClientObj(childValueSubscription);
                        childValueSubscription.ValueSubscriptionObj = null;
                    }

                    valueSubscriptionObj.ChildValueSubscriptionsList = null;
                }
                else
                {
                    RemoveClientObj(valueSubscription);
                }
            });
        }

        /// <summary>
        ///     Returns the id used on the OPC UA server for the element id (mapped with ElementIdsMap when the map has a simple one-to-one entry).
        /// </summary>
        private string GetMappedId(string elementId)
        {
            if (ElementIdsMap is null)
                return elementId;
            List<string?>? mapValues = ElementIdsMap.GetFromMap(elementId);
            if (mapValues is not null && mapValues.Count > 1 && !String.IsNullOrEmpty(mapValues[1]) && mapValues.Skip(2).All(v => String.IsNullOrEmpty(v)))
                return mapValues[1]!;
            return elementId;
        }

        #endregion

        #region private fields

        private static readonly TimeSpan ReconnectPeriod = TimeSpan.FromSeconds(10);

        private readonly ILoggerFactory _loggerFactory;

        private OpcUaClientOptions? _options;

        private DateTime _lastFailedConnectionDateTimeUtc = DateTime.MinValue;

        private DateTime _lastSuccessfulConnectionDateTimeUtc = DateTime.MinValue;

        private Task? _workingTask;

        private CancellationTokenSource? _cancellationTokenSource;

        private readonly Dictionary<IValueSubscription, ValueSubscriptionObj> _valueSubscriptionsCollection =
            new(ReferenceEqualityComparer<object>.Default);

        #endregion

        protected class ConstItem
        {
            public readonly HashSet<IValueSubscription> Subscribers = new(ReferenceEqualityComparer<object>.Default);

            public Any Value;
        }

        private class ValueSubscriptionObj
        {
            public ValueSubscriptionObj(string elementId, IValueSubscription valueSubscription)
            {
                ElementId = elementId;
                ValueSubscription = valueSubscription;
            }

            public readonly string ElementId;

            public readonly IValueSubscription ValueSubscription;

            public List<ChildValueSubscription>? ChildValueSubscriptionsList;

            public SszConverter? Converter;

            /// <summary>
            ///     null or Count > 1
            /// </summary>
            public List<string?>? MapValues;

            public void ChildValueSubscriptionUpdated()
            {
                if (ChildValueSubscriptionsList is null) return;

                if (ChildValueSubscriptionsList.Any(vs => StatusCodes.IsBad(vs.ValueStatusTimestamp.StatusCode)))
                {
                    ValueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.Bad });
                    return;
                }
                if (ChildValueSubscriptionsList.Any(vs => StatusCodes.IsUncertain(vs.ValueStatusTimestamp.StatusCode)))
                {
                    ValueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain });
                    return;
                }

                var values = new List<object?>();
                foreach (var childValueSubscription in ChildValueSubscriptionsList)
                    values.Add(childValueSubscription.ValueStatusTimestamp.Value.ValueAsObject());
                SszConverter converter = Converter ?? SszConverter.Empty;
                var convertedValue = converter.Convert(
                    values.ToArray(),
                    null,
                    Ssz.Utils.Logging.LoggersSet.Empty);
                if (convertedValue == SszConverter.DoNothing)
                    return;
                ValueSubscription.Update(new ValueStatusTimestamp(new Any(convertedValue), StatusCodes.Good, DateTime.UtcNow));
            }
        }

        private class ChildValueSubscription : IValueSubscription
        {
            public ChildValueSubscription(ValueSubscriptionObj valueSubscriptionObj, string elementId)
            {
                ValueSubscriptionObj = valueSubscriptionObj;
                ElementId = elementId;

                var constAny = ElementIdsMap.TryGetConstValue(elementId);
                if (constAny.HasValue)
                {
                    ValueStatusTimestamp = new ValueStatusTimestamp(constAny.Value);
                    IsConst = true;
                }
            }

            public ValueSubscriptionObj? ValueSubscriptionObj;

            public string ElementId { get; }

            public void Update(string mappedElementIdOrConst)
            {
            }

            public ValueStatusTimestamp ValueStatusTimestamp = new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain };

            public readonly bool IsConst;

            public void Update(ValueStatusTimestamp valueStatusTimestamp)
            {
                ValueStatusTimestamp = valueStatusTimestamp;

                ValueSubscriptionObj?.ChildValueSubscriptionUpdated();
            }
        }
    }
}
