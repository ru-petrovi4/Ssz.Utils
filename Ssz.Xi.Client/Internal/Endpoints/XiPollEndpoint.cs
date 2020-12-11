using System;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
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
        public XiPollEndpoint(EndpointDefinition endpointDefinition,
            ServiceEndpoint serviceEndpoint, TimeSpan receiveTimeout, TimeSpan sendTimeout,
            int maxItemsInObjectGraph)
            : base(
                endpointDefinition, serviceEndpoint, receiveTimeout, sendTimeout,
                maxItemsInObjectGraph)
        {
            CreateChannel();
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method creates the WCF Channel Factory for this endpoint and then uses it to create the channel.
        /// </summary>
        /// <returns> Returns TRUE if the channel was created, otherwise FALSE. </returns>
        public override bool CreateChannel()
        {
            var cfIPoll = new ChannelFactory<IPoll>(ServiceEndpoint.Binding, ServiceEndpoint.Address);
            cfIPoll.Credentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Delegation;
            SetMaxItemsInObjectGraph(ServiceEndpoint.Contract.Operations, MaxItemsInObjectGraph);

            /*
            if (Context.UserInfo != null)
                XiClientCredentials.SetClientCredentials(Context.UserData, ServiceEndpoint, cfIPoll.Credentials,
                                                            Context.UserInfo);
                */

            _iPoll = cfIPoll.CreateChannel();

            return Channel != null;
        }

        /// <summary>
        ///     This data member is the WCF channel for this endpoint.
        /// </summary>
        public override ICommunicationObject Channel
        {
            get
            {
                var result = _iPoll as ICommunicationObject;
                if (result == null) throw new InvalidOperationException();
                return result;
            }
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
                if (_iPoll == null) throw new InvalidOperationException();
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