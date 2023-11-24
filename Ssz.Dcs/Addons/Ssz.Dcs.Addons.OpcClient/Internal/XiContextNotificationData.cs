using System;

namespace Ssz.Xi.Client.Internal
{
    /// <summary>
    ///     The data contained in a context notification.  The ReasonForNotification property specifies
    ///     why the notification is being sent.
    /// </summary>
    internal class XiContextNotificationData : EventArgs
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates an XiContextNotificationData object from the reason for the
        ///     notification and the accompanying data.
        /// </summary>
        /// <param name="reasonForNotification"> The reason for the notification. </param>
        /// <param name="data"> The details of the notification. </param>
        public XiContextNotificationData(XiContextNotificationType reasonForNotification, object? data)
        {
            _reasonForNotification = reasonForNotification;
            _data = data;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This property specifies the reason for the notification.
        /// </summary>
        public XiContextNotificationType ReasonForNotification
        {
            get { return _reasonForNotification; } //set { _reason = value; }
        }

        /// <summary>
        ///     This property contains the details about the notification.
        /// </summary>
        public object? Data
        {
            get { return _data; }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member is the private representation of the Data property.
        /// </summary>
        private readonly object? _data;

        /// <summary>
        ///     This data member is the private representation of the ReasonForNotification property.
        /// </summary>
        private readonly XiContextNotificationType _reasonForNotification;

        #endregion
    }
}