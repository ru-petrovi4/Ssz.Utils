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
	/// This class defines attributes that describe modifications 
	/// performed to a history value. 
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ModificationAttributesList : DataValueArrays
	{
		/// <summary>
		/// The time that the modification was performed.
		/// </summary>
		[DataMember] public DateTime[]? DoubleModificationTimeStamps { get; private set; }

		/// <summary>
		/// The type of modification performed. 
		/// </summary>
		[DataMember] public ModificationType[]? DoubleModificationTypes { get; private set; }

		/// <summary>
		/// The name or other system-specific identifier of the 
		/// operator who performed the modification. 
		/// </summary>
		[DataMember] public string[]? DoubleOperatorNames { get; private set; }

		/// <summary>
		/// The time that the modification was performed.
		/// </summary>
		[DataMember] public DateTime[]? LongModificationTimeStamps { get; private set; }

		/// <summary>
		/// The type of modification performed. 
		/// </summary>
		[DataMember] public ModificationType[]? LongModificationTypes { get; private set; }

		/// <summary>
		/// The name or other system-specific identifier of the 
		/// operator who performed the modification. 
		/// </summary>
		[DataMember] public string[]? LongOperatorNames { get; private set; }

		/// <summary>
		/// The time that the modification was performed.
		/// </summary>
		[DataMember] public DateTime[]? ObjectModificationTimeStamps { get; private set; }

		/// <summary>
		/// The type of modification performed. 
		/// </summary>
		[DataMember] public ModificationType[]? ObjectModificationTypes { get; private set; }

		/// <summary>
		/// The name or other system-specific identifier of the 
		/// operator who performed the modification. 
		/// </summary>
		[DataMember] public string[]? ObjectOperatorNames { get; private set; }


		/// <summary>
		/// This constructor initializes a ModificationAttributesList object with arrays of the 
		/// specified sizes.
		/// </summary>
		/// <param name="doubleArraySize">The size of the DoubleModificationTimeStamps, 
		/// DoubleModificationTypes, and the DoubleOperatorNames arrays.</param>
		/// <param name="longArraySize">The size of the LongModificationTimeStamps, 
		/// LongModificationTypes, and the LongOperatorNames arrays.</param>
		/// <param name="objectArraySize">The size of the ObjectModificationTimeStamps, 
		/// ObjectModificationTypes, and the ObjectOperatorNames arrays.</param>
		public ModificationAttributesList(int doubleArraySize, int longArraySize, int objectArraySize)
			: base(doubleArraySize, longArraySize, objectArraySize)
		{
			if (0 == doubleArraySize)
			{
				DoubleModificationTimeStamps = null;
				DoubleModificationTypes = null;
				DoubleOperatorNames = null;
			}
			else
			{
				DoubleModificationTimeStamps = new DateTime[doubleArraySize];
				DoubleModificationTypes = new ModificationType[doubleArraySize];
				DoubleOperatorNames = new string[doubleArraySize];
			}
			if (0 == longArraySize)
			{
				LongModificationTimeStamps = null;
				LongModificationTypes = null;
				LongOperatorNames = null;
			}
			else
			{
				LongModificationTimeStamps = new DateTime[longArraySize];
				LongModificationTypes = new ModificationType[longArraySize];
				LongOperatorNames = new string[longArraySize];
			}
			if (0 == objectArraySize)
			{
				ObjectModificationTimeStamps = null;
				ObjectModificationTypes = null;
				ObjectOperatorNames = null;
			}
			else
			{
				ObjectModificationTimeStamps = new DateTime[objectArraySize];
				ObjectModificationTypes = new ModificationType[objectArraySize];
				ObjectOperatorNames = new string[objectArraySize];
			}
		}

		/// <summary>
		/// This method is used to set the entries at a specific index in the 
		/// DoubleModificationTimeStamps, DoubleModificationTypes, and the 
		/// DoubleOperatorNames arrays.
		/// </summary>
		/// <param name="idx">The index of the entries to be updated.</param>
		/// <param name="modificationTimeStamp">The modificationTimeStamp to be set for the entry.</param>
		/// <param name="modificationType">The modificationType to be set for the entry.</param>
		/// <param name="operatorName">The operatorName to be set for the entry.</param>
		/// <param name="statusCode">The statusCode to be set for the entry.</param>
		/// <param name="timeStamp">The timeStamp to be set for the entry.</param>
		/// <param name="value">The double value to be set for the entry.</param>
		public void SetDouble(int idx, DateTime modificationTimeStamp, ModificationType modificationType, 
			string operatorName, uint statusCode, DateTime timeStamp, double value)
		{
			if (DoubleModificationTimeStamps == null ||
				DoubleModificationTypes == null ||
				DoubleOperatorNames == null) throw new InvalidOperationException();
			DoubleModificationTimeStamps[idx] = modificationTimeStamp;
			DoubleModificationTypes[idx] = modificationType;
			DoubleOperatorNames[idx] = operatorName;
			base.SetDouble(idx, statusCode, timeStamp, value);
		}

		/// <summary>
		/// This method is used to set the entries at a specific index in the 
		/// LongModificationTimeStamps, LongModificationTypes, and the 
		/// LonOperatorNames arrays
		/// </summary>
		/// <param name="idx">The index of the entries to be updated.</param>
		/// <param name="modificationTimeStamp">The modificationTimeStamp to be set for the entry.</param>
		/// <param name="modificationType">The modificationType to be set for the entry.</param>
		/// <param name="operatorName">The operatorName to be set for the entry.</param>
		/// <param name="statusCode">The statusCode to be set for the entry.</param>
		/// <param name="timeStamp">The timeStamp to be set for the entry.</param>
		/// <param name="value">The long value to be set for the entry.</param>
		public void SetUint(int idx, DateTime modificationTimeStamp, ModificationType modificationType,
			string operatorName, uint statusCode, DateTime timeStamp, uint value)
		{
			if (LongModificationTimeStamps == null ||
				LongModificationTypes == null ||
				LongOperatorNames == null) throw new InvalidOperationException();
			LongModificationTimeStamps[idx] = modificationTimeStamp;
			LongModificationTypes[idx] = modificationType;
			LongOperatorNames[idx] = operatorName;
			base.SetUint(idx, statusCode, timeStamp, value);
		}

		/// <summary>
		/// This method is used to set the entries at a specific index in the 
		/// ObjectModificationTimeStamps, ObjectModificationTypes, and the 
		/// ObjectOperatorNames arrays.
		/// </summary>
		/// <param name="idx">The index of the entries to be updated.</param>
		/// <param name="modificationTimeStamp">The modificationTimeStamp to be set for the entry.</param>
		/// <param name="modificationType">The modificationType to be set for the entry.</param>
		/// <param name="operatorName">The operatorName to be set for the entry.</param>
		/// <param name="statusCode">The statusCode to be set for the entry.</param>
		/// <param name="timeStamp">The timeStamp to be set for the entry.</param>
		/// <param name="value">The object value to be set for the entry.</param>
		public void SetObject(int idx, DateTime modificationTimeStamp, ModificationType modificationType,
			string operatorName, uint statusCode, DateTime timeStamp, object value)
		{
			if (ObjectModificationTimeStamps == null ||
				ObjectModificationTypes == null ||
				ObjectOperatorNames == null) throw new InvalidOperationException();
			ObjectModificationTimeStamps[idx] = modificationTimeStamp;
			ObjectModificationTypes[idx] = modificationType;
			ObjectOperatorNames[idx] = operatorName;
			base.SetObject(idx, statusCode, timeStamp, value);
		}

	}
}