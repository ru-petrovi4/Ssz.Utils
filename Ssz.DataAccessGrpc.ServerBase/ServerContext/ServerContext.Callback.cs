using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public void SetResponseStream(object responseStream)
        {
            _responseStreamWriter = (IAsyncStreamWriter<CallbackMessage>)responseStream;
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="contextStatusMessage"></param>
        public void AddCallbackMessage(ContextStatusMessage contextStatusMessage)
        {
            if (Disposed || _responseStreamWriter is null)
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
            if (Disposed || _responseStreamWriter is null)
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
            if (Disposed || _responseStreamWriter is null)
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
            if (Disposed || _responseStreamWriter is null)
                return;

            lock (_messagesSyncRoot)
            {
                _longrunningPassthroughCallbackMessagesCollection.Add(longrunningPassthroughCallbackMessage);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listServerAlias"></param>
        /// <param name="isEnabled"></param>
        /// <returns></returns>
        public void EnableListCallback(uint listServerAlias, ref bool isEnabled)
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

            _responseStreamWriter = null;

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

            if (_responseStreamWriter is not null)
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
                            await _responseStreamWriter.WriteAsync(callbackMessage);
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
                            await _responseStreamWriter.WriteAsync(callbackMessage);
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
                            await _responseStreamWriter.WriteAsync(callbackMessage);
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
                        await _responseStreamWriter.WriteAsync(callbackMessage);
                    }
                }
            }
        }

        #endregion

        #region private fields

        private IAsyncStreamWriter<CallbackMessage>? _responseStreamWriter;

        private readonly Task _callbackWorkingTask;        

        private readonly Object _messagesSyncRoot = new Object();

        private List<ContextStatusMessage> _contextStatusMessagesCollection = new();

        private List<ElementValuesCallbackMessage> _elementValuesCallbackMessagesCollection = new();

        private List<EventMessagesCallbackMessage> _eventMessagesCallbackMessagesCollection = new();

        private List<LongrunningPassthroughCallbackMessage> _longrunningPassthroughCallbackMessagesCollection = new();

        #endregion                
    }    
}