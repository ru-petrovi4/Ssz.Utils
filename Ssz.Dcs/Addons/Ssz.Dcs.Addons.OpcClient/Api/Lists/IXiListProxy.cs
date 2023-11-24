using System;
using System.Collections.Generic;
using Ssz.Xi.Client.Api.ListItems;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api.Lists
{
    public interface IXiListProxy<out TXiListItem> : IDisposable
        where TXiListItem : IXiListItem
    {
        bool Disposed { get; }

        /// <summary>
        ///     This property is provided for the Xi Client application to associate this list
        ///     with an object of its choosing.
        /// </summary>
        object? ClientTag { get; set; }

        bool EnableListUpdating(bool enableUpdating);

        bool Readable { get; set; }
        bool Writeable { get; set; }
        bool Callbackable { get; set; }
        bool Pollable { get; set; }

        TXiListItem PrepareAddItem(InstanceId instanceId);

        /// <summary>
        ///     Returns list Items than was not added or null.
        /// </summary>
        /// <returns> List Items than was not added or null. </returns>
        IEnumerable<TXiListItem>? CommitAddItems();

        /// <summary>
        /// </summary>
        /// <returns> List Items than was not added or null. </returns>
        IEnumerable<TXiListItem>? CommitRemoveItems();

        IEnumerable<TXiListItem> ListItems { get; }
    }
}