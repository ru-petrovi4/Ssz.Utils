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
using System.Xml;

namespace Xi.Common.Support
{
	/// <summary>
	/// A time specified as either an absolute or relative value.
	/// </summary>
	[Serializable]
	public class Time
	{
		/// <summary>
		/// Initializes the object with its default values.
		/// </summary>
		public Time() { }

		/// <summary>
		/// Initializes the object with an absolute time.
		/// </summary>
		/// <param name="time">The absolute time.</param>
		public Time(DateTime time)
		{
			AbsoluteTime = time;
		}

		/// <summary>
		/// Initializes the object with a relative time.
		/// </summary>
		/// <param name="time">The relative time.</param>
		public Time(string time)
		{
			Time value = Parse(time);

			m_absoluteTime = DateTime.MinValue;
			m_baseTime = value.m_baseTime;
			m_offsets = value.m_offsets;
		}

		/// <summary>
		/// Whether the time is a relative or absolute time.
		/// </summary>
		public bool IsRelative
		{
			get { return (m_absoluteTime == DateTime.MinValue); }
			set { m_absoluteTime = DateTime.MinValue; }
		}

		/// <summary>
		/// The time as abolute UTC value.
		/// </summary>
		public DateTime AbsoluteTime
		{
			get { return m_absoluteTime; }
			set { m_absoluteTime = value; }
		}

		/// <summary>
		/// The base for a relative time value.
		/// </summary>
		public RelativeTime BaseTime
		{
			get { return m_baseTime; }
			set { m_baseTime = value; }
		}

		/// <summary>
		/// The set of offsets to be applied to the base of a relative time.
		/// </summary>
		public TimeOffsetCollection Offsets
		{
			get { return m_offsets; }
		}

		/// <summary>
		/// Converts a relative time to an absolute time by using the system clock.
		/// </summary>
		public DateTime ResolveTime()
		{
			// nothing special to do for absolute times.
			if (!IsRelative)
			{
				return m_absoluteTime;
			}

			// get local time from the system.
			DateTime time = DateTime.UtcNow;

			int years = time.Year;
			int months = time.Month;
			int days = time.Day;
			int hours = time.Hour;
			int minutes = time.Minute;
			int seconds = time.Second;
			int milliseconds = time.Millisecond;

			// move to the beginning of the period indicated by the base time.
			switch (BaseTime)
			{
				case RelativeTime.Year:
					{
						months = 0;
						days = 0;
						hours = 0;
						minutes = 0;
						seconds = 0;
						milliseconds = 0;
						break;
					}

				case RelativeTime.Month:
					{
						days = 0;
						hours = 0;
						minutes = 0;
						seconds = 0;
						milliseconds = 0;
						break;
					}

				case RelativeTime.Week:
				case RelativeTime.Day:
					{
						hours = 0;
						minutes = 0;
						seconds = 0;
						milliseconds = 0;
						break;
					}

				case RelativeTime.Hour:
					{
						minutes = 0;
						seconds = 0;
						milliseconds = 0;
						break;
					}

				case RelativeTime.Minute:
					{
						seconds = 0;
						milliseconds = 0;
						break;
					}

				case RelativeTime.Second:
					{
						milliseconds = 0;
						break;
					}
			}

			// contruct base time.
			time = new DateTime(years, months, days, hours, minutes, seconds, milliseconds);

			// adjust to beginning of week.
			if (BaseTime == RelativeTime.Week && time.DayOfWeek != DayOfWeek.Sunday)
			{
				time = time.AddDays(-((int)time.DayOfWeek));
			}

			// add offsets.
			foreach (TimeOffset offset in Offsets)
			{
				switch (offset.Type)
				{
					case RelativeTime.Year: { time = time.AddYears(offset.Value); break; }
					case RelativeTime.Month: { time = time.AddMonths(offset.Value); break; }
					case RelativeTime.Week: { time = time.AddDays(offset.Value * 7); break; }
					case RelativeTime.Day: { time = time.AddDays(offset.Value); break; }
					case RelativeTime.Hour: { time = time.AddHours(offset.Value); break; }
					case RelativeTime.Minute: { time = time.AddMinutes(offset.Value); break; }
					case RelativeTime.Second: { time = time.AddSeconds(offset.Value); break; }
				}
			}

			// return resolved time.
			return time;
		}

		/// <summary>
		/// Returns a String that represents the current Object.
		/// </summary>
		/// <returns>A String that represents the current Object.</returns>
		public override string ToString()
		{
			if (!IsRelative)
			{
				return ConvertToString(m_absoluteTime);
			}

			StringBuilder buffer = new StringBuilder(256);

			buffer.Append(BaseTypeToString(BaseTime));
			buffer.Append(Offsets.ToString());

			return buffer.ToString();
		}

		public static string ConvertToString(object source)
		{
			// check for null
			if (source == null) return "";

			System.Type type = source.GetType();

			// check for invalid values in date times.
			if (type == typeof(DateTime))
			{
				if (((DateTime)source) == DateTime.MinValue)
				{
					return String.Empty;
				}

				DateTime date = (DateTime)source;

				if (date.Millisecond > 0)
				{
					return date.ToString("yyyy-MM-dd HH:mm:ss.fff");
				}
				else
				{
					return date.ToString("yyyy-MM-dd HH:mm:ss");
				}
			}

			// use only the local name for qualified names.
			if (type == typeof(XmlQualifiedName))
			{
				return ((XmlQualifiedName)source).Name;
			}

			// use only the name for system types.
			if (type.FullName == "System.RuntimeType")
			{
				return ((System.Type)source).Name;
			}

			// treat byte arrays as a special case.
			if (type == typeof(byte[]))
			{
				byte[] bytes = (byte[])source;

				StringBuilder buffer = new StringBuilder(bytes.Length * 3);

				for (int ii = 0; ii < bytes.Length; ii++)
				{
					buffer.Append(bytes[ii].ToString("X2"));
					buffer.Append(" ");
				}

				return buffer.ToString();
			}

			// show the element type and length for arrays.
			if (type.IsArray)
			{
				return String.Format("{0}[{1}]", type.GetElementType().Name, ((Array)source).Length);
			}

			// instances of array are always treated as arrays of objects.
			if (type == typeof(Array))
			{
				return String.Format("Object[{0}]", ((Array)source).Length);
			}

			// default behavoir.
			return source.ToString();
		}

		/// <summary>
		/// Parses a string representation of a time.
		/// </summary>
		/// <param name="buffer">The string representation to parse.</param>
		/// <returns>A Time object initailized with the string.</returns>
		public static Time Parse(string buffer)
		{
			// remove trailing and leading white spaces.
			buffer = buffer.Trim();

			Time time = new Time();

			// determine if string is a relative time.
			bool isRelative = false;

			foreach (RelativeTime baseTime in Enum.GetValues(typeof(RelativeTime)))
			{
				string token = BaseTypeToString(baseTime);

				if (buffer.StartsWith(token))
				{
					buffer = buffer.Substring(token.Length).Trim();
					time.BaseTime = baseTime;
					isRelative = true;
					break;
				}
			}

			// parse an absolute time string.
			if (!isRelative)
			{
				time.AbsoluteTime = System.Convert.ToDateTime(buffer).ToUniversalTime();
				return time;
			}

			// parse the offset portion of the relative time.
			if (buffer.Length > 0)
			{
				time.Offsets.Parse(buffer);
			}

			return time;
		}

		#region Private Members
		/// <summary>
		/// Converts a base time to a string token.
		/// </summary>
		/// <param name="baseTime">The base time value to convert.</param>
		/// <returns>The string token representing the base time.</returns>
		private static string BaseTypeToString(RelativeTime baseTime)
		{
			switch (baseTime)
			{
				case RelativeTime.Now: { return "NOW"; }
				case RelativeTime.Second: { return "SECOND"; }
				case RelativeTime.Minute: { return "MINUTE"; }
				case RelativeTime.Hour: { return "HOUR"; }
				case RelativeTime.Day: { return "DAY"; }
				case RelativeTime.Week: { return "WEEK"; }
				case RelativeTime.Month: { return "MONTH"; }
				case RelativeTime.Year: { return "YEAR"; }
			}

			throw new ArgumentOutOfRangeException("baseTime", baseTime.ToString(), "Invalid value for relative base time.");
		}
		#endregion

		#region Private Members
		private DateTime m_absoluteTime = DateTime.MinValue;
		private RelativeTime m_baseTime = RelativeTime.Now;
		private TimeOffsetCollection m_offsets = new TimeOffsetCollection();
		#endregion
	}
}
