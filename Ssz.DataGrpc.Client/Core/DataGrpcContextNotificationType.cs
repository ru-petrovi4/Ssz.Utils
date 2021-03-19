namespace Ssz.DataGrpc.Client.Core
{
    /// <summary>
    ///     This enumeration indicates why the notification is being sent.
    /// </summary>
    public enum DataGrpcContextNotificationType
    {
        /// <summary>
        ///     The server shutting down.
        ///     The Data property contains a string that describes the reason for the shutdown.
        /// </summary>
        Shutdown,

        /// <summary>
        ///     The WCF connection to the resource management endpoint has been unexpectedly disconnected.
        ///     The Data property contains a string that describes the failure.
        /// </summary>
        ResourceManagementDisconnected,

        /// <summary>
        ///     The WCF connection to the resource management endpoint has been unexpectedly disconnected and is not recoverable.
        ///     The Data property contains a string that describes the failure.
        /// </summary>
        ResourceManagementFail,        

        /// <summary>
        ///     Data updates or event messages cached by the server for polling have been discarded by the server due to failure to
        ///     receive a poll for them.
        ///     The Data property contains a uint that indicates the number discarded.
        /// </summary>
        Discards,

        /// <summary>
        ///     A type conversion error has occurred in the client on received data.
        ///     The Data property contains a string that describes the conversion error.
        /// </summary>
        TypeConversionError,

        /// <summary>
        ///     A FaultException was received from the server for a ClientKeepAlive request that was issued by the ClientBase.
        ///     The FaultException type accompanies this notification type.
        /// </summary>
        ClientKeepAliveException,

        /// <summary>
        ///     Callback from server hasn't been recieved > CallbackRate.
        /// </summary>
        ServerKeepAliveError,

        /// <summary>
        ///     A FaultException was received from the server for Poll request that was issued by the ClientBase.
        ///     The FaultException type accompanies this notification type.
        /// </summary>
        PollException,

        /// <summary>
        ///     A general Exception was received for a request that was issued by the ClientBase.
        ///     The Exception type accompanies this notification type.
        /// </summary>
        GeneralException
    }
}