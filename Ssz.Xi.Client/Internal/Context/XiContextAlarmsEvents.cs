﻿using System;
using System.Collections.Generic;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Context
{
    /// <summary>
    ///     This partial class defines the Alarms and Events related aspects of the XiContext class.
    /// </summary>
    internal partial class XiContext
    {
        #region public functions

        /// <summary>
        ///     <para>
        ///         This method is used to request summary information for the alarms that can be generated for a given event
        ///         source.
        ///     </para>
        /// </summary>
        /// <param name="eventSourceId"> The InstanceId for the event source for which alarm summaries are being requested. </param>
        /// <returns> The summaries of the alarms that can be generated by the specified event source. </returns>
        public IEnumerable<AlarmSummary>? GetAlarmSummary(InstanceId eventSourceId)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement == null) throw new InvalidOperationException();
            List<AlarmSummary>? alarmSummaries = null;
            try
            {
                alarmSummaries = _iResourceManagement.GetAlarmSummary(ContextId, eventSourceId);
                SetResourceManagementLastCallUtc();
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
            return alarmSummaries;
        }

        #endregion
    }
}