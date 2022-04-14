namespace Ssz.DataGrpc.Client.Data
{
    /// <summary>
    ///     Use a method of this type to receive notifications from the Context.
    /// </summary>
    /// <param name="sender"> The calling object. </param>
    /// <param name="notificationData"> The data contained in the notification. </param>
    internal delegate void ClientContextNotification(object sender, ClientContextNotificationData notificationData);
}