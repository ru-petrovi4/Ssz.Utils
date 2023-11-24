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

using System.Text;
using System.Diagnostics;

using Xi.Contracts.Data;

namespace Xi.Common.Support.Extensions
{
	public static class ContractDataExt
	{
		/// <summary>
		/// This method creates an identifier for the event/alarm being reported. If two occurrences 
		/// of the same alarm are reported, they will have the same key.  However, different occurrences 
		/// of the same event will have different message keys.
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static string MakeMessageKey(this EventMessage msg)
		{
			StringBuilder sb = new StringBuilder();
			try
			{
				switch (msg.EventType)
				{
					case EventType.SystemEvent:
						sb.Append(msg.EventId.SourceId.ToString());
						if (msg.EventId.Condition != null)
						{
							foreach (var conditionName in msg.EventId.Condition)
							{
								sb.Append("+");
								sb.Append(conditionName);
							}
							sb.Append("+");
						}
						sb.Append(msg.OccurrenceTime.ToString("u"));
						sb.Append("+event");
						sb.Append("+");
						sb.Append(msg.TextMessage);
						break;

					case EventType.OperatorActionEvent:
						sb.Append(msg.OperatorName);
						sb.Append("+");
						sb.Append(msg.OccurrenceTime.ToString("u"));
						sb.Append("+action");
						sb.Append(msg.TextMessage);
						break;

					case EventType.SimpleAlarm:
					case EventType.EclipsedAlarm:
					case EventType.GroupedAlarm:
						sb.Append(msg.EventId.SourceId.ToString());
						if (msg.EventId.Condition != null)
						{
							foreach (var conditionName in msg.EventId.Condition)
							{
								sb.Append("+");
								sb.Append(conditionName);
							}
							sb.Append("+alarm");
						}
						else
						{
							sb.Append("alarm+");
							sb.Append(msg.TextMessage);
						}
						break;

					case EventType.Alert:
						sb.Append(msg.EventId.SourceId.ToString());
						if (msg.EventId.Condition != null)
						{
							foreach (var conditionName in msg.EventId.Condition)
							{
								sb.Append("+");
								sb.Append(conditionName);
							}
						}
						sb.Append("+");
						sb.Append(msg.OccurrenceTime.ToString("u"));
						sb.Append("+alert");
						sb.Append("+");
						sb.Append(msg.TextMessage);
						break;

					default:
						Debug.Fail("New Event Type added?");
						return null;
				}
			}
			catch { return null; }
			return sb.ToString();
		}

		public static string ToText(this EventId evtId)
		{
			StringBuilder sb = new StringBuilder("ID=\"");
			sb.Append(evtId.SourceId.ToString());
			sb.Append("\"  ");
			if (null != evtId.MultiplexedAlarmContainer)
			{
				sb.Append("MAC=[");
				sb.Append(evtId.MultiplexedAlarmContainer.ToString());
				sb.Append("]");
			}
			if (null != evtId.Condition)
			{
				foreach (var cond in evtId.Condition)
				{
					sb.Append("Cond=[");
					sb.Append(cond.ToString());
					sb.Append("]");
				}
			}
			return sb.ToString();
		}
	}
}
