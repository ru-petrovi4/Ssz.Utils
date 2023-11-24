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

using Xi.Common.Support;

namespace Xi.Server.Base
{
	/// <summary>
	/// This class provides common basic values for the Data List Values.
	/// A Data List is used to represent real time process values.  This
	/// base class provides some common properties runtime Data Values.
	/// </summary> 
	public class DataListValueBase
		: ValueRoot
	{
		/// <summary>
		/// Constructor for the Data List Value Base class.
		/// </summary>
		/// <param name="ownerList"></param>
		public DataListValueBase(uint clientAlias, uint serverAlias)
			: base(clientAlias, serverAlias)
		{
		}

		private bool _updatingEnabled;
		/// <summary>
		/// This property is used to track or indicate the 
		/// active or inactive state of this Data List Value.
		/// </summary>
		public bool UpdatingEnabled { get { return _updatingEnabled; } set { _updatingEnabled = value; } }

		private uint _statusCode;
		/// <summary>
		/// 
		/// </summary>
		public override uint StatusCode { get { return _statusCode; } set { _statusCode = value; } }

		private DateTime _TimeStamp;
		/// <summary>
		/// The Time Stamp is a UTC time.
		/// </summary>
		public DateTime TimeStamp { get { return _TimeStamp; } set { _TimeStamp = value; } }

		private TransportDataType _valueTransportTypeKey;
		/// <summary>
		/// The Value Transport Type Key property provides the data type for transport.
		/// </summary>
		public override TransportDataType ValueTransportTypeKey
		{
			get { return _valueTransportTypeKey; }
			protected set { _valueTransportTypeKey = value; }
		}

		private double _doubleValue;
		private uint _uintValue;
		private object _objectValue;
		/// <summary>
		/// The actual Data Value is simply stored as a .NET object.  Thus
		/// it may represent any data value from a simple intrisic data type, 
		/// an array of intrisic data types or an instance of a class.  
		/// Note that if a class is to be represented it must be defined 
		/// with a Data Contract and Data Memebers.
		/// </summary>
		public object Value
		{
			get
			{
				switch (_valueTransportTypeKey)
				{
					case TransportDataType.Double:
						return _doubleValue;
					case TransportDataType.Uint:
						return _uintValue;
					case TransportDataType.Object:
						return _objectValue;
					default:
						break;
				}
				return null;
			}
			//set { _value = value; }
		}

		/// <summary>
		/// Used to set the value when transproted as a double.
		/// </summary>
		public double DoubleValue
		{
			get { return _doubleValue; }
			internal set { _doubleValue = value; ValueTransportTypeKey = TransportDataType.Double; }
		}
		/// <summary>
		/// Used to set the value when transproted as a long.
		/// </summary>
		public uint UintValue
		{
			get { return _uintValue; }
			internal set { _uintValue = value; ValueTransportTypeKey = TransportDataType.Uint; }
		}
		/// <summary>
		/// Used to set the value when transproted as a object.
		/// </summary>
		public object ObjectValue
		{
			get { return _objectValue; }
			internal set { _objectValue = value; ValueTransportTypeKey = TransportDataType.Object; }
		}

		/// <summary>
		/// This method is used to update this Data Value Base 
		/// with new values.  It has the side effect of adding this 
		/// Data Value Base to the queue of changed Entry Root maintained 
		/// by List Root.This method 
		/// </summary>
		/// <param name="statusCode"></param>
		/// <param name="timeStamp"></param>
		/// <param name="value"></param>
		public virtual void DoubleValueUpdate(uint statusCode, DateTime timeStamp, double value)
		{
			StatusCode = statusCode;
			TimeStamp = timeStamp;
			DoubleValue = value;
			EntryQueued = true;
		}
		public virtual void UintValueUpdate(uint statusCode, DateTime timeStamp, uint value)
		{
			StatusCode = statusCode;
			TimeStamp = timeStamp;
			UintValue = value;
			EntryQueued = true;
		}
		public virtual void ObjectValueUpdate(uint statusCode, DateTime timeStamp, object value)
		{
			StatusCode = statusCode;
			TimeStamp = timeStamp;
			ObjectValue = value;
			EntryQueued = true;
		}

	}
}
