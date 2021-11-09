using System;
using System.Collections.Generic;
using Ssz.Utils;
using Ssz.Xi.Client.Api.ListItems;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.ListItems
{
    /// <summary>
    ///     This class defines an element of an Xi Event List. It contains the Event Message sent by the server.
    /// </summary>
    internal class XiEventListItem : XiListItemRoot, IXiEventListItem
    {
        #region construction and destruction

        /// <summary>
        ///     The constructor for XiEventListElements
        /// </summary>
        /// <param name="messageKey">
        ///     The message clientListId that uniquely identifies the event. For alarms, this identifies the
        ///     alarm itself, independent of its state, or the occurrence being reported.
        /// </param>
        /// <param name="eventMessage"> The Event Message reported by the server. </param>
        public XiEventListItem(EventMessage eventMessage, string? messageKey = null)
        {
            _messageKey = messageKey;
            _eventMessage = eventMessage.ToEventMessage();
            if (string.IsNullOrEmpty(_messageKey)) _messageKey = Guid.NewGuid().ToString();
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This property contains the Event Message received from the server in an Event Notification.
        /// </summary>
        public Ssz.Utils.DataAccess.EventMessage EventMessage
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

        /// <summary>
        ///     This property contains the list of vendor-specific fields selected by the client application
        ///     for the Event Message.
        /// </summary>
        public IEnumerable<XiEventMessageFieldValue>? VendorFields
        {
            get { return _vendorFields is not null ? _vendorFields.ToArray() : null; }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     This method is used to request that category-specific fields be
        ///     included in event messages generated for alarms and events of
        ///     the category for the Event List.
        /// </summary>
        /// <param name="categoryId"> The category for which event message fields are being added. </param>
        /// <param name="fieldValue">
        ///     The category-specific field to be included in the event messages generated for alarms and
        ///     events of the category.
        /// </param>
        /// <returns> The Client Alias generated as the _vendorFields dictionary clientListId for the field. </returns>
        private uint AddEventMessageField(uint categoryId, XiEventMessageFieldValue fieldValue)
        {
            if (_vendorFields is null) _vendorFields = new ObjectManager<XiEventMessageFieldValue>(10);
            return _vendorFields.Add(fieldValue);
        }

        #endregion

        #region private fields

        /// <summary>
        ///     The private representation of the EventMessage property
        /// </summary>
        private Ssz.Utils.DataAccess.EventMessage _eventMessage;

        /// <summary>
        ///     The private representation of the MessageKey property
        /// </summary>
        private string? _messageKey;

        /// <summary>
        ///     The private representation of the VendorFields property
        /// </summary>
        private ObjectManager<XiEventMessageFieldValue>? _vendorFields;

        #endregion
    }
}