using System;
using Ssz.Xi.Client.Api.ListItems;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.ListItems
{
    /// <summary>
    ///     This is the base class for elements of all XiLists (e.g. DataList, EventList).
    ///     XiLists maintain their elements in a Keyed Collection.
    /// </summary>
    internal abstract class XiDataAndDataJournalListItemBase : XiListItemRoot, IXiListItem,
        IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates a new XiList element and sets its state to NewValue.
        /// </summary>
        /// <param name="clientAlias"> The client alias to be assigned to this list element. </param>
        /// <param name="instanceId"> The InstanceId for this list element. </param>
        protected XiDataAndDataJournalListItemBase(uint clientAlias, InstanceId instanceId)
        {
            ClientAlias = clientAlias;
            InstanceId = new InstanceId(instanceId);
            ServerAlias = 0;
            ResultCode = 0xFFFFFFFFu;
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                if (IsInClientList || IsInServerList)
                    throw new Exception("Xi Value not in the a Disposable Value state");
                // Release and Dispose managed resources.
            }
            // Release unmanaged resources.
            // Set large fields to null.
            _valueTypeId = null;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method provides the hexidecimal representation of the Result Code.
        ///     It does not request the Result Code string from the server.
        /// </summary>
        public string ResultCodeAsHex()
        {
            return "0x" + ResultCode.ToString("X8");
        }

        /// <summary>
        ///     This method is used to set the state of the data object to allow it to be
        ///     removed from the server. If the data object has not yet been added to the
        ///     server, This method is used to set the state of the data object to disposable.
        /// </summary>
        /// <returns> Returns true if the state of data object was successfully set to RemoveableFromServer or Disposable. </returns>
        public void PrepareForRemove()
        {
            PreparedForRemove = true;
        }

        public bool IsUseable
        {
            get { return IsInClientList && IsInServerList && !PreparedForRemove; }
        }

        /*
        /// <summary>
        /// This property when TRUE indicates that the state of this list element is Enabled. It can 
        /// be used to change the  state from Enabled to Disabled or from Disabled to Enabled. An
        /// attempt to change the state to Enabled or Disabled from any other state leaves the 
        /// state unchanged.
        /// </summary>
        public bool Enabled
        {
            get { return XiListElementState.Enabled == _state; }
            internal set
            {
                if (XiListElementState.Enabled == _state
                    || XiListElementState.Disabled == _state)
                    _state = (value) ? XiListElementState.Enabled : XiListElementState.Disabled;
            }
        }*/

        /// <summary>
        ///     This property provides the number of times this Xi Value
        ///     has been updated with a new value.
        /// </summary>
        public uint UpdateCount { get; private set; }

        /// <summary>
        ///     Enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///     This property contains the ClientBase-assigned identifier for this list element.
        ///     This identifier is unique within the XiList.
        /// </summary>
        public uint ClientAlias { get; set; }

        /// <summary>
        ///     This property contains the server-assigned identifier for this list element.
        ///     This identifier is unique within the XiList.
        /// </summary>
        public uint ServerAlias { get; set; }

        /// <summary>
        ///     This property provides the Xi TypeId for the value contained
        ///     in this list element.
        /// </summary>
        public TypeId? ValueTypeId
        {
            get { return _valueTypeId; }
            set
            {
                _valueTypeId = value;
                if (_valueTypeId is null)
                {
                    ValueTypeCode = Utils.Any.TypeCode.Empty;
                    return;
                }
                if (_valueTypeId.Namespace is not null)
                {
                    ValueTypeCode = Utils.Any.TypeCode.Object;
                    return;
                }
                if (_valueTypeId.SchemaType is not null)
                {
                    ValueTypeCode = Utils.Any.TypeCode.Object;
                    return;
                }

                if (0 == string.Compare(typeof (Object).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.Object;
                    return;
                }
                if (0 == string.Compare(typeof (Single).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.Single;
                    return;
                }
                if (0 == string.Compare(typeof (Int32).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.Int32;
                    return;
                }
                if (0 == string.Compare(typeof (String).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.String;
                    return;
                }
                if (0 == string.Compare(typeof (SByte).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.SByte;
                    return;
                }
                if (0 == string.Compare(typeof (Int16).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.Int16;
                    return;
                }
                if (0 == string.Compare(typeof (Int64).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.Int64;
                    return;
                }
                if (0 == string.Compare(typeof (Byte).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.Byte;
                    return;
                }
                if (0 == string.Compare(typeof (UInt16).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.UInt16;
                    return;
                }
                if (0 == string.Compare(typeof (UInt32).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.UInt32;
                    return;
                }
                if (0 == string.Compare(typeof (UInt64).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.UInt64;
                    return;
                }
                if (0 == string.Compare(typeof (Double).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.Double;
                    return;
                }
                if (0 == string.Compare(typeof (DateTime).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.DateTimeOffset;
                    return;
                }
                if (0 == string.Compare(typeof (Boolean).ToString(), _valueTypeId.LocalId, true))
                {
                    ValueTypeCode = Utils.Any.TypeCode.Boolean;
                    return;
                }

                ValueTypeCode = Utils.Any.TypeCode.Object;
            }
        }

        public Utils.Any.TypeCode ValueTypeCode { get; private set; }

        /// <summary>
        ///     This property indicates whether the value associated with the list element is readable.
        /// </summary>
        public bool IsReadable { get; set; }

        /// <summary>
        ///     This property indicates whether the value associated with the list element is writable.
        /// </summary>
        public bool IsWritable { get; set; }

        /// <summary>
        ///     The Result Code provides the latest status as provided by the Xi Server.
        ///     It is initially set to a failed state to indicated that the current value
        ///     is not valid.
        /// </summary>
        public uint ResultCode { get; set; }

        /// <summary>
        ///     This property is the InstanceId of this XiList element if it has one.
        /// </summary>
        public InstanceId InstanceId { get; private set; }

        /// <summary>
        ///     In Client List
        /// </summary>
        public bool IsInClientList
        {
            get { return ClientAlias != 0; }
        }

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
        ///     the Xi Client Base classes.
        /// </summary>
        /// <returns> Returns the newly incremented update count. </returns>
        protected uint IncrementUpdateCount()
        {
            return ++UpdateCount;
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This property provides the Xi TypeId for the value contained
        ///     in this list element.
        /// </summary>
        private TypeId? _valueTypeId;

        #endregion
    }
}