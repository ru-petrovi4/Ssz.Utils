namespace Ssz.Xi.Client.Api.ListItems
{
    /// <summary>
    /// 
    /// </summary>
    public interface IXiDataJournalListItem : IXiListItem
    {
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
        IXiDataJournalValueStatusTimestampSet GetExistingOrNewValueStatusTimestampSet(string? calculationLocalId = null);

        /// <summary>
        ///     This method removes a value set from the historical data object
        /// </summary>
        /// <param name="valueStatusTimestampSet"> The value set to remove </param>
        void Remove(IXiDataJournalValueStatusTimestampSet valueStatusTimestampSet);
    }
}