using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using Ssz.Utils;
using Xi.Contracts;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal
{
    /// <summary>
    ///     This class is used to locate a server and obtain its list of ServiceEndpoints.
    /// </summary>
    internal class XiServerInfo : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates an XiEndpointDiscovery instance from a ServerEntry
        ///     Preconditions: serverEntry is not null.
        /// </summary>
        public XiServerInfo(ServerEntry serverEntry, List<EndpointConfigurationEx> endpointConfigurationExList, string host)
        {
            _serverEntry = serverEntry;
            _endpointConfigurationExList = endpointConfigurationExList;

            if (_serverEntry.EndpointServerSettings is not null)
            foreach (EndpointConfiguration endpointConfiguration in _serverEntry.EndpointServerSettings)
            {
                var endpointUri = new UriBuilder(endpointConfiguration.EndpointUrl ?? "");
                endpointUri.Host = host;
                endpointConfiguration.EndpointUrl = endpointUri.Uri.OriginalString;
            }
            foreach (EndpointConfigurationEx endpointConfigurationEx in _endpointConfigurationExList)
            {
                var endpointUri = new UriBuilder(endpointConfigurationEx.EndpointUrl ?? "");
                endpointUri.Host = host;
                endpointConfigurationEx.EndpointUrl = endpointUri.Uri.OriginalString;
            }

            FillAllServiceEndpointsCollection(_endpointConfigurationExList);

            #region store _allServiceEndpointsCollection in lists per contractTypeName

            if (_allServiceEndpointsCollection is not null)
                foreach (ServiceEndpoint serviceEndpoint in _allServiceEndpointsCollection)
                {
                    // TODO
                    //if (serviceEndpoint.ListenUri is null) continue;
                    /*
                    var listenUriBuilder = new UriBuilder(serviceEndpoint.ListenUri);
                    listenUriBuilder.Host = (new Uri(serverEntry.ServerDescription.ServerDiscoveryUrl)).Host;
                    if (serviceEndpoint.Address.Identity is not null)
                        // there is an identity of the server (used with Kerberos)
                        serviceEndpoint.Address = new EndpointAddress(listenUriBuilder.Uri,
                            serviceEndpoint.Address.Identity);
                    else // no identity 
                        serviceEndpoint.Address = new EndpointAddress(listenUriBuilder.Uri);*/

                    if (serviceEndpoint.Contract.Name == typeof (IServerDiscovery).Name)
                    {
                        _serverDiscoveryEndpoint = serviceEndpoint; // used to fetch server info
                    }
                    else if (serviceEndpoint.Contract.Name == typeof (IResourceManagement).Name)
                    {
                        _resourceManagementServiceEndpoints.Add(serviceEndpoint);
                    }
                    else if (serviceEndpoint.Contract.Name == typeof (IRead).Name)
                    {
                        _readServiceEndpoints.Add(serviceEndpoint);
                    }
                    else if (serviceEndpoint.Contract.Name == typeof (IWrite).Name)
                    {
                        _writeServiceEndpoints.Add(serviceEndpoint);
                    }
                    else if (serviceEndpoint.Contract.Name == typeof (IPoll).Name)
                    {
                        _pollServiceEndpoints.Add(serviceEndpoint);
                    }
                    else if (serviceEndpoint.Contract.Name == typeof (IRegisterForCallback).Name)
                    {
                        _registerForCallbackServiceEndpoints.Add(serviceEndpoint);
                    }
                }

            #endregion

            #region Rank _resourceManagementServiceEndpoints

            // This will become the list of Ranked Endpoints
            var rankedResourceManagementServiceEndpoints = new List<ServiceEndpoint>();
            try
            {                
                // now get the tcp endpoints
                foreach (ServiceEndpoint serviceEndpoint in _resourceManagementServiceEndpoints)
                {
                    if (serviceEndpoint.Binding.GetType() == typeof (NetTcpBinding))
                    {
                        rankedResourceManagementServiceEndpoints.Add(serviceEndpoint);
                        break;
                    }
                }
                // now get the wshttp endpoints
                foreach (ServiceEndpoint serviceEndpoint in _resourceManagementServiceEndpoints)
                {
                    if (serviceEndpoint.Binding.GetType() == typeof (WSHttpBinding))
                    {
                        rankedResourceManagementServiceEndpoints.Add(serviceEndpoint);
                        break;
                    }
                }
                // now get the basic http endpoints
                foreach (ServiceEndpoint serviceEndpoint in _resourceManagementServiceEndpoints)
                {
                    if (serviceEndpoint.Binding.GetType() == typeof (BasicHttpBinding))
                    {
                        rankedResourceManagementServiceEndpoints.Add(serviceEndpoint);
                        break;
                    }
                }
                _resourceManagementServiceEndpoints = rankedResourceManagementServiceEndpoints;
            }
            catch
            {
            } // do nothing if dns is not here

            #endregion

            #region update the endpoint settings with the EndpointConfiguration from the ServerEntry

            string? contractTypeName = null;
            if (_serverEntry.EndpointServerSettings is not null)
            foreach (EndpointConfiguration endpointConfiguration in _serverEntry.EndpointServerSettings)
            {
                // In case the server used the fully qualified contractTypeName name 
                if (endpointConfiguration.ContractType == typeof (IResourceManagement).ToString())
                    endpointConfiguration.ContractType = typeof (IResourceManagement).Name;
                else if (endpointConfiguration.ContractType == typeof (IRead).ToString())
                    endpointConfiguration.ContractType = typeof (IRead).Name;
                else if (endpointConfiguration.ContractType == typeof (IWrite).ToString())
                    endpointConfiguration.ContractType = typeof (IWrite).Name;
                else if (endpointConfiguration.ContractType == typeof (IPoll).ToString())
                    endpointConfiguration.ContractType = typeof (IPoll).Name;
                else if (endpointConfiguration.ContractType == typeof (IRegisterForCallback).ToString())
                    endpointConfiguration.ContractType = typeof (IRegisterForCallback).Name;

                if (endpointConfiguration.ContractType == typeof (IResourceManagement).Name)
                {
                    contractTypeName = typeof (IResourceManagement).Name;
                    foreach (ServiceEndpoint serviceEndpoint in _resourceManagementServiceEndpoints)
                    {
                        if ((serviceEndpoint.Address.Uri == new Uri(endpointConfiguration.EndpointUrl ?? "")) &&
                            (serviceEndpoint.Contract.Name == contractTypeName))
                        {
                            ModifyEndpoint(serviceEndpoint, endpointConfiguration);
                            break;
                        }
                    }
                }
                else if (endpointConfiguration.ContractType == typeof (IRead).Name)
                {
                    contractTypeName = typeof (IRead).Name;
                    foreach (ServiceEndpoint serviceEndpoint in _readServiceEndpoints)
                    {
                        if ((serviceEndpoint.Address.Uri == new Uri(endpointConfiguration.EndpointUrl ?? "")) &&
                            (serviceEndpoint.Contract.Name == contractTypeName))
                        {
                            ModifyEndpoint(serviceEndpoint, endpointConfiguration);
                            break;
                        }
                    }
                }
                else if (endpointConfiguration.ContractType == typeof (IWrite).Name)
                {
                    contractTypeName = typeof (IWrite).Name;
                    foreach (ServiceEndpoint serviceEndpoint in _writeServiceEndpoints)
                    {
                        if ((serviceEndpoint.Address.Uri == new Uri(endpointConfiguration.EndpointUrl ?? "")) &&
                            (serviceEndpoint.Contract.Name == contractTypeName))
                        {
                            ModifyEndpoint(serviceEndpoint, endpointConfiguration);
                            break;
                        }
                    }
                }
                else if (endpointConfiguration.ContractType == typeof (IPoll).Name)
                {
                    contractTypeName = typeof (IPoll).Name;
                    foreach (ServiceEndpoint serviceEndpoint in _pollServiceEndpoints)
                    {
                        if ((serviceEndpoint.Address.Uri == new Uri(endpointConfiguration.EndpointUrl ?? "")) &&
                            (serviceEndpoint.Contract.Name == contractTypeName))
                        {
                            ModifyEndpoint(serviceEndpoint, endpointConfiguration);
                            break;
                        }
                    }
                }
                else if (endpointConfiguration.ContractType == typeof (IRegisterForCallback).Name)
                {
                    contractTypeName = typeof (IRegisterForCallback).Name;
                    foreach (ServiceEndpoint serviceEndpoint in _registerForCallbackServiceEndpoints)
                    {
                        if ((serviceEndpoint.Address.Uri == new Uri(endpointConfiguration.EndpointUrl ?? "")) &&
                            (serviceEndpoint.Contract.Name == contractTypeName))
                        {
                            ModifyEndpoint(serviceEndpoint, endpointConfiguration);
                            break;
                        }
                    }
                }
            }

            #endregion
        }

        /// <summary>
        ///     This method disposes of the object.  It is invoked by the client application, client base, or
        ///     the destructor of this object.
        /// </summary>
        public void Dispose()
        {
            lock (_syncRoot)
            {
                Dispose(true);
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method disposes of the object.  It is invoked by the parameterless Dispose()
        ///     method of this object.
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
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            //_serverDiscoveryUri = null;
            if (disposing)
            {
                _resourceManagementServiceEndpoints.Clear();
                _readServiceEndpoints.Clear();
                _writeServiceEndpoints.Clear();
                _pollServiceEndpoints.Clear();
                _registerForCallbackServiceEndpoints.Clear();
            }            

            _disposed = true;
        }

        /// <summary>
        ///     The standard destructor invoked by the .NET garbage collector during Finalize.
        /// </summary>
        ~XiServerInfo()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        ///// <summary>
        /////     This method searches the list of endpoints for the endpoint with the specified contractType and
        /////     endpoint URL.
        ///// </summary>
        ///// <param name="contractType"> The contractType type of the desired endpoint. </param>
        ///// <param name="url"> The URL of the desired endpoint </param>
        ///// <returns> Returns the endpoint with the specified contractType and endpoint URL </returns>
        //public ServiceEndpoint? GetServiceEndpointByContractAndUrl(string contractType, string url)
        //{
        //    lock (_syncRoot)
        //    {
        //        if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

        //        ServiceEndpoint? svcEP = null;
        //        foreach (ServiceEndpoint serviceEndpoint in _allServiceEndpointsCollection)
        //        {                    
        //            if (0 == string.Compare(serviceEndpoint.Contract.Name, contractType, true))
        //            {
        //                svcEP = serviceEndpoint;
        //                break;
        //            }

        //            //if (serviceEndpoint.ListenUri is not null)
        //            //{
        //            //    var ub = new UriBuilder(serviceEndpoint.ListenUri);
        //            //    if ((0 == string.Compare(serviceEndpoint.Contract.Name, contractType, true)) &&
        //            //        (0 == string.Compare(ub.Uri.OriginalString, url, true)))
        //            //    {
        //            //        svcEP = serviceEndpoint;
        //            //        break;
        //            //    }
        //            //}
        //        }
        //        return svcEP;
        //    }
        //}

        /// <summary>
        ///     This method searches the list of endpoints for the endpoint with the specified contractType and
        ///     protocol scheme.
        /// </summary>
        /// <param name="contractType"> The contractType type of the desired endpoints. </param>
        /// <param name="scheme"> The protocol scheme of the desired endpoints. </param>
        /// <returns> Returns the endpoints with the specified contractType and protocol scheme. </returns>
        public IEnumerable<ServiceEndpoint> GetServiceEndpointsByScheme(string contractType, string scheme)
        {
            lock (_syncRoot)
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

                var serviceEndpoints = new List<ServiceEndpoint>();
                if (_allServiceEndpointsCollection is not null)
                    foreach (ServiceEndpoint serviceEndpoint in _allServiceEndpointsCollection)
                    {
                        if (0 == string.Compare(serviceEndpoint.Contract.Name, contractType, true)) serviceEndpoints.Add(serviceEndpoint);
                        // TODO
                        //if (serviceEndpoint.ListenUri is not null)
                        //{
                        //    var ub = new UriBuilder(serviceEndpoint.ListenUri);
                        //    if ((0 == string.Compare(serviceEndpoint.Contract.Name, contractType, true)) &&
                        //        (0 == string.Compare(ub.Scheme, scheme, true))) serviceEndpoints.Add(serviceEndpoint);
                        //}
                    }
                return serviceEndpoints;
            }
        }

        /// <summary>
        ///     This method searches the list of endpoints for the endpoint with the specified contractType and
        ///     binding type.
        /// </summary>
        /// <param name="contractType"> The contractType type of the desired endpoints. </param>
        /// <param name="binding"> The binding type of the desired endpoints. </param>
        /// <returns> Returns the endpoint with the specified contractType and binding type. </returns>
        public IEnumerable<ServiceEndpoint> GetServiceEndpointsByBinding(string contractType, Type binding)
        {
            lock (_syncRoot)
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

                return GetServiceEndpointsByBindingInternal(contractType, binding);
            }
        }

        /// <summary>
        ///     This method ranks the Read, Write, or Subscribe endpoints.  Those with the same binding type
        ///     as the Resource Management endpoint used to connect to the server are ranked first, followed
        ///     by tcp, wshttp, and basic http. This method is called after the client connects to the server.
        /// </summary>
        /// <param name="connectedResourceManagementServiceEndpoint">
        ///     The resource management endpoint to which the client is
        ///     connected.
        /// </param>
        public void RankReadWriteSubscribeEndpoints(ServiceEndpoint connectedResourceManagementServiceEndpoint)
        {
            lock (_syncRoot)
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

                _readServiceEndpoints = RankReadWriteSubscribeEndpoints(connectedResourceManagementServiceEndpoint,
                    _readServiceEndpoints);
                _writeServiceEndpoints = RankReadWriteSubscribeEndpoints(connectedResourceManagementServiceEndpoint,
                    _writeServiceEndpoints);
                _pollServiceEndpoints = RankReadWriteSubscribeEndpoints(connectedResourceManagementServiceEndpoint,
                    _pollServiceEndpoints);
                _registerForCallbackServiceEndpoints =
                    RankReadWriteSubscribeEndpoints(connectedResourceManagementServiceEndpoint,
                        _registerForCallbackServiceEndpoints);
            }
        }

        /// <summary>
        ///     This method searches the list of endpoints for the endpoints with the specified contractType type
        /// </summary>
        /// <param name="contractType"> The contractType type of the desired endpoint. </param>
        /// <returns> Returns the endpoints with the specified contractType type </returns>
        public List<ServiceEndpoint>? GetRankedReadWriteSubscribeServiceEndpoints(string contractType)
        {
            lock (_syncRoot)
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

                if (contractType == typeof (IRead).Name) return _readServiceEndpoints;
                if (contractType == typeof (IWrite).Name) return _writeServiceEndpoints;
                if (contractType == typeof (IPoll).Name) return _pollServiceEndpoints;
                if (contractType == typeof (IRegisterForCallback).Name) return _registerForCallbackServiceEndpoints;
                return null;
            }
        }

        /// <summary>
        ///     The ServerEntry for the server.
        /// </summary>
        public ServerEntry ServerEntry
        {
            get { return _serverEntry; }
        }

        /// <summary>
        ///     The list of ranked resource management endpoints supported by the server.
        /// </summary>
        public IEnumerable<ServiceEndpoint> ResourceManagementServiceEndpoints
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

                    return _resourceManagementServiceEndpoints.ToArray();
                }
            }
        }

        /// <summary>
        ///     The list of ranked read endpoints supported by the server.
        /// </summary>
        public IEnumerable<ServiceEndpoint> ReadServiceEndpoints
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

                    return _readServiceEndpoints.ToArray();
                }
            }
        }

        /// <summary>
        ///     The list of ranked write endpoints supported by the server.
        /// </summary>
        public IEnumerable<ServiceEndpoint> WriteServiceEndpoints
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

                    return _writeServiceEndpoints.ToArray();
                }
            }
        }

        /// <summary>
        ///     The list of ranked poll endpoints supported by the server.
        /// </summary>
        public IEnumerable<ServiceEndpoint> PollServiceEndpoints
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

                    return _pollServiceEndpoints.ToArray();
                }
            }
        }

        /// <summary>
        ///     The list of ranked callback endpoints supported by the server.
        /// </summary>
        public IEnumerable<ServiceEndpoint> RegisterForCallbackServiceEndpoints
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

                    return _registerForCallbackServiceEndpoints.ToArray();
                }
            }
        }

        /// <summary>
        ///     The ServiceEndpoint object of the server's Server Discovery Enpoint.
        /// </summary>
        public ServiceEndpoint? ServerDiscoveryEndpoint
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointDiscovery.");

                    return _serverDiscoveryEndpoint;
                }
            }
        }

        #endregion

        #region private functions

        ///// <summary>
        /////     This method loads the endpoint/binding configuration from the server's MEX endpoint
        /////     and builds the lists for each service contractTypeName.
        ///// </summary>
        //private void FillAllServiceEndpointsCollection(List<MexEndpointInfo>? mexEndpoints)
        //{
        //    if (mexEndpoints is null || mexEndpoints.Count == 0) return;

        //    foreach (MexEndpointInfo mexEndpoinnt in mexEndpoints)
        //    {
        //        if (!string.IsNullOrEmpty(mexEndpoinnt.Url))
        //        {
        //            try
        //            {
        //                //ServiceEndpointCollection endpoints = MetadataResolver.Resolve(typeof(IResourceManagement),
        //                //                    MexEndpointAddress, MetadataExchangeClientMode.MetadataExchange);
        //                // ---> fails because the default buffer size is too small
        //                // Alternate approach:
        //                var mexBnd = new WSHttpBinding(_mexSecurityMode, _mexReliableEnabled);
        //                mexBnd.MaxReceivedMessageSize = _mexReceiveBufferSize;
        //                var mexClient = new MetadataExchangeClient(mexBnd);

        //                //ServiceEndpointCollection endpoints = MetadataResolver.Resolve(contracts,
        //                //                    MexEndpointAddress, MetadataExchangeClientMode.MetadataExchange, mexClient );
        //                // result is empty for unknown reasons
        //                // Alternate approach:
        //                MetadataSet metadataSet = mexClient.GetMetadata(new EndpointAddress(mexEndpoinnt.Url));
        //                var importer = new WsdlImporter(metadataSet);
        //                Collection<ContractDescription> contractDescriptions = importer.ImportAllContracts();

        //                //Resolve wsdl into ServiceEndpointCollection
        //                var mexUriBuilder = new UriBuilder(mexEndpoinnt.Url);
        //                _allServiceEndpointsCollection = MetadataResolver.Resolve(contractDescriptions,
        //                    mexUriBuilder.Uri,
        //                    MetadataExchangeClientMode.
        //                        MetadataExchange, mexClient);

        //                break;
        //            }
        //            catch (Exception ex)
        //            {
        //                // do nothing if this mex ep doesn't work
        //                Trace.Fail(ex.Message, ex.StackTrace);
        //            }
        //        }
        //    }
        //}

        /// <summary>
        ///     This method loads the endpoint/binding configuration
        ///     and builds the lists for each service contractTypeName.
        /// </summary>
        private void FillAllServiceEndpointsCollection(IEnumerable<EndpointConfigurationEx> endpointConfigurationExList)
        {
            _allServiceEndpointsCollection = new Collection<ServiceEndpoint>();

            foreach (EndpointConfigurationEx endpointConfigurationEx in endpointConfigurationExList)
            {
                try
                {
                    Binding? binding = null;
                    // TODO: Add other cases.
                    switch (endpointConfigurationEx.BindingType)
                    {
                        case "NetTcpBinding":
                        {
                            SecurityMode securityMode;
                            Enum.TryParse(endpointConfigurationEx.SecurityMode, out securityMode);
                            binding = new NetTcpBinding(securityMode);
                        }
                            break;
                        case "BasicHttpBinding":
                        {
                                BasicHttpSecurityMode securityMode;
                                Enum.TryParse(endpointConfigurationEx.SecurityMode, out securityMode);
                                binding = new BasicHttpBinding(securityMode);
                        }
                            break;
                    }
                    if (binding is null) continue;
                    var serviceEndpoint =
                        new ServiceEndpoint(new ContractDescription(endpointConfigurationEx.ContractType),
                            binding, new EndpointAddress(endpointConfigurationEx.EndpointUrl));
                    _allServiceEndpointsCollection.Add(serviceEndpoint);
                }
                catch (Exception)
                {
                }
            }

            /*
            var uri =
                new Uri("net.tcp://" + _serverEntry.ServerDescription.HostName +
                        ":60081/SszCtcmXiServer/ResourceManagement");
            var serviceEndpoint = new ServiceEndpoint(new ContractDescription(typeof (IResourceManagement).Name),
                                                        new NetTcpBinding(SecurityMode.None), new EndpointAddress(uri));
            _allServiceEndpointsCollection.Add(serviceEndpoint);

            uri =
                new Uri("net.tcp://" + _serverEntry.ServerDescription.HostName +
                        ":60081/SszCtcmXiServer/Read");
            serviceEndpoint = new ServiceEndpoint(new ContractDescription(typeof (IRead).Name),
                                                    new NetTcpBinding(SecurityMode.None), new EndpointAddress(uri));
            _allServiceEndpointsCollection.Add(serviceEndpoint);

            uri =
                new Uri("net.tcp://" + _serverEntry.ServerDescription.HostName +
                        ":60081/SszCtcmXiServer/Write");
            serviceEndpoint = new ServiceEndpoint(new ContractDescription(typeof (IWrite).Name),
                                                    new NetTcpBinding(SecurityMode.None), new EndpointAddress(uri));
            _allServiceEndpointsCollection.Add(serviceEndpoint);

            uri =
                new Uri("net.tcp://" + _serverEntry.ServerDescription.HostName +
                        ":60081/SszCtcmXiServer/Callback");
            serviceEndpoint = new ServiceEndpoint(new ContractDescription(typeof (IRegisterForCallback).Name),
                                                    new NetTcpBinding(SecurityMode.None), new EndpointAddress(uri));
            _allServiceEndpointsCollection.Add(serviceEndpoint);

            uri =
                new Uri("net.tcp://" + _serverEntry.ServerDescription.HostName +
                        ":60081/SszCtcmXiServer/Poll");
            serviceEndpoint = new ServiceEndpoint(new ContractDescription(typeof (IPoll).Name),
                                                    new NetTcpBinding(SecurityMode.None), new EndpointAddress(uri));
            _allServiceEndpointsCollection.Add(serviceEndpoint);

            
            var serviceEndpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(IResourceManagement)));
            _serviceEndpointCollection.Add(serviceEndpoint);

            serviceEndpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(IRead)));
            _serviceEndpointCollection.Add(serviceEndpoint);

            serviceEndpoint = new ServiceEndpoint(new ContractDescription(typeof(IWrite).Name));
            _serviceEndpointCollection.Add(serviceEndpoint);

            serviceEndpoint = new ServiceEndpoint(new ContractDescription(typeof(IRegisterForCallback).Name));
            _serviceEndpointCollection.Add(serviceEndpoint);

            serviceEndpoint = new ServiceEndpoint(new ContractDescription(typeof(IPoll).Name));
            _serviceEndpointCollection.Add(serviceEndpoint);
            */
        }

        /// <summary>
        ///     This method searches the list of endpoints for the endpoint with the specified contractType and
        ///     binding type.
        /// </summary>
        /// <param name="contractType"> The contractType type of the desired endpoints. </param>
        /// <param name="bindingType"> The binding type of the desired endpoints. </param>
        /// <returns> Returns the endpoint with the specified contractType and binding type. </returns>
        private IEnumerable<ServiceEndpoint> GetServiceEndpointsByBindingInternal(string contractType, Type bindingType)
        {
            var serviceEndpoints = new List<ServiceEndpoint>();
            if (_allServiceEndpointsCollection is not null)
            foreach (ServiceEndpoint serviceEndpoint in _allServiceEndpointsCollection)
            {
                if ((string.Compare(serviceEndpoint.Contract.Name, contractType, true) == 0) &&
                        (serviceEndpoint.Binding.GetType() == bindingType)) serviceEndpoints.Add(serviceEndpoint);
                // TODO
                //if (serviceEndpoint.ListenUri is not null)
                //{
                //    var ub = new UriBuilder(serviceEndpoint.ListenUri);
                //    if ((string.Compare(serviceEndpoint.Contract.Name, contractType, true) == 0) &&
                //        (serviceEndpoint.Binding.GetType() == binding)) serviceEndpoints.Add(serviceEndpoint);
                //}
            }
            return serviceEndpoints;
        }

        /// <summary>
        ///     This method ranks the Read, Write, or Subscribe endpoints.  Those with the same binding type
        ///     as the Resource Management endpoint used to connect to the server are ranked first, followed
        ///     by tcp, wshttp, and basic http. This method is called after the client connects to the server.
        /// </summary>
        /// <param name="connectedResourceManagementServiceEndpoint">
        ///     The resource management endpoint to which the client is
        ///     connected.
        /// </param>
        /// <param name="serviceEndpoints"> The list of read, write, and subscribe ServiceEndpoint retrieved by MEX. </param>
        /// <returns> The list of ranked endpoints. </returns>
        private List<ServiceEndpoint> RankReadWriteSubscribeEndpoints(
            ServiceEndpoint connectedResourceManagementServiceEndpoint, List<ServiceEndpoint> serviceEndpoints)
        {
            if (0 == serviceEndpoints.Count)
            {
                //Logger?.LogDebug("null == serviceEndpoints || 0 == serviceEndpoints.Count");
                return serviceEndpoints;
            }
            List<ServiceEndpoint> listServiceEndpoints;
            try
            {
                // First pull out those with the same binding type as the connectedResourceManagementEndpoint
                listServiceEndpoints =
                    GetServiceEndpointsByBindingInternal(serviceEndpoints[0].Contract.Name,
                        connectedResourceManagementServiceEndpoint.Binding.GetType()).ToList();
                
                // now get the tcp endpoints
                foreach (ServiceEndpoint serviceEndpoint in serviceEndpoints)
                {
                    if ((serviceEndpoint.Binding.GetType() !=
                         connectedResourceManagementServiceEndpoint.Binding.GetType()) &&
                        (serviceEndpoint.Binding.GetType() == typeof (NetTcpBinding)))
                        listServiceEndpoints.Add(serviceEndpoint);
                }
                // now get the wshttp endpoints
                foreach (ServiceEndpoint serviceEndpoint in serviceEndpoints)
                {
                    if ((serviceEndpoint.Binding.GetType() !=
                         connectedResourceManagementServiceEndpoint.Binding.GetType()) &&
                        (serviceEndpoint.Binding.GetType() == typeof (WSHttpBinding)))
                        listServiceEndpoints.Add(serviceEndpoint);
                }
                // now get the basic http endpoints
                foreach (ServiceEndpoint serviceEndpoint in serviceEndpoints)
                {
                    if ((serviceEndpoint.Binding.GetType() !=
                         connectedResourceManagementServiceEndpoint.Binding.GetType()) &&
                        (serviceEndpoint.Binding.GetType() == typeof (BasicHttpBinding)))
                        listServiceEndpoints.Add(serviceEndpoint);
                }
            }
            catch
            {
                listServiceEndpoints = new List<ServiceEndpoint>();
            } // do nothing if dns is not here
            return listServiceEndpoints;
        }

        /// <summary>
        ///     This method applies the EndpointConfiguration info contained in the ServerEntry
        ///     of the server to the endpoint definitions retrieved by MEX.
        /// </summary>
        /// <param name="serviceEndpoint"> The ServiceEndpoint to update. </param>
        /// <param name="epc"> The EndpointConfiguration for the ServiceEndpoint. </param>
        private void ModifyEndpoint(ServiceEndpoint serviceEndpoint, EndpointConfiguration epc)
        {
            serviceEndpoint.Binding.OpenTimeout = epc.OpenTimeout;
            serviceEndpoint.Binding.CloseTimeout = epc.CloseTimeout;
            serviceEndpoint.Binding.SendTimeout = epc.SendTimeout;
            serviceEndpoint.Binding.ReceiveTimeout = epc.ReceiveTimeout;

            var bhBnd = serviceEndpoint.Binding as BasicHttpBinding;
            if (bhBnd is not null)
            {
                bhBnd.MaxBufferSize = (int) epc.MaxBufferSize;
                bhBnd.MaxReceivedMessageSize = epc.MaxBufferSize;
            }
            var wshBnd = serviceEndpoint.Binding as WSHttpBinding;
            if (wshBnd is not null) wshBnd.MaxReceivedMessageSize = (int) epc.MaxBufferSize;            
            var tcpBnd = serviceEndpoint.Binding as NetTcpBinding;
            if (tcpBnd is not null)
            {
                tcpBnd.MaxBufferSize = (int) epc.MaxBufferSize;
                tcpBnd.MaxReceivedMessageSize = epc.MaxBufferSize;
            }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This static data member contains the local IP address bytes. The local IP address is always 127.0.0.1.
        /// </summary>
        private static readonly byte[] LocalhostBytes =
        {
            127, 0, 0, 1
        };

        /// <summary>
        ///     This data member contains the local IP address of 127.0.0.1
        /// </summary>
        private static readonly IPAddress LocalhostIpAddress = new IPAddress(LocalhostBytes);

        /// <summary>
        ///     This data member is the private representation of the ServerEntry interface property
        /// </summary>
        private readonly ServerEntry _serverEntry;

        private readonly List<EndpointConfigurationEx> _endpointConfigurationExList;

        /// <summary>
        ///     This data member is the private representation of the PollServiceEndpoints interface property.
        /// </summary>
        private List<ServiceEndpoint> _pollServiceEndpoints = new List<ServiceEndpoint>();

        /// <summary>
        ///     This data member is the private representation of the ReadServiceEndpoints interface property.
        /// </summary>
        private List<ServiceEndpoint> _readServiceEndpoints = new List<ServiceEndpoint>();

        /// <summary>
        ///     This data member is the private representation of the RegisterForCallbackServiceEndpoints interface property.
        /// </summary>
        private List<ServiceEndpoint> _registerForCallbackServiceEndpoints = new List<ServiceEndpoint>();

        /// <summary>
        ///     This data member is the private representation of the ResourceManagementServiceEndpoints interface property.
        /// </summary>
        private List<ServiceEndpoint> _resourceManagementServiceEndpoints = new List<ServiceEndpoint>();

        /// <summary>
        ///     This data member contains the ServiceEndpoint definition of the server's Server Discovery endpoint
        /// </summary>
        private readonly ServiceEndpoint? _serverDiscoveryEndpoint;

        /// <summary>
        ///     This data member is the private representation of the WriteServiceEndpoints interface property.
        /// </summary>
        private List<ServiceEndpoint> _writeServiceEndpoints = new List<ServiceEndpoint>();

        /// <summary>
        ///     This member indicates, when TRUE, that the object has been disposed by the Dispose(bool isDisposing) method.
        /// </summary>
        private bool _disposed;

        /// <summary>
        ///     This data member is the collection of server endpoints retrieved by MEX
        /// </summary>
        private Collection<ServiceEndpoint>? _allServiceEndpointsCollection;

        /// <summary>
        ///     This data member is used to lock the list.
        /// </summary>
        private readonly object _syncRoot = new object();

        /// <summary>
        ///     Mex WsHttpBinding constructor parameter
        /// </summary>
        private const bool _mexReliableEnabled = false;

        #endregion
    }
}