using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;
using Ssz.Utils;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Internal.Endpoints;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.OPC.Wrapper.Impl;

namespace Ssz.Xi.Client.Internal.Context
{

    #region Context Management

    /// <summary>
    ///     This partial class defines the Context Management related aspects of the XiContext class.  Two
    ///     static Initiate() methods are defined to create and establish a new context with the Xi server,
    ///     one that in which the calling client application supplies the user credentials, and one in which
    ///     the ClientBase calls into the Xi Client Credentials Project.Current for the user credentials when necessary.
    /// </summary>
    internal partial class XiContext
    {
        #region construction and destruction

        /// <summary>
        ///     <para>
        ///         This method is invoked to create an instance of the XiContext class. The XiContext class implements the
        ///         IXiContext interface. This call establishes the Xi with the server for the calling client application. This
        ///         method accepts the user connection info (credentials) to be used to connect to the server from the caller.
        ///     </para>
        ///     <para> This constructor should only be called by the static Initiate() methods. </para>
        /// </summary>
        /// <param name="resourceManagementServiceEndpoint"></param>
        /// <param name="contextTimeout">
        ///     This parameter supplies an Xi Server context timeout in milliseconds. The Xi ClientBase
        ///     limits this to be not less than seven seconds or greater than thirty minutes.
        /// </param>
        /// <param name="contextOptions">
        ///     This parameter enables various debug and tracing options used to aide in diagnosing
        ///     issues. See ContextOptions enum for the valid values.
        /// </param>
        /// <param name="localeId"> The localed id requested to be used for the context. </param>
        /// <param name="callbackRate"></param>
        /// <param name="applicationName"></param>
        /// <param name="workstationName"></param>
        /// <param name="xiServerInfo"></param>
        /// <param name="keepAliveSkipCount"></param>
        /// <param name="xiCallbackDoer"></param>
        /// <returns>
        ///     An instance of the XiContext class is returned to the client. The client then uses this instance for further
        ///     interactions with the Xi Server.
        /// </returns>
        public XiContext(CaseInsensitiveDictionary<string?> contextParams,
            uint contextTimeout,
            uint contextOptions, uint localeId, string applicationName,
            string workstationName, uint keepAliveSkipCount,
            TimeSpan callbackRate, IDispatcher xiCallbackDoer)
        {
            _contextParams = contextParams;
            _callbackEndpointLastCallUtc = DateTime.UtcNow;
            _xiCallbackDoer = xiCallbackDoer;

            try
            {
                _xiServerInfo = new XiServiceMain(XiServiceMain.MainProgramType.ServiceModeDataServer);
                _xiServerInfo.OnStartDataServer();
                _serverKeepAliveSkipCount = keepAliveSkipCount;
                _serverCallbackRate = callbackRate;

                if (applicationName is not null) _applicationName = applicationName;
                else
                {
                    string appDomainName = AppDomain.CurrentDomain.FriendlyName;
                    _applicationName = appDomainName.Replace(".vshost.", ".");
                }

                if (workstationName is not null) _workstationName = workstationName;
                else _workstationName = Dns.GetHostName();

                _serverContextTimeoutInMs = ValidateServerContextTimeout(contextTimeout);

                // make the send timeout that WCF uses to timeout a request greater than the context timeout to allow the keep-alive mechanism to work properly
                _sendTimeout = new TimeSpan(0, 0, 0, 0, (int) (_serverContextTimeoutInMs + _serverContextTimeoutInMs));

                _contextOptions = contextOptions;

                //if (localeId != 0)
                //{
                //    if (_xiServerInfo.ServerEntry.ServerDescription?.SupportedLocaleIds is not null &&
                //        _xiServerInfo.ServerEntry.ServerDescription.SupportedLocaleIds.Count > 0)
                //    {
                //        _localeId = 0;

                //        foreach (
                //            uint supportedLocaleId in _xiServerInfo.ServerEntry.ServerDescription.SupportedLocaleIds)
                //        {
                //            if (localeId == supportedLocaleId)
                //            {
                //                _localeId = localeId;
                //                break;
                //            }
                //        }

                //        if (_localeId == 0)
                //            _localeId = _xiServerInfo.ServerEntry.ServerDescription.SupportedLocaleIds[0];
                //    }
                //    else
                //    {
                //        _localeId = localeId;
                //    }
                //}
                _localeId = localeId;

                _contextOptions = contextOptions;                

                _iResourceManagement = _xiServerInfo as IResourceManagement;
                if (_iResourceManagement is null)
                    throw new Exception("Failed to create the IResourceManagement WCF Channel");

                _readEndpoint = new XiReadEndpoint(_xiServerInfo as IRead, 
                    new EndpointDefinition { EndpointId = nameof(IRead) },
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    0);
                _writeEndpoint = new XiWriteEndpoint(_xiServerInfo as IWrite,
                    new EndpointDefinition { EndpointId = nameof(IWrite) },
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    0);
                _pollEndpoint = new XiPollEndpoint(_xiServerInfo as IPoll,
                    new EndpointDefinition { EndpointId = nameof(IPoll) },
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    0);
                _callbackEndpoint = new XiCallbackEndpoint(_xiServerInfo as IRegisterForCallback,
                    new EndpointDefinition { EndpointId = nameof(IRead) },
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    0,
                    _xiCallbackDoer);

                try
                {
                    _contextId = _iResourceManagement.Initiate(_applicationName, _workstationName, ref _localeId,
                        ref _serverContextTimeoutInMs, ref _contextOptions, out _reInitiateKey);
                }
                catch (Exception ex)
                {
                    // if the connect failed, close this channel factory instance and try again                    
                    _iResourceManagement = null;
                    throw new ResourceManagementInitiateException(ex);
                }

                if (_contextId is null) throw new Exception("Server returns null contextId.");

                SetResourceManagementLastCallUtc();

                lock (StaticActiveContextsSyncRoot)
                {
                    StaticActiveContexts.Add(ContextId, this);
                }
            }            
            catch
            {
                _contextId = null;
                if (_xiServerInfo is not null)
                {
                    _xiServerInfo.OnStopDataServer();
                    _xiServerInfo = null;
                }
                throw;
            }
        }

        /// <summary>
        ///     This method disposes of the object.  It is invoked by the client application, client base, or
        ///     the destructor of this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method disposes of the object.  It is invoked by the parameterless Dispose()
        ///     method of this object.  It is expected that the Xi client application
        ///     will perform a Dispose() on each active context to close the connection with the
        ///     Xi Server.  Failure to perform the close will result in the Xi Context remaining
        ///     active until the application exits.
        /// </summary>
        /// <summary>
        /// </summary>
        /// <param name="disposing">
        ///     <para>
        ///         This parameter indicates, when TRUE, this Dispose() method was called directly or indirectly by a user's
        ///         code. When FALSE, this method was called by the runtime from inside the finalizer.
        ///     </para>
        ///     <para>
        ///         When called by user code, references within the class should be valid and should be disposed of properly.
        ///         When called by the finalizer, references within the class are not guaranteed to be valid and attempts to
        ///         dispose of them should not be made.
        ///     </para>
        /// </param>
        /// <returns> Returns TRUE to indicate that the object has been disposed. </returns>
        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // close the context with the server if necessary
                if (_iResourceManagement is not null)
                {
                    try
                    {
                        if (ContextId != @"" && !ServerContextIsClosing) 
                            _iResourceManagement.Conclude(ContextId);
                    }
                    catch
                    {
                    }

                    _iResourceManagement = null;
                }

                // Dispose of the Read, Write, and Subscribe Endpoints
                if (_readEndpoint is not null)
                {
                    _readEndpoint.Dispose();
                    _readEndpoint = null;
                }
                if (_writeEndpoint is not null)
                {
                    _writeEndpoint.Dispose();
                    _writeEndpoint = null;
                }
                if (_pollEndpoint is not null)
                {
                    _pollEndpoint.Dispose();
                    _pollEndpoint = null;
                }
                if (_callbackEndpoint is not null)
                {
                    _callbackEndpoint.Dispose();
                    _callbackEndpoint = null;
                }

                ServerContextIsClosing = true;

                // remove the context from the list of contexts
                if (ContextId != @"")
                    lock (StaticActiveContextsSyncRoot)
                    {
                        StaticActiveContexts.Remove(ContextId);
                    }

                if (_xiServerInfo is not null)
                {
                    _xiServerInfo.OnStopDataServer();                    
                }
            }

            _iResourceManagement = null;
            _readEndpoint = null;
            _writeEndpoint = null;
            _pollEndpoint = null;
            _callbackEndpoint = null;            
            _listArray = null;            

            _disposed = true;
        }

        /// <summary>
        ///     The standard destructor invoked by the .NET garbage collector during Finalize.
        /// </summary>
        ~XiContext()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method is invoked to find a context in the static StaticActiveContexts dictionary for a specified context id.
        /// </summary>
        /// <param name="contextId"> The context to look up. </param>
        /// <returns> The context if found, otherwise null. </returns>
        public static XiContext? LookUpContext(string contextId)
        {
            XiContext? context = null;
            try
            {
                lock (StaticActiveContextsSyncRoot)
                {
                    StaticActiveContexts.TryGetValue(contextId, out context);
                }
            }
            catch (TimeoutException)
            {
                return null;
            }
            return context;
        }

        public void NotifyCallbackRecieved()
        {
            lock (_callbackEndpointLastCallUtcSyncRoot)
            {
                _callbackEndpointLastCallUtc = DateTime.UtcNow;
            }
        }

        /// <summary>
        ///     This method is called by the ClientBase to notify the client application of context events.
        /// </summary>
        /// <param name="sender"> The calling object. </param>
        /// <param name="contextNotificationData"> The notification parameters. </param>
        public void RaiseContextNotifyEvent(object sender, XiContextNotificationData contextNotificationData)
        {
            ContextNotifyEvent(sender, contextNotificationData);
        }

        /// <summary>
        ///     <para>
        ///         This method is used to get the description of the server. This method can be called before a context has
        ///         been established with the server.
        ///     </para>
        /// </summary>
        /// <returns> The description of the server. </returns>
        public ServerDescription? Identify()
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            try
            {
                _serverDescription = _iResourceManagement.Identify(ContextId);
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
            SetResourceManagementLastCallUtc();
            return _serverDescription;
        }

        /// <summary>
        ///     This method is used to get the state of the server, and
        ///     the state of any wrapped servers.
        /// </summary>
        /// <returns> The status of the Xi server and the status of wrapped servers. </returns>
        public IEnumerable<ServerStatus>? Status()
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            try
            {
                _serverStatusList = _iResourceManagement.Status(ContextId);
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
            SetResourceManagementLastCallUtc();
            return _serverStatusList;
        }

        /// <summary>
        ///     <para> This method returns text descriptions of error codes. </para>
        /// </summary>
        /// <param name="resultCodes"> The result codes for which text descriptions are being requested. </param>
        /// <returns>
        ///     The list of result codes and if a result code indicates success, the requested text descriptions. The size
        ///     and order of this list matches the size and order of the resultCodes parameter.
        /// </returns>
        public IEnumerable<RequestedString>? LookupResultCodes(IEnumerable<uint> resultCodes)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            List<RequestedString>? requestedStrings = null;
            try
            {
                List<uint> errorCodeList = resultCodes.ToList();
                requestedStrings = _iResourceManagement.LookupResultCodes(ContextId, errorCodeList);
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
            SetResourceManagementLastCallUtc();
            return requestedStrings;
        }

        /// <summary>
        ///     <para>
        ///         This method is used to find objects in the server. The client uses the findCriteria parameter to identify a
        ///         starting branch and a set of filter criteria. It also specifies the maximum number of objects to return.
        ///     </para>
        ///     <para>
        ///         The server examines the objects that are children of the specified branch and selects those that match the
        ///         filter criteria. Note that "children" are objects whose root paths can be created by appending their names to
        ///         the path used to identify the starting branch.
        ///     </para>
        ///     <para>
        ///         The object attributes of the selected objects are returned to the client. The number returned is limited by
        ///         the number specified in the numberToReturn parameter. If the number returned is less than than that number,
        ///         then the client can safely assume that the server has no more to return.
        ///     </para>
        ///     <para>
        ///         However, if the number returned is equal to that number, then the client can retrieve the next set of
        ///         results by issuing another FindObjects() call with the findCriteria parameter set to null. A null findCriteria
        ///         indicates to the server to continue returning results from those remaining in the list. The client eventually
        ///         detects the end of the list by receiving a response that returns less than specified by the numberToReturn
        ///         parameter.
        ///     </para>
        /// </summary>
        /// <param name="findCriteria">
        ///     The criteria used by the server to find objects. If this parameter is null, then this call
        ///     is a continuation of the previous find.
        /// </param>
        /// <param name="numberToReturn"> The maximum number of objects to return in a single response. </param>
        /// <returns>
        ///     <para> The list of object attributes for the objects that met the filter criteria. </para>
        ///     <para>
        ///         Returns null if the starting object is a leaf, or no objects were found that meet the filter criteria, or if
        ///         the call was made with a null findCriteria and there are no more objects to return.
        ///     </para>
        /// </returns>
        public IEnumerable<ObjectAttributes>? FindObjects(FindCriteria findCriteria, uint numberToReturn)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            List<ObjectAttributes>? objectAttributes = null;
            if (_iResourceManagement is null) throw new InvalidOperationException();
            try
            {
                objectAttributes = _iResourceManagement.FindObjects(ContextId, findCriteria, numberToReturn);
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
            SetResourceManagementLastCallUtc();
            return objectAttributes;
        }

        /// <summary>
        ///     This method is used to read the standard MIB.
        /// </summary>
        /// <returns> The standard MIB is returned. </returns>
        public StandardMib? GetStandardMib()
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            return GetStandardMibInternal();
        }

        /// <summary>
        ///     This method is used to
        ///     keep the context alive.
        /// </summary>
        public void KeepContextAlive(DateTime nowUtc)
        {
            uint timeDiffInMs = (uint) (nowUtc - _resourceManagementLastCallUtc).TotalMilliseconds + 500;

            bool bKeepAliveIntervalExpired = timeDiffInMs >= KeepAliveIntervalMs;

            if (bKeepAliveIntervalExpired)
            {
                if (!ServerContextIsClosing) 
                    KeepEndpointsAlive(nowUtc);
            }
            
            if (_pendingContextNotificationData is not null)
            {
                RaiseContextNotifyEvent(this, _pendingContextNotificationData);
                _pendingContextNotificationData = null;
            }
        }

        /// <summary>
        ///     This event is used to notify the ClientBase user of events that occur within the ClientBase.
        ///     Caution: Be sure to disconnect the event handler prior to returning.
        /// </summary>
        public event XiContextNotification ContextNotifyEvent = delegate { };

        /// <summary>
        ///     This property is the server-unique identifier of the context. It is returned by the server
        ///     when the client application creates the context.        
        /// </summary>
        public string ContextId
        {
            get { return _contextId ?? ""; }
        }

        /// <summary>
        ///     The Windows LocaleId (language/culture id) for the context.  Its default value
        ///     is automatically set to the LocaleId of the calling client application.
        /// </summary>
        public uint LocaleId
        {
            get { return _localeId; }
        }        

        /// <summary>
        ///     The ContextOptions for this context. See Contracts.Constants.ContextOptions for standard values.
        /// </summary>
        public uint ContextOptions
        {
            get { return _contextOptions; }
        }

        /// <summary>
        ///     The name of the client application exe.
        /// </summary>
        public string ApplicationName
        {
            get { return _applicationName; }
        }

        /// <summary>
        ///     The name of the client workstation.
        /// </summary>
        public string WorkstationName
        {
            get { return _workstationName; }
        }

        /// <summary>
        ///     This property is the Resource Management Interface used to access IResourceManagement methods on the server
        /// </summary>
        public IResourceManagement? ResourceManagement
        {
            get { return _iResourceManagement; }
        }

        /// <summary>
        ///     A unique clientListId used to identify a client context when re-initiating a context.
        ///     This clientListId is provided to prevent interlopers from using a ContextId that they
        ///     obtain by watching watching unencrypted Xi Endpoint traffic.
        /// </summary>
        public string ReInitiateKey
        {
            get { return _reInitiateKey; }
        }

        /// <summary>
        ///     The publically visible context timeout provided to the server (in msecs). If the server fails to
        ///     receive a call from the client for this period, it will close the context.
        ///     Within this time period, if there was a communications failure, the client can
        ///     attempt to ReInitiate the connection with the server for this context.
        /// </summary>
        public uint ContextTimeout
        {
            get { return _serverContextTimeoutInMs; }
        }

        /// <summary>
        ///     Inidicates, when TRUE, that the context is closing or has completed closing
        ///     and will not accept any more requests on the context.
        /// </summary>
        public bool ServerContextIsClosing
        {
            get { return _serverContextIsClosing; }
            set { _serverContextIsClosing = value; }
        }

        /// <summary>
        ///     This property is the standard MIB of the server.  This property is retrieved from the server during establishment
        ///     of the context.
        /// </summary>
        public StandardMib StandardMib
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

                if (null == _standardMib) GetStandardMibInternal(); // This method updates _StandardMib
                if (null == _standardMib) throw new Exception("Failed to obtain the Standard MIB");

                return _standardMib;
            }
        }

        #endregion

        #region private functions

        private void SetResourceManagementLastCallUtc()
        {
            _resourceManagementLastCallUtc = DateTime.UtcNow;
        }

        /// <summary>
        ///     This method is used to read the standard MIB.
        /// </summary>
        /// <returns> The standard MIB is returned. </returns>
        private StandardMib? GetStandardMibInternal()
        {
            if (_iResourceManagement is null) throw new InvalidOperationException();
            try
            {
                _standardMib = _iResourceManagement.GetStandardMib(ContextId);
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
            SetResourceManagementLastCallUtc();
            return _standardMib;
        }

        /// <summary>
        ///     <para>
        ///         This method is invoked to ensure that the inactivity timeout to be used for the context falls within
        ///         prescribed limits (is not too short or too long). If the timeout period supplied in the call is outside one of
        ///         these limits, this method returns the limit that was exceeded as timeout to use.
        ///     </para>
        ///     <para> The maximum value for the context timeout is 30 minutes. </para>
        ///     <para>
        ///         The minimum value for the context timeout is 2 times the keep-alive timer period plus the time it takes WCF
        ///         to detect and report a communications failure. This should allow the client base to detect a failure and
        ///         respond to it before the context times out in the server
        ///     </para>
        ///     <para> Note that the keep-alive logic will send a keep alive one keep-alive timer period prior to this timeout. </para>
        /// </summary>
        /// <param name="serverContextTimeoutInMs">
        ///     The inactivity timeout period of the context. This is the same value used by
        ///     the server to time out and close the context for inactivity.
        /// </param>
        /// <returns> The context timeout to use. </returns>
        private uint ValidateServerContextTimeout(uint serverContextTimeoutInMs)
        {            
            if (serverContextTimeoutInMs < 9000) return 9000; // The minimum timeout is nine seconds.
            if (serverContextTimeoutInMs > 30 * 60 * 1000) return 30 * 60 * 1000; // The maximum timeout is 30 minutes.
            return serverContextTimeoutInMs;
            /*
            uint validatedTimeoutInMs;

            uint minTimeInMs = KeepAliveTimerPeriodInMs + KeepAliveTimerPeriodInMs + WcfFailedCommsExceptionTimeInMs +
                               TimeToRecoverCommunicationsInMs;
            const uint maxTimeInMs = 30*60*1000; // 30 min

            if (serverContextTimeoutInMs < minTimeInMs) validatedTimeoutInMs = minTimeInMs;
            else if (serverContextTimeoutInMs > maxTimeInMs) validatedTimeoutInMs = maxTimeInMs;
            else validatedTimeoutInMs = serverContextTimeoutInMs;

            return validatedTimeoutInMs;*/
        }

        /*
        /// <summary>
        ///   This method is invoked to issue a ReInitiate message to the Xi Server.
        /// </summary>
        private void ReInitiateContext()
        {
            try
            {
                if (_iResourceManagement is not null)
                {
                    // close the existing channel
                    ChannelCloser.Close(_iResourceManagement);
                    _iResourceManagement = null;
                }

                // Reopen the IResourceManagement channel
                var cfIResourceManagement = new ChannelFactory<IResourceManagement>(_connectedResourceManagementServiceEndpoint.Binding,
                                                                                    _connectedResourceManagementServiceEndpoint.Address);

                cfIResourceManagement.Credentials.Windows.AllowedImpersonationLevel =
                    TokenImpersonationLevel.Delegation;
                XiEndpointRoot.SetMaxItemsInObjectGraph(cfIResourceManagement.Endpoint.Contract.Operations,
                                            _resourceManagementEndpointConfig.MaxItemsInObjectGraph);
                    

                _iResourceManagement = cfIResourceManagement.CreateChannel();

                _iResourceManagement.ReInitiate(ContextId, ref _contextOptions, ref _reInitiateKey);
                SetResourceManagementLastCallUtc();
                   
                // if the callback endpoint is being used, reestablish it if a callback has not been received
                // since the resource management EP failure was detected (if the callback was received after the 
                // resource management EP failure was detected, then the callback EP should be working
                // only do this if the context has been re-initiated
                if (_callbackEndpoint is not null)
                {
                    using (_callbackEndpoint.SyncRoot.Enter())
                    {
                        if (!_callbackEndpoint.Disposed) ReEstablishCallbackEp();
                    }
                }
            }
            catch (FaultException<XiFault> fex)
            {
                // if not a server shutdown exception, notify the client of this server error
                if (!IsServerShutdownOrNoContext(fex))
                    _pendingContextNotificationData =
                                    new XiContextNotificationData(XiContextNotificationType.ReInitiateException, fex);
            }
            catch
            {
                _pendingContextNotificationData =
                                    new XiContextNotificationData(
                                        XiContextNotificationType.ResourceManagementDisconnected,
                                        null);
            }
        }

        /// <summary>
        ///   This method is invoked to close the callback endpoint, reopen it, and send a SetCallback 
        ///   message to the server.
        ///   Preconditions _callbackEndpoint must be locked.
        /// </summary>
        private void ReEstablishCallbackEp()
        {
            try
            {
                ChannelCloser.Close(_callbackEndpoint.Channel);
                _callbackEndpoint.CreateChannel();
                
                _callbackEndpoint.SetCallback(ContextId, _callbackEndpoint.KeepAliveSkipCount, _callbackEndpoint.CallbackRate);
                
                _callbackEndpoint.LastCallUtc = DateTime.UtcNow;
            }
            catch
            {
                _pendingContextNotificationData =
                                    new XiContextNotificationData(XiContextNotificationType.EndpointDisconnected,
                                                                _callbackEndpoint);
            }
        }*/

        /// <summary>
        ///     This method checks the exception and notifies the client application using the Abort
        ///     callback if the server has shutdown or if the context is not open in the server.
        /// </summary>
        /// <param name="fe"> The exception to check </param>
        /// <returns> Returns TRUE if the server has shutdown or the context is not open in the server. Otherwise FALSE. </returns>
        private bool IsServerShutdownOrNoContextServerFault(FaultException<XiFault> fe)
        {
            //if (fe.Detail.ErrorCode == XiFaultCodes.E_SERVER_SHUTDOWN)
            //{
            //    var serverStatus = new ServerStatus();
            //    if (_serverDescription is not null)
            //    {
            //        serverStatus.ServerName = _serverDescription.ServerName;
            //        serverStatus.ServerType = _serverDescription.ServerTypes;
            //    }
            //    serverStatus.ServerState = ServerState.Aborting;
            //    serverStatus.CurrentTime = DateTime.UtcNow;
            //    serverStatus.Info = fe.Message;
            //    ServerContextIsClosing = true;
            //    _pendingContextNotificationData = new XiContextNotificationData(XiContextNotificationType.Shutdown,
            //        serverStatus);
            //    return true;
            //}
            //if (fe.Detail.ErrorCode == XiFaultCodes.E_NOCONTEXT)
            //{
            //    var serverStatus = new ServerStatus();
            //    if (_serverDescription is not null)
            //    {
            //        serverStatus.ServerName = _serverDescription.ServerName;
            //        serverStatus.ServerType = _serverDescription.ServerTypes;
            //    }
            //    serverStatus.ServerState = ServerState.Aborting;
            //    serverStatus.CurrentTime = DateTime.UtcNow;
            //    serverStatus.Info = "The context with the server is not open";
            //    ServerContextIsClosing = true;
            //    _pendingContextNotificationData = new XiContextNotificationData(XiContextNotificationType.Shutdown,
            //        serverStatus);
            //    return true;
            //}
            return false;
        }

        /// <summary>
        ///     <para> Re throws. </para>
        ///     <para>
        ///         This method processes an exception thrown when the client application calls one of the methods on the
        ///         IResourceManagment interface.
        ///     </para>
        ///     <para>
        ///         If the exception is a FaultException, the exception is from the server and is rethrown unless the exception
        ///         indicates that the server has shutdown. In this case the Abort callback is called to notify the client of the
        ///         shutdown.
        ///     </para>
        ///     <para>
        ///         If the exception is a CommunicationException, then the ThrowOnDisconnectedEndpoint() method is called on the
        ///         ResourceManagment endpoint to throw the exception back to the calling client application to notify it of the
        ///         failed endpoint.
        ///     </para>
        ///     <para> For all other exceptions, the exception is rethrown. </para>
        /// </summary>
        /// <param name="ex"> The exception that was thrown. </param>
        private void ProcessRemoteMethodCallException(Exception ex)
        {
            if (ex is FaultException<XiFault>)
            {
                // if not a server shutdown, then throw the error message from the server
                //if (IsServerShutdownOrNoContextServerFault(ex as FaultException<XiFault>)) return;
                _pendingContextNotificationData =
                    new XiContextNotificationData(XiContextNotificationType.EndpointDisconnected,
                        ex);

                //Logger?.LogDebug(ex);
            }
            //else if (ex is CommunicationException)
            //{
            //    _pendingContextNotificationData =
            //        new XiContextNotificationData(XiContextNotificationType.EndpointDisconnected,
            //            ex);
            //}
            else
            {
                _pendingContextNotificationData = new XiContextNotificationData(XiContextNotificationType.EndpointFail,
                    ex);
            }

            throw ex;
        }

        /// <summary>
        ///     This method keeps the endpoints of the context alive, but only if the ResourceManagment
        ///     endpoint is connected to the server
        /// </summary>
        private void KeepEndpointsAlive(DateTime nowUtc)
        {
            // Test the ResourceManagement state individually since its state can change as a result of any of the actions taken
            try
            {
                if (!ServerContextIsClosing) // reduce the timing window by checking this again here
                {
                    // set to ResponsePending to prevent multiple calls from queueing up internally in WCF
                    if (_iResourceManagement is null) throw new InvalidOperationException();
                    _iResourceManagement.ClientKeepAlive(ContextId);
                    // set this immediately
                    _resourceManagementLastCallUtc = nowUtc;
                    // only set last access if the call to the server succeeded
                }
            }
            catch (FaultException<XiFault> fex)
            {
                // if not a server shutdown exception, notify the client of this server error
                if (!IsServerShutdownOrNoContextServerFault(fex))
                    _pendingContextNotificationData =
                        new XiContextNotificationData(XiContextNotificationType.ClientKeepAliveException, null);
            }
            catch
            {
                // if not a comms exception, then the server must be down so treat it like a comms exception
            }

            /*
            // Only check the state of the other endpoints if the ResourceManagement endpoint is open
            if (ResourceManagementChannel is not null && ResourceManagementChannel.State == CommunicationState.Opened && _callbackEndpoint is not null)
            {
                using (_callbackEndpoint.SyncRoot.Enter())
                {
                    // if a Read Keep-Alive was sent and the response was not received or if the endpoint has timed-out, 
                    // reset the endpoint
                    if (!_callbackEndpoint.Disposed && _callbackEndpoint.Channel.State != CommunicationState.Opened)
                    {
                        ReEstablishCallbackEp();
                    }
                }
            }

            // and this should be handled in the same execution of this method
            if (ResourceManagementChannel is null || ResourceManagementChannel.State != CommunicationState.Opened) ReInitiateContext();
            */

            // check to see if a callback hasn't been received during the callback rate interval
            // Do this whether or not the ResourceManagement endpoint is open
            if (_callbackEndpoint is not null)
            {
                if (!_callbackEndpoint.Disposed)
                {
                    DateTime callbackEndpointLastCallUtc;
                    lock (_callbackEndpointLastCallUtcSyncRoot)
                    {
                        callbackEndpointLastCallUtc = _callbackEndpointLastCallUtc;
                    }
                    // Use an int for timeDiffMsecs to cover the case when a just received callback came after 
                    // timeThisLoop
                    // Subtract 2 seconds from the time difference to provide a little buffer to accommodate 
                    // server timer variations and network delays
                    var timeDiffMsecs = (int) (nowUtc - callbackEndpointLastCallUtc).TotalMilliseconds;
                    if (timeDiffMsecs >
                        _callbackEndpoint.CallbackRate.TotalMilliseconds +
                        _callbackEndpoint.CallbackRate.TotalMilliseconds)
                    {
                        _pendingContextNotificationData =
                            new XiContextNotificationData(XiContextNotificationType.ServerKeepAliveError, null);
                    }
                }
            }
        }

        #endregion

        #region private fields

        private static readonly object StaticActiveContextsSyncRoot = new object();

        /// <summary>
        ///     This static data member contains the dictionary of all contexts defined for this instance of the Client Base.
        /// </summary>
        private static readonly Dictionary<string, XiContext> StaticActiveContexts =
            new Dictionary<string, XiContext>();

        private XiContextNotificationData? _pendingContextNotificationData;

        private CaseInsensitiveDictionary<string?> _contextParams;

        /// <summary>
        ///     This data member is the Endpoint Discovery object used to access the server for its
        ///     connection information. The Endpoint Discovery object retrieves the endpoints of the
        ///     server that are used for browsing, reading, writing, and subscribing.  It also sorts
        ///     them into the preferred order of use. For example, if the client and server are on the
        ///     same machine, the netPipe endpoints will sort to the top.
        /// </summary>
        private XiServiceMain _xiServerInfo;

        /// <summary>
        ///     This property contains the client-requested keepAliveSkipCount for the subscription.
        ///     The server may negotiate this value up or down. The keepAliveSkipCount indicates
        ///     the number of consecutive UpdateRate cycles for a list that occur with nothing to
        ///     send before an empty callback is sent to indicate a keep-alive message. For example,
        ///     if the value of this parameter is 1, then a keep-alive callback will be sent each
        ///     UpdateRate cycle for each list assigned to the callback for which there is nothing
        ///     to send.  A value of 0 indicates that keep-alives are not to be sent for any list
        ///     assigned to the callback.
        /// </summary>
        private readonly uint _serverKeepAliveSkipCount;

        /// <summary>
        ///     <para>
        ///         This property indicates the maximum time between callbacks that are sent to the client. The server may
        ///         negotiate this value up or down, but a null value or a value representing 0 time is not valid.
        ///     </para>
        ///     <para>
        ///         If there are no callbacks to be sent containing data or events for this period of time, an empty callback
        ///         will be sent as a keep-alive. The timer for this time-interval starts when the SetCallback() response is
        ///         returned by the server.
        ///     </para>
        /// </summary>
        private readonly TimeSpan _serverCallbackRate;

        /// <summary>
        ///     This member indicates, when TRUE, that the object has been disposed by the Dispose(bool isDisposing) method.
        /// </summary>
        private bool _disposed;

        /// <summary>
        ///     This data member represents the ApplicationName public property
        /// </summary>
        private readonly string _applicationName;

        /// <summary>
        ///     This data member is the private representation of the ContextId interface property.
        /// </summary>
        private readonly string? _contextId;

        /// <summary>
        ///     This data member represents the WorkstationName public property
        /// </summary>
        private readonly string _workstationName;

        /// <summary>
        ///     This data member represents the ContextOptions public property
        /// </summary>
        private readonly uint _contextOptions = (uint) global::Xi.Contracts.Constants.ContextOptions.NoOptions;

        /// <summary>
        ///     The private representation of the IResourceManagement public property
        /// </summary>
        private IResourceManagement? _iResourceManagement;

        /// <summary>
        ///     This data member is the private representation of the LocaleId interface property.
        /// </summary>
        private readonly uint _localeId = (uint) Thread.CurrentThread.CurrentCulture.LCID;        

        /// <summary>
        ///     This data member is the private representation of the public ContextIimeout property
        /// </summary>
        private readonly uint _serverContextTimeoutInMs; // Context timeout provided to Server

        /// <summary>
        ///     The time of receipt of the response to the last successful IResourceManagement call.
        /// </summary>
        private DateTime _resourceManagementLastCallUtc;

        /// <summary>
        ///     This data member is the private representation of the public ReInitateKey property
        /// </summary>
        private readonly string _reInitiateKey;

        /// <summary>
        ///     This data member contains the Server Description for this Xi Context.  Is set by
        ///     the Identify() method during context establishment.
        /// </summary>
        private ServerDescription? _serverDescription;

        /// <summary>
        ///     The status of the server
        /// </summary>
        private List<ServerStatus>? _serverStatusList;

        /// <summary>
        ///     This data member is the private representation of the StandardMib interface property.
        /// </summary>
        private StandardMib? _standardMib;

        /// <summary>
        ///     Inidicates, when TRUE, that the context is closing or has completed closing
        ///     and will not accept any more requests on the context.
        /// </summary>
        private volatile bool _serverContextIsClosing;        

        private DateTime _callbackEndpointLastCallUtc;
        private readonly object _callbackEndpointLastCallUtcSyncRoot = new object();

        private readonly IDispatcher _xiCallbackDoer;

        /// <summary>
        ///     The time interval that controls when ClientKeepAlive messages are
        ///     sent to the server.  If no IResourceManagement messages are sent to
        ///     the server for this period of time, a ClientKeepAlive message is
        ///     sent.  The value is expressed in milliseconds.  This value is the
        ///     same for all contexts.
        /// </summary>
        private const uint KeepAliveIntervalMs = 10000;

        /// <summary>
        ///     The frequency, in milliseconds, for firing the _keepAliveTimer
        /// </summary>
        private const uint KeepAliveTimerPeriodInMs = 5000;

        /// <summary>
        ///     The estimated time for the underlying communications problem to recover.
        /// </summary>
        private const uint TimeToRecoverCommunicationsInMs = 60000;

        /// <summary>
        ///     The estimated time for WCF to detect and call the Exception for failed
        ///     communications with the server. This time should be set to a time period
        ///     to cover worst case detection time.
        /// </summary>
        private const uint WcfFailedCommsExceptionTimeInMs = 60000;

        #endregion        
    }

    #endregion // Context Management
}