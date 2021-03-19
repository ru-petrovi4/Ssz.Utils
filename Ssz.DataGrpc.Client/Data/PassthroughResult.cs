namespace Ssz.DataGrpc.Client.Data
{
    /// <summary>
    ///     This class defines the response messsage that is returned
    ///     to the client for a passthrough request.
    /// </summary>    
    public class PassthroughResult
    {
        #region public functions
        
        public uint ResultCode { get; set; }

        public byte[] ReturnData { get; set; } = new byte[0];

        #endregion
    }
}