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
using System.Text;
using System.Collections;

namespace Xi.Common.Support
{
	/// <summary>
	/// An offset component of a relative time.
	/// </summary>
	[Serializable]
	public struct TimeOffset
	{
		/// <summary>
		/// A signed value indicated the magnitude of the time offset.
		/// </summary>
		public int Value
		{
			get { return m_value; }
			set { m_value = value; }
		}

		/// <summary>
		/// The time interval to use when applying the offset.
		/// </summary>
		public RelativeTime Type
		{
			get { return m_type; }
			set { m_type = value; }
		}

		/// <summary>
		/// Converts a offset type to a string token.
		/// </summary>
		/// <param name="offsetType">The offset type value to convert.</param>
		/// <returns>The string token representing the offset type.</returns>
		internal static string OffsetTypeToString(RelativeTime offsetType)
		{
			switch (offsetType)
			{
				case RelativeTime.Second: { return "S"; }
				case RelativeTime.Minute: { return "M"; }
				case RelativeTime.Hour: { return "H"; }
				case RelativeTime.Day: { return "D"; }
				case RelativeTime.Week: { return "W"; }
				case RelativeTime.Month: { return "MO"; }
				case RelativeTime.Year: { return "Y"; }
			}

			throw new ArgumentOutOfRangeException("offsetType", offsetType.ToString(), "Invalid value for relative time offset type.");
		}

		#region Private Members
		private int m_value;
		private RelativeTime m_type;
		#endregion
	}

	/// <summary>
	/// A collection of time offsets used in a relative time.
	/// </summary>
	[Serializable]
	public class TimeOffsetCollection : ArrayList
	{
		/// <summary>
		/// Accessor for elements in the time offset collection.
		/// </summary>
		public new TimeOffset this[int index]
		{
			get { return this[index]; }
			set { this[index] = value; }
		}

		/// <summary>
		/// Adds a new offset to the collection.
		/// </summary>
		/// <param name="value">The offset value.</param>
		/// <param name="type">The offset type.</param>
		public int Add(int value, RelativeTime type)
		{
			TimeOffset offset = new TimeOffset();

			offset.Value = value;
			offset.Type = type;

			return base.Add(offset);
		}

		/// <summary>
		/// Returns a String that represents the current Object.
		/// </summary>
		/// <returns>A String that represents the current Object.</returns>
		public override string ToString()
		{
			StringBuilder buffer = new StringBuilder(256);

			foreach (TimeOffset offset in (ICollection)this)
			{
				if (offset.Value >= 0)
				{
					buffer.Append("+");
				}

				buffer.AppendFormat("{0}", offset.Value);
				buffer.Append(TimeOffset.OffsetTypeToString(offset.Type));
			}

			return buffer.ToString();
		}

		/// <summary>
		/// Initializes the collection from a set of offsets contained in a string. 
		/// </summary>
		/// <param name="buffer">A string containing the time offset fields.</param>
		public void Parse(string buffer)
		{
			// clear existing offsets.
			Clear();

			// parse the offsets.
			bool positive = true;
			int magnitude = 0;
			string units = "";
			int state = 0;

			// state = 0 - looking for start of next offset field.
			// state = 1 - looking for beginning of offset value.
			// state = 2 - reading offset value.
			// state = 3 - reading offset type.

			for (int ii = 0; ii < buffer.Length; ii++)
			{
				// check for sign part of the offset field.
				if (buffer[ii] == '+' || buffer[ii] == '-')
				{
					if (state == 3)
					{
						Add(CreateOffset(positive, magnitude, units));

						magnitude = 0;
						units = "";
						state = 0;
					}

					if (state != 0)
					{
						throw new FormatException("Unexpected token encountered while parsing relative time string.");
					}

					positive = buffer[ii] == '+';
					state = 1;
				}

				// check for integer part of the offset field.
				else if (Char.IsDigit(buffer, ii))
				{
					if (state == 3)
					{
						Add(CreateOffset(positive, magnitude, units));

						magnitude = 0;
						units = "";
						state = 0;
					}

					if (state != 0 && state != 1 && state != 2)
					{
						throw new FormatException("Unexpected token encountered while parsing relative time string.");
					}

					magnitude *= 10;
					magnitude += System.Convert.ToInt32(buffer[ii] - '0');

					state = 2;
				}

				// check for units part of the offset field.
				else if (!Char.IsWhiteSpace(buffer, ii))
				{
					if (state != 2 && state != 3)
					{
						throw new FormatException("Unexpected token encountered while parsing relative time string.");
					}

					units += buffer[ii];
					state = 3;
				}
			}

			// process final field.
			if (state == 3)
			{
				Add(CreateOffset(positive, magnitude, units));
				state = 0;
			}

			// check final state.
			if (state != 0)
			{
				throw new FormatException("Unexpected end of string encountered while parsing relative time string.");
			}
		}

		#region ICollection Members
		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(TimeOffset[] array, int index)
		{
			CopyTo((Array)array, index);
		}
		#endregion

		#region IList Members
		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, TimeOffset value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(TimeOffset value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(TimeOffset value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(TimeOffset value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(TimeOffset value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Creates a new offset object from the components extracted from a string.
		/// </summary>
		private static TimeOffset CreateOffset(bool positive, int magnitude, string units)
		{
			foreach (RelativeTime offsetType in Enum.GetValues(typeof(RelativeTime)))
			{
				if (offsetType == RelativeTime.Now)
				{
					continue;
				}

				if (units == TimeOffset.OffsetTypeToString(offsetType))
				{
					TimeOffset offset = new TimeOffset();

					offset.Value = (positive) ? magnitude : -magnitude;
					offset.Type = offsetType;

					return offset;
				}
			}

			throw new ArgumentOutOfRangeException("units", units, "String is not a valid offset time type.");
		}
		#endregion
	}
}
