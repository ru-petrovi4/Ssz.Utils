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
		[DataMember] public DateTime[]? DoubleModificationTimestamps { get; private set; }

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
		[DataMember] public DateTime[]? LongModificationTimestamps { get; private set; }

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
		[DataMember] public DateTime[]? ObjectModificationTimestamps { get; private set; }

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
		/// <param name="doubleArraySize">The size of the DoubleModificationTimestamps, 
		/// DoubleModificationTypes, and the DoubleOperatorNames arrays.</param>
		/// <param name="longArraySize">The size of the LongModificationTimestamps, 
		/// LongModificationTypes, and the LongOperatorNames arrays.</param>
		/// <param name="objectArraySize">The size of the ObjectModificationTimestamps, 
		/// ObjectModificationTypes, and the ObjectOperatorNames arrays.</param>
		public ModificationAttributesList(int doubleArraySize, int longArraySize, int objectArraySize)
			: base(doubleArraySize, longArraySize, objectArraySize)
		{
			if (0 == doubleArraySize)
			{
				DoubleModificationTimestamps = null;
				DoubleModificationTypes = null;
				DoubleOperatorNames = null;
			}
			else
			{
				DoubleModificationTimestamps = new DateTime[doubleArraySize];
				DoubleModificationTypes = new ModificationType[doubleArraySize];
				DoubleOperatorNames = new string[doubleArraySize];
			}
			if (0 == longArraySize)
			{
				LongModificationTimestamps = null;
				LongModificationTypes = null;
				LongOperatorNames = null;
			}
			else
			{
				LongModificationTimestamps = new DateTime[longArraySize];
				LongModificationTypes = new ModificationType[longArraySize];
				LongOperatorNames = new string[longArraySize];
			}
			if (0 == objectArraySize)
			{
				ObjectModificationTimestamps = null;
				ObjectModificationTypes = null;
				ObjectOperatorNames = null;
			}
			else
			{
				ObjectModificationTimestamps = new DateTime[objectArraySize];
				ObjectModificationTypes = new ModificationType[objectArraySize];
				ObjectOperatorNames = new string[objectArraySize];
			}
		}

		/// <summary>
		/// This method is used to set the entries at a specific index in the 
		/// DoubleModificationTimestamps, DoubleModificationTypes, and the 
		/// DoubleOperatorNames arrays.
		/// </summary>
		/// <param name="idx">The index of the entries to be updated.</param>
		/// <param name="modificationTimestamp">The modificationTimestamp to be set for the entry.</param>
		/// <param name="modificationType">The modificationType to be set for the entry.</param>
		/// <param name="operatorName">The operatorName to be set for the entry.</param>
		/// <param name="statusCode">The statusCode to be set for the entry.</param>
		/// <param name="timestamp">The timestamp to be set for the entry.</param>
		/// <param name="value">The double value to be set for the entry.</param>
		public void SetDouble(int idx, DateTime modificationTimestamp, ModificationType modificationType, 
			string operatorName, uint statusCode, DateTime timestamp, double value)
		{
			if (DoubleModificationTimestamps is null ||
				DoubleModificationTypes is null ||
				DoubleOperatorNames is null) throw new InvalidOperationException();
			DoubleModificationTimestamps[idx] = modificationTimestamp;
			DoubleModificationTypes[idx] = modificationType;
			DoubleOperatorNames[idx] = operatorName;
			base.SetDouble(idx, statusCode, timestamp, value);
		}

		/// <summary>
		/// This method is used to set the entries at a specific index in the 
		/// LongModificationTimestamps, LongModificationTypes, and the 
		/// LonOperatorNames arrays
		/// </summary>
		/// <param name="idx">The index of the entries to be updated.</param>
		/// <param name="modificationTimestamp">The modificationTimestamp to be set for the entry.</param>
		/// <param name="modificationType">The modificationType to be set for the entry.</param>
		/// <param name="operatorName">The operatorName to be set for the entry.</param>
		/// <param name="statusCode">The statusCode to be set for the entry.</param>
		/// <param name="timestamp">The timestamp to be set for the entry.</param>
		/// <param name="value">The long value to be set for the entry.</param>
		public void SetUint(int idx, DateTime modificationTimestamp, ModificationType modificationType,
			string operatorName, uint statusCode, DateTime timestamp, uint value)
		{
			if (LongModificationTimestamps is null ||
				LongModificationTypes is null ||
				LongOperatorNames is null) throw new InvalidOperationException();
			LongModificationTimestamps[idx] = modificationTimestamp;
			LongModificationTypes[idx] = modificationType;
			LongOperatorNames[idx] = operatorName;
			base.SetUint(idx, statusCode, timestamp, value);
		}

		/// <summary>
		/// This method is used to set the entries at a specific index in the 
		/// ObjectModificationTimestamps, ObjectModificationTypes, and the 
		/// ObjectOperatorNames arrays.
		/// </summary>
		/// <param name="idx">The index of the entries to be updated.</param>
		/// <param name="modificationTimestamp">The modificationTimestamp to be set for the entry.</param>
		/// <param name="modificationType">The modificationType to be set for the entry.</param>
		/// <param name="operatorName">The operatorName to be set for the entry.</param>
		/// <param name="statusCode">The statusCode to be set for the entry.</param>
		/// <param name="timestamp">The timestamp to be set for the entry.</param>
		/// <param name="value">The object value to be set for the entry.</param>
		public void SetObject(int idx, DateTime modificationTimestamp, ModificationType modificationType,
			string operatorName, uint statusCode, DateTime timestamp, object value)
		{
			if (ObjectModificationTimestamps is null ||
				ObjectModificationTypes is null ||
				ObjectOperatorNames is null) throw new InvalidOperationException();
			ObjectModificationTimestamps[idx] = modificationTimestamp;
			ObjectModificationTypes[idx] = modificationType;
			ObjectOperatorNames[idx] = operatorName;
			base.SetObject(idx, statusCode, timestamp, value);
		}

	}
}