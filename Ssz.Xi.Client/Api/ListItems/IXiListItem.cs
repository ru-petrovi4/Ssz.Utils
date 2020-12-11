using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api.ListItems
{
    /// <summary>
    /// 
    /// </summary>
    public interface IXiListItem
    {
        /// <summary>
        /// 
        /// </summary>
        uint ResultCode { get; }
        /// <summary>
        ///     For use by client code.
        /// </summary>        
        object? Obj { get; set; }
        InstanceId InstanceId { get; }
        bool IsReadable { get; }
        bool IsWritable { get; }
        bool IsInClientList { get; }

        /// <summary>
        ///     In Server List
        /// </summary>
        bool IsInServerList { get; }

        /// <summary>
        ///     Marked For Add To Server
        /// </summary>
        bool PreparedForAdd { get; }

        /// <summary>
        ///     Marked For Remove From Server
        /// </summary>
        bool PreparedForRemove { get; }

        void PrepareForRemove();

        /// <summary>
        ///     This property provides the number of times this Xi Value
        ///     has been updated with a new value.
        /// </summary>
        uint UpdateCount { get; }
    }
}