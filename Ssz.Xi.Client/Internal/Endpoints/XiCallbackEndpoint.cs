using System;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using Ssz.Utils;
using Ssz.Xi.Client.Api;
using Xi.Contracts;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Endpoints
{
    /// <summary>
    ///     This class defines endpoints that support the Xi IRegisterForCallback and ICallback interfaces.
    /// </summary>
    internal sealed class XiCallbackEndpoint : XiEndpointRoot
    {
        #region construction and destruction

        /// <summary>
        ///     The contstructor for IRegisterForCallback/ICallback endpoints
        /// </summary>
        /// <param name="endpointDefinition"> The EndpointDefinition for this endpoint. </param>
        /// <param name="serviceEndpoint"> TThe ServiceEndpoint definition for this endpoint. </param>
        /// <param name="receiveTimeout">
        ///     The inactivity time interval to be used by the server to timeout this endpoint when no
        ///     requests are received from the client.
        /// </param>
        /// <param name="sendTimeout"> The length of time WCF will wait for a response before throwing an exception. </param>
        /// <param name="maxItemsInObjectGraph"> The number of objects the server will serialize into a single response. </param>
        /// <param name="xiCallbackDoer"></param>
        internal XiCallbackEndpoint(EndpointDefinition endpointDefinition,
            ServiceEndpoint serviceEndpoint, TimeSpan receiveTimeout, TimeSpan sendTimeout,
            int maxItemsInObjectGraph, IDispatcher xiCallbackDoer)
            : base(
                endpointDefinition, serviceEndpoint, receiveTimeout, sendTimeout,
                maxItemsInObjectGraph)
        {
            _xiCallbackDoer = xiCallbackDoer;

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

            _xiCallback = null;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method is invoked to allow the client to set or change the
        ///     keepAliveSkipCount and callbackRate. The first time this method is
        ///     invoked the server obtains the callback interface from the client.
        ///     Therefore, this method must be called at least once for each
        ///     callback endpoint to enable the server to make the callbacks.
        /// </summary>
        /// <param name="contextId"> The context to which this endpoint belongs. </param>
        /// <param name="keepAliveSkipCount">
        ///     The client-requested keepAliveSkipCount for lists that the server may negotiate up or
        ///     down. The keepAliveSkipCount indicates the number of consecutive UpdateRate cycles for a list that occur with
        ///     nothing to send before an empty callback is sent to indicate a keep-alive message. For example, if the value of
        ///     this parameter is 1, then a keep-alive callback will be sent each UpdateRate cycle for each list assigned to the
        ///     callback for which there is nothing to send. A value of 0 indicates that keep-alives are not to be sent for any
        ///     list assigned to the callback.
        /// </param>
        /// <param name="callbackRate">
        ///     <para>
        ///         The callback rate indicates the maximum time between callbacks that are sent to the client. The server may
        ///         negotiate this value up or down, but a null value or a value representing 0 time is not valid.
        ///     </para>
        ///     <para>
        ///         If there are no callbacks to be sent containing data or events for this period of time, an empty callback
        ///         will be sent as a keep-alive. The timer for this time-interval starts when the SetCallback() response is
        ///         returned by the server.
        ///     </para>
        /// </param>
        /// <returns> The results of the operation, including the negotiated keep-alive skip count and callback rate. </returns>
        public void SetCallback(string contextId, uint keepAliveSkipCount, TimeSpan callbackRate)
        {
            if (_iRegisterForCallback is null) throw new Exception("No IRegisterForCallback Endpoint");

            _keepAliveSkipCount = keepAliveSkipCount;
            _callbackRate = callbackRate;

            SetCallbackResult scr = _iRegisterForCallback.SetCallback(contextId, keepAliveSkipCount,
                callbackRate);
        }

        /// <summary>
        ///     This method creates the WCF Channel Factory for this endpoint and then uses it to create the channel.
        /// </summary>
        /// <returns> Returns TRUE if the channel was created, otherwise FALSE. </returns>
        public override bool CreateChannel()
        {
            _xiCallback = new XiCallback(_xiCallbackDoer);

            /*
            var netTcpBinding = this.ServiceEndpoint.Binding as NetTcpBinding;
            if (netTcpBinding is not null)
            {
                netTcpBinding.MaxBufferSize = 2147483647;
                netTcpBinding.MaxReceivedMessageSize = 2147483647;
                netTcpBinding.Security.Mode = SecurityMode.None;
            }*/

            var cfIRegisterForCallback = new DuplexChannelFactory<IRegisterForCallback>(new InstanceContext(_xiCallback),
                ServiceEndpoint.Binding,
                ServiceEndpoint.Address);
            cfIRegisterForCallback.Credentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Delegation;
            SetMaxItemsInObjectGraph(ServiceEndpoint.Contract.Operations, MaxItemsInObjectGraph);

            /*
            if (Context.UserInfo is not null)
            {
                XiClientCredentials.SetClientCredentials(Context.UserData, ServiceEndpoint,
                                                            cfIRegisterForCallback.Credentials, Context.UserInfo);
            }
                */

            _iRegisterForCallback = cfIRegisterForCallback.CreateChannel();

            return Channel is not null;
        }

        /// <summary>
        ///     This data member is the WCF channel for this endpoint.
        /// </summary>
        public override ICommunicationObject Channel
        {
            get
            {
                var result = _iRegisterForCallback as ICommunicationObject;
                if (result is null) throw new InvalidOperationException();
                return result;
            }
        }

        /// <summary>
        ///     The Keep Alive Skip Count for the callback endpoint. The keepAliveSkipCount
        ///     indicates the number of consecutive UpdateRate cycles for a list that occur
        ///     with nothing to send before an empty callback is sent to indicate a keep-alive
        ///     message. For example, if the value of this parameter is 1, then a keep-alive
        ///     callback will be sent each UpdateRate cycle for each list assigned to the callback
        ///     for which there is nothing to send.  A value of 0 indicates that keep-alives are
        ///     not to be sent for any list assigned to the callback.
        /// </summary>
        public uint KeepAliveSkipCount
        {
            get { return _keepAliveSkipCount; }
        }

        /// <summary>
        ///     <para>
        ///         The callback rate indicates the maximum time between callbacks that are sent to the client. The server may
        ///         negotiate this value up or down, but a null value or a value representing 0 time is not valid.
        ///     </para>
        ///     <para>
        ///         If there are no callbacks to be sent containing data or events for this period of time, an empty callback
        ///         will be sent as a keep-alive. The timer for this time-interval starts when the SetCallback() response is
        ///         returned by the server.
        ///     </para>
        /// </summary>
        public TimeSpan CallbackRate
        {
            get { return _callbackRate; }
        }

        /// <summary>
        ///     This property is used to make calls on the Xi IRegisterForCallback interface.
        /// </summary>
        public IRegisterForCallback? Proxy
        {
            get { return _iRegisterForCallback; }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member is the private representation of the CallbackRate property.
        /// </summary>
        private TimeSpan _callbackRate;

        /// <summary>
        ///     This data member is the private representation of the Proxy property.
        /// </summary>
        private IRegisterForCallback? _iRegisterForCallback;

        /// <summary>
        ///     This data member is the private representation of the KeepAliveSkipCount property.
        /// </summary>
        private uint _keepAliveSkipCount;

        /// <summary>
        ///     This data member is used to make calls on the Xi ICallback interface.
        /// </summary>
        private XiCallback? _xiCallback;
        private readonly IDispatcher _xiCallbackDoer;

        #endregion
    }
}