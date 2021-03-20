using System;
using Xi.Common.Support;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.ListItems
{
    /// <summary>
    ///   This is the Root Class for all types of List Entries
    /// </summary>
    public abstract class ServerElementListItemBase : IDisposable
    {
        #region construction and destruction        

        /// <summary>
        ///   Constructor that requires the clientAlias and serverAlias to be specified.
        /// </summary>
        /// <param name = "clientAlias"></param>
        /// <param name = "serverAlias"></param>
        public ListItemRoot(uint clientAlias, uint serverAlias)
        {
            ClientAlias = clientAlias;
            ServerAlias = serverAlias;

            Disposed = false;
        }

        /// <summary>
        ///   This is the implementation of the IDisposable.Dispose method.  The client 
        ///   application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   This method is invoked when the IDisposable.Dispose or Finalize actions are 
        ///   requested.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;
            /*
			if (disposing)
			{
				// Release and Dispose managed resources.			
			}*/
            // Release unmanaged resources.
            // Set large fields to null.			
            Disposed = true;
        }

        /// <summary>
        ///   Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~ListItemRoot()
        {
            Dispose(false);
        }

        #endregion

        //if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ValueRoot."); 

        #region public functions

        /// <summary>
        ///   ClientAlias is in reference to the Xi client.
        /// </summary>
        public uint ClientAlias { get; set; }

        /// <summary>
        ///   ServerAlias is in referece to the Xi server.
        /// </summary>
        public uint ServerAlias { get; set; }

        /// <summary>
        ///   This flag being true indicates that this value has changed since the 
        ///   last poll request or data change callback was issued.
        /// </summary>
        public bool EntryQueued { get; set; }

        /// <summary>
        ///   The Xi Status for this data value
        /// </summary>
        public uint StatusCode { get; set; }

        /// <summary>
        ///   This property is provides the data type used to transported the value.
        /// </summary>
        public virtual TransportDataType ValueTransportTypeKey { get { return TransportDataType.Unknown; } }

        /// <summary>
        ///   Keep a copy of the InstanceId for local use
        /// </summary>
        public InstanceId InstanceId;

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion
    }
}