using System;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using Xi.Contracts;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Endpoints
{
    /// <summary>
    ///     This class defines endpoints that support the Xi IWrite interface.
    /// </summary>
    internal sealed class XiWriteEndpoint : XiEndpointRoot
    {
        #region construction and destruction

        /// <summary>
        ///     The contstructor for IWrite endpoints
        /// </summary>
        /// <param name="endpointDefinition"> The EndpointDefinition for this endpoint. </param>
        /// <param name="serviceEndpoint"> TThe ServiceEndpoint definition for this endpoint. </param>
        /// <param name="receiveTimeout">
        ///     The inactivity time interval to be used by the server to timeout this endpoint when no
        ///     requests are received from the client.
        /// </param>
        /// <param name="sendTimeout"> The length of time WCF will wait for a response before throwing an exception. </param>
        /// <param name="maxItemsInObjectGraph"> The number of objects the server will serialize into a single response. </param>
        internal XiWriteEndpoint(EndpointDefinition endpointDefinition,
            ServiceEndpoint serviceEndpoint, TimeSpan receiveTimeout, TimeSpan sendTimeout,
            int maxItemsInObjectGraph)
            : base(
                endpointDefinition, serviceEndpoint, receiveTimeout, sendTimeout,
                maxItemsInObjectGraph)
        {
            CreateChannel();
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
        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method creates the WCF Channel Factory for this endpoint and then uses it to create the channel.
        /// </summary>
        /// <returns> Returns TRUE if the channel was created, otherwise FALSE. </returns>
        public override bool CreateChannel()
        {
            var cfIWrite = new ChannelFactory<IWrite>(ServiceEndpoint.Binding, ServiceEndpoint.Address);
            cfIWrite.Credentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Delegation;
            SetMaxItemsInObjectGraph(ServiceEndpoint.Contract.Operations, MaxItemsInObjectGraph);

            /*
            if (Context.UserInfo is not null)
                XiClientCredentials.SetClientCredentials(Context.UserData, ServiceEndpoint, cfIWrite.Credentials,             
                                                            Context.UserInfo);
                */

            _iWrite = cfIWrite.CreateChannel();

            return Channel is not null;
        }

        /// <summary>
        ///     This data member is the WCF channel for this endpoint.
        /// </summary>
        public override ICommunicationObject Channel
        {
            get 
            {
                var result = _iWrite as ICommunicationObject;
                if (result is null) throw new InvalidOperationException();
                return result; 
            }
        }

        /// <summary>
        ///     This data member is used to make calls on the Xi IWrite interface,
        /// </summary>
        public IWrite Proxy
        {
            get
            {
                if (_iWrite is null) throw new InvalidOperationException();
                return _iWrite; 
            }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member is the private representation of the Proxy property.
        /// </summary>
        private IWrite? _iWrite;

        #endregion
    }
}