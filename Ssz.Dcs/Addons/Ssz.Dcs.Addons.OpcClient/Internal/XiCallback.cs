using System;
using Ssz.Utils;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Internal.Context;
using Xi.Contracts;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal
{
    /// <summary>
    ///     This class provides the method for the ICallback interface
    /// </summary>
    internal class XiCallback : ICallback
    {
        #region construction and destruction

        public XiCallback(IDispatcher xiCallbackDoer)
        {
            _xiCallbackDoer = xiCallbackDoer;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     <para>
        ///         This callback method is implemented by the client to be notified when the server server state changes to
        ///         Aborting. Clients that use the poll interface instead of this callback interface can add the ServerDescription
        ///         object to a data object list to be notified when the server state transitions to the aborting state.
        ///     </para>
        /// </summary>
        /// <param name="contextId"> The context identifier. </param>
        /// <param name="serverStatus"> The ServerStatus object that describes the server that is shutting down. </param>
        /// <param name="reason"> The reason the context is being closed. </param>
        void ICallback.Abort(string contextId, ServerStatus serverStatus, string reason)
        {
            XiContext? context = XiContext.LookUpContext(contextId);
            if (context is not null)
            {
                context.ServerContextIsClosing = true;                
                _xiCallbackDoer.BeginInvoke(ct => context.Abort(serverStatus, reason));
            }
        }

        /// <summary>
        ///     <para> This callback method is implemented by the client to receive data changes. </para>
        ///     <para>
        ///         Servers send data changes to the client that have not been reported to the client via this method. Changes
        ///         consists of:
        ///     </para>
        ///     <para> 1) values for data objects that were added to the list, </para>
        ///     <para>
        ///         2) values for data objects whose current values have changed since the last time they were reported to the
        ///         client via this interface. If a deadband filter has been defined for the list, floating point values are not
        ///         considered to have changed unless they have changed by the deadband amount.
        ///     </para>
        ///     <para> 3) historical values that meet the list filter criteria, including the deadband. </para>
        ///     <para>
        ///         In addition, the server may insert a special value that indicates the server or one of its wrapped servers
        ///         are shutting down.
        ///     </para>
        ///     <para>
        ///         This value is inserted as the first value in the list of values in the callback. Its ListId and ClientId are
        ///         both 0 and its data type is ServerStatus.
        ///     </para>
        /// </summary>
        /// <param name="contextId"> The context identifier. </param>
        /// <param name="clientListId"> The client identifier of the list for which data changes are being reported. </param>
        /// <param name="updatedValues"> The values being reported. </param>
        void ICallback.InformationReport(string contextId, uint clientListId, DataValueArraysWithAlias updatedValues)
        {
            XiContext? context = XiContext.LookUpContext(contextId);
            if (context is not null)
            {                
                _xiCallbackDoer.BeginInvoke(ct => context.ElementValuesCallback(clientListId, updatedValues));
            }
        }

        /// <summary>
        ///     <para> This callback method is implemented by the client to receive alarms and events. </para>
        ///     <para>
        ///         Servers send event messages to the client via this interface. Event messages are sent when there has been a
        ///         change to the specified event list. A new alarm or event that has been added to the list, a change to an alarm
        ///         already in the list, or the deletion of an alarm from the list constitutes a change to the list.
        ///     </para>
        ///     <para>
        ///         Once an event has been reported from the list, it is automatically deleted from the list. Alarms are only
        ///         deleted from the list when they transition to inactive and acknowledged.
        ///     </para>
        /// </summary>
        /// <param name="contextId"> The context identifier. </param>
        /// <param name="clientListId"> The client identifier of the list for which alarms/events are being reported. </param>
        /// <param name="eventsArray"> The list of alarms/events are being reported. </param>
        void ICallback.EventNotification(string contextId, uint clientListId, EventMessage[] eventsArray)
        {
            XiContext? context = XiContext.LookUpContext(contextId);
            if (context is not null)
            {                
                _xiCallbackDoer.BeginInvoke(ct => context.EventMessagesCallback(clientListId, eventsArray));
            }
        }

        /// <summary>
        ///     This method returns the results of invoking an asynchronous passthrough.
        /// </summary>
        /// <param name="contextId"> The context identifier. </param>
        /// <param name="invokeId"> The identifier for this invocation of the passthrough defined by the client in the request. </param>
        /// <param name="passthroughResult">
        ///     The result of executing the passthrough, consisting of the result code, the jobId
        ///     supplied in the request, and a byte array. It is up to the client application to interpret this byte array.
        /// </param>
        void ICallback.PassthroughCallback(string contextId, int invokeId, PassthroughResult passthroughResult)
        {
            XiContext? context = XiContext.LookUpContext(contextId);
            if (context is not null)
            {                
                _xiCallbackDoer.BeginInvoke(ct => context.PassthroughCallback(invokeId, passthroughResult));
            }
        }

        #endregion

        #region private fields

        private readonly IDispatcher _xiCallbackDoer;

        #endregion
    }
}