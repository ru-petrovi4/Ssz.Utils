/**********************************************************************
 * Copyright © 2009, 2010, 2011, 2012 OPC Foundation, Inc. 
 *
 * The source code and all binaries built with the OPC .NET 3.0 source
 * code are subject to the terms of the Express Interface Public
 * License (Xi-PL).  See http://www.opcfoundation.org/License/Xi-PL/
 *
 * The source code may be distributed from an OPC member company in
 * its original or modified form to its customers and to any others who
 * have software that needs to interoperate with the OPC member's OPC
* .NET 3.0 products. No other redistribution is permitted.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *********************************************************************/

using System;
using System.Collections.Generic;

using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class defines the methods to be overridden by the server implementation 
	/// to support the Alarms and Events methods of the IResourceManagement interface.
	/// </summary>
	public abstract partial class ContextBase<TList>
		where TList : ListRoot
	{
		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="eventSourceId">
		/// The InstanceId for the event source for which alarm summaries are 
		/// being requested.
		/// </param>
		/// <returns>
		/// The summaries of the alarms that can be generated by the specified 
		/// event source.  
		/// </returns>
		public abstract List<AlarmSummary> OnGetAlarmSummary(InstanceId eventSourceId);

		/// <summary>
		/// This method is to be overridden by the implementation class.
		/// </summary>
		/// <param name="enableFlag">
		/// This flag indicates, when TRUE, that alarms are to be enabled, and when FALSE, that they 
		/// are to be disabled.
		/// </param>
		/// <param name="areaFlag">
		/// This flag indicates, when TRUE, that the eventContainerIds parameter contains a list of 
		/// InstanceIds for areas, and when FALSE, that it contains a list of InstanceIds for event sources.
		/// </param>
		/// <param name="eventContainerId">
		/// The InstanceId for the area or the event source for which alarms are to be enabled or disabled.
		/// </param>
		/// <returns>Null if all requested enable/disable operations succeeded. Otherwise, the list of result codes. The size and 
		/// order of this list matches that of the eventContainerIds.  Standard result code values are defined by 
		/// the Xi.Contracts.Constants.XiFaultCodes class. There is one result code for each eventContainerId.</returns>
		public virtual List<UInt32> OnEnableAlarms(bool enableFlag, bool areaFlag, List<InstanceId> eventContainerIds)
		{
			// TODO:  Implement the implementation class override for this method if supported, 
			//        and also set the corresponding bit in StandardMib.MethodsSupported.
			throw FaultHelpers.Create(XiFaultCodes.E_NOTIMPL, "IResourceManagement.EnableAlarms");
		}

		/// <summary>
		/// This method returns the enable state for a specified area or event source.
		/// It throws a fault if the requested operation cannot be performed successfully.
		/// </summary>
		/// <param name="contextId">The context identifier.</param>
		/// <param name="areaFlag">
		/// This flag indicates, when TRUE, that the eventContainerIds parameter contains a list of 
		/// InstanceIds for areas, and when FALSE, that it contains a list of InstanceIds for event sources.</param>
		/// <param name="eventContainerId">
		/// The InstanceId for the area or the event source for which alarms are to be enabled or disabled.
		/// </param>
		/// <returns>An object with the enabled state and result code for each requested InstanceId.
		/// </returns>
		public virtual List<AlarmEnabledState> OnGetAlarmsEnabledState(bool areaFlag, List<InstanceId> eventContainerIds)
		{
			// TODO:  Implement the implementation class override for this method if supported, 
			//        and also set the corresponding bit in StandardMib.MethodsSupported.
			throw FaultHelpers.Create(XiFaultCodes.E_NOTIMPL, "IResourceManagement.GetAlarmsEnabledState");
		}

	}
}