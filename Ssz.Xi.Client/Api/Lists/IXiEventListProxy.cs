using System;
using System.Collections.Generic;
using Ssz.Xi.Client.Api.EventHandlers;
using Ssz.Xi.Client.Api.ListItems;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api.Lists
{
    public interface IXiEventListProxy : IDisposable
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

        /// <summary>
        ///     Throws or returns new IXiEventListItems (not null, but possibly zero-lenghth).
        /// </summary>
        /// <param name="filterSet"></param>
        IXiEventListItem[] PollEventChanges(FilterSet? filterSet);

        /// <summary>
        ///     This event is used to notify the client application when new events are received.
        /// </summary>
        event XiEventNotificationEventHandler EventNotificationEvent;

        /// <summary>
        ///     <para>This method is used to acknowledge one or more alarms.</para>
        /// </summary>
        /// <param name="operatorName">
        ///     The name or other identifier of the operator who is acknowledging
        ///     the alarm.
        /// </param>
        /// <param name="comment">
        ///     An optional comment submitted by the operator to accompany the
        ///     acknowledgement.
        /// </param>
        /// <param name="alarmsToAck">
        ///     The list of alarms to acknowledge.
        /// </param>
        /// <returns>
        ///     The list EventIds and result codes for the alarms whose
        ///     acknowledgement failed. Returns null if all acknowledgements
        ///     succeeded.
        /// </returns>
        List<EventIdResult>? AcknowledgeAlarms(string? operatorName, string? comment, List<EventId> alarmsToAck);
    }
}