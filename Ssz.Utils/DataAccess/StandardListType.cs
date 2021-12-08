namespace Ssz.Utils.DataAccess
{
    /// <summary>
    ///     This enumeration specifies the standard types of lists.
    ///     The enumerated values between 0 and 4095 inclusive are reserved
    ///     for standard types.
    /// </summary>
    public enum StandardListType
    {
        /// <summary>
        ///     The type of list that contains data objects.
        /// </summary>
        ElementValueList = 1,

        /// <summary>
        ///     The type of list that contains historical data objects.
        /// </summary>
        ElementValuesJournalList = 2,

        /// <summary>
        ///     The type of list that contains alarms and events.
        /// </summary>
        EventList = 3
    }
}