using System;
using System.Collections.Generic;
using Ssz.Utils;
using Ssz.DataGrpc.Server;

namespace Ssz.DataGrpc.Client.Core.ListItems
{
    /// <summary>
    ///     This class defines an element of an DataGrpc Event List. It contains the Event Message sent by the server.
    /// </summary>
    public class ClientEventListItem
    {
        #region construction and destruction

        /// <summary>
        ///     The constructor for ClientEventListElements
        /// </summary>
        /// <param name="messageKey">
        ///     The message clientListId that uniquely identifies the event. For alarms, this identifies the
        ///     alarm itself, independent of its state, or the occurrence being reported.
        /// </param>
        /// <param name="eventMessage"> The Event Message reported by the server. </param>
        public ClientEventListItem(EventMessage eventMessage, string? messageKey = null)
        {
            _messageKey = messageKey;
            _eventMessage = eventMessage;
            if (string.IsNullOrEmpty(_messageKey)) _messageKey = Guid.NewGuid().ToString();
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This property contains the Event Message received from the server in an Event Notification.
        /// </summary>
        public EventMessage EventMessage
        {
            get { return _eventMessage; }
            set
            {
                _eventMessage = value;
                if (string.IsNullOrEmpty(_messageKey)) _messageKey = Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        ///     The unique message identifier constructed from fields of the Event Message by the ClientBase. The fields
        ///     used to construct the Message Key are specific to the EventType of the Event Message. For alarms, the message
        ///     clientListId identifies the alarm itself, independent of its state, or the occurrence being reported.
        /// </summary>
        public string? MessageKey
        {
            get { return _messageKey; }
        }        

        #endregion

        #region private fields

        /// <summary>
        ///     The private representation of the EventMessage property
        /// </summary>
        private EventMessage _eventMessage;

        /// <summary>
        ///     The private representation of the MessageKey property
        /// </summary>
        private string? _messageKey;

        #endregion
    }
}