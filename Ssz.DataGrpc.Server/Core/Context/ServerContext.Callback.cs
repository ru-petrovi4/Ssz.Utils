using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Ssz.DataGrpc.Server.Core.Lists;
using Xi.Common.Support;
using Xi.Common.Support.Extensions;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.Context
{
    /// <summary>
    ///   This partial class defines the methods that support the methods 
    ///   of the ICallback interface.
    /// </summary>
    public partial class ServerContext        
    {
        #region public functions

        /// <summary>
        ///   This method is used to indicate to the client that the Xi Server is shutting down.
        /// </summary>
        /// <param name="reason"> </param>
        public void OnAbort(ServerStatus serverStatus, string reason)
        {
            if (_iCallback == null) return;
            Task.Factory.StartNew(() => OnAbortTask(serverStatus, reason));
        }

        /// <summary>
        ///   This method invokes an Information Report back to the Xi client for data changes.
        /// </summary>
        /// <param name="listId"> </param>
        /// <param name="readValueList"> </param>
        public virtual void OnInformationReport(uint listId, DataValueArraysWithAlias readValueList)
        {
            if (_iCallback == null) return;
            Task.Factory.StartNew(() => OnInformationReportTask(listId, readValueList));
        }

        /// <summary>
        ///   This method invokes an Event Notification back to the Xi client when an event needs to be reported.
        /// </summary>
        /// <param name="listId"> </param>
        /// <param name="eventsArray"> </param>
        public virtual void OnEventNotification(uint listId, EventMessage[] eventsArray)
        {
            if (_iCallback == null) return;
            Task.Factory.StartNew(() => OnEventNotificationTask(listId, eventsArray));
        }

        /// <summary>
        ///   This method is invoked by a Xi client to establish the clients ICallback interface.
        /// </summary>
        /// <param name="iCallBack"> The reference to the callback to set. </param>
        /// <param name="keepAliveSkipCount"> The number of consecutive UpdateRate cycles that occur with nothing to send before an empty callback is sent to indicate a keep-alive message. For example, if the value of this parameter is 1, then a keep-alive callback will be sent each UpdateRate cycle for which there is nothing to send. A value of 0 indicates that keep-alives are not to be sent. </param>
        /// <param name="callbackRate">
        ///   <para> Optional rate that specifies how often callbacks are to be sent to the client. </para>
        ///   </para> TimeSpan.Zero if not used. When not used, the UpdateRate of the lists assigned to this callback dictates when callbacks are sent. </para>
        ///   <para> When present, the server buffers list outputs when the callback rate is longer than list UpdateRates. </para>
        /// </param>
        /// <returns> The results of the operation, including the negotiated keep-alive skip count and callback rate. </returns>
        public SetCallbackResult OnSetCallback(ICallback iCallBack, uint keepAliveSkipCount, TimeSpan callbackRate)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                _iCallback = iCallBack;

                if (_iRegisterForCallbackEndpointEntry == null)
                {
                    OperationContext ctx = OperationContext.Current;
                    List<EndpointDefinition> epDefs = (from ep in ctx.Host.Description.Endpoints
                                                       where
                                                           ep.Contract.Name.EndsWith(typeof (IRegisterForCallback).Name) &&
                                                           ep.Address.Uri.OriginalString ==
                                                           ctx.Channel.LocalAddress.Uri.OriginalString
                                                       select new EndpointDefinition
                                                                  {
                                                                      EndpointId = Guid.NewGuid().ToString(),
                                                                      BindingName = ep.Binding.Name,
                                                                      ContractType = ep.Contract.Name,
                                                                      Url = ep.Address.Uri.AbsoluteUri,
                                                                      EndpointDescription = ep,
                                                                  }).ToList();

                    if ((epDefs == null) || (epDefs.Count == 0))
                        throw RpcExceptionHelper.Create("Unable to locate connected IRegisterForCallback Endpoint");

                    _iRegisterForCallbackEndpointEntry = new EndpointEntry<TListRoot>(epDefs[0]);

                    AuthorizeEndpointUse(_iRegisterForCallbackEndpointEntry);
                }

                return OnNegotiateCallbackParams(keepAliveSkipCount, callbackRate);
            }
        }

        /// <summary>
        ///   Invoke this method to stop callbacks by letting the callback interface go.
        /// </summary>
        /// <returns> </returns>
        public virtual uint OnClearCallback()
        {
            _iCallback = null;

            return XiFaultCodes.S_OK;
        }

        /// <summary>
        ///   Indicates, when TRUE, that the Callback endpoint is open
        /// </summary>
        public bool CallbackEndpointOpen
        {
            get { return (_iCallback != null); }
        }

        /// <summary>
        ///   Indicates, when TRUE, that the Poll endpoint is open
        /// </summary>
        public bool PollEndpointOpen
        {
            get { return (_iPollEndpointEntry != null); }
        }

        /*
		public EndpointEntry<TList> IPollEndpointEntry { get { return _iPollEndpointEntry; } }

		public EndpointEntry<TList> IRegisterForCallbackEndpointEntry { get { return _iRegisterForCallbackEndpointEntry; } }
		 */

        /// <summary>
        ///   The time of the completion of the last callback sent on this context. 
        ///   The value is not set until the callback call returns.
        /// </summary>
        public DateTime LastCallbackTimeUtc
        {
            get
            {
                using (SyncRoot.Enter())
                {
                    return _lastCallbackTimeUtc;
                }
            }
        }

        /// <summary>
        ///   The time interval for keep-alive callbacks
        /// </summary>
        public TimeSpan CallbackRate
        {
            get { return _callbackRate; }
        }

        #endregion

        #region protected functions

        /// <summary>
        ///   This method can be overriddent by the implementation class to negotitate the 
        ///   keep-alive skip count and the callback rate.
        /// </summary>
        /// <param name="keepAliveSkipCount"> The number of consecutive UpdateRate cycles that occur with nothing to send before an empty callback is sent to indicate a keep-alive message. For example, if the value of this parameter is 1, then a keep-alive callback will be sent each UpdateRate cycle for which there is nothing to send. A value of 0 indicates that keep-alives are not to be sent. </param>
        /// <returns> The results of the operation, including the negotiated keep-alive skip count and callback rate. </returns>
        protected virtual SetCallbackResult OnNegotiateCallbackParams(uint keepAliveSkipCount, TimeSpan callbackRate)
        {
            _keepAliveSkipCount = 0; // Per-list keep-alives are not supported

            // Set the callback rate (the keep-alive rate) to between 5 seconds and one minute
            if (callbackRate.TotalMilliseconds < 5000) _callbackRate = new TimeSpan(0, 0, 0, 0, 5000);
            else if (callbackRate.TotalMilliseconds > 60000) _callbackRate = new TimeSpan(0, 0, 0, 0, 60000);
            else _callbackRate = callbackRate;
            return new SetCallbackResult(XiFaultCodes.S_OK, _keepAliveSkipCount, _callbackRate);
        }

        #endregion

        #region private functions

        /// <summary>
        ///   This method is used to indicate to the client that the Xi Server is shutting down.
        /// </summary>
        /// <param name="reason"> </param>
        private void OnAbortTask(ServerStatus serverStatus, string reason)
        {
            ICallback iCallback = _iCallback;

            if (iCallback == null) return;

            try
            {
                iCallback.Abort(Id, serverStatus, reason);
            }
            catch
            {
                OnClearCallback();
            }
        }

        /// <summary>
        ///   This method invokes an Information Report back to the Xi client for data changes.
        /// </summary>
        /// <param name="listId"> </param>
        /// <param name="valueArrays"> </param>
        private void OnInformationReportTask(uint listId, DataValueArraysWithAlias valueArrays)
        {
            ICallback iCallback = _iCallback;

            if (iCallback == null) return;

            try
            {
                const int maxStringLength = 5000;
                List<Tuple<string, uint, uint, DateTime>> largeStringsList = null;
                if (valueArrays != null && valueArrays.HasObjectValues())
                {
                    for (int i = 0; i < valueArrays.ObjectValues.Length; i++)
                    {
                        var sValue = valueArrays.ObjectValues[i] as String;
                        if (sValue != null && sValue.Length > maxStringLength)
                        {
                            if (largeStringsList == null) largeStringsList = new List<Tuple<string, uint, uint, DateTime>>();
                            var objectAlias = valueArrays.ObjectAlias[i];
                            var objectStatusCode = valueArrays.ObjectStatusCodes[i];
                            var objectTimeStamp = valueArrays.ObjectTimeStamps[i];
                            largeStringsList.Add(Tuple.Create(sValue, objectAlias, objectStatusCode, objectTimeStamp));
                            valueArrays.ObjectAlias[i] = 0;
                            valueArrays.ObjectValues[i] = null;
                        }
                    }
                }

                iCallback.InformationReport(Id, listId, valueArrays);

                if (largeStringsList != null)
                {
                    foreach (var tuple in largeStringsList)
                    {
                        var objectValue = tuple.Item1;
                        var objectAlias = tuple.Item2;
                        var objectStatusCode = tuple.Item3;
                        var objectTimeStamp = tuple.Item4;

                        int len = objectValue.Length;
                        int startIndex = 0;
                        for (;;)
                        {
                            if (startIndex >= len) break;
                            string sValue;
                            if (startIndex + maxStringLength >= len) sValue = objectValue.Substring(startIndex);
                            else sValue = objectValue.Substring(startIndex, maxStringLength);
                            startIndex += maxStringLength;

                            valueArrays = new DataValueArraysWithAlias(0, 0, 1);
                            valueArrays.ObjectValues[0] = sValue;
                            valueArrays.ObjectAlias[0] = objectAlias;
                            valueArrays.ObjectStatusCodes[0] = objectStatusCode;
                            valueArrays.ObjectTimeStamps[0] = objectTimeStamp;

                            iCallback.InformationReport(Id, listId, valueArrays);
                        }
                    }
                }

                using (SyncRoot.Enter())
                {
                    _lastCallbackTimeUtc = DateTime.UtcNow;
                }
            }
            catch /*(Exception ex)*/
            {
                OnClearCallback();
            }
        }

        /// <summary>
        ///   This method invokes an Event Notification back to the Xi client when an event needs to be reported.
        /// </summary>
        /// <param name="listId"> </param>
        /// <param name="eventsArray"> </param>
        private void OnEventNotificationTask(uint listId, EventMessage[] eventsArray)
        {
            ICallback iCallback = _iCallback;

            if (iCallback == null) return;

            try
            {
                iCallback.EventNotification(Id, listId, eventsArray);
                using (SyncRoot.Enter())
                {
                    _lastCallbackTimeUtc = DateTime.UtcNow;
                }
            }
            catch /*(Exception ex)*/
            {
                OnClearCallback();
            }
        }

        #endregion

        #region private fields

        private EndpointEntry<TListRoot> _iPollEndpointEntry;
        private uint _keepAliveSkipCount;
        private EndpointEntry<TListRoot> _iRegisterForCallbackEndpointEntry;
        private DateTime _lastCallbackTimeUtc;
        private TimeSpan _callbackRate;

        private ICallback _iCallback;

        #endregion
    }
}