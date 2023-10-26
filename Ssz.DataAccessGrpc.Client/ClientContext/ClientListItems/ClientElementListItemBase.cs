using System;
using Ssz.Utils.DataAccess;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Core;
using Ssz.Utils;
using Google.Protobuf.WellKnownTypes;

namespace Ssz.DataAccessGrpc.Client.ClientListItems
{
    /// <summary>
    ///     This is the base class for elements of all DataAccessGrpcLists (e.g. ElementValueList, EventList).
    ///     DataAccessGrpcLists maintain their elements in a Keyed Collection.
    /// </summary>
    internal abstract class ClientElementListItemBase
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates a new DataAccessGrpcList element and sets its state to NewValue.
        /// </summary>        
        /// <param name="elementId"> The InstanceId for this list element. </param>
        protected ClientElementListItemBase(string elementId)
        {            
            ElementId = elementId;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This property is provided for the DataAccessGrpc Client
        ///     application to associate this list element with an
        ///     object of its choosing.
        /// </summary>
        public object? Obj { get; set; }

        /// <summary>
        ///     This method is used to set the state of the data object to allow it to be
        ///     removed from the ServerBase. If the data object has not yet been added to the
        ///     server, This method is used to set the state of the data object to disposable.
        /// </summary>
        /// <returns> Returns true if the state of data object was successfully set to RemoveableFromServer or Disposable. </returns>
        public void PrepareForRemove()
        {
            PreparedForRemove = true;
        }

        /// <summary>
        ///     This property provides the number of times this DataAccessGrpc Value
        ///     has been updated with a new value.
        /// </summary>
        public uint UpdateCount { get; private set; }

        /// <summary>
        ///     This property contains the ClientBase-assigned identifier for this list element.
        ///     This identifier is unique within the DataAccessGrpcList.
        /// </summary>
        public uint ClientAlias { get; set; }

        /// <summary>
        ///     This property contains the server-assigned identifier for this list element.
        ///     This identifier is unique within the DataAccessGrpcList.
        /// </summary>
        public uint ServerAlias { get; set; }

        public ResultInfo? AddItemResultInfo { get; set; }

        /// <summary>
        ///     This property is the InstanceId of this DataAccessGrpcList element if it has one.
        /// </summary>
        public string ElementId { get; }

        /// <summary>
        ///     In Client List
        /// </summary>
        public bool IsInClientList { get; set; }        

        /// <summary>
        ///     In Server List
        /// </summary>
        public bool IsInServerList { get; set; }

        /// <summary>
        ///     Marked For Add To Server
        /// </summary>
        public bool PreparedForAdd { get; set; }

        /// <summary>
        ///     Marked For Remove From Server
        /// </summary>
        public bool PreparedForRemove { get; set; }

        #endregion

        #region protected functions

        /// <summary>
        ///     This method is used to increment the update count when a new
        ///     value is present.  This method should only be invoked within
        ///     the DataAccessGrpc Client Base classes.
        /// </summary>
        /// <returns> Returns the newly incremented update count. </returns>
        protected uint IncrementUpdateCount()
        {
            UpdateCount += 1;
            return UpdateCount;
        }

        #endregion
    }
}