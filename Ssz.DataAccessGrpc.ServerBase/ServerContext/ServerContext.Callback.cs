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
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="contextStatusMessage"></param>
        public void AddCallbackMessage(ContextStatusMessage contextStatusMessage)
        {
            if (Disposed || _responseStream is null)
                return;

            lock (_messagesSyncRoot)
            {
                _contextStatusMessagesCollection.Add(contextStatusMessage);
            }
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="elementValuesCallbackMessage"></param>
        public void AddCallbackMessage(ElementValuesCallbackMessage elementValuesCallbackMessage)
        {
            if (Disposed || _responseStream is null)
                return;

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
            if (Disposed || _responseStream is null)
                return;

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
            if (Disposed || _responseStream is null)
                return;

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
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(3, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    await OnLoopInWorkingThreadAsync(cancellationToken);                    
                }                
                catch when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, @"ServerContext Callback Thread Exception");                    
                }                
            }

            _responseStream = null;

            Logger.LogDebug(@"ServerContext Callback Thread Exit");
        }

        private async Task OnLoopInWorkingThreadAsync(CancellationToken cancellationToken)
        {
            List<ContextStatusMessage> contextStatusMessagesCollection;
            List<ElementValuesCallbackMessage> elementValuesCallbackMessagesCollection;
            List<EventMessagesCallbackMessage> eventMessagesCallbackMessagesCollection;
            List<LongrunningPassthroughCallbackMessage> longrunningPassthroughCallbackMessagesCollection;
            lock (_messagesSyncRoot)
            {
                contextStatusMessagesCollection = _contextStatusMessagesCollection;
                _contextStatusMessagesCollection = new List<ContextStatusMessage>();

                elementValuesCallbackMessagesCollection = _elementValuesCallbackMessagesCollection;
                _elementValuesCallbackMessagesCollection = new List<ElementValuesCallbackMessage>();

                eventMessagesCallbackMessagesCollection = _eventMessagesCallbackMessagesCollection;
                _eventMessagesCallbackMessagesCollection = new List<EventMessagesCallbackMessage>();

                longrunningPassthroughCallbackMessagesCollection = _longrunningPassthroughCallbackMessagesCollection;
                _longrunningPassthroughCallbackMessagesCollection = new List<LongrunningPassthroughCallbackMessage>();
            }

            if (_responseStream is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (contextStatusMessagesCollection.Count > 0)
                {
                    //Logger.LogDebug("ServerContext contextStatusMessagesCollection.Count=" + contextStatusMessagesCollection.Count);

                    foreach (ContextStatusMessage contextStatusMessage in contextStatusMessagesCollection)
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var callbackMessage = new CallbackMessage();
                            callbackMessage.ContextStatus = new ContextStatus
                            {
                                StateCode = contextStatusMessage.StateCode
                            };
                            await _responseStream.WriteAsync(callbackMessage);
                        }
                        finally
                        {
                            if (contextStatusMessage.StateCode == ContextStateCodes.STATE_ABORTING)
                                CallbackWorkingTask_CancellationTokenSource.Cancel();
                        }                        
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (elementValuesCallbackMessagesCollection.Count > 0)
                {
                    //Logger.LogDebug("ServerContext elementValuesCallbackMessagesCollection.Count=" + elementValuesCallbackMessagesCollection.Count);

                    foreach (var elementValuesCallbackMessage in elementValuesCallbackMessagesCollection)
                    {
                        foreach (ElementValuesCallback elementValuesCallback in elementValuesCallbackMessage.SplitForCorrectGrpcMessageSize())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var callbackMessage = new CallbackMessage
                            {
                                ElementValuesCallback = elementValuesCallback
                            };
                            await _responseStream.WriteAsync(callbackMessage);
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (eventMessagesCallbackMessagesCollection.Count > 0)
                {
                    //Logger.LogDebug("ServerContext eventMessagesCallbackMessagesCollection.Count=" + eventMessagesCallbackMessagesCollection.Count);

                    foreach (var eventMessagesCallbackMessage in eventMessagesCallbackMessagesCollection)
                    {
                        foreach (EventMessagesCallback eventMessagesCallback in eventMessagesCallbackMessage.SplitForCorrectGrpcMessageSize())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            Logger.LogDebug("_responseStream.WriteAsync(callbackMessage)");
                            var callbackMessage = new CallbackMessage
                            {
                                EventMessagesCallback = eventMessagesCallback
                            };
                            await _responseStream.WriteAsync(callbackMessage);
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (longrunningPassthroughCallbackMessagesCollection.Count > 0)
                {
                    Logger.LogDebug("ServerContext longrunningPassthroughCallbackMessagesCollection.Count=" + longrunningPassthroughCallbackMessagesCollection.Count);

                    foreach (var longrunningPassthroughCallbackMessage in longrunningPassthroughCallbackMessagesCollection)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Logger.LogDebug("_responseStream.WriteAsync(callbackMessage)");
                        var callbackMessage = new CallbackMessage
                        {
                            LongrunningPassthroughCallback = new LongrunningPassthroughCallback
                            {
                                JobId = longrunningPassthroughCallbackMessage.JobId,
                                ProgressPercent = longrunningPassthroughCallbackMessage.ProgressPercent,
                                ProgressLabel = longrunningPassthroughCallbackMessage.ProgressLabel ?? @"",
                                ProgressDetails = longrunningPassthroughCallbackMessage.ProgressDetails ?? @"",
                                StatusCode = longrunningPassthroughCallbackMessage.StatusCode,
                            }
                        };
                        await _responseStream.WriteAsync(callbackMessage);
                    }
                }
            }
        }

        #endregion

        #region private fields

        private IServerStreamWriter<CallbackMessage>? _responseStream;

        private readonly Task _callbackWorkingTask;        

        private readonly Object _messagesSyncRoot = new Object();

        private List<ContextStatusMessage> _contextStatusMessagesCollection = new();

        private List<ElementValuesCallbackMessage> _elementValuesCallbackMessagesCollection = new();

        private List<EventMessagesCallbackMessage> _eventMessagesCallbackMessagesCollection = new();

        private List<LongrunningPassthroughCallbackMessage> _longrunningPassthroughCallbackMessagesCollection = new();

        #endregion

        public class ContextStatusMessage
        {
            /// <summary>
            /// 
            /// </summary>
            public uint StateCode;
        }

        public class ElementValuesCallbackMessage
        {
            #region public functions            

            public uint ListClientAlias;

            public readonly Dictionary<uint, ValueStatusTimestamp> ElementValues = new Dictionary<uint, ValueStatusTimestamp>();

            public List<ElementValuesCallback> SplitForCorrectGrpcMessageSize()
            {
                byte[] fullElementValuesCollection;
                using (var memoryStream = new MemoryStream(1024))
                { 
                    using (var writer = new SerializationWriter(memoryStream))
                    {
                        using (writer.EnterBlock(1))
                        {
                            writer.Write(ElementValues.Count);
                            foreach (var kvp in ElementValues)
                            {
                                uint serverAlias = kvp.Key;
                                ValueStatusTimestamp valueStatusTimestamp = kvp.Value;

                                writer.Write(serverAlias);                                
                                valueStatusTimestamp.SerializeOwnedData(writer, null);
                            }
                        }                        
                    }                    
                    fullElementValuesCollection = memoryStream.ToArray();
                }

                List<ElementValuesCallback> list = new();
                foreach (DataChunk elementValuesCollection in ProtobufHelper.SplitForCorrectGrpcMessageSize(fullElementValuesCollection))
                {
                    var elementValuesCallback = new ElementValuesCallback();
                    elementValuesCallback.ListClientAlias = ListClientAlias;
                    elementValuesCallback.ElementValuesCollection = elementValuesCollection;
                    list.Add(elementValuesCallback);
                }
                return list;
            }

            #endregion
        }

        public class EventMessagesCallbackMessage
        {
            #region public functions

            public uint ListClientAlias;

            public List<EventMessage> EventMessages = new();

            public CaseInsensitiveDictionary<string?>? CommonFields;            

            public List<EventMessagesCallback> SplitForCorrectGrpcMessageSize()
            {
                List<EventMessagesCallback> result = new();
                foreach (EventMessagesCollection eventMessagesCollection in ProtobufHelper.SplitForCorrectGrpcMessageSize(EventMessages, CommonFields))
                {
                    var eventMessagesCallback = new EventMessagesCallback();
                    eventMessagesCallback.ListClientAlias = ListClientAlias;
                    eventMessagesCallback.EventMessagesCollection = eventMessagesCollection;
                    result.Add(eventMessagesCallback);
                }
                return result;
            }

            #endregion
        }

        public class LongrunningPassthroughCallbackMessage : Ssz.Utils.DataAccess.LongrunningPassthroughCallback
        {            
        }
    }
}