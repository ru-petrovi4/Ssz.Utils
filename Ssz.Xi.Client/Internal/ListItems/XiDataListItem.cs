﻿using System;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Api.ListItems;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.ListItems
{
    /// <summary>
    ///     The Xi Data List Value class is used by the Xi Data List
    ///     to represent a single process data value along with its
    ///     status / quality and time stamp.
    /// </summary>
    internal class XiDataListItem : XiDataAndDataJournalListItemBase, IXiDataListItem
    {
        #region construction and destruction
        
        /// <summary>
        ///     This constructor creates an Xi Data Object using its client alias and Instance Id.          ///
        /// </summary>
        /// <param name="clientAlias"> The client alias to be assigned to this Xi Value as its local handle. </param>
        /// <param name="instanceId"> The InstanceId used by the server to identify the data object. </param>
        public XiDataListItem(uint clientAlias, InstanceId instanceId)
            : base(clientAlias, instanceId)
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
        /// <param name="statusCode"> The Xi StatusCode of the value. </param>
        /// <param name="timeStampUtc"> The timestamp of the value. </param>
        /// <param name="valueUInt32"> The value </param>
        public void UpdateValue(uint valueUInt32, uint statusCode, DateTime timeStampUtc)
        {
            XiValueStatusTimestamp.Value.Set(valueUInt32, ValueTypeCode, false);
            XiValueStatusTimestamp.StatusCode = statusCode;
            XiValueStatusTimestamp.TimestampUtc = timeStampUtc;
            IncrementUpdateCount();
        }

        /// <summary>
        ///     This method is called by the ClientBase when a new value has been received for
        ///     the data object from the server. It, in turn, calls the Update() method on the
        ///     DataValue property to complete the update, and then increments the update count
        ///     for the data object.
        /// </summary>
        /// <param name="statusCode"> The Xi StatusCode of the value. </param>
        /// <param name="timeStampUtc"> The timestamp of the value. </param>
        /// <param name="valueDouble"> The value </param>
        public void UpdateValue(double valueDouble, uint statusCode, DateTime timeStampUtc)
        {
            XiValueStatusTimestamp.Value.Set(valueDouble, ValueTypeCode, false);
            XiValueStatusTimestamp.StatusCode = statusCode;
            XiValueStatusTimestamp.TimestampUtc = timeStampUtc;
            IncrementUpdateCount();
        }

        /// <summary>
        ///     This method is called by the ClientBase when a new value has been received for
        ///     the data object from the server. It, in turn, calls the Update() method on the
        ///     DataValue property to complete the update, and then increments the update count
        ///     for the data object.
        /// </summary>
        /// <param name="statusCode"> The Xi StatusCode of the value. </param>
        /// <param name="timeStampUtc"> The timestamp of the value. </param>
        /// <param name="valueObject"> The value </param>
        public void UpdateValue(object? valueObject, uint statusCode, DateTime timeStampUtc)
        {
            XiValueStatusTimestamp.Value.Set(valueObject);
            XiValueStatusTimestamp.StatusCode = statusCode;
            XiValueStatusTimestamp.TimestampUtc = timeStampUtc;
            IncrementUpdateCount();
        }

        /// <summary>
        ///     This method is used to set PendingWriteValue to a value to be written.
        ///     If the data object is not writable, this method sets PendingWriteValue
        ///     to null. After preparing one or more data objects to be written, the
        ///     client application issues the data list CommitDataObjectWrites() method
        ///     to write them to the server in a single call.
        /// </summary>
        /// <param name="xiValueStatusTimestamp"> The data value to be written. </param>
        /// <returns> Returns TRUE if the data object is writable, otherwise FALSE. </returns>
        public bool PrepareForWrite(XiValueStatusTimestamp xiValueStatusTimestamp)
        {
            if (!IsWritable)
            {
                _pendingWriteXiValueStatusTimestamp = null;
                return false;
            }
            _pendingWriteXiValueStatusTimestamp = xiValueStatusTimestamp;
            return true;
        }

        public void HasWritten(uint resultCodeWrite)
        {
            _pendingWriteXiValueStatusTimestamp = null;
            _resultCodeWrite = ResultCodeWrite;
        }

        public bool PrepareForRead()
        {
            if (!IsReadable)
            {
                PreparedForRead = false;
                return false;
            }
            PreparedForRead = true;
            return true;
        }

        public void HasRead()
        {
            PreparedForRead = false;
        }

        public bool PrepareForTouch()
        {
            if (!Enabled)
            {
                PreparedForTouch = false;
                return false;
            }
            PreparedForTouch = true;
            return true;
        }

        public void HasTouched()
        {
            PreparedForTouch = false;
        }

        /// <summary>
        ///     This property contains the data value for the data object.
        /// </summary>
        public XiValueStatusTimestamp XiValueStatusTimestamp
        {
            get { return _xiValueStatusTimestamp; }
        }

        /// <summary>
        ///     This property contains the data value that is to be written to the data object.
        ///     Prior to writing a value to the server, the client application sets the
        ///     PendingWriteValue for one or more data objects using the PrepWriteValue() method,
        ///     and then issues the data list CommitDataObjectWrites() method to write them
        ///     to the server in a single call.
        /// </summary>
        public XiValueStatusTimestamp? PendingWriteXiValueStatusTimestamp
        {
            get { return _pendingWriteXiValueStatusTimestamp; }
        }

        /// <summary>
        ///     This property contains the result code associated with writing the PendingWriteXiValueStatusTimestamp.
        ///     See XiFaultCodes class for standardized result codes.
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
            get { return _pendingWriteXiValueStatusTimestamp != null; }
        }

        /// <summary>
        ///     Marked For Read From Server
        /// </summary>
        public bool PreparedForRead { get; private set; }

        /// <summary>
        ///     Marked For Read From Server
        /// </summary>
        public bool PreparedForTouch { get; private set; }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member is the private representation of the PendingWriteDataValue property.
        /// </summary>
        private XiValueStatusTimestamp? _pendingWriteXiValueStatusTimestamp;

        private uint _resultCodeWrite;

        /// <summary>
        ///     This data member is the private representation of the DataValue property.
        /// </summary>
        private XiValueStatusTimestamp _xiValueStatusTimestamp = new XiValueStatusTimestamp();

        #endregion
    }
}