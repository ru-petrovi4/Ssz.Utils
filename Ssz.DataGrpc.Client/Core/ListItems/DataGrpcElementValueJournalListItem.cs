using System;
using System.Collections.Generic;
using System.Globalization;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Common;
using Ssz.DataGrpc.Server;

namespace Ssz.DataGrpc.Client.Core.ListItems
{
    /// <summary>
    ///     This class defines a historical data object.  Each historical data
    ///     object contains a list of historical values and a list of
    ///     historical data properties.  IEnumerable interfaces are provided to
    ///     allow easy iteration of the historical values.
    ///     Each historical data object may have multiple lists of values obtained
    ///     via different calculations.
    /// </summary>
    public class DataGrpcElementValueJournalListItem : DataGrpcElementListItemBase
    {
        #region construction and destruction        

        /// <summary>
        ///     Constructs a new instance of the <see cref="DataGrpcElementValueJournalListItem" /> class.
        /// </summary>        
        /// <param name="elementId"> The InstanceId that identifies this historical data object. </param>
        public DataGrpcElementValueJournalListItem(string elementId)
            : base(elementId)
        {
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (Disposed) return;
        //    if (disposing)
        //    {
        //        // Release and Dispose managed resources.
        //        foreach (var kvp in _dataGrpcValueStatusTimestampSetsDictionary)
        //        {
        //            kvp.Value.Dispose();
        //        }
        //        _dataGrpcValueStatusTimestampSetsDictionary.Clear();

        //        if (_journalDataPropertyValueArray != null)
        //        {
        //            foreach (JournalDataPropertyValue propValue in _journalDataPropertyValueArray)
        //            {
        //                if (propValue.PropertyValues != null)
        //                {
        //                    propValue.PropertyValues.Clear();
        //                    propValue.PropertyValues = null;
        //                }
        //            }
        //        }
        //    }
        //    // Release unmanaged resources.
        //    // Set large fields to null.            
        //    _journalDataChangedValues = null;
        //    _journalDataPropertyValueArray = null;

        //    base.Dispose(disposing);
        //}

        #endregion

        //#region public functions

        ///// <summary>
        /////     This method is used to create a new list of historical values for this historical
        /////     data object.  The values for this list are populated using historical read
        /////     methods defined by the IXIElementValueJournalList interface.
        ///// </summary>
        ///// <param name="calculationLocalId">
        /////     The type of calculation to be used to create the list of HistoricalValues for this
        /////     data object, as defined by the StandardMib.DataJournalOptions.MathLibrary of the server. This data object may not
        /////     have two value sets with the same CalculationTypeId.
        ///// </param>
        ///// <returns> Returns the newly created list of historical values. </returns>
        //public IDataGrpcJournalValueStatusTimestampSet GetExistingOrNewValueStatusTimestampSet(
        //    string? calculationLocalId = null)
        //{
        //    if (String.IsNullOrEmpty(calculationLocalId))
        //        calculationLocalId = JournalDataSampleTypes.RawDataSamples.ToString(CultureInfo.InvariantCulture);

        //    DataGrpcJournalValueStatusTimestampSet? existingValueStatusTimestampSet;
        //    if (_dataGrpcValueStatusTimestampSetsDictionary.TryGetValue(calculationLocalId, out existingValueStatusTimestampSet))
        //        return existingValueStatusTimestampSet;

        //    var newValueStatusTimestampSet = new DataGrpcJournalValueStatusTimestampSet(this,
        //        new TypeId(null, null, calculationLocalId));
        //    _dataGrpcValueStatusTimestampSetsDictionary[calculationLocalId] = newValueStatusTimestampSet;
        //    return newValueStatusTimestampSet;
        //}

        ///// <summary>
        /////     This method removes a value set from the historical data object
        ///// </summary>
        ///// <param name="vstSet"> The value set to remove </param>
        //public void Remove(IDataGrpcJournalValueStatusTimestampSet vstSet)
        //{
        //    if (vstSet.CalculationTypeId.LocalId == null) return;
        //    _dataGrpcValueStatusTimestampSetsDictionary.Remove(vstSet.CalculationTypeId.LocalId);
        //}

        ///// <summary>
        /////     This method sets the value of the PropertyValues property.
        ///// </summary>
        ///// <param name="propertyValues"> The new property values to set. </param>
        //public void SetPropertyValues(JournalDataPropertyValue[]? propertyValues)
        //{
        //    if (null != _journalDataPropertyValueArray)
        //    {
        //        foreach (JournalDataPropertyValue propValue in _journalDataPropertyValueArray)
        //        {
        //            if (null != propValue.PropertyValues) propValue.PropertyValues.Clear();
        //            propValue.PropertyValues = null;
        //        }
        //        _journalDataPropertyValueArray = null;
        //    }
        //    _journalDataPropertyValueArray = propertyValues;
        //}

        ///// <summary>
        /////     This method is used to update the historical values for this data object
        ///// </summary>
        ///// <param name="journalDataValues"> The new list of historical values. </param>
        //public void Update(JournalDataValues journalDataValues)
        //{
        //    var dataGrpcValueStatusTimestampSet =
        //        (DataGrpcJournalValueStatusTimestampSet)
        //            GetExistingOrNewValueStatusTimestampSet(journalDataValues.Calculation?.LocalId);
        //    dataGrpcValueStatusTimestampSet.UpdateValueSet(journalDataValues);
        //}

        ///// <summary>
        /////     This method sets the value of the _journalDataChangedValues private data member.
        ///// </summary>
        ///// <param name="dataChanges"> The new changed data values to set. </param>
        //public void SetDataChanges(JournalDataChangedValues dataChanges)
        //{
        //    if (null != _journalDataChangedValues)
        //    {
        //        if (null != _journalDataChangedValues.ModificationAttributes)
        //            _journalDataChangedValues.ModificationAttributes.Clear();
        //        _journalDataChangedValues.ModificationAttributes = null;
        //        _journalDataChangedValues = null;
        //    }
        //    _journalDataChangedValues = dataChanges;
        //}

        ///// <summary>
        /////     This property contains the set of historical property values for the historical data object.
        /////     Property values are obtained using the ReadJournalDataProperties() method defined by the
        /////     IXIElementValueJournalList interface.
        ///// </summary>
        //public JournalDataPropertyValue[]? PropertyValues
        //{
        //    get { return _journalDataPropertyValueArray; }
        //}

        ///// <summary>
        /////     This property indicates the number of historical value sets for the historical data object.
        ///// </summary>
        //public int Count
        //{
        //    get { return _dataGrpcValueStatusTimestampSetsDictionary.Count; }
        //}

        ///// <summary>
        /////     This property contains the changed historical values of the historical data object.
        /////     It is populated by the ReadJournalDataChanges() method.
        ///// </summary>
        //public JournalDataChangedValues? DataChanges
        //{
        //    get { return _journalDataChangedValues; }
        //}

        //public IEnumerable<IDataGrpcJournalValueStatusTimestampSet> ValueStatusTimestampSets
        //{
        //    get { return _dataGrpcValueStatusTimestampSetsDictionary.Values; }
        //}

        //#endregion

        //#region private fields

        ///// <summary>
        /////     This data member contains the historical value sets for this historical data object.
        ///// </summary>
        //private readonly Dictionary<string, DataGrpcJournalValueStatusTimestampSet> _dataGrpcValueStatusTimestampSetsDictionary =
        //    new Dictionary<string, DataGrpcJournalValueStatusTimestampSet>();

        ///// <summary>
        /////     This data member is the private representation of the DataChanges public property.
        ///// </summary>
        //private JournalDataChangedValues? _journalDataChangedValues;

        ///// <summary>
        /////     This data member is the private representation of the PropertyValues interface property.
        ///// </summary>
        //private JournalDataPropertyValue[]? _journalDataPropertyValueArray;

        //#endregion
    }
}