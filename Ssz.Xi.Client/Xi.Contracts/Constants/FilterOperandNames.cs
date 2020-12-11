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
using System.Text;

namespace Xi.Contracts.Constants
{
	/// <summary>
	/// A string that represents the name of operand of a single filter criterion.  
	/// All values are case independent and should be up-shifted or down-shifted by 
	/// the server when used in comparisons.. They are defined here in camel case 
	/// for read-ability in displays.
	/// </summary>
	public class FilterOperandNames
	{
		#region FindObjects Operands
		/// <summary>
		/// <para>The access rights of an object.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The valid values for this operand are FilterOperandValues.Read 
		/// and FilterOperandValues.Write. </para>
		/// </summary>
		public const string AccessRight = "AccessRight";

		/// <summary>
		/// <para>The default behavior for filtering is to select both branches 
		/// and leaves. This filter operand allows the client to select one 
		/// or the other. </para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The valid values for this operand are FilterOperandValues.Branch 
		/// and FilterOperandValues.Leaf. </para>
		/// </summary>
		public const string BranchOrLeaf = "BranchOrLeaf";

		/// <summary>
		/// <para>The name of the data type.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values are constructed by using the ToString() method 
		/// for the TypeId of the data type.</para>
		/// </summary>
		public const string DataType = "DataType";

		/// <summary>
		/// <para>The name of the object.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid names are strings.  Servers may optionally support 
		/// the use of the '?' character to represent any single character 
		/// and the '*' to represent multiple characters.</para>
		/// </summary>
		public const string Name = "Name";

		/// <summary>
		/// <para>An operand with an integer value that specifies whether the server 
		/// is to return ObjectAttributes only for the object identified by the 
		/// starting path or for it plus the objects found below it.  The default 
		/// behavior when this filter operand is omitted is to return ObjectAttributes 
		/// only for the objects found below the object identified by the starting path.</para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The valid values for this operand are the integer representations 
		/// of the StartingObjectFilterValues enumeration.</para>
		/// <para>For example, (int)StartingObjectFilterValues.AllObjects
		/// is used to return ObjectAttributes for the starting object and all 
		/// objects below it. </para>
		/// </summary>
		public const string StartingObjectAttributes = "StartingObjectAttributes";

		#endregion // FindObjects Operands

		#region Alarms & Events Operands
		/// <summary>
		/// <para>The alarm state of an alarm message.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The valid values are defined by the AlarmState enumeration.</para>
		/// </summary>
		public const string AlarmState = "AlarmState";

		/// <summary>
		/// <para>The area of an event message.</para> 
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values for this operand are objects whose role is 
		/// ObjectRole.AreaRoleId. </para>
		/// </summary>
		public const string Area = "Area";

		/// <summary>
		/// <para>The category of an event message.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The valid values for this operand are the categoryIds of the 
		/// categories defined by the EventCategoryConfigurations Mib element. </para>
		/// </summary>
		public const string EventCategory = "EventCategory";

		/// <summary>
		/// <para>The string representation of the InstanceId of an Event Condition 
		/// of an event message.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid names are strings generated using the InstanceId ToString() 
		/// method.  Servers may optionally support the use of the '?' character 
		/// to represent any single character and the '*' to represent multiple 
		/// characters.</para>
		/// </summary>
		public const string EventConditionName = "EventConditionName";

		/// <summary>
		/// <para>The priority of an event message.</para>  
		/// <para>All operators defined by the FilterOperator class are valid.</para>
		/// <para>The valid values for this operand are integer values. </para>
		/// </summary>
		public const string EventPriority = "EventPriority";

		/// <summary>
		/// <para>The string representation of the InstanceId of an Event Source 
		/// of an event message.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid names are strings generated using the InstanceId ToString() 
		/// method.  Servers may optionally support the use of the '?' character 
		/// to represent any single character and the '*' to represent multiple 
		/// characters in the LocalId property of the InstanceId.</para>
		/// </summary>
		public const string EventSourceId = "EventSourceId";

		/// <summary>
		/// <para>The type of an event message.</para> 
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values for this operand are defined by the 
		/// EventType enumeration. </para>
		/// </summary>
		public const string EventType = "EventType";

		/// <summary>
		/// <para>The name of a grouped or eclipsed alarm of an event message.</para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid names are strings.  Servers may optionally support 
		/// the use of the '?' character to represent any single character 
		/// and the '*' to represent multiple characters.</para>
		/// </summary>
		public const string MultiplexedAlarmContainer = "MultiplexedAlarmContainer";

		/// <summary>
		/// <para>The operator name of an event message.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values are strings.  Servers may optionally support the 
		/// use of the '?' character to represent any single character and the 
		/// '*' to represent multiple characters.</para>
		/// </summary>
		public const string OperatorName = "OperatorName";

		/// <summary>
		/// <para>The occurrence id of an event message.</para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values are strings.  Servers may optionally support the 
		/// use of the '?' character to represent any single character and the 
		/// '*' to represent multiple characters.</para>
		/// </summary>
		public const string OccurrenceId = "OccurrenceId";

		/// <summary>
		/// <para>The occurrence time of an event.</para>  
		/// <para>All operators defined by the FilterOperator class are valid.</para>
		/// <para>The valid values for this operand are DateTime values. </para>
		/// </summary>
		public const string OccurrenceTime = "OccurrenceTime";

		/// <summary>
		/// <para>The text message of an event message.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values are strings.  Servers may optionally support the 
		/// use of the '?' character to represent any single character and the 
		/// '*' to represent multiple characters.</para>
		/// </summary>
		public const string TextMessage = "TextMessage";

		/// <summary>
		/// <para>The last active time of an alarm.</para>  
		/// <para>All operators defined by the FilterOperator class are valid.</para>
		/// <para>The valid values for this operand are DateTime values. </para>
		/// </summary>
		public const string TimeLastActive = "TimeLastActive";

		#endregion // Alarms & Events Operands

		#region Data Access Operands
		/// <summary>
		/// <para>The absolute deadband for a floating point value.  If a value has changed by 
		/// this absolute amount it is considered to have changed for subscription purposes, and 
		/// will be returned in a poll response or in a callback.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The valid values for this operand are double values. </para>
		/// </summary>
		public const string AbsoluteDeadband = "AbsoluteDeadband";

		/// <summary>
		/// <para>The percent deadband for a floating point value.  If a value has changed by 
		/// this percent it is considered to have changed for subscription purposes, and will 
		/// be returned in a poll response or in a callback.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The valid values for this operand are double values. </para>
		/// </summary>
		public const string PercentDeadband = "PercentDeadband";

		#endregion // Data Access Operands

		#region Historical Data Access Operands

		/// <summary>
		/// <para>The flag that indicates, when TRUE, that a data journal is 
		/// collecting history a value. </para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The value of this operand is a boolean value. </para>
		/// </summary>
		public const string Archiving = "Archiving";

		/// <summary>
		/// <para>The id of the data object within the server that generated a 
		/// historical value.</para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values are strings.  Servers may optionally support the 
		/// use of the '?' character to represent any single character and the 
		/// '*' to represent multiple characters.</para>
		/// </summary>
		public const string DataObjectId = "DataObjectId";

		/// <summary>
		/// <para>The equation used to derive a value.</para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values are strings.  Servers may optionally support the 
		/// use of the '?' character to represent any single character and the 
		/// '*' to represent multiple characters.</para>
		/// </summary>
		public const string DerivingEquation = "DerivingEquation";

		/// <summary>
		/// <para>The engineering units of a value. </para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>All operators defined by the FilterOperator class are valid.</para>
		/// <para>Valid values are strings.  Servers may optionally support the 
		/// use of the '?' character to represent any single character and the 
		/// '*' to represent multiple characters.</para>
		/// </summary>
		public const string EngineeringUnits = "EngineeringUnits";

		/// <summary>
		/// <para>The minimum change in a data value that causes the value of a data 
		/// object to be recorded by the data journal. The ExceptionDeviationType 
		/// indicates whether the change is calcuated using absolute value, percent of span, 
		/// or percent of value.  </para>
		/// <para>All operators defined by the FilterOperator class are valid.</para>
		/// <para>Valid values are doubles.  </para>
		/// </summary>
		public const string ExceptionDeviation = "ExceptionDeviation";

		/// <summary>
		/// <para>Indicates whether ExceptionDeviation is expressed in absolute value, 
		/// percent of span, or percent of value.  </para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The valid values for this operand are FilterOperandValues.AbsoluteValue, 
		/// FilterOperandValues.PercentOfSpan, and FilterOperandValues.PercentOfValue. </para>
		/// </summary>
		public const string ExceptionDeviationType = "ExceptionDeviationType";

		/// <summary>
		/// <para>The maximum time interval between entries of a historical value. </para>
		/// <para>All operators defined by the FilterOperator class are valid.</para>
		/// <para>Valid values are TimeSpans.  </para>
		/// </summary>
		public const string MaxTimeInterval = "MaxTimeInterval";

		/// <summary>
		/// <para>The highest valid value for a data object (its top of span).</para>
		/// <para>All operators defined by the FilterOperator class are valid, except for 
		/// "GTE" and "GT".</para>
		/// <para>The valid values for this operand are dependent on the type of the value. </para>
		/// </summary>
		public const string MaxValue = "MaxValue";

		/// <summary>
		/// <para>The highest valid value for a data object (its bottom of span).</para>
		/// <para>All operators defined by the FilterOperator class are valid, except for 
		/// "LTE" and "LT".</para>
		/// <para>The valid values for this operand are dependent on the type of the value. </para>
		/// </summary>
		public const string MinValue = "MinValue";

		/// <summary>
		/// <para>The upper limit for the normal maximum of a historical value.</para>
		/// <para>All operators defined by the FilterOperator class are valid.</para>
		/// <para>Valid values are the string respresentations of the data object value.</para>
		/// </summary>
		public const string NormalMaximum = "NormalMaximum";

		/// <summary>
		/// <para>The lower limit for the normal minimum of a historical value.</para>
		/// <para>All operators defined by the FilterOperator class are valid.</para>
		/// <para>Valid values are the string respresentations of the data object value.</para>
		/// </summary>
		public const string NormalMinimum = "NormalMinimum";

		/// <summary>
		/// <para>The timestamp of a value represented by an expression.</para>  
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The valid values for this operand are defined by the OPC HDA 
		/// specification. </para>
		/// </summary>
		public const string OpcHdaTimestampExpression = "OpcHdaTimestampExpression";

		/// <summary>
		/// <para>The name or IP address of the machine which the server that 
		/// generated a historical data value runs.</para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values are strings.  Servers may optionally support the 
		/// use of the '?' character to represent any single character and the 
		/// '*' to represent multiple characters.</para>
		/// </summary>
		public const string ServerMachineName = "ServerMachineName";

		/// <summary>
		/// <para>The name the server that generated a historical data value runs.</para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values are strings.  Servers may optionally support the 
		/// use of the '?' character to represent any single character and the 
		/// '*' to represent multiple characters.</para>
		/// </summary>
		public const string ServerName = "ServerName";

		/// <summary>
		/// <para>The type of server that generated a historical value.</para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>Valid values include the ToString("G") values for the ServerType 
		/// enumeration.  </para>
		/// </summary>
		public const string ServerType = "ServerType";

		/// <summary>
		/// <para>The flag that indicates, when TRUE, that a data journal value 
		/// is stepped. When FALSE, it is interpolated. </para>
		/// <para>The only valid FilterOperator is "EQ" (equals).</para>
		/// <para>The value of this operand is a boolean value. </para>
		/// </summary>
		public const string Stepped = "Stepped";

		/// <summary>
		/// <para>The timestamp of a value in DateTime format.</para>  
		/// <para>All operators defined by the FilterOperator class are valid.</para>
		/// <para>The valid values for this operand are DateTime values. </para>
		/// </summary>
		public const string Timestamp = "Timestamp";

		#endregion // Historical Data Access Operands

		#region Historical Alarms and Events Operands
		#endregion // Historical Alarms and Events Operands

	}
}
