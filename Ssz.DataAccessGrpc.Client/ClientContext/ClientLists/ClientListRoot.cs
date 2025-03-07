using System;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client.ClientLists
{
    /// <summary>
    ///     This abstract class definition allows for the implantation of methods that are
    ///     common to two or more DataAccessGrpc List types.  The DataAccessGrpc Values maintained by this class
    ///     must be a subclass of DataAccessGrpc Value Base.  In general the only time a declaration
    ///     of this type would be used is when the data type can also be processed
    ///     as being of type DataAccessGrpc Value Base.
    /// </summary>
    internal abstract class ClientListRoot : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     DataAccessGrpc List Base is the common base class for all DataAccessGrpc Lists defined within
        ///     the Client Base Assembly.
        /// </summary>
        /// <param name="context"> </param>
        protected ClientListRoot(ClientContext context)
        {
            _clientContext = context;
        }

        /// <summary>
        ///     This is the implementation of the IDisposable.Dispose method.  The client
        ///     application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method is invoked when the IDisposable.Dispose or Finalize actions are
        ///     requested.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {                
                if (_clientContext.ContextIsOperational)
                {
                    try
                    {
                        var t = _clientContext.DeleteListAsync(this);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            // Release unmanaged resources.
            // Set large fields to null.            
            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~ClientListRoot()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     <para>
        ///         This method is used to enable or disable updating of an entire list. When this method is called, the enabled
        ///         state of the list is changed, but the enabled state of the individual elements of the list is unchanged.
        ///     </para>
        ///     <para>
        ///         When a list is disabled, the server excludes it from participating in callbacks and polls. However, at the
        ///         option of the server, the server may continue updating its cache for the elements of the list.
        ///     </para>
        ///     <para> Calling this method also causes the local copy of the list attributes to be updated. </para>
        /// </summary>
        /// <param name="enable">
        ///     Indicates, when TRUE, that updating of the list is to be enabled, and when FALSE, that
        ///     updating of the list is to be disabled.
        /// </param>
        /// <returns> Returns TRUE if the list was successfully enabled or disabled. </returns>
        public async Task<bool> EnableListCallbackAsync(bool enable)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientListRoot.");
            
            if (CallbackIsEnabled != enable)
            {
                CallbackIsEnabled = await _clientContext.EnableListCallbackAsync(ListServerAlias, enable);                
            }
            return CallbackIsEnabled;
        }

        /// <summary>
        ///     <para> This method is used to cause a list to be "touched". </para>
        ///     <para>
        ///         For lists that contain data objects, this method causes the server to update all data objects in the list
        ///         that are currently enabled (see the ClientElementValueList EnableListElementUpdating() method), mark them as changed (even
        ///         if their values did not change), and then return them all to the client in the next callback or poll.
        ///     </para>
        ///     <para>
        ///         For lists that contain events, this method causes the server to mark all alarms/event in the list as
        ///         changed, and then return them all to the client in the next callback.
        ///     </para>
        /// </summary>
        /// <returns> The result code for the operation. See DataAccessGrpcFaultCodes class for standardized result codes. </returns>
        public void TouchList()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientListRoot.");

            _clientContext.TouchList(ListServerAlias);
        }

        /// <summary>
        ///     This property provides the DataAccessGrpc Context to which this list belongs.
        /// </summary>
        public ClientContext Context
        {
            get { return _clientContext; }
        }

        /// <summary>
        ///     The Client LocalId provides a handle by which this DataAccessGrpc List is known within
        ///     the client code.  This value is established by the client code.
        /// </summary>
        public uint ListClientAlias { get; set; }


        public bool IsInServerContext { get; set; }

        /// <summary>
        ///     The Server LocalId provides a handle by which this DataAccessGrpc List is known within
        ///     the server code.  This value is established by the server code.
        /// </summary>
        public uint ListServerAlias { get; set; }

        /// <summary>
        ///     This property identifies the Standard DataAccessGrpc List Type of this list.
        /// </summary>
        public uint ListType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool CallbackIsEnabled { get; private set; }

        /// <summary>
        ///     This property is provided for the DataAccessGrpc Client application to associate this list
        ///     with an object of its choosing.
        /// </summary>
        public object? ClientTag { get; set; }

        public bool Disposed { get; private set; }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member is the private representation of
        ///     the public Context property.
        /// </summary>
        private readonly ClientContext _clientContext;        

        #endregion
    }
}


///// <summary>
/////     Use this property to obtain the List Type as a string value.
///// </summary>
//public string ListTypeAsString
//{
//    get
//    {
//        if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientListRoot.");

//        if (ListAttributes is null) throw new InvalidOperationException();
//        if (ListAttributes.ListType < 4096)
//        {
//            var lt = (StandardListType) ListAttributes.ListType;
//            return lt.ToString("G");
//        }
//        return ListAttributes.ListType.ToString();
//    }
//}