using System.Collections.Generic;
using Ssz.Xi.Client.Internal.ListItems;

namespace Ssz.Xi.Client.Internal
{
    /// <summary>
    /// </summary>
    internal class XiAeSorter : IComparer<XiEventListItem>
    {
        #region public functions

        /// <summary>
        ///     This method is a placeholder for comparing two XiEventListItem objects.  The implementer
        ///     should complete this method if desired.
        /// </summary>
        /// <param name="eventValueA"> The first XiEventListItem to compare. </param>
        /// <param name="eventValueB"> The second XiEventListItem to compare. </param>
        /// <returns>
        ///     Returns 0 if eventValueA equals eventValueB, 1 if eventValueB is greater than eventValueA, and -1 if
        ///     eventValueB is less than eventValueA.
        /// </returns>
        public static int DoCompare(XiEventListItem? eventValueA, XiEventListItem? eventValueB)
        {
            if (null == eventValueA)
            {
                if (null == eventValueB) return 0;
                return 1;
            }
            return 0;
            // TODO - Complete this method to sort the list view of the events.
            // See example below.
            //if (null == eventValueB) return -1;
            //if (eventValueA.EventListElement.EventMessage.Priority > eventValueB.EventListElement.EventMessage.Priority) return -1;
            //if (eventValueA.EventListElement.EventMessage.Priority < eventValueB.EventListElement.EventMessage.Priority) return 1;
            //int stateA = (null != eventValueA.EventListElement.EventMessage.AlarmData)
            //    ? (int)eventValueA.EventListElement.EventMessage.AlarmData.AlarmState : 0;
            //int stateB = (null != eventValueB.EventListElement.EventMessage.AlarmData)
            //    ? (int)eventValueB.EventListElement.EventMessage.AlarmData.AlarmState : 0;
            //if (stateA < stateB) return 1;
            //if (stateA > stateB) return -1;
            //return DateTime.Compare(eventValueB.EventListElement.EventMessage.OccurrenceTime, eventValueA.EventListElement.EventMessage.OccurrenceTime);
        }

        int IComparer<XiEventListItem>.Compare(XiEventListItem? eventValueA,
            XiEventListItem? eventValueB)
        {
            return DoCompare(eventValueA, eventValueB);
        }

        #endregion
    }
}