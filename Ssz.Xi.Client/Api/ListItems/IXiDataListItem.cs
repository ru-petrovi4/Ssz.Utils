namespace Ssz.Xi.Client.Api.ListItems
{
    public interface IXiDataListItem : IXiListItem
    {
        XiValueStatusTimestamp XiValueStatusTimestamp { get; }

        /// <summary>
        ///     Marked For Touch From Server
        /// </summary>
        bool PreparedForTouch { get; }

        /// <summary>
        ///     Marked For Write to Server
        /// </summary>
        bool PreparedForWrite { get; }

        /// <summary>
        ///     Marked For Read From Server
        /// </summary>
        bool PreparedForRead { get; }

        bool PrepareForTouch();
        bool PrepareForWrite(XiValueStatusTimestamp xiValueStatusTimestamp);
        bool PrepareForRead();
    }
}