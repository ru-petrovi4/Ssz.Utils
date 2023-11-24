using System;
using System.Security.Principal;
using Xi.Contracts;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Endpoints
{
    /// <summary>
    ///     This class defines endpoints that support the Xi IPoll interface.
    /// </summary>
    internal sealed class XiPollEndpoint : XiEndpointRoot
    {
        #region construction and destruction

        /// <summary>
        ///     The contstructor for IPoll endpoints
        /// </summary>
        /// <param name="endpointDefinition"> The EndpointDefinition for this endpoint. </param>
        /// <param name="serviceEndpoint"> TThe ServiceEndpoint definition for this endpoint. </param>
        /// <param name="receiveTimeout">
        ///     The inactivity time interval to be used by the server to timeout this endpoint when no
        ///     requests are received from the client.
        /// </param>
        /// <param name="sendTimeout"> The length of time WCF will wait for a response before throwing an exception. </param>
        /// <param name="maxItemsInObjectGraph"> The number of objects the server will serialize into a single response. </param>
        public XiPollEndpoint(IPoll? iPoll, EndpointDefinition endpointDefinition,
            TimeSpan receiveTimeout, TimeSpan sendTimeout,
            int maxItemsInObjectGraph)
            : base(
                endpointDefinition, receiveTimeout, sendTimeout,
                maxItemsInObjectGraph)
        {
            _iPoll = iPoll;
        }

        #endregion        

        #region internal functions

        /// <summary>
        ///     This data member is used to make calls on the Xi IPoll interface,
        /// </summary>
        internal IPoll Proxy
        {
            get 
            {
                if (_iPoll is null) throw new InvalidOperationException();
                return _iPoll; 
            }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member is the private representation of the Proxy property.
        /// </summary>
        private IPoll? _iPoll;

        #endregion
    }
}