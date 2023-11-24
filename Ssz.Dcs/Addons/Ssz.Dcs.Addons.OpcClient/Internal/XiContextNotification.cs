namespace Ssz.Xi.Client.Internal
{
    /// <summary>
    ///     Use a method of this type to receive notifications from the Context.
    /// </summary>
    /// <param name="sender"> The calling object. </param>
    /// <param name="notificationData"> The data contained in the notification. </param>
    internal delegate void XiContextNotification(object sender, XiContextNotificationData notificationData);
}