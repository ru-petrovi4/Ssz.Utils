using System;
using System.Collections.Generic;
using System.Globalization;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Api.ListItems;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.ListItems
{
    /// <summary>
    ///     This class defines a historical data object.  Each historical data
    ///     object contains a list of historical values and a list of
    ///     historical data properties.  IEnumerable interfaces are provided to
    ///     allow easy iteration of the historical values.
    ///     Each historical data object may have multiple lists of values obtained
    ///     via different calculations.
    /// </summary>
    internal class XiDataJournalListItem : XiDataAndDataJournalListItemBase, IXiDataJournalListItem
    {
        #region construction and destruction        

        /// <summary>
        ///     Constructs a new instance of the <see cref="XiDataJournalListItem" /> class.
        /// </summary>
        /// <param name="clientAlias"> The client alias assigned to this historical data object. </param>
        /// <param name="instanceId"> The InstanceId that identifies this historical data object. </param>
        public XiDataJournalListItem(uint clientAlias, InstanceId instanceId)
            : base(clientAlias, instanceId)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
            {
                // Release and Dispose managed resources.
                foreach (var kvp in _xiValueStatusTimestampSetsDictionary)
                {
                    kvp.Value.Dispose();
                }
                _xiValueStatusTimestampSetsDictionary.Clear();

                if (_journalDataPropertyValueArray != null)
                {
                    foreach (JournalDataPropertyValue propValue in _journalDataPropertyValueArray)
                    {
                        if (propValue.PropertyValues != null)
                        {
                            propValue.PropertyValues.Clear();
                            propValue.PropertyValues = null;
                        }
                    }
                }
            }
            // Release unmanaged resources.
            // Set large fields to null.            
            _journalDataChangedValues = null;
            _journalDataPropertyValueArray = null;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method is used to create a new list of historical values for this historical
        ///     data object.  The values for this list are populated using historical read
        ///     methods defined by the IXIDataJournalList interface.
        /// </summary>
        /// <param name="calculationLocalId">
        ///     The type of calculation to be used to create the list of HistoricalValues for this
        ///     data object, as defined by the StandardMib.DataJournalOptions.MathLibrary of the server. This data object may not
        ///     have two value sets with the same CalculationTypeId.
        /// </param>
        /// <returns> Returns the newly created list of historical values. </returns>
        public IXiDataJournalValueStatusTimestampSet GetExistingOrNewValueStatusTimestampSet(
            string? calculationLocalId = null)
        {
            if (String.IsNullOrEmpty(calculationLocalId))
                calculationLocalId = JournalDataSampleTypes.RawDataSamples.ToString(CultureInfo.InvariantCulture);

            XiDataJournalValueStatusTimestampSet? existingValueStatusTimestampSet;
            if (_xiValueStatusTimestampSetsDictionary.TryGetValue(calculationLocalId, out existingValueStatusTimestampSet))
                return existingValueStatusTimestampSet;

            var newValueStatusTimestampSet = new XiDataJournalValueStatusTimestampSet(this,
                new Ssz.Utils.DataAccess.TypeId("", "", calculationLocalId));
            _xiValueStatusTimestampSetsDictionary[calculationLocalId] = newValueStatusTimestampSet;
            return newValueStatusTimestampSet;
        }

        /// <summary>
        ///     This method removes a value set from the historical data object
        /// </summary>
        /// <param name="valueStatusTimestampSet"> The value set to remove </param>
        public void Remove(IXiDataJournalValueStatusTimestampSet valueStatusTimestampSet)
        {
            if (valueStatusTimestampSet.CalculationTypeId.LocalId == null) return;
            _xiValueStatusTimestampSetsDictionary.Remove(valueStatusTimestampSet.CalculationTypeId.LocalId);
        }

        /// <summary>
        ///     This method sets the value of the PropertyValues property.
        /// </summary>
        /// <param name="propertyValues"> The new property values to set. </param>
        public void SetPropertyValues(JournalDataPropertyValue[]? propertyValues)
        {
            if (null != _journalDataPropertyValueArray)
            {
                foreach (JournalDataPropertyValue propValue in _journalDataPropertyValueArray)
                {
                    if (null != propValue.PropertyValues) propValue.PropertyValues.Clear();
                    propValue.PropertyValues = null;
                }
                _journalDataPropertyValueArray = null;
            }
            _journalDataPropertyValueArray = propertyValues;
        }

        /// <summary>
        ///     This method is used to update the historical values for this data object
        /// </summary>
        /// <param name="journalDataValues"> The new list of historical values. </param>
        public void Update(JournalDataValues journalDataValues)
        {
            var xiValueStatusTimestampSet =
                (XiDataJournalValueStatusTimestampSet)
                    GetExistingOrNewValueStatusTimestampSet(journalDataValues.Calculation?.LocalId);
            xiValueStatusTimestampSet.UpdateValueSet(journalDataValues);
        }

        /// <summary>
        ///     This method sets the value of the _journalDataChangedValues private data member.
        /// </summary>
        /// <param name="dataChanges"> The new changed data values to set. </param>
        public void SetDataChanges(JournalDataChangedValues dataChanges)
        {
            if (null != _journalDataChangedValues)
            {
                if (null != _journalDataChangedValues.ModificationAttributes)
                    _journalDataChangedValues.ModificationAttributes.Clear();
                _journalDataChangedValues.ModificationAttributes = null;
                _journalDataChangedValues = null;
            }
            _journalDataChangedValues = dataChanges;
        }

        /// <summary>
        ///     This property contains the set of historical property values for the historical data object.
        ///     Property values are obtained using the ReadJournalDataProperties() method defined by the
        ///     IXIDataJournalList interface.
        /// </summary>
        public JournalDataPropertyValue[]? PropertyValues
        {
            get { return _journalDataPropertyValueArray; }
        }

        /// <summary>
        ///     This property indicates the number of historical value sets for the historical data object.
        /// </summary>
        public int Count
        {
            get { return _xiValueStatusTimestampSetsDictionary.Count; }
        }

        /// <summary>
        ///     This property contains the changed historical values of the historical data object.
        ///     It is populated by the ReadJournalDataChanges() method.
        /// </summary>
        public JournalDataChangedValues? DataChanges
        {
            get { return _journalDataChangedValues; }
        }

        public IEnumerable<IXiDataJournalValueStatusTimestampSet> ValueStatusTimestampSets
        {
            get { return _xiValueStatusTimestampSetsDictionary.Values; }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member contains the historical value sets for this historical data object.
        /// </summary>
        private readonly Dictionary<string, XiDataJournalValueStatusTimestampSet> _xiValueStatusTimestampSetsDictionary =
            new Dictionary<string, XiDataJournalValueStatusTimestampSet>();

        /// <summary>
        ///     This data member is the private representation of the DataChanges public property.
        /// </summary>
        private JournalDataChangedValues? _journalDataChangedValues;

        /// <summary>
        ///     This data member is the private representation of the PropertyValues interface property.
        /// </summary>
        private JournalDataPropertyValue[]? _journalDataPropertyValueArray;

        #endregion
    }
}