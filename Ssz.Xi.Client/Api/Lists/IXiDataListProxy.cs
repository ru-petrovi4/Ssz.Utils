using System.Collections.Generic;
using Ssz.Xi.Client.Api.EventHandlers;
using Ssz.Xi.Client.Api.ListItems;

namespace Ssz.Xi.Client.Api.Lists
{
    public interface IXiDataListProxy : IXiListProxy<IXiDataListItem>
    {
        /// <summary>
        ///     <para>
        ///         This method is used to enable or disable updating of individual elements of a list. If the
        ///         dataObjectsToEnableOrDisable parameter is null, then all elements of the list are enabled/disabled. This call
        ///         does not change the enabled state of the list itself.
        ///     </para>
        ///     <para>
        ///         When an element of the list is disabled, the server excludes it from participating in callbacks and polls.
        ///         However, at the option of the server, the server may continue updating its cache for the element.
        ///     </para>
        /// </summary>
        /// <param name="enableUpdating">
        ///     Indicates, when TRUE, that updating of the list is to be enabled, and when FALSE, that
        ///     updating of the list is to be disabled.
        /// </param>
        /// <param name="dataObjectsToEnableOrDisable"> The list of data objects to be enabled or disabled. </param>
        void EnableListElementUpdating(bool enableUpdating,
            IEnumerable<IXiDataListItem>? dataObjectsToEnableOrDisable);

        uint TouchList();

        /// <summary>
        ///     <para> Returns List Items whose touch failed. </para>
        /// </summary>
        /// <returns> List Items whose touch failed. </returns>
        IEnumerable<IXiDataListItem>? CommitTouchDataListItems();

        /// <summary>
        ///     <para> Returns List Items whose read failed. </para>
        /// </summary>
        /// <returns> List Items whose read failed. </returns>
        IEnumerable<IXiDataListItem>? CommitReadDataListItems();

        /// <summary>
        ///     <para> Returns List Items whose write failed. </para>
        /// </summary>
        /// <returns> List Items whose write failed. </returns>
        IEnumerable<IXiDataListItem>? CommitWriteDataListItems();

        /// <summary>
        ///     <para> Throws or returns changed IXiDataListItems (not null, but possibly zero-lenghth). </para>
        /// </summary>
        IXiDataListItem[] PollDataChanges();
        
        event XiInformationReportEventHandler InformationReport;
    }
}