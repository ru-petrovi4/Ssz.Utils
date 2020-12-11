using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using Ssz.Xi.Client.Internal.Lists;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Endpoints
{
    /// <summary>
    ///     This abstract base class provides the common functionality for a client endpoint.
    /// </summary>
    internal abstract class XiEndpointRoot
    {
        #region construction and destruction

        /// <summary>
        ///     The constructor for endpoints
        /// </summary>
        /// <param name="endpointDefinition"> The EndpointDefinition for this endpoint. </param>
        /// <param name="serviceEndpoint"> TThe ServiceEndpoint definition for this endpoint. </param>
        /// <param name="receiveTimeout">
        ///     The inactivity time interval to be used by the server to timeout this endpoint when no
        ///     requests are received from the client.
        /// </param>
        /// <param name="sendTimeout"> The length of time WCF will wait for a response before throwing an exception. </param>
        /// <param name="maxItemsInObjectGraph"> The number of objects the server will serialize into a single response. </param>
        protected XiEndpointRoot(EndpointDefinition endpointDefinition,
            ServiceEndpoint serviceEndpoint, TimeSpan receiveTimeout, TimeSpan sendTimeout,
            int maxItemsInObjectGraph)
        {
            _endpointDefinition = endpointDefinition;
            ServiceEndpoint = serviceEndpoint;
            ServiceEndpoint.Binding.ReceiveTimeout = receiveTimeout;
            ServiceEndpoint.Binding.SendTimeout = sendTimeout;
            MaxItemsInObjectGraph = maxItemsInObjectGraph;
            LastCallUtc = DateTime.UtcNow;
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
            if (Disposed) return;

            if (disposing)
            {
                ChannelCloser.Close(Channel);
                _assignedXiLists.Clear();
            }

            Disposed = true;
        }

        /// <summary>
        ///     The standard destructor invoked by the .NET garbage collector during Finalize.
        /// </summary>
        ~XiEndpointRoot()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method determines if the connection for the specified endpoint has been created, and if not,
        ///     attempts to create it. It returns FALSE if the connection is not or cannot be created.
        /// </summary>
        /// <param name="endpoint"> The specified endpoint </param>
        /// <returns> It returns FALSE if the connection is not or cannot be created. </returns>
        public static bool CreateChannelIfNotCreated(XiEndpointRoot endpoint)
        {
            if (endpoint == null) throw new Exception("null Endpoint.");

            if (endpoint.Channel == null) return endpoint.CreateChannel();

            return true;
        }

        /// <summary>
        ///     This method sets the MaxItemsInObjectGraph for the WCF connection to the server
        /// </summary>
        /// <param name="operations">
        ///     The OperationDescriptionCollection defined for the channel that is to be updated with the
        ///     specified MaxItemsInObjectGraph.
        /// </param>
        /// <param name="maxItemsInObjectGraph"> The MaxItemsInObjectGraph value to be used. </param>
        public static void SetMaxItemsInObjectGraph(OperationDescriptionCollection operations, int maxItemsInObjectGraph)
        {
            if (maxItemsInObjectGraph > 0) // use the default if maxItemsInObjectGraph is not supplied
            {
                foreach (OperationDescription op in operations)
                {
                    var dataContractBehavior = op.Behaviors.Find<DataContractSerializerOperationBehavior>();
                    if (dataContractBehavior != null)
                        dataContractBehavior.MaxItemsInObjectGraph = maxItemsInObjectGraph;
                }
            }
        }

        /// <summary>
        ///     This method indicates, when TRUE is returned, that the specified XiList
        ///     has been assigned to this endpoint.
        /// </summary>
        /// <param name="list"> The specified XiList. </param>
        /// <returns> Returns TRUE if the specified XiList has been assigned to this endpoint, otherwise FALSE. </returns>
        public bool HasListAttached(XiListRoot list)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointRoot.");

            return _assignedXiLists.Exists(l => l.ServerListId == list.ServerListId);
        }

        /// <summary>
        ///     This method adds (assigns) the specified Xi List from the endpoint.
        /// </summary>
        /// <param name="xiList"> The specified Xi List. </param>
        public void AssignList(XiListRoot xiList)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointRoot.");

            _assignedXiLists.Add(xiList);
        }

        /// <summary>
        ///     This method removes (unassigns) the specified Xi List from the endpoint.
        /// </summary>
        /// <param name="xiList"> The specified Xi List. </param>
        public void UnassignList(XiListRoot xiList)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEndpointRoot.");

            _assignedXiLists.Remove(xiList);
        }

        /// <summary>
        ///     This method creates the WCF Channel Factory for this endpoint and then uses it to create the channel.
        /// </summary>
        /// <returns> Returns TRUE if the channel was created, otherwise FALSE. </returns>
        public abstract bool CreateChannel();

        /// <summary>
        ///     The endpoint configuration name as specified by the endpoint name attribute
        ///     in the server's App.config file. This name may not be unique.
        /// </summary>
        public string Name
        {
            get { return ServiceEndpoint.Name; }
        }

        /// <summary>
        ///     The ServiceModel.Description.ServiceEndpoint.Binding.Name for this endpoint.
        /// </summary>
        public string BindingName
        {
            get
            {
                return ServiceEndpoint.Binding.Name;
            }
        }

        /// <summary>
        ///     The ServiceModel.Description.ServiceEndpoint.Contract.Name for this endpoint.
        /// </summary>
        public string ContractName
        {
            get
            {
                return ServiceEndpoint.Contract.Name;
            }
        }

        /// <summary>
        ///     The ServiceModel.Description.ServiceEndpoint.Address.Uri.AbsoluteUri for this endpoint.
        /// </summary>
        public string UriAddress
        {
            get
            {
                return ServiceEndpoint.Address.Uri.AbsoluteUri;
            }
        }

        /// <summary>
        ///     This member corresponds to the receiveTimeout attribute in the
        ///     binding element associated with this endpoint.
        /// </summary>
        public TimeSpan ReceiveTimeout
        {
            get
            {
                return ServiceEndpoint.Binding.ReceiveTimeout;
            }
        }

        /// <summary>
        ///     This member corresponds to the sendTimeout attribute in the
        ///     binding element associated with this endpoint.
        /// </summary>
        public TimeSpan SendTimeout
        {
            get
            {
                return ServiceEndpoint.Binding.SendTimeout;
            }
        }

        /// <summary>
        ///     The unique identifier of the endpoint assigned by the server.
        /// </summary>
        public string EndpointId
        {
            get { return _endpointDefinition.EndpointId ?? ""; }
        }

        /// <summary>
        ///     This data member is the WCF channel for this endpoint.
        /// </summary>
        public abstract ICommunicationObject Channel { get; }

        /// <summary>
        ///     The time of receipt of the response to the last successful call on this endpoint.
        /// </summary>
        public DateTime LastCallUtc { get; set; }

        /// <summary>
        ///     This member indicates, when TRUE, that the object has been disposed by the Dispose(bool isDisposing) method.
        /// </summary>
        public bool Disposed { get; private set; }

        #endregion

        #region protected functions

        /// <summary>
        ///     This data member contains the server's Max Items In Object Graph for this endpoint
        /// </summary>
        protected int MaxItemsInObjectGraph { get; private set; }

        /// <summary>
        ///     This data member contains the ServiceEndpoint definition of the endpoint received from the server using MEX.
        /// </summary>
        protected ServiceEndpoint ServiceEndpoint { get; private set; }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member contains the list of Xi Lists assigned to this endpoint.
        /// </summary>
        private readonly List<XiListRoot> _assignedXiLists = new List<XiListRoot>();

        /// <summary>
        ///     This data member contains the EndpointDefinition returned by the server when the
        ///     OpenEndpoint() method is called.
        /// </summary>
        private readonly EndpointDefinition _endpointDefinition;

        #endregion
    }
}