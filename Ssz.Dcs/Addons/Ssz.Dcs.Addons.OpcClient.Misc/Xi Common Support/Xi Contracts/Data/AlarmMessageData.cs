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
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This class defines the alarm data that is transferred in Event 
	/// Messages that report an alarm.
	/// <para>The concepts for alarms and events accessible through 
	/// this interface are defined in EEMUA Publication 191 "Alarm 
	/// Systems: A Guide to Design, Management and Procurement".
	/// See http://www.eemua.org</para>
	/// <para>EEMUA Publication 191 generally defines messages to 
	/// report alarms and events as "text information presented to 
	/// the operator that describes the alarm condition."</para>  
	/// <para>The members of this class represent the individual 
	/// pieces of alarm information to be included in the text 
	/// information that is presented to the operator for alarms.</para>  
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class AlarmMessageData
	{
		/// <summary>
		/// The current state of the alarm.
		/// </summary>
		[DataMember] public AlarmState AlarmState;

		/// <summary>
		/// The state change(s) that caused the alarm message to sent.
		/// The Xi.Contract.Constants.AlarmStateChangeCodes class defines 
		/// the values for this member.
		/// </summary>
		[DataMember] public uint AlarmStateChange;

		/// <summary>
		/// The time that the alarm last transitioned to the active state.  
		/// This time is independent of the current state of the alarm.  
		/// Null if the alarm has never been active. 
		/// </summary>
		[DataMember] public Nullable<DateTime> TimeLastActive;

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
        public Ssz.Utils.DataAccess.AlarmMessageData ToAlarmMessageData()
        {
            var alarmMessageData = new Ssz.Utils.DataAccess.AlarmMessageData();
            alarmMessageData.AlarmState = (Ssz.Utils.DataAccess.AlarmState)AlarmState;
            alarmMessageData.AlarmStateChange = AlarmStateChange;
            if (TimeLastActive != null)
            {
                alarmMessageData.TimeLastActive = TimeLastActive.Value;
            }
            return alarmMessageData;
        }
    }
}