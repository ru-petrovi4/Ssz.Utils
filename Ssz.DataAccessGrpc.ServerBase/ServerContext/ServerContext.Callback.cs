using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;

namespace Ssz.DataAccessGrpc.ServerBase
{
    /// <summary>
    ///   This partial class defines the methods that support the methods 
    ///   of the ICallback interface.
    /// </summary>
    public partial class ServerContext
    {
        #region public functions

        public void SetResponseStream(IServerStreamWriter<CallbackMessage> responseStream)
        {
            _responseStream = responseStream;

            if (_callbackWorkingTask is null)
            {
                _callbackWorkingTask = Task.Factory.StartNew(() =>
                {
                    CallbackWorkingTaskMainAsync(_callbackWorkingTask_CancellationTokenSource.Token).Wait();
                }, TaskCreationOptions.LongRunning);
            }
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="contextInfoMessage"></param>
        public void AddCallbackMessage(ContextInfoMessage contextInfoMessage)
        {
            if (_responseStream is null) return;

            lock (_messagesSyncRoot)
            {
                _contextInfoMessagesCollection.Add(contextInfoMessage);
            }
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="elementValuesCallbackMessage"></param>
        public void AddCallbackMessage(ElementValuesCallbackMessage elementValuesCallbackMessage)
        {
            if (_responseStream is null) return;

            lock (_messagesSyncRoot)
            {
                _elementValuesCallbackMessagesCollection.Add(elementValuesCallbackMessage);
            }            
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="eventMessagesCallbackMessage"></param>
        public void AddCallbackMessage(EventMessagesCallbackMessage eventMessagesCallbackMessage)
        {
            if (_responseStream is null) return;

            lock (_messagesSyncRoot)
            {
                _eventMessagesCallbackMessagesCollection.Add(eventMessagesCallbackMessage);
            }
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="longrunningPassthroughCallbackMessage"></param>
        public void AddCallbackMessage(LongrunningPassthroughCallbackMessage longrunningPassthroughCallbackMessage)
        {
            if (_responseStream is null) return;

            lock (_messagesSyncRoot)
            {
                _longrunningPassthroughCallbackMessagesCollection.Add(longrunningPassthroughCallbackMessage);
            }
        }

        #endregion

        #region internal functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listServerAlias"></param>
        /// <param name="isEnabled"></param>
        /// <returns></returns>
        internal void EnableListCallback(uint listServerAlias, ref bool isEnabled)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            serverList.EnableListCallback(isEnabled);
            isEnabled = serverList.ListCallbackIsEnabled;            
        }

        #endregion        

        #region private functions

        private async Task CallbackWorkingTaskMainAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await Task.Delay(10);
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    await OnLoopInWorkingThreadAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, @"ServerContext Callback Thread Exception");
                }                
            }

            Logger.LogDebug(@"ServerContext Callback Thread Exit");
        }

        private async Task OnLoopInWorkingThreadAsync(CancellationToken cancellationToken)
        {
            List<ContextInfoMessage> contextInfoMessagesCollection;
            List<ElementValuesCallbackMessage> elementValuesCallbackMessagesCollection;
            List<EventMessagesCallbackMessage> eventMessagesCallbackMessagesCollection;
            List<LongrunningPassthroughCallbackMessage> longrunningPassthroughCallbackMessagesCollection;
            lock (_messagesSyncRoot)
            {
                contextInfoMessagesCollection = _contextInfoMessagesCollection;
                elementValuesCallbackMessagesCollection = _elementValuesCallbackMessagesCollection;
                eventMessagesCallbackMessagesCollection = _eventMessagesCallbackMessagesCollection;
                longrunningPassthroughCallbackMessagesCollection = _longrunningPassthroughCallbackMessagesCollection;
                _contextInfoMessagesCollection = new List<ContextInfoMessage>();
                _elementValuesCallbackMessagesCollection = new List<ElementValuesCallbackMessage>();
                _eventMessagesCallbackMessagesCollection = new List<EventMessagesCallbackMessage>();
                _longrunningPassthroughCallbackMessagesCollection = new List<LongrunningPassthroughCallbackMessage>();
            }            

            foreach (ContextInfoMessage contextInfoMessage in contextInfoMessagesCollection)
            {
                if (contextInfoMessage.State == State.Aborting)
                {
                    _callbackWorkingTask_CancellationTokenSource.Cancel();
                    break;
                }
            }

            if (_responseStream is null) return;

            if (contextInfoMessagesCollection.Count > 0)
            {
                Logger.LogDebug("ServerContext contextInfoMessagesCollection.Count=" + contextInfoMessagesCollection.Count);
                foreach (ContextInfoMessage contextInfoMessage in contextInfoMessagesCollection)
                {
                    if (contextInfoMessage.State == State.Aborting)
                    {
                        var callbackMessage = new CallbackMessage();
                        callbackMessage.ContextInfo = new ContextInfo
                        {
                            State = contextInfoMessage.State
                        };
                        await _responseStream.WriteAsync(callbackMessage);
                    }
                    else
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            var callbackMessage = new CallbackMessage();
                            callbackMessage.ContextInfo = new ContextInfo
                            {
                                State = contextInfoMessage.State
                            };
                            await _responseStream.WriteAsync(callbackMessage);
                        }                        
                    }                    
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            if (elementValuesCallbackMessagesCollection.Count > 0)
            {
                //Logger.LogDebug("ServerContext elementValuesCallbackMessagesCollection.Count=" + elementValuesCallbackMessagesCollection.Count);

                foreach (var g in elementValuesCallbackMessagesCollection.GroupBy(m => m.ListClientAlias))
                {   
                    var elementValuesCallbackMessagesForList = g.ToArray();
                    if (elementValuesCallbackMessagesForList.Length == 0) continue;
                    var elementValuesCallbackMessage = elementValuesCallbackMessagesForList[0];
                    foreach (var m in elementValuesCallbackMessagesForList.Skip(1))
                    {
                        elementValuesCallbackMessage.CombineWith(m);
                    }
                    foreach (ElementValuesCallback elementValuesCallback in elementValuesCallbackMessage.SplitForCorrectGrpcMessageSize())
                    {
                        var callbackMessage = new CallbackMessage
                        {
                            ElementValuesCallback = elementValuesCallback
                        };
                        await _responseStream.WriteAsync(callbackMessage);
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            if (eventMessagesCallbackMessagesCollection.Count > 0)
            {
                Logger.LogDebug("ServerContext eventMessagesCallbackMessagesCollection.Count=" + eventMessagesCallbackMessagesCollection.Count);

                foreach (var g in eventMessagesCallbackMessagesCollection.GroupBy(m => m.ListClientAlias))
                {   
                    var eventMessagesCallbackMessagesForList = g.ToArray();
                    if (eventMessagesCallbackMessagesForList.Length == 0) continue;
                    var eventMessagesCallbackMessage = eventMessagesCallbackMessagesForList[0];
                    foreach (var m in eventMessagesCallbackMessagesForList.Skip(1))
                    {
                        eventMessagesCallbackMessage.CombineWith(m);
                    }
                    foreach (EventMessagesCallback eventMessagesCallback in eventMessagesCallbackMessage.SplitForCorrectGrpcMessageSize())
                    {
                        Logger.LogDebug("_responseStream.WriteAsync(callbackMessage)");
                        var callbackMessage = new CallbackMessage
                        {
                            EventMessagesCallback = eventMessagesCallback
                        };
                        await _responseStream.WriteAsync(callbackMessage);
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            if (longrunningPassthroughCallbackMessagesCollection.Count > 0)
            {
                Logger.LogDebug("ServerContext longrunningPassthroughCallbackMessagesCollection.Count=" + longrunningPassthroughCallbackMessagesCollection.Count);

                foreach (var longrunningPassthroughCallbackMessage in longrunningPassthroughCallbackMessagesCollection)
                {
                    Logger.LogDebug("_responseStream.WriteAsync(callbackMessage)");
                    var callbackMessage = new CallbackMessage
                    {
                        LongrunningPassthroughCallback = new LongrunningPassthroughCallback
                        {
                            JobId = longrunningPassthroughCallbackMessage.JobId,
                            ProgressPercent = longrunningPassthroughCallbackMessage.ProgressPercent,
                            ProgressLabel = longrunningPassthroughCallbackMessage.ProgressLabel ?? @"",
                            ProgressDetail = longrunningPassthroughCallbackMessage.ProgressDetail ?? @"",
                            JobStatusCode = longrunningPassthroughCallbackMessage.JobStatusCode,
                        }
                    };
                    await _responseStream.WriteAsync(callbackMessage);
                }
            }
        }

        #endregion

        #region private fields

        private IServerStreamWriter<CallbackMessage>? _responseStream;

        private Task? _callbackWorkingTask;

        private readonly CancellationTokenSource _callbackWorkingTask_CancellationTokenSource = new CancellationTokenSource();

        private readonly Object _messagesSyncRoot = new Object();

        private List<ContextInfoMessage> _contextInfoMessagesCollection = new();

        private List<ElementValuesCallbackMessage> _elementValuesCallbackMessagesCollection = new();

        private List<EventMessagesCallbackMessage> _eventMessagesCallbackMessagesCollection = new();

        private List<LongrunningPassthroughCallbackMessage> _longrunningPassthroughCallbackMessagesCollection = new();

        #endregion

        public class ContextInfoMessage
        {
            public State State;
        }

        public class ElementValuesCallbackMessage
        {
            #region public functions            

            public uint ListClientAlias;

            public readonly Dictionary<uint, ValueStatusTimestamp> ElementValues = new Dictionary<uint, ValueStatusTimestamp>();

            public void CombineWith(ElementValuesCallbackMessage nextElementValuesCallbackMessage)
            {
                foreach (var kvp in nextElementValuesCallbackMessage.ElementValues)
                {
                    ElementValues[kvp.Key] = kvp.Value;
                }                
            }

            public Queue<ElementValuesCallback> SplitForCorrectGrpcMessageSize()
            {
                var fullElementValuesCollection = new ElementValuesCollection();
                using (var memoryStream = new MemoryStream(1024))
                { 
                    using (var writer = new SerializationWriter(memoryStream))
                    {
                        foreach (var kvp in ElementValues)
                        {
                            uint alias = kvp.Key;
                            ValueStatusTimestamp valueStatusTimestamp = kvp.Value;

                            switch (valueStatusTimestamp.Value.ValueStorageType)
                            {
                                case Ssz.Utils.Any.StorageType.Double:
                                    fullElementValuesCollection.DoubleAliases.Add(alias);
                                    fullElementValuesCollection.DoubleValues.Add(valueStatusTimestamp.Value.StorageDouble);
                                    fullElementValuesCollection.DoubleValueStatusCodes.Add(valueStatusTimestamp.ValueStatusCode);
                                    fullElementValuesCollection.DoubleTimestamps.Add(DateTimeHelper.ConvertToTimestamp(valueStatusTimestamp.TimestampUtc));                                    
                                    break;
                                case Ssz.Utils.Any.StorageType.UInt32:
                                    fullElementValuesCollection.UintAliases.Add(alias);
                                    fullElementValuesCollection.UintValues.Add(valueStatusTimestamp.Value.StorageUInt32);
                                    fullElementValuesCollection.UintValueStatusCodes.Add(valueStatusTimestamp.ValueStatusCode);
                                    fullElementValuesCollection.UintTimestamps.Add(DateTimeHelper.ConvertToTimestamp(valueStatusTimestamp.TimestampUtc));                                    
                                    break;
                                case Ssz.Utils.Any.StorageType.Object:
                                    fullElementValuesCollection.ObjectAliases.Add(alias);
                                    writer.WriteObject(valueStatusTimestamp.Value.StorageObject);
                                    fullElementValuesCollection.ObjectValueStatusCodes.Add(valueStatusTimestamp.ValueStatusCode);
                                    fullElementValuesCollection.ObjectTimestamps.Add(DateTimeHelper.ConvertToTimestamp(valueStatusTimestamp.TimestampUtc));                                    
                                    break;
                            }
                        }
                    }
                    memoryStream.Position = 0;
                    fullElementValuesCollection.ObjectValues = Google.Protobuf.ByteString.FromStream(memoryStream);
                }

                var queue = new Queue<ElementValuesCallback>();
                foreach (ElementValuesCollection elementValuesCollection in fullElementValuesCollection.SplitForCorrectGrpcMessageSize())
                {
                    var elementValuesCallback = new ElementValuesCallback();
                    elementValuesCallback.ListClientAlias = ListClientAlias;
                    elementValuesCallback.ElementValuesCollection = elementValuesCollection;
                    queue.Enqueue(elementValuesCallback);
                }
                return queue;
            }

            #endregion
        }

        public class EventMessagesCallbackMessage
        {
            #region public functions

            public uint ListClientAlias;

            public List<EventMessage> EventMessages = new();

            public CaseInsensitiveDictionary<string?>? CommonFields;

            public void CombineWith(EventMessagesCallbackMessage nextEventMessagesCallbackMessage)
            {
                EventMessages.AddRange(nextEventMessagesCallbackMessage.EventMessages);
                if (nextEventMessagesCallbackMessage.CommonFields is not null)
                {
                    if (CommonFields is null)
                    {
                        CommonFields = new CaseInsensitiveDictionary<string?>(nextEventMessagesCallbackMessage.CommonFields);
                    }
                    else
                    {
                        foreach (var kvp in nextEventMessagesCallbackMessage.CommonFields)
                            CommonFields[kvp.Key] = kvp.Value;
                    }                    
                }
            }

            public Queue<EventMessagesCallback> SplitForCorrectGrpcMessageSize()
            {
                var fullEventMessagesCollection = new EventMessagesCollection();
                fullEventMessagesCollection.EventMessages.AddRange(EventMessages);
                if (CommonFields is not null)
                {
                    foreach (var kvp in CommonFields)
                        fullEventMessagesCollection.CommonFields.Add(kvp.Key,
                            kvp.Value is not null ? new NullableString { Data = kvp.Value } : new NullableString { Null = NullValue.NullValue });
                }

                var queue = new Queue<EventMessagesCallback>();
                foreach (EventMessagesCollection eventMessagesCollection in fullEventMessagesCollection.SplitForCorrectGrpcMessageSize())
                {
                    var eventMessagesCallback = new EventMessagesCallback();
                    eventMessagesCallback.ListClientAlias = ListClientAlias;
                    eventMessagesCallback.EventMessagesCollection = eventMessagesCollection;                                       
                    queue.Enqueue(eventMessagesCallback);
                }
                return queue;
            }

            #endregion
        }

        public class LongrunningPassthroughCallbackMessage : Ssz.Utils.DataAccess.LongrunningPassthroughCallback
        {            
        }
    }
}