using System;
using Ssz.DataGrpc.Common;

namespace Ssz.DataGrpc.Client.Core.ListItems
{
    /// <summary>
    ///     The DataGrpc Data List Value class is used by the DataGrpc Data List
    ///     to represent a single process data value along with its
    ///     status / quality and time stamp.
    /// </summary>
    public class DataGrpcElementValueListItem : DataGrpcElementListItemBase
    {
        #region construction and destruction
        
        /// <summary>
        ///     This constructor creates an DataGrpc Data Object using its client alias and Instance Id.
        /// </summary>        
        /// <param name="elementId"> The InstanceId used by the server to identify the data object. </param>
        public DataGrpcElementValueListItem(string elementId)
            : base(elementId)
        {
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method is called by the ClientBase when a new value has been received for
        ///     the data object from the server. It, in turn, calls the Update() method on the
        ///     DataValue property to complete the update, and then increments the update count
        ///     for the data object.
        /// </summary>
        /// <param name="statusCode"> The DataGrpc StatusCode of the value. </param>
        /// <param name="timeStampUtc"> The timestamp of the value. </param>
        /// <param name="valueUInt32"> The value </param>
        public void UpdateValue(uint valueUInt32, uint statusCode, DateTime timeStampUtc)
        {
            DataGrpcValueStatusTimestamp.Value.Set(valueUInt32, ValueTypeCode, false);
            DataGrpcValueStatusTimestamp.StatusCode = statusCode;
            DataGrpcValueStatusTimestamp.TimestampUtc = timeStampUtc;
            IncrementUpdateCount();
        }

        /// <summary>
        ///     This method is called by the ClientBase when a new value has been received for
        ///     the data object from the server. It, in turn, calls the Update() method on the
        ///     DataValue property to complete the update, and then increments the update count
        ///     for the data object.
        /// </summary>
        /// <param name="statusCode"> The DataGrpc StatusCode of the value. </param>
        /// <param name="timeStampUtc"> The timestamp of the value. </param>
        /// <param name="valueDouble"> The value </param>
        public void UpdateValue(double valueDouble, uint statusCode, DateTime timeStampUtc)
        {
            DataGrpcValueStatusTimestamp.Value.Set(valueDouble, ValueTypeCode, false);
            DataGrpcValueStatusTimestamp.StatusCode = statusCode;
            DataGrpcValueStatusTimestamp.TimestampUtc = timeStampUtc;
            IncrementUpdateCount();
        }

        /// <summary>
        ///     This method is called by the ClientBase when a new value has been received for
        ///     the data object from the server. It, in turn, calls the Update() method on the
        ///     DataValue property to complete the update, and then increments the update count
        ///     for the data object.
        /// </summary>
        /// <param name="statusCode"> The DataGrpc StatusCode of the value. </param>
        /// <param name="timeStampUtc"> The timestamp of the value. </param>
        /// <param name="valueObject"> The value </param>
        public void UpdateValue(object? valueObject, uint statusCode, DateTime timeStampUtc)
        {
            DataGrpcValueStatusTimestamp.Value.Set(valueObject);
            DataGrpcValueStatusTimestamp.StatusCode = statusCode;
            DataGrpcValueStatusTimestamp.TimestampUtc = timeStampUtc;
            IncrementUpdateCount();
        }

        /// <summary>
        ///     This method is used to set PendingWriteValue to a value to be written.
        ///     If the data object is not writable, this method sets PendingWriteValue
        ///     to null. After preparing one or more data objects to be written, the
        ///     client application issues the data list CommitDataObjectWrites() method
        ///     to write them to the server in a single call.
        /// </summary>
        /// <param name="dataGrpcValueStatusTimestamp"> The data value to be written. </param>
        /// <returns> Returns TRUE if the data object is writable, otherwise FALSE. </returns>
        public bool PrepareForWrite(DataGrpcValueStatusTimestamp dataGrpcValueStatusTimestamp)
        {
            if (!IsWritable)
            {
                _pendingWriteDataGrpcValueStatusTimestamp = null;
                return false;
            }
            _pendingWriteDataGrpcValueStatusTimestamp = dataGrpcValueStatusTimestamp;
            return true;
        }

        public void HasWritten(uint resultCodeWrite)
        {
            _pendingWriteDataGrpcValueStatusTimestamp = null;
            _resultCodeWrite = resultCodeWrite;
        }

        public bool PrepareForRead()
        {            
            PreparedForRead = true;
            return true;
        }

        public void HasRead()
        {
            PreparedForRead = false;
        }

        /// <summary>
        ///     This property contains the data value for the data object.
        /// </summary>
        public DataGrpcValueStatusTimestamp DataGrpcValueStatusTimestamp
        {
            get { return _dataGrpcValueStatusTimestamp; }
        }

        /// <summary>
        ///     This property contains the data value that is to be written to the data object.
        ///     Prior to writing a value to the server, the client application sets the
        ///     PendingWriteValue for one or more data objects using the PrepWriteValue() method,
        ///     and then issues the data list CommitDataObjectWrites() method to write them
        ///     to the server in a single call.
        /// </summary>
        public DataGrpcValueStatusTimestamp? PendingWriteDataGrpcValueStatusTimestamp
        {
            get { return _pendingWriteDataGrpcValueStatusTimestamp; }
        }

        /// <summary>
        ///     This property contains the result code associated with writing the PendingWriteDataGrpcValueStatusTimestamp.
        ///     See DataGrpcFaultCodes class for standardized result codes.
        /// </summary>
        public uint ResultCodeWrite
        {
            get { return _resultCodeWrite; }
        }

        /// <summary>
        ///     Marked For Write to Server
        /// </summary>
        public bool PreparedForWrite
        {
            get { return _pendingWriteDataGrpcValueStatusTimestamp != null; }
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
        private DataGrpcValueStatusTimestamp? _pendingWriteDataGrpcValueStatusTimestamp;

        private uint _resultCodeWrite;

        /// <summary>
        ///     This data member is the private representation of the DataValue property.
        /// </summary>
        private DataGrpcValueStatusTimestamp _dataGrpcValueStatusTimestamp = new DataGrpcValueStatusTimestamp();

        #endregion
    }
}