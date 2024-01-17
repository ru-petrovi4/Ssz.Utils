using System;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using Grpc.Core;

namespace Ssz.DataAccessGrpc.Client.ClientListItems
{
    /// <summary>
    ///     The DataAccessGrpc Data List Value class is used by the DataAccessGrpc Data List
    ///     to represent a single process data value along with its
    ///     status / quality and time stamp.
    /// </summary>
    internal class ClientElementValueListItem : ClientElementListItemBase
    {
        #region construction and destruction
        
        /// <summary>
        ///     This constructor creates an DataAccessGrpc Data Object using its client alias and Instance Id.
        /// </summary>        
        /// <param name="elementId"> The InstanceId used by the server to identify the data object. </param>
        public ClientElementValueListItem(string elementId)
            : base(elementId)
        {
        }

        #endregion

        #region public functions        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        public void Update(ValueStatusTimestamp valueStatusTimestamp)
        {                     
            _valueStatusTimestamp = valueStatusTimestamp;
            IncrementUpdateCount();
        }

        /// <summary>
        ///     This method is used to set PendingWriteValue to a value to be written.
        ///     If the data object is not writable, this method sets PendingWriteValue
        ///     to null. After preparing one or more data objects to be written, the
        ///     client application issues the data list CommitDataObjectWrites() method
        ///     to write them to the server in a single call.
        /// </summary>
        /// <param name="valueStatusTimestamp"> The data value to be written. </param>
        /// <returns> Returns TRUE if the data object is writable, otherwise FALSE. </returns>
        public bool PrepareForWrite(ValueStatusTimestamp valueStatusTimestamp)
        {
            //if (!IsWritable)
            //{
            //    _pendingWriteValueStatusTimestamp = null;
            //    return false;
            //}
            _pendingWriteValueStatusTimestamp = valueStatusTimestamp;
            return true;
        }

        public void HasWritten(ResultInfo writeResultInfo)
        {
            _pendingWriteValueStatusTimestamp = null;
            _writeResultInfo = writeResultInfo;
        }

        /// <summary>
        ///     This property contains the data value for the data object.
        /// </summary>
        public ValueStatusTimestamp ValueStatusTimestamp
        {
            get { return _valueStatusTimestamp; }
        }

        /// <summary>
        ///     This property contains the data value that is to be written to the data object.
        ///     Prior to writing a value to the server, the client application sets the
        ///     PendingWriteValue for one or more data objects using the PrepWriteValue() method,
        ///     and then issues the data list CommitDataObjectWrites() method to write them
        ///     to the server in a single call.
        /// </summary>
        public ValueStatusTimestamp? PendingWriteValueStatusTimestamp
        {
            get { return _pendingWriteValueStatusTimestamp; }
        }

        /// <summary>
        ///     This property contains the ResultInfo associated with writing the PendingWriteValueStatusTimestamp.        
        /// </summary>
        public ResultInfo? WriteResultInfo
        {
            get { return _writeResultInfo; }
        }

        /// <summary>
        ///     Marked For Write to Server
        /// </summary>
        public bool PreparedForWrite
        {
            get { return _pendingWriteValueStatusTimestamp is not null; }
        }

        /// <summary>
        ///     Marked For Read From Server
        /// </summary>
        public bool PreparedForRead { get; private set; }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member is the private representation of the PendingWriteDataValue property.
        /// </summary>
        private ValueStatusTimestamp? _pendingWriteValueStatusTimestamp;

        private ResultInfo? _writeResultInfo;

        /// <summary>
        ///     This data member is the private representation of the DataValue property.
        /// </summary>
        private ValueStatusTimestamp _valueStatusTimestamp = new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain };

        #endregion
    }
}