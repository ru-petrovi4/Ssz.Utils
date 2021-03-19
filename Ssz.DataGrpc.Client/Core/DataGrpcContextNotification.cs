namespace Ssz.DataGrpc.Client.Core
{
    /// <summary>
    ///     Use a method of this type to receive notifications from the Context.
    /// </summary>
    /// <param name="sender"> The calling object. </param>
    /// <param name="notificationData"> The data contained in the notification. </param>
    public delegate void DataGrpcContextNotification(object sender, DataGrpcContextNotificationData notificationData);
}