using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Ssz.Utils.DataAccess;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Internal.ListItems;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal
{
    /// <summary>
    ///     This class is used to represent a sample set from a historical read.
    /// </summary>
    internal class XiDataJournalValueStatusTimestampSet : IDisposable,
        IXiDataJournalValueStatusTimestampSet
    {
        #region construction and destruction

        /// <summary>
        ///     Constructor for a Data Journal List Value List that will
        ///     be associated with a Data Journal List Value.
        /// </summary>
        /// <param name="owningXiDataJournalListItem">
        ///     Identifies the historical data object to which this historical value set
        ///     belongs.
        /// </param>
        /// <param name="calculationTypeId"> The CalculationTypeId associated with the value set. </param>
        public XiDataJournalValueStatusTimestampSet(XiDataJournalListItem owningXiDataJournalListItem,
            Ssz.Utils.DataAccess.TypeId calculationTypeId)
        {
            _owningXiDataJournalListItem = owningXiDataJournalListItem;
            _calculationTypeId = calculationTypeId;
            ResultCode = XiFaultCodes.E_FAIL;
            StartTime = DateTime.UtcNow;
            EndTime = DateTime.UtcNow;
        }

        /// <summary>
        ///     This method disposes of the object.  It is invoked by the client application, client base, or
        ///     the destructor of this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method disposes of the object.  It is invoked by the parameterless Dispose()
        ///     method of this object.
        /// </summary>
        /// <param name="disposing">
        ///     <para>
        ///         This parameter indicates, when TRUE, this Dispose() method was called directly or indirectly by a user's
        ///         code. When FALSE, this method was called by the runtime from inside the finalizer.
        ///     </para>
        ///     <para>
        ///         When called by user code, references within the class should be valid and should be disposed of properly.
        ///         When called by the finalizer, references within the class are not guaranteed to be valid and attempts to
        ///         dispose of them should not be made.
        ///     </para>
        /// </param>
        /// <returns> Returns TRUE to indicate that the object has been disposed. </returns>
        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _xiValueStatusTimestampsList.Clear();
            }

            _disposed = true;
        }

        /// <summary>
        ///     The standard destructor invoked by the .NET garbage collector during Finalize.
        /// </summary>
        ~XiDataJournalValueStatusTimestampSet()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     The LocalId portion of the CalculationTypeId. Set to 0 if the LocalId is not a uint.
        /// </summary>
        public static uint GetCalculationTypeLocalId(Ssz.Utils.DataAccess.TypeId calculationTypeId)
        {
            return Convert.ToUInt32(calculationTypeId.LocalId);
        }

        /// <summary>
        ///     The LocalId portion of the CalculationTypeId. Set to 0 if the LocalId is not a uint.
        /// </summary>
        public static Ssz.Utils.DataAccess.TypeId GetCalculationTypeId(uint calculationTypeLocalId)
        {
            return new Ssz.Utils.DataAccess.TypeId("", "", calculationTypeLocalId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     This method gets the non-typed enumerator for values in the historical data object value set.
        /// </summary>
        /// <returns> Returns the enumerator for the list of objects in the historical data object value set. </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _xiValueStatusTimestampsList.GetEnumerator();
        }

        /// <summary>
        ///     This method returns the typed enumerator for the list of values in the historical data object value set.
        ///     This enumerator allows the var type to be used in foreach calls.
        /// </summary>
        /// <returns> Returns the enumerator for list of values in the historical data object value set. </returns>
        IEnumerator<ValueStatusTimestamp> IEnumerable<ValueStatusTimestamp>.GetEnumerator()
        {
            return _xiValueStatusTimestampsList.GetEnumerator();
        }

        /// <summary>
        ///     This property contains the interface for the historical data object to
        ///     which this historical value set belongs.
        /// </summary>
        public XiDataJournalListItem OwningXiDataJournalListItem
        {
            get { return _owningXiDataJournalListItem; }
        }

        /// <summary>
        ///     The type of calculation used to create the value set as defined by the
        ///     StandardMib.DataJournalOptions.MathLibrary of the server and by
        ///     Xi.Contracts.Constants.JournalDataSampleTypes. The historical data object
        ///     for which this value set is defined may not have two value sets with the
        ///     same CalculationTypeId.
        /// </summary>
        public Ssz.Utils.DataAccess.TypeId CalculationTypeId
        {
            get { return _calculationTypeId; }
        }

        /// <summary>
        ///     This property contains the Result Code associated with reading this value
        ///     set from the server. See XiFaultCodes class for standardized result codes.
        /// </summary>
        public uint ResultCode { get; private set; }

        /// <summary>
        ///     This property defines the starting time for this list historical values.
        ///     Values in the value set will be between the StartTime and EndTime. The
        ///     read method used to access the values specifies whether or not values
        ///     with the starting or ending times are to be included in this value set.
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        ///     This property defines the ending time for this list historical values.
        ///     Values in the value set will be between the StartTime and EndTime. The
        ///     read method used to access the values specifies whether or not values
        ///     with the starting or ending times are to be included in this value set.
        /// </summary>
        public DateTime EndTime { get; private set; }

        /// <summary>
        ///     This property contains the number of values in this value set.
        /// </summary>
        public int Count
        {
            get { return _xiValueStatusTimestampsList.Count; }
        }

        /// <summary>
        ///     The property defines an object that the client application can use to
        ///     associate this value set with an object of its choice.
        /// </summary>
        public object? Tag { get; set; }

        #endregion

        #region internal functions

        /// <summary>
        ///     This method clears the value set
        /// </summary>
        internal void Reset()
        {
            ResultCode = XiFaultCodes.E_FAIL;
            StartTime = DateTime.UtcNow;
            EndTime = DateTime.UtcNow;
            _xiValueStatusTimestampsList.Clear();
        }

        /// <summary>
        ///     This method updates the values of the value set.
        /// </summary>
        /// <param name="journalDataValues"> The new values used to update the value set. </param>
        internal void UpdateValueSet(JournalDataValues journalDataValues)
        {
            if (journalDataValues.HistoricalValues is null) return;

            ResultCode = journalDataValues.ResultCode;
            StartTime = journalDataValues.StartTime;
            EndTime = journalDataValues.EndTime;
            _xiValueStatusTimestampsList.Clear();

            if (journalDataValues.HistoricalValues.DoubleStatusCodes is not null &&
                journalDataValues.HistoricalValues.DoubleValues is not null &&
                journalDataValues.HistoricalValues.DoubleTimeStamps is not null)
            {
                for (int idx = 0; idx < journalDataValues.HistoricalValues.DoubleStatusCodes.Length; idx++)
                {
                    var xiValueStatusTimestamp = new ValueStatusTimestamp();
                    xiValueStatusTimestamp.Value.Set(journalDataValues.HistoricalValues.DoubleValues[idx],
                        _owningXiDataJournalListItem.ValueTypeCode, false);
                    xiValueStatusTimestamp.ValueStatusCode = journalDataValues.HistoricalValues.DoubleStatusCodes[idx];
                    xiValueStatusTimestamp.TimestampUtc = journalDataValues.HistoricalValues.DoubleTimeStamps[idx];

                    _xiValueStatusTimestampsList.Add(xiValueStatusTimestamp);
                }
            }
            if (journalDataValues.HistoricalValues.UintStatusCodes is not null &&
                journalDataValues.HistoricalValues.UintValues is not null &&
                journalDataValues.HistoricalValues.UintTimeStamps is not null)
            {
                for (int idx = 0; idx < journalDataValues.HistoricalValues.UintStatusCodes.Length; idx++)
                {
                    var xiValueStatusTimestamp = new ValueStatusTimestamp();
                    xiValueStatusTimestamp.Value.Set(journalDataValues.HistoricalValues.UintValues[idx],
                        _owningXiDataJournalListItem.ValueTypeCode, false);
                    xiValueStatusTimestamp.ValueStatusCode = journalDataValues.HistoricalValues.UintStatusCodes[idx];
                    xiValueStatusTimestamp.TimestampUtc = journalDataValues.HistoricalValues.UintTimeStamps[idx];

                    _xiValueStatusTimestampsList.Add(xiValueStatusTimestamp);
                }
            }
            if (journalDataValues.HistoricalValues.ObjectStatusCodes is not null &&
                journalDataValues.HistoricalValues.ObjectValues is not null &&
                journalDataValues.HistoricalValues.ObjectTimeStamps is not null)
            {
                for (int idx = 0; idx < journalDataValues.HistoricalValues.ObjectStatusCodes.Length; idx++)
                {
                    var xiValueStatusTimestamp = new ValueStatusTimestamp();
                    xiValueStatusTimestamp.Value.Set(journalDataValues.HistoricalValues.ObjectValues[idx]);
                    xiValueStatusTimestamp.ValueStatusCode = journalDataValues.HistoricalValues.ObjectStatusCodes[idx];
                    xiValueStatusTimestamp.TimestampUtc = journalDataValues.HistoricalValues.ObjectTimeStamps[idx];

                    _xiValueStatusTimestampsList.Add(xiValueStatusTimestamp);
                }
            }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     The private representation of the OwningHistoricalDataObject interface property and the
        ///     _owningXiDataJournalListItem public property.
        /// </summary>
        private XiDataJournalListItem _owningXiDataJournalListItem;

        /// <summary>
        ///     The private representation of the CalculationTypeId and CalculationTypeLocalId interface properties.
        /// </summary>
        private Ssz.Utils.DataAccess.TypeId _calculationTypeId;

        /// <summary>
        ///     This member indicates, when TRUE, that the object has been disposed by the Dispose(bool isDisposing) method.
        /// </summary>
        private bool _disposed;

        /// <summary>
        ///     The private representation of the _xiValueStatusTimestampsList interface property.
        /// </summary>
        private readonly List<ValueStatusTimestamp> _xiValueStatusTimestampsList = new List<ValueStatusTimestamp>();

        #endregion
    }
}