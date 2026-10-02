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
using Ssz.DataAccessGrpc.Common;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;

namespace Ssz.DataAccessGrpc.ServerBase;

/// <summary>
///   This partial class defines the methods that support the methods 
///   of the ICallback interface.
/// </summary>
public partial class ServerContext
{
    #region public functions

    public void SetResponseStream(object responseStream)
    {
        _responseStreamWriter = (IServerStreamWriter<CallbackMessage>)responseStream;
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
            catch (System.InvalidOperationException ex)
            {
                Logger.LogDebug(ex, @"ServerContext Callback Thread InvalidOperationException");
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
        lock (_messagesSyncRoot)
        {
            _copy_ContextStatusMessagesCollection.Swap(_contextStatusMessagesCollection);            

            _copy_ElementValuesCallbackMessagesCollection.Swap(_elementValuesCallbackMessagesCollection);            

            _copy_EventMessagesCallbackMessagesCollection.Swap(_eventMessagesCallbackMessagesCollection);            

            _copy_LongrunningPassthroughCallbackMessagesCollection.Swap(_longrunningPassthroughCallbackMessagesCollection);            
        }

        try
        {
            if (_responseStreamWriter is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_copy_ContextStatusMessagesCollection.Count > 0)
                {
                    //Logger.LogDebug("ServerContext contextStatusMessagesCollection.Count=" + contextStatusMessagesCollection.Count);

                    foreach (ContextStatusMessage contextStatusMessage in _copy_ContextStatusMessagesCollection)
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var callbackMessage = new CallbackMessage();
                            callbackMessage.ContextStatus = new ContextStatus
                            {
                                StateCode = contextStatusMessage.StateCode
                            };
                            if (!IsConcludeCalledByClient) // Optimization
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

                if (_copy_ElementValuesCallbackMessagesCollection.Count > 0)
                {
                    //Logger.LogDebug("ServerContext elementValuesCallbackMessagesCollection.Count=" + elementValuesCallbackMessagesCollection.Count);

                    foreach (var elementValuesCallbackMessage in _copy_ElementValuesCallbackMessagesCollection)
                    {
                        foreach (ElementValuesCallback elementValuesCallback in elementValuesCallbackMessage.SplitForCorrectGrpcMessageSize())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var callbackMessage = new CallbackMessage
                            {
                                ElementValuesCallback = elementValuesCallback
                            };
                            if (!IsConcludeCalledByClient) // Optimization
                                await _responseStreamWriter.WriteAsync(callbackMessage);
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (_copy_EventMessagesCallbackMessagesCollection.Count > 0)
                {
                    //Logger.LogDebug("ServerContext eventMessagesCallbackMessagesCollection.Count=" + eventMessagesCallbackMessagesCollection.Count);

                    foreach (var eventMessagesCallbackMessage in _copy_EventMessagesCallbackMessagesCollection)
                    {
                        foreach (EventMessagesCallback eventMessagesCallback in eventMessagesCallbackMessage.SplitForCorrectGrpcMessageSize())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            Logger.LogDebug("_responseStream.WriteAsync(callbackMessage)");
                            var callbackMessage = new CallbackMessage
                            {
                                EventMessagesCallback = eventMessagesCallback
                            };
                            if (!IsConcludeCalledByClient) // Optimization
                                await _responseStreamWriter.WriteAsync(callbackMessage);
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (_copy_LongrunningPassthroughCallbackMessagesCollection.Count > 0)
                {
                    Logger.LogDebug("ServerContext longrunningPassthroughCallbackMessagesCollection.Count=" + _copy_LongrunningPassthroughCallbackMessagesCollection.Count);

                    foreach (var longrunningPassthroughCallbackMessage in _copy_LongrunningPassthroughCallbackMessagesCollection)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Logger.LogDebug("_responseStream.WriteAsync(callbackMessage)");
                        var callbackMessage = new CallbackMessage
                        {
                            LongrunningPassthroughCallback = new Common.LongrunningPassthroughCallback
                            {
                                JobId = longrunningPassthroughCallbackMessage.JobId,
                                ProgressPercent = longrunningPassthroughCallbackMessage.ProgressPercent,
                                ProgressLabel = longrunningPassthroughCallbackMessage.ProgressLabel ?? @"",
                                ProgressDetails = longrunningPassthroughCallbackMessage.ProgressDetails ?? @"",
                                StatusCode = longrunningPassthroughCallbackMessage.StatusCode,
                            }
                        };
                        if (!IsConcludeCalledByClient) // Optimization
                            await _responseStreamWriter.WriteAsync(callbackMessage);
                    }
                }
            }
        }
        finally
        {
            _copy_ContextStatusMessagesCollection.Clear();
            _copy_ElementValuesCallbackMessagesCollection.Clear();
            _copy_EventMessagesCallbackMessagesCollection.Clear();
            _copy_LongrunningPassthroughCallbackMessagesCollection.Clear();
        }
    }

    #endregion

    #region private fields

    private IServerStreamWriter<CallbackMessage>? _responseStreamWriter;

    private readonly Task _callbackWorkingTask;        

    private readonly Object _messagesSyncRoot = new Object();

    private FastList<ContextStatusMessage> _contextStatusMessagesCollection = new(1024);
    private FastList<ContextStatusMessage> _copy_ContextStatusMessagesCollection = new(1024);

    private FastList<ElementValuesCallbackMessage> _elementValuesCallbackMessagesCollection = new(1024);
    private FastList<ElementValuesCallbackMessage> _copy_ElementValuesCallbackMessagesCollection = new(1024);

    private FastList<EventMessagesCallbackMessage> _eventMessagesCallbackMessagesCollection = new(1024);
    private FastList<EventMessagesCallbackMessage> _copy_EventMessagesCallbackMessagesCollection = new(1024);

    private FastList<LongrunningPassthroughCallbackMessage> _longrunningPassthroughCallbackMessagesCollection = new(1024);
    private FastList<LongrunningPassthroughCallbackMessage> _copy_LongrunningPassthroughCallbackMessagesCollection = new(1024);

    #endregion                
}    