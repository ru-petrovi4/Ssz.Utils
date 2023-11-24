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

using Xi.Contracts.Data;
using Xi.Contracts.Constants;

namespace Xi.Common.Support
{
	public class FilterComparisonValueConversion
	{
		/// <summary>
		/// This method converts a string version of an operand's comparison value to the 
		/// appropriate object type.
		/// </summary>
		/// <param name="filterCriterion">The FilterCriterion that contain the operand name 
		/// and comparison value.</param>
		/// <returns>An error message if an error occurred. Otherwise null.</returns>
		public static string ConvertStringComparisonValueToObject(FilterCriterion filterCriterion)
		{
			string errorMessage = null;
			// Convert Comparison values from text to the appropriate object and value
			if (   (filterCriterion.OperandName == FilterOperandNames.EventPriority)
				|| (filterCriterion.OperandName == FilterOperandNames.MaxTimeInterval)
			   )
			{
				try
				{
					filterCriterion.ComparisonValue = Convert.ToUInt32((string)filterCriterion.ComparisonValue);
				}
				catch
				{
					errorMessage = "Comparison Value must be an unsigned integer.";
				}
			}
			else if (   (filterCriterion.OperandName == FilterOperandNames.MaxValue)
					 || (filterCriterion.OperandName == FilterOperandNames.MinValue)
					 || (filterCriterion.OperandName == FilterOperandNames.NormalMaximum)
					 || (filterCriterion.OperandName == FilterOperandNames.NormalMinimum)
					)
			{
				try
				{
					// see if there is a decimal point
					int pos = ((string)filterCriterion.ComparisonValue).IndexOf('.');
					if (pos > -1)
						filterCriterion.ComparisonValue = Convert.ToDouble((string)filterCriterion.ComparisonValue);
					else
						filterCriterion.ComparisonValue = Convert.ToInt64((string)filterCriterion.ComparisonValue);
				}
				catch
				{
					errorMessage = "Comparison Value must be an integer or floating point value.";
				}
			}
			else if (   (filterCriterion.OperandName == FilterOperandNames.PercentDeadband)
					 || (filterCriterion.OperandName == FilterOperandNames.AbsoluteDeadband)
					 || (filterCriterion.OperandName == FilterOperandNames.ExceptionDeviation))
			{
				try
				{
					filterCriterion.ComparisonValue = Convert.ToDouble((string)filterCriterion.ComparisonValue);
				}
				catch
				{
					errorMessage = "Comparison Value must be a floating point value.";
				}
			}
			else if (filterCriterion.OperandName == FilterOperandNames.AccessRight)
			{
				if (string.Compare("Read", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = FilterOperandValues.Read;
				else if (string.Compare("Write", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = FilterOperandValues.Write;
				else
				{
					errorMessage = "Comparison Value must be either Read or Write.";
				}
			}
			else if (filterCriterion.OperandName == FilterOperandNames.AlarmState)
			{
				if (string.Compare("Active", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = AlarmState.Active;
				else if (string.Compare("Disabled", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = AlarmState.Disabled;
				else if (string.Compare("Initial", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = AlarmState.Initial;
				else if (string.Compare("Suppressed", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = AlarmState.Suppressed;
				else if (string.Compare("Unacked", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = AlarmState.Unacked;
				else
				{
					errorMessage = "Comparison Value must be Initial, or one or more of Active, Disabled, Suppressed, Unacked.";
				}
			}
			else if (  (filterCriterion.OperandName == FilterOperandNames.Archiving)
					 ||(filterCriterion.OperandName == FilterOperandNames.Stepped))
			{
				if (string.Compare("True", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = true;
				else if (string.Compare("False", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = false;
				else
				{
					errorMessage = "Comparison Value must be True or False.";
				}
			}
			else if (filterCriterion.OperandName == FilterOperandNames.BranchOrLeaf)
			{
				if (string.Compare("Branch", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = FilterOperandValues.Branch;
				else if (string.Compare("Leaf", (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = FilterOperandValues.Leaf;
				else
				{
					errorMessage = "Comparison Value must be Branch or Leaf.";
				}
			}
			else if (filterCriterion.OperandName == FilterOperandNames.EventType)
			{
				if (string.Compare(EventType.Alert.ToString("G"), (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = EventType.Alert;
				else if (string.Compare(EventType.EclipsedAlarm.ToString("G"), (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = EventType.EclipsedAlarm;
				else if (string.Compare(EventType.GroupedAlarm.ToString("G"), (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = EventType.GroupedAlarm;
				else if (string.Compare(EventType.OperatorActionEvent.ToString("G"), (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = EventType.OperatorActionEvent;
				else if (string.Compare(EventType.SimpleAlarm.ToString("G"), (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = EventType.SimpleAlarm;
				else if (string.Compare(EventType.SystemEvent.ToString("G"), (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = EventType.SystemEvent;
				else
				{
					errorMessage = "Comparison Value must be Alert, EclipsedAlarm, GroupedAlarm, OperatorActionEvent, SimpleAlarm, or SystemEvent.";
				}
			}
			else if (filterCriterion.OperandName == FilterOperandNames.ExceptionDeviationType)
			{
				if (string.Compare(FilterOperandValues.AbsoluteValue, (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = FilterOperandValues.AbsoluteValue;
				else if (string.Compare(FilterOperandValues.PercentOfSpan, (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = FilterOperandValues.PercentOfSpan;
				else if (string.Compare(FilterOperandValues.PercentOfValue, (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = FilterOperandValues.PercentOfValue;
				else
				{
					errorMessage = "Comparison Value must be AbsoluteValue, PercentOfSpan, or PercentOfValue.";
				}
			}
			else if (filterCriterion.OperandName == FilterOperandNames.StartingObjectAttributes)
			{
				if (string.Compare(StartingObjectFilterValues.AllObjects.ToString("G"), (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = (int)StartingObjectFilterValues.AllObjects;
				else if (string.Compare(StartingObjectFilterValues.StartingObjectOnly.ToString("G"), (string)filterCriterion.ComparisonValue, true) == 0)
					filterCriterion.ComparisonValue = (int)StartingObjectFilterValues.StartingObjectOnly;
				else
				{
					errorMessage = "Comparison Value must be AllObjects or StartingObjectOnly.";
				}
			}
			else if (   (filterCriterion.OperandName == FilterOperandNames.OccurrenceTime)
					 || (filterCriterion.OperandName == FilterOperandNames.TimeLastActive)
					 || (filterCriterion.OperandName == FilterOperandNames.Timestamp))
			{
				try
				{
					filterCriterion.ComparisonValue = Convert.ToDateTime((string)filterCriterion.ComparisonValue);
				}
				catch
				{
					errorMessage = "Comparison Value must be a date/time.";
				}
			}
			return errorMessage;
		}

	}
}
