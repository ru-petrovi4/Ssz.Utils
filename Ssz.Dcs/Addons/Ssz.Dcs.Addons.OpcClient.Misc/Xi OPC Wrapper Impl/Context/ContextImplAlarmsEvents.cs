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
using System.Linq;

using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.OPC.COM.API;
using Xi.Server.Base;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// This partial class defines the Alarms and Events methods of the server 
	/// implementation that override the virtual methods defined in the 
	/// Context folder of the ServerBase project.
	/// </summary>
	public partial class ContextImpl
		: ContextBase<ListRoot>
	{
		/// <summary>
		/// This method implements the server-specific behavior of the corresponding 
		/// Xi interface method.  It overrides its virtual method in the ContextBase 
		/// class of the ServerBase project.
		/// </summary>
		/// <param name="eventSourceId">
		/// The InstanceId for the event source for which alarm summaries are 
		/// being requested.
		/// </param>
		/// <returns>
		/// The summaries of the alarms that can be generated by the specified 
		/// event source.  
		/// </returns>
		public override List<AlarmSummary> OnGetAlarmSummary(InstanceId eventSourceId)
		{
			if (IsAccessibleAlarmsAndEvents == false)
				ThrowDisconnectedServerException(IOPCEventServer_ProgId);

			List<AlarmSummary> alarmSummaries = null;
			List<string> conditionNames = null;
			if ((eventSourceId.ResourceType != null) && (eventSourceId.ResourceType != InstanceIds.ResourceType_AE))
				throw FaultHelpers.Create("Invalid Resource Type in Event Source Id = " + eventSourceId.LocalId);
			else if (eventSourceId.System != null)
				throw FaultHelpers.Create("Server does not support use of System Name in Event Source Id = " + eventSourceId.LocalId);
			else
			{
				cliHRESULT HR1 = IOPCEventServer.QuerySourceConditions(eventSourceId.LocalId, out conditionNames);
				if (false == HR1.Succeeded)
				{
					ThrowOnDisconnectedServer(HR1.hResult, IOPCEventServer_ProgId);
				}
				foreach (var conditionName in conditionNames)
				{
					List<cliOPCCONDITIONSTATE> conditionStates;
					cliHRESULT HR2 = IOPCEventServer.GetConditionState(eventSourceId.LocalId, conditionName, null, out conditionStates);
					if (false == HR2.Succeeded)
					{
						ThrowOnDisconnectedServer(HR2.hResult, IOPCEventServer_ProgId);
					}
					if (conditionStates != null)
					{
						foreach (var conditionState in conditionStates)
						{
							AlarmSummary alarmSum = new AlarmSummary();

							// set the name of the alarm
							alarmSum.Name = conditionName;

							// set the alarm type
							if (conditionState.dwNumSCs > 1)
								alarmSum.EventType = EventType.EclipsedAlarm;
							else
								alarmSum.EventType = EventType.SimpleAlarm;

							// set the alarm state
							if ((conditionState.wState & (ushort)OPCAECONDITIONSTATE.OPC_CONDITION_ENABLED) == 0)
								alarmSum.State &= AlarmState.Disabled;
							if ((conditionState.wState & (ushort)OPCAECONDITIONSTATE.OPC_CONDITION_ACTIVE) != 0)
								alarmSum.State &= AlarmState.Active;
							if ((conditionState.wState & (ushort)OPCAECONDITIONSTATE.OPC_CONDITION_ACKED) == 0)
								alarmSum.State &= AlarmState.Unacked;

							// set other attributes
							alarmSum.AlarmStateStatusCode = conditionState.wQuality;
							alarmSum.MostRecentActiveCondition = conditionState.sActiveSubCondition;
							alarmSum.TimeAlarmLastActive = conditionState.dtCondLastActive;
							alarmSum.TimeAlarmLastInactive = conditionState.dtCondLastInactive;
							alarmSum.TimeMostRecentConditionActive = conditionState.dtSubCondLastActive;
							alarmSum.TimeLastAck = conditionState.dtLastAckTime;
							alarmSum.AcknowledgingOperator = conditionState.sAcknowledgerID;
							alarmSum.OperatorLastAckComment = conditionState.sComment;

							// create the condition list
							List<AlarmCondition> conditionList = new List<AlarmCondition>();
							for (int i = 0; i < (int)conditionState.dwNumSCs; i++)
							{
								AlarmCondition cond = new AlarmCondition();
								cond.TypeId = new TypeId();
								cond.TypeId.SchemaType = XiSchemaType.OPC;
								cond.TypeId.Namespace = XiOPCWrapperServer.ServerDescription.VendorName;
								cond.TypeId.LocalId = conditionState.sSCNames.ElementAt(i);
								cond.Priority = conditionState.dwSCSeverities.ElementAt(i);
								cond.TextMessage = conditionState.sSCDescriptions.ElementAt(i);
								if (conditionName == conditionState.sActiveSubCondition)
									cond.IsActive = true;
								conditionList.Add(cond);
							}
							alarmSummaries.Add(alarmSum);
						}
					}
				}
			}
			return alarmSummaries;
		}

        public void NotifyClientsAboutChanges(string userId, InstanceId tag, object value)
        {
            OperatorActions.Add(new EventMessage
                {
                    EventType = EventType.OperatorActionEvent,
                    TextMessage = string.Format("НОВ ЗНАЧЕНИЕ = {2:#.##}", userId, tag.LocalId, 
                        value),
                    OccurrenceTime = DateTime.UtcNow,
                    CategoryId = 256,
                    OperatorName = userId,
                    Priority = 1,
                    EventId = new EventId
                    {
                        SourceId = new InstanceId(tag),
                        Condition = new List<TypeId>() { new TypeId(typeof(double)) },
                        OccurrenceId = Guid.NewGuid().ToString()
                    }
                });

            foreach (var list in _XiLists.Values.OfType<EventsList>())
            {
                list.OnEvent(1234, true, true, new EventMessage[] { });
            }
        }

        public static List<EventMessage> OperatorActions = new List<EventMessage>();
	}
}
