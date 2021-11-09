using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using Ssz.Xi.Client.Internal.Endpoints;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Context
{

    #region Context Endpoint Management

    /// <summary>
    ///     This partial class defines the Endpoint Management related aspects of the XiContext class.
    /// </summary>
    internal partial class XiContext
    {
        #region public functions

        /// <summary>
        ///     This method is used to open access to an Xi Read, Write, Poll, or Callback endpoint in the server.
        ///     If opened, does nothing.
        /// </summary>
        /// <param name="contractType">
        ///     The name of the type of Xi Contract (IRead, IWrite, IPoll, ICallback). E.g contractType =
        ///     typeof(IRead).Name;
        /// </param>
        /// <returns> Returns a reference to the endpoint that was opened. </returns>
        public void OpenEndpointForContract(string contractType)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_openedEndpoinsContractTypes.Contains(contractType)) return;

            ServiceEndpoint? serviceEndpoint =
                _xiServerInfo.GetServiceEndpointsByBinding(contractType,
                    _connectedResourceManagementServiceEndpoint.Binding.
                        GetType()).FirstOrDefault();
            if (serviceEndpoint is null) throw new InvalidOperationException();
            OpenEndpoint(serviceEndpoint);
        }

        /// <summary>
        ///     This method is used to get the endpoint that implements the specified contractTypeName type.
        /// </summary>
        /// <param name="contractTypeName"> The contractTypeName type obtained using the typeof() method. (e.g. typeof(IRead)) </param>
        /// <returns> Returns the requested endpoint if successful, otherwise null. </returns>
        public XiEndpointRoot? GetEndpointByContract(string contractTypeName)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            XiEndpointRoot? iXiEndpointBase = null;

            if (contractTypeName == typeof (IRead).Name) iXiEndpointBase = _readEndpoint;
            else if (contractTypeName == typeof (IWrite).Name) iXiEndpointBase = _writeEndpoint;
            else if (contractTypeName == typeof (IPoll).Name) iXiEndpointBase = _pollEndpoint;
            else if (contractTypeName == typeof (IRegisterForCallback).Name) iXiEndpointBase = _callbackEndpoint;

            return iXiEndpointBase;
        }

        /// <summary>
        ///     This method is used to close an endpoint.
        /// </summary>
        /// <param name="endpointId"> The endpoint to close. </param>
        public void CloseEndpoint(string endpointId)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            try
            {
                _iResourceManagement.CloseEndpoint(ContextId, endpointId);
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
        }

        /// <summary>
        ///     This method adds a list to an endpoint.
        /// </summary>
        /// <param name="serverListId"> The identifier of the list to add to the endpoint. </param>
        /// <param name="endpointId"> A string value that uniquely identifies the endpoint to which the list is to be added. </param>
        /// <returns>
        ///     The list identifier and result code for the list whose add failed. Returns null if the add succeeded. Throws
        ///     a fault if the specified context could not be found.
        /// </returns>
        public uint AddListToEndpoint(uint serverListId, string endpointId)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (!string.IsNullOrEmpty(endpointId))
            {
                AliasResult? aliasResult = null;
                if (_iResourceManagement is null) throw new InvalidOperationException();
                try
                {
                    aliasResult = _iResourceManagement.AddListToEndpoint(ContextId, endpointId, serverListId);
                    SetResourceManagementLastCallUtc();
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
                return (null == aliasResult) ? XiFaultCodes.S_OK : aliasResult.Result;
            }
            return XiFaultCodes.E_FAIL;
        }

        /// <summary>
        ///     This method removes (deassigns) a list from an endpoint.
        /// </summary>
        /// <param name="serverListId"> The server identifier of the list to remove. </param>
        /// <param name="endpointId"> The server identifier of the endpoint. </param>
        /// <returns> The result code. See XiFaultCodes class for standardized result codes. </returns>
        public uint RemoveListFromEndpoint(uint serverListId, string endpointId)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (!string.IsNullOrEmpty(endpointId))
            {
                var listIds = new List<uint>();
                listIds.Add(serverListId);
                List<AliasResult>? listAliasResult = null;
                if (_iResourceManagement is null) throw new InvalidOperationException();
                try
                {
                    listAliasResult = _iResourceManagement.RemoveListsFromEndpoint(ContextId, endpointId, listIds);
                    SetResourceManagementLastCallUtc();
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
                return (listAliasResult is null) || (listAliasResult.Count == 0)
                    ? XiFaultCodes.S_OK
                    : listAliasResult[0].Result;
            }
            return XiFaultCodes.E_FAIL;
        }

        /// <summary>
        ///     This property is the WCF channel send timeout for all endpoints used by this context.  It defines
        ///     how long the channel will wait for a response. It should be longer than the context timeout to make
        ///     the keep-alive mechanism to work properly.
        /// </summary>
        public TimeSpan SendTimeout
        {
            get { return _sendTimeout; }
        }

        /// <summary>
        ///     This property is the endpoint used to access IRead methods on the server.
        /// </summary>
        public XiReadEndpoint? ReadEndpoint
        {
            get { return _readEndpoint; }
            set { _readEndpoint = value; }
        }

        /// <summary>
        ///     This property is the endpoint used to access IWrite methods on the server.
        /// </summary>
        public XiWriteEndpoint? WriteEndpoint
        {
            get { return _writeEndpoint; }
            set { _writeEndpoint = value; }
        }

        /// <summary>
        ///     This property is the endpoint used to access IPoll methods on the server.
        /// </summary>
        public XiPollEndpoint? PollEndpoint
        {
            get { return _pollEndpoint; }
            set { _pollEndpoint = value; }
        }

        /// <summary>
        ///     This property is the endpoint used to access IRegisterForCallback methods on the server
        ///     and receive ICallback methods from the server.
        /// </summary>
        public XiCallbackEndpoint? CallbackEndpoint
        {
            get { return _callbackEndpoint; }
            set { _callbackEndpoint = value; }
        }

        /// <summary>
        ///     This property indicates the type of endpoint (either poll or callback)
        ///     that has been opened for subscriptions.  Null if neither has been opened.
        /// </summary>
        public XiEndpointRoot? SubscribeEndpointClass
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

                return _callbackEndpoint ?? _pollEndpoint as XiEndpointRoot;
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     <para>
        ///         This method opens an endpoint that can be used to access one or more lists. Each newly opened endpoint is
        ///         assigned its own unique identifier. It may be that the server supports only one endpoint of each type (e.g.
        ///         Read). In this case a second attempt to open a Read endpoint will succeed and the EndpointId of the already
        ///         opened Read endpoint will be returned.
        ///     </para>
        /// </summary>
        /// <param name="serviceEndpoint">
        ///     The serviceEndpoint of the endpoint to be opened. ServiceEndpoints are retrieved from
        ///     the server by the DiscoverServer() method.
        /// </param>
        private void OpenEndpoint(ServiceEndpoint serviceEndpoint)
        {
            EndpointDefinition? epd = null;

            if (serviceEndpoint is not null)
            {
                if (_iResourceManagement is null) throw new InvalidOperationException();
                try
                {
                    epd = _iResourceManagement.OpenEndpoint(ContextId, serviceEndpoint.Contract.Name,
                        serviceEndpoint.Address.Uri.OriginalString);
                    SetResourceManagementLastCallUtc();
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }

                if (epd is not null)
                {
                    EndpointConfiguration? epConfig =
                        _xiServerInfo.ServerEntry.EndpointServerSettings?.FirstOrDefault(
                            epc =>
                                epc.ContractType == serviceEndpoint.Contract.Name                                
                                 );
                    // TODO
                    //&&
                    // (new Uri(epc.EndpointUrl) == serviceEndpoint.ListenUri)
                    if (epConfig is not null)
                    {
                        int maxItemsInObjectGraph = epConfig.MaxItemsInObjectGraph;
                        if (serviceEndpoint.Contract.Name == typeof (IRead).Name)
                        {
                            if (_readEndpoint is null)
                                _readEndpoint = new XiReadEndpoint(epd, serviceEndpoint, ReceiveTimeout,
                                    _sendTimeout, maxItemsInObjectGraph);
                        }

                        else if (serviceEndpoint.Contract.Name == typeof (IWrite).Name)
                        {
                            if (_writeEndpoint is null)
                            {
                                _writeEndpoint = new XiWriteEndpoint(epd, serviceEndpoint, ReceiveTimeout,
                                    _sendTimeout, maxItemsInObjectGraph);
                            }
                        }

                        else if (serviceEndpoint.Contract.Name == typeof (IPoll).Name)
                        {
                            if (_pollEndpoint is null)
                                _pollEndpoint = new XiPollEndpoint(epd, serviceEndpoint, ReceiveTimeout,
                                    _sendTimeout, maxItemsInObjectGraph);
                        }

                        else if (serviceEndpoint.Contract.Name == typeof (IRegisterForCallback).Name)
                        {
                            if (_callbackEndpoint is null)
                            {
                                _callbackEndpoint = new XiCallbackEndpoint(epd, serviceEndpoint, ReceiveTimeout,
                                    _sendTimeout, maxItemsInObjectGraph, _xiCallbackDoer);
                                _callbackEndpoint.SetCallback(ContextId, _serverKeepAliveSkipCount, _serverCallbackRate);
                            }
                        }
                    }
                }

                _openedEndpoinsContractTypes.Add(serviceEndpoint.Contract.Name);
            }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member is the private representation of the SendTimeout interface property.
        /// </summary>
        private static TimeSpan _sendTimeout;

        /// <summary>
        ///     The private receive timeout for all endpoints used by all contexts.
        ///     This timeout is not used on the client side according to Microsoft documentation, but is
        ///     defined because it is part of the setup of the WCF channel.
        /// </summary>
        private static readonly TimeSpan ReceiveTimeout = new TimeSpan(1, 0, 0);

        private readonly HashSet<string> _openedEndpoinsContractTypes = new HashSet<string>();

        /// <summary>
        ///     The private representation of the CallbackEndpoint interface property
        /// </summary>
        private XiCallbackEndpoint? _callbackEndpoint;

        /// <summary>
        ///     The private representation of the PollEndpoint interface property
        /// </summary>
        private XiPollEndpoint? _pollEndpoint;

        /// <summary>
        ///     The private representation of the ReadEndpoint interface property
        /// </summary>
        private XiReadEndpoint? _readEndpoint;

        /// <summary>
        ///     The private representation of the WriteEndpoint interface property
        /// </summary>
        private XiWriteEndpoint? _writeEndpoint;

        #endregion
    }

    #endregion // Context Endpoint Management
}