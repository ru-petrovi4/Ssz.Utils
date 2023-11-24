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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

using Xi.Contracts.Data;

namespace Xi.Common.Support
{
	/// <summary>
	/// Define a delegate to provide for result code lookups in general.
	/// </summary>
	/// <param name="resultCode"></param>
	/// <returns></returns>
	public delegate IEnumerable<RequestedString> LookupResultCodes(IEnumerable<uint> resultCodes);

	/// <summary>
	/// This class is used to lookup error codes
	/// </summary>
	public class FaultStrings
	{
		protected readonly Dictionary<uint, string> _errorCodesToStringDictionary;
		private static FaultStrings _instance;

		/// <summary>
		/// Internal constructor which creates the default set if the server does not supply them
		/// </summary>
		protected FaultStrings()
		{
			Debug.Assert(null == _instance);
			_errorCodesToStringDictionary = new Dictionary<uint, string>();

			_instance = this;
			StreamReader sr = null;
			Stream stm = null;
			try
			{
				using (stm = Assembly.GetAssembly(typeof(FaultStrings))
					.GetManifestResourceStream("Xi.Common.Support.ErrorCodes.xml"))
				{
					if (null != stm)
					{
						using (sr = new StreamReader(stm))
						{
							this.LoadDictionary(sr);
						}
					}
					sr = null;
				}
				stm = null;

				using (stm = Assembly.GetAssembly(typeof(FaultStrings))
					.GetManifestResourceStream("Xi.Common.Support.ErrorCodesOpc.xml"))
				{
					if (null != stm)
					{
						using (sr = new StreamReader(stm))
						{
							this.LoadDictionary(sr);
						}
					}
					sr = null;
				}
				stm = null;
			}
			catch { }
			finally
			{
				if (stm != null)
					stm.Dispose();
				if (sr != null)
					sr.Dispose();
			}
		}

		private void LoadDictionary(StreamReader sr)
		{
			string id;
			string text;
			try
			{
				XElement root = XElement.Parse(sr.ReadToEnd());
				foreach (var err in root.Elements("error"))
				{
					id = err.Attribute("id").Value;
					text = err.Attribute("text").Value;
					_errorCodesToStringDictionary.Add(
						ParseErrorNumber(err.Attribute("id").Value),
						err.Attribute("text").Value);
				}
			}
			catch (Exception ex)
			{
				string msg = ex.Message;
				Debug.Assert(null == ex);
			}
		}

		/// <summary>
		/// Parses out error codes and allows for both integer and hexidecimal varieties.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static uint ParseErrorNumber(string value)
		{
			if (value != null)
			{
				string val = value.Trim();
				bool isHex = false;
				if (val.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
				{
					isHex = true;
					val = val.Substring(2);
				}
				if (!isHex && val.IndexOfAny("ABCDEFabcdef".ToCharArray()) >= 0)
					isHex = true;

				uint uiValue;
				if (uint.TryParse(val, ((isHex) ? NumberStyles.HexNumber : NumberStyles.Integer),
					CultureInfo.InvariantCulture, out uiValue))
					return uiValue;
			}
			throw new FormatException(
				string.Format("Improper Error Code Formatting [{0}]. Expected numeric or hex number.", value));
		}

		/// <summary>
		/// Retrieves the error text for a given error code.
		/// </summary>
		/// <param name="errorCode">Error Code</param>
		/// <returns>Text Message</returns>
		public static string Get(uint errorCode)
		{
			return Get(errorCode, null);
		}

		/// <summary>
		/// Retrieves the error text for a given error code.
		/// </summary>
		/// <param name="errorCode">Error Code</param>
		/// <param name="lookUpResultCodes">Delegate to lookup an error code</param>
		/// <returns>Text Message</returns>
		public static string Get(uint errorCode, LookupResultCodes lookUpResultCodes)
		{
			if (_instance == null)
				_instance = new FaultStrings();

			string msg;

			// First find out if this is a Xi Message defined in ErrorCodes.xml
			if (_instance._errorCodesToStringDictionary.TryGetValue(errorCode, out msg))
				return msg;

			// Second check for a Look Up Result Code delegate.
			// If present use the delegate to find the error code.
			if (null != lookUpResultCodes)
			{
				msg = GetResultCodeString(errorCode, lookUpResultCodes);
				if (!string.IsNullOrEmpty(msg))
				{
					_instance._errorCodesToStringDictionary.Add(errorCode, msg);
					return msg;
				}
			}

			// Third find out if this is a Win32 Error Code
			// NOTE: If the Win32 Exception does not find the error code then it produces a generic message.
			System.ComponentModel.Win32Exception winExcep =
				new System.ComponentModel.Win32Exception(unchecked((int)errorCode));
			msg = winExcep.Message;
			if (!string.IsNullOrEmpty(msg))
			{
				_instance._errorCodesToStringDictionary.Add(errorCode, msg);
				return msg;
			}

			return string.Format("Unknown Fault Code 0x{0:X}", errorCode);
		}

		/// <summary>
		/// This helper method may be used to lookup a single error message string.
		/// </summary>
		/// <param name="resultCode"></param>
		/// <returns></returns>
		public static string GetResultCodeString(uint resultCode, LookupResultCodes lookupResultCodes)
		{
			List<uint> errList = new List<uint>();
			errList.Add(resultCode);
			IEnumerable<RequestedString> iEnumRequestedStrings = lookupResultCodes(errList);
			foreach (var requestedString in iEnumRequestedStrings)
			{
				return requestedString.String;
			}
			return null;
		}
	}
}
