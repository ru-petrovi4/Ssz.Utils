using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;
using Ssz.Dcs.Addons.OpcClient;
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
        /// <param name="contextParams"></param>
        /// <param name="localeId"></param>
        /// <param name="applicationName"></param>
        /// <param name="workstationName"></param>
        /// <param name="keepAliveSkipCount"></param>
        /// <param name="callbackRate"></param>
        /// <param name="xiCallbackDoer"></param>
        public XiContext(
            CaseInsensitiveDictionary<string?> contextParams,            
            uint localeId, string applicationName,
            string workstationName,
            TimeSpan callbackRate, IDispatcher xiCallbackDoer)
        {
            _contextParams = contextParams;            
            _xiCallbackDoer = xiCallbackDoer;            

            try
            {
                XiOPCWrapperServer.Initialize(contextParams);

                _xiServer = new XiOPCWrapperServer();               
                
                _serverCallbackRate = callbackRate;

                if (applicationName is not null) _applicationName = applicationName;
                else
                {
                    string appDomainName = AppDomain.CurrentDomain.FriendlyName;
                    _applicationName = appDomainName.Replace(".vshost.", ".");
                }

                if (workstationName is not null) _workstationName = workstationName;
                else _workstationName = Dns.GetHostName();

                //if (localeId != 0)
                //{
                //    if (_xiServerEntry.ServerDescription?.SupportedLocaleIds is not null &&
                //        _xiServerEntry.ServerDescription.SupportedLocaleIds.Count > 0)
                //    {
                //        _localeId = 0;

                //        foreach (
                //            uint supportedLocaleId in _xiServerEntry.ServerDescription.SupportedLocaleIds)
                //        {
                //            if (localeId == supportedLocaleId)
                //            {
                //                _localeId = localeId;
                //                break;
                //            }
                //        }

                //        if (_localeId == 0)
                //            _localeId = _xiServerEntry.ServerDescription.SupportedLocaleIds[0];
                //    }
                //    else
                //    {
                //        _localeId = localeId;
                //    }
                //}
                _localeId = localeId;       

                _iResourceManagement = _xiServer as IResourceManagement;
                if (_iResourceManagement is null)
                    throw new Exception("Failed to create the IResourceManagement WCF Channel");

                _callbackEndpoint = new XiCallbackEndpoint(_xiServer as IRegisterForCallback,                    
                    _xiCallbackDoer);

                try
                {
                    _contextId = _iResourceManagement.Initiate(_applicationName, _workstationName, ref _localeId);
                }
                catch (Exception ex)
                {
                    // if the connect failed, close this channel factory instance and try again                    
                    _iResourceManagement = null;
                    throw new ResourceManagementInitiateException(ex);
                }

                if (_contextId is null) throw new Exception("Server returns null contextId.");

                _callbackEndpoint.SetCallback(ContextId, _serverCallbackRate);

                lock (StaticActiveContextsSyncRoot)
                {
                    StaticActiveContexts.Add(ContextId, this);
                }
            }            
            catch
            {                
                if (_xiServer is not null)
                {
                    ((IResourceManagement)_xiServer).Conclude(_contextId ?? @"");
                    _xiServer = null;
                }
                _contextId = null;
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
            }

            _iResourceManagement = null;            
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
            
            return objectAttributes;
        }        

        /// <summary>
        ///     This method is used to
        ///     keep the context alive.
        /// </summary>
        public void DoWork(DateTime nowUtc)
        {   
            if ((uint)(nowUtc - _lastStatusDateTimeUtc).TotalMilliseconds > StatusIntervalMs)
            {
                _lastStatusDateTimeUtc = nowUtc;
                Status();
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
        ///     Inidicates, when TRUE, that the context is closing or has completed closing
        ///     and will not accept any more requests on the context.
        /// </summary>
        public bool ServerContextIsClosing
        {
            get { return _serverContextIsClosing; }
            set { _serverContextIsClosing = value; }
        }        

        #endregion

        #region private functions 

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
            else
            {
                _pendingContextNotificationData = new XiContextNotificationData(XiContextNotificationType.EndpointFail,
                    ex);
            }

            throw ex;
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
        private XiOPCWrapperServer? _xiServer;        

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
        ///     The private representation of the IResourceManagement public property
        /// </summary>
        private IResourceManagement? _iResourceManagement;

        private XiCallbackEndpoint? _callbackEndpoint;

        /// <summary>
        ///     This data member is the private representation of the LocaleId interface property.
        /// </summary>
        private readonly uint _localeId = (uint) Thread.CurrentThread.CurrentCulture.LCID;                        

        /// <summary>
        ///     The status of the server
        /// </summary>
        private List<ServerStatus>? _serverStatusList;        

        /// <summary>
        ///     Inidicates, when TRUE, that the context is closing or has completed closing
        ///     and will not accept any more requests on the context.
        /// </summary>
        private volatile bool _serverContextIsClosing;                

        private readonly IDispatcher _xiCallbackDoer;

        /// <summary>
        ///     The time interval that controls when ClientKeepAlive messages are
        ///     sent to the server.
        /// </summary>
        private const uint StatusIntervalMs = 10000;
        
        private DateTime _lastStatusDateTimeUtc = DateTime.UtcNow;        

        #endregion        
    }

    #endregion // Context Management
}