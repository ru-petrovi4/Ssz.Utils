using System;
using Ssz.DataAccessGrpc.Client.Data;
using Ssz.Utils.DataAccess;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Core;

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

        /// <summary>
        ///     This property provides the DataAccessGrpc TypeId for the value contained
        ///     in this list element.
        /// </summary>
        public Ssz.DataAccessGrpc.ServerBase.TypeId? DataTypeId
        {
            get { return _dataTypeId; }
            set
            {
                _dataTypeId = value;
                if (_dataTypeId is null)
                {
                    ValueTypeCode = TypeCode.Empty;
                    return;
                }
                if (_dataTypeId.Namespace is not null)
                {
                    ValueTypeCode = TypeCode.Object;
                    return;
                }
                if (_dataTypeId.SchemaType is not null)
                {
                    ValueTypeCode = TypeCode.Object;
                    return;
                }

                if (0 == string.Compare(typeof (Object).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.Object;
                    return;
                }
                if (0 == string.Compare(typeof (Single).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.Single;
                    return;
                }
                if (0 == string.Compare(typeof (Int32).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.Int32;
                    return;
                }
                if (0 == string.Compare(typeof (String).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.String;
                    return;
                }
                if (0 == string.Compare(typeof (SByte).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.SByte;
                    return;
                }
                if (0 == string.Compare(typeof (Int16).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.Int16;
                    return;
                }
                if (0 == string.Compare(typeof (Int64).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.Int64;
                    return;
                }
                if (0 == string.Compare(typeof (Byte).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.Byte;
                    return;
                }
                if (0 == string.Compare(typeof (UInt16).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.UInt16;
                    return;
                }
                if (0 == string.Compare(typeof (UInt32).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.UInt32;
                    return;
                }
                if (0 == string.Compare(typeof (UInt64).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.UInt64;
                    return;
                }
                if (0 == string.Compare(typeof (Double).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.Double;
                    return;
                }
                if (0 == string.Compare(typeof (DateTime).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.DateTime;
                    return;
                }
                if (0 == string.Compare(typeof (Boolean).ToString(), _dataTypeId.LocalId, true))
                {
                    ValueTypeCode = TypeCode.Boolean;
                    return;
                }

                ValueTypeCode = TypeCode.Object;
            }
        }

        public TypeCode ValueTypeCode { get; private set; }        

        /// <summary>
        ///     This property indicates whether the value associated with the list element is readable.
        /// </summary>
        public bool IsReadable { get; set; }

        /// <summary>
        ///     This property indicates whether the value associated with the list element is writable.
        /// </summary>
        public bool IsWritable { get; set; }

        /// <summary>
        ///     The Status Code provides the latest status as provided by the DataAccessGrpc ServerBase.
        ///     It is initially set to a failed state to indicated that the current value
        ///     is not valid.
        /// </summary>
        public StatusCode StatusCode { get; set; } = StatusCode.Unknown;

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
            return ++UpdateCount;
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This property provides the DataAccessGrpc TypeId for the value contained
        ///     in this list element.
        /// </summary>
        private Ssz.DataAccessGrpc.ServerBase.TypeId? _dataTypeId;

        #endregion
    }
}