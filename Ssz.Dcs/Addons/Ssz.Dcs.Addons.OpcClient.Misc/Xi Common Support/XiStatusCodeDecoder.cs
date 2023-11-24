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

using Xi.Contracts.Constants;

namespace Xi.Common.Support
{
	/// <summary>
	/// The Xi Status code is made up of several parts as follows.
	///  Intel bit numbering
	///   3 3 2 2 2 2 2 2   2 2 2 2 1 1 1 1   1 1 1 1 1 1
	///   1 0 9 8 7 6 5 4   3 2 1 0 9 8 7 6   5 4 3 2 1 0 9 8   7 6 5 4 3 2 1 0
	///   Xi Documentation bit numbering
	///   3 3 2 2 2 2 2 2   2 2 2 2 2 1 1 1   1 1 1 1 1 1 1
	///   2 1 0 9 8 7 6 5   4 3 2 1 0 9 8 7   6 5 4 3 2 1 0 9   8 7 6 5 4 3 2 1
	/// Bit patter for Status Groups "Bad", "Uncertain" and "Good"
	/// -------------------------------------------------------------------------
	/// | S S B B B B L L | V V V N C A A A | D D D D D D D D | D D D D D D D D |
	/// -------------------------------------------------------------------------
	/// Bit pattern for Status Group "Bad Server Access"
	/// -------------------------------------------------------------------------
	/// | S S B B B B 0 0 | X F F F F F F F | D D D D D D D D | D D D D D D D D |
	/// -------------------------------------------------------------------------
	///
	///   S S = Status Group (Bits)
	///       B B B B = Status Detail
	///   0 0 = Bad
	///       0 0 0 0 = Non Specific
	///       0 0 0 1 = Configuration Error
	///       0 0 1 0 = Not Connected
	///       0 0 1 1 = Device Failure
	///       0 1 0 0 = Sensor Failure
	///       0 1 0 1 = Last Known Value
	///       0 1 1 0 = Communications Failure
	///       0 1 1 1 = Out of Service
	///       1 0 0 0 = Waiting for Initial Data Value
	///
	///   0 1 = Uncertain
	///       0 0 0 0 = Non Specific
	///       0 0 0 1 = Usable Value
	///       0 1 0 0 = Sensor Not Accurate
	///       0 1 0 1 = Engineering Units Exceeded
	///       0 1 1 0 = Sub Normal
	///
	///   1 0 = Bad Server Access {Only failures where the value is not usable.}
	///       0 0 0 0 = Non Specific {Deprecated}
	///       0 0 0 1 = Instance Id Invalid {Deprecated}
	///       0 0 1 0 = Object Unknown {Deprecated}
	///       0 0 1 1 = Object Element Unknown {Deprecated}
	///       0 1 0 0 = Access Denied {Deprecated} *** Encode this a a Win32 Access Denied *** 0x98000005 ***
	///                 S R C N --- HRESULT
	///                 Sev C N --- NTSTATUS
	///       0 1 0 0 = 0 0 0 0 = 0x90 <-> 0x0 = Success HRESULT & NTSTATUS Success
	///       0 1 0 1 = 1 0 0 0 = 0x94 <-> 0x8 = Fail HRESULT & NTSTATUS Warning
	///       0 1 1 0 = 0 1 0 0 = 0x98 <-> 0x4 = NTSTATUS Informational
	///       0 1 1 1 = 1 1 0 0 = 0x9C <-> 0xC = NTSTATUS Error
	///       1 0 0 0 = 0 0 0 1 = 0xA0 <-> 0x1 = HRESULT with NTSTATUS Success
	///       1 0 0 1 = 0 1 0 1 = 0xA4 <-> 0x5 = HRESULT with NTSTATUS Informaional
	///       1 0 1 0 = 1 0 0 1 = 0xA8 <-> 0x9 = HRESULT with NTSTATUS Warning
	///       1 0 1 1 = 1 1 0 1 = 0xAC <-> 0xD = HRESULT wiht NTSTATUS Error
	///       1 1 0 0 = 0 0 1 0 = 0xB0 <-> 0x2 = Customer Defined Success
	///       1 1 0 1 = 0 1 1 0 = 0xB4 <-> 0x6 = Customer Defined Informational
	///       1 1 1 0 = 1 0 1 0 = 0xB8 <-> 0xA = Customer Defined Warning
	///       1 1 1 1 = 1 1 1 0 = 0xBC <-> 0xE = Customer Defined Error
	///       ******************************************************************************
	///       * When the Status Group is Bad Server Access then the lower 24 Bits are used *
	///       * to define the Facility and Code as defined by Microsoft.                   *
	///       * See http://msdn.microsoft.com/en-us/library/cc231196(v=PROT.10).aspx       *
	///       ******************************************************************************
	///
	///   1 1 = Good
	///       0 0 0 0 = Non Specific (Normal Condition)
	///       0 1 1 0 = Local Override
	///
	///   Additional Status Information (when not Bad Server Access)
	///               L L = Limited Flags
	///               0 0 = Not Limited
	///               0 1 = Low Limited
	///               1 0 = High Limited
	///               1 1 = Constant
	///
	///                     V V V = Additional Historical Status
	///                     0 0 0 = Not Used
	///                     0 0 1 = Raw Value
	///                     0 1 0 = No Value
	///                     0 1 1 = Extra Value
	///                     1 0 0 = Lost Value
	///                     1 0 1 = Interpolated Value
	///                     1 1 0 = Calculated Value
	///                     1 1 1 = Partial Calculated Value
	///
	///                           N = Historical Bounding Value
	///                           0 = No Bounding Value
	///                           1 = Bounding Value Included
	///
	///                             C = Historical Conversion
	///                             0 = No Conversion Error
	///                             1 = Conversion Error
	///
	///                               A A A = Additional Deltals
	///                               0 0 0 = Not Used (No HRESULT value present)
	///                               0 0 1 = Vendor Specific
	///                               0 1 0 = HRESULT (Facility Code 0)
	///                               0 1 1 = Xi HRESULT (Facility Code = 0x777)
	///                               1 0 0 = IO Error Code (Facility Code = 4)
	///                               1 0 1 = ITF Error Code (Facility code = 4)
	///                               1 1 0 = Win32 Error Code (Facility code = 7) *** Changed from 5 ***
	///                               1 1 1 = Additional Error Code
	/// </summary>
	internal static class StatusCodeStatusBits
	{
		internal static readonly HResultBitCodes[] hresultNTStatusCode = {
			HResultBitCodes.EncodingNTStatusError,				// 0x0
			HResultBitCodes.EncodingNTStatusError,				// 0x1
			HResultBitCodes.EncodingNTStatusError,				// 0x2
			HResultBitCodes.EncodingNTStatusError,				// 0x3
			HResultBitCodes.EncodingHResultSuccessNTStatusSuccess,		// 0x4 > 0x90 <-> 0x0 = Success HRESULT & NTSTATUS Success
			HResultBitCodes.EncodingHResultFailNTStatusInfo,			// 0x5 > 0x94 <-> 0x8 = Fail HRESULT & NTSTATUS Warning
			HResultBitCodes.EncodingNTStatusInformational,				// 0x6 > 0x98 <-> 0x4 = NTSTATUS Informational
			HResultBitCodes.EncodingNTStatusError,						// 0x7 > 0x9C <-> 0xC = NTSTATUS Error
			HResultBitCodes.EncodingHResultSuccessNTStatusSuccess | HResultBitCodes.NTStatus,	// 0x8 > 0xA0 <-> 0x1 = HRESULT with NTSTATUS Success
			HResultBitCodes.EncodingNTStatusInformational | HResultBitCodes.NTStatus,			// 0x9 > 0xA4 <-> 0x5 = HRESULT with NTSTATUS Informaional
			HResultBitCodes.EncodingNTStatusWarning | HResultBitCodes.NTStatus,					// 0xA > 0xA8 <-> 0x9 = HRESULT with NTSTATUS Warning
			HResultBitCodes.EncodingNTStatusError | HResultBitCodes.NTStatus,					// 0xB > 0xAC <-> 0xD = HRESULT wiht NTSTATUS Error
			HResultBitCodes.EncodingHResultSuccessNTStatusSuccess | HResultBitCodes.Customer,	// 0xC > 0xB0 <-> 0x2 = Customer Defined Success
			HResultBitCodes.EncodingNTStatusInformational | HResultBitCodes.Customer,			// 0xD > 0xB4 <-> 0x6 = Customer Defined Informational
			HResultBitCodes.EncodingNTStatusWarning | HResultBitCodes.Customer,					// 0xE > 0xB8 <-> 0xA = Customer Defined Warning
			HResultBitCodes.EncodingNTStatusError | HResultBitCodes.Customer,					// 0xF > 0xBC <-> 0xE = Customer Defined Error
		};

		internal static readonly XiStatusCodeStatusBits[] statusCodeStatusBits = {
			XiStatusCodeStatusBits.BadServerAccessWithSuccessHResultBits,			// 0x90 <-> 0x0 = Success HRESULT & NTSTATUS Success
			XiStatusCodeStatusBits.BadServerAccessHResultNTStatusSuccessBits,		// 0xA0 <-> 0x1 = HRESULT with NTSTATUS Success
			XiStatusCodeStatusBits.BadServerAccessCustNTStatusSuccessBits,			// 0xB0 <-> 0x2 = Customer Defined Success
			XiStatusCodeStatusBits.BadServerAccessNonSpecificBits,					// Non supported code
			XiStatusCodeStatusBits.BadServerAccessSuccessNTStatusInfoBits,			// 0x98 <-> 0x4 = NTSTATUS Informational
			XiStatusCodeStatusBits.BadServerAccessHResultNTStatusInfoBits,			// 0xA4 <-> 0x5 = HRESULT with NTSTATUS Informaional
			XiStatusCodeStatusBits.BadServerAccessCustNTStatusInfoBits,				// 0xB4 <-> 0x6 = Customer Defined Informational
			XiStatusCodeStatusBits.BadServerAccessNonSpecificBits,					// Non supported code
			XiStatusCodeStatusBits.BadServerAccessHResultFailNTStatusWarningBits,	// 0x94 <-> 0x8 = Fail HRESULT & NTSTATUS Warning
			XiStatusCodeStatusBits.BadServerAccessHResultNTStatusWarningBits,		// 0xA8 <-> 0x9 = HRESULT with NTSTATUS Warning
			XiStatusCodeStatusBits.BadServerAcccessCustNTStatusWarningBits,			// 0xB8 <-> 0xA = Customer Defined Warning
			XiStatusCodeStatusBits.BadServerAccessNonSpecificBits,					// Non supported code
			XiStatusCodeStatusBits.BadServerAccessNTStatusErrorBits,				// 0x9C <-> 0xC = NTSTATUS Error
			XiStatusCodeStatusBits.BadServerAccessHResultNTStatusErrorBits,			// 0xAC <-> 0xD = HRESULT wiht NTSTATUS Error
			XiStatusCodeStatusBits.BadServerAccessCustNTStatusErrorBits,			// 0xBC <-> 0xE = Customer Defined Error
			XiStatusCodeStatusBits.BadServerAccessNonSpecificBits,					// Non supported code
		};

	}

	/// <summary>
	/// A Value Status Code is associated with a Xi Data Value providing 
	/// information about the status of the value.  This version of the 
	/// Value Status Code is intended primarily for use by an OPC Xi 
	/// client to interpret the meaning of the status value.
	/// </summary>
	public struct XiStatusCodeDecoder
	{
		/// <summary>
		/// As the Xi Status Code is represented as a uint in the Xi Contracts 
		/// this constructor should be sufficient for an Xi client.
		/// </summary>
		/// <param name="statusCode">A Xi Status Code</param>
		public XiStatusCodeDecoder(uint statusCode)
		{
			_statusCode = statusCode;
			_lookUpResultCode = null;
		}

		/// <summary>
		/// As the Xi Status Code is represented as a uint in the Xi Contracts 
		/// this constructor should be sufficient for an Xi client.
		/// </summary>
		/// <param name="statusCode"></param>
		/// <param name="lookUpResultCode"></param>
		public XiStatusCodeDecoder(uint statusCode, LookupResultCodes lookUpResultCode)
			: this(statusCode)
		{
			_lookUpResultCode = lookUpResultCode;
		}

		public uint Status { get { return _statusCode; } }

		/// <summary>
		/// A value is considered good when the Quality Bits specifies "Good".
		/// </summary>
		public bool IsGood
		{
			get
			{
				return (XiStatusCodeStatusBits.StatusCodeStatusGroupGoodBits
							== (XiStatusCodeStatusBits)(_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask));
			}
		}

		/// <summary>
		/// A value is considered uncertain when the Quality Bits specifies the "Uncertain".
		/// </summary>
		public bool IsUncertain
		{
			get
			{
				return (XiStatusCodeStatusBits.StatusCodeStatusGroupUncertainBits
							== (XiStatusCodeStatusBits)(_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask));
			}
		}

		/// <summary>
		/// A value is considered usable when the Quality Bits specify either "Good" or "Uncertain".
		/// </summary>
		public bool IsUsable
		{
			get
			{
				return (   (XiStatusCodeStatusBits.StatusCodeStatusGroupGoodBits
							== (XiStatusCodeStatusBits)(_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask))
						|| (XiStatusCodeStatusBits.StatusCodeStatusGroupUncertainBits
							== (XiStatusCodeStatusBits)(_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask)));
			}
		}

		/// <summary>
		/// A value is considered bad when the Quality Bits specifies the "Bad".
		/// </summary>
		public bool IsBad
		{
			get
			{
				return (XiStatusCodeStatusBits.StatusCodeStatusGroupBadBits
							== (XiStatusCodeStatusBits)(_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask));
			}
		}

		public bool WaitingForInitialValue
		{
			get
			{
				return (XiStatusCodeStatusBits.BadWaitingForInitialDataBits
						  == (XiStatusCodeStatusBits)(_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusMask));
			}
		}

		/// <summary>
		/// When the Quality Bits are "Bad Server Access" the value is not usable.  
		/// An HRESULT is encoded in the status.
		/// The HRESULT may be retrieved from the HRESULT property.
		/// <para>This property returns "true" when the Xi Status Value 
		/// represents an HRESULT encoded in the status.</para>
		/// </summary>
		public bool IsHRESULT
		{
			get
			{
				return (XiStatusCodeStatusBits.StatusCodeStatusGroupServerBadBits
						  == (XiStatusCodeStatusBits)(_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask));
			}
		}

		/// <summary>
		/// A subset of HRESULT values may be included with in a Xi Status Code.  
		/// It should be noted that all HRESULT values that may be included in 
		/// a Xi Status Code are SUCCEEDED(hr).  Any FAILED(hr) the value is 
		/// considered not useable and bad and the HRESULT is encoded in the 
		/// Xi Status Code.
		/// <para>This property returns "true" when an HRESULT is encoded in the status.</para>
		/// </summary>
		public bool HasHRESULT
		{
			get
			{
				return (XiStatusCodeStatusBits.StatusCodeStatusGroupServerBadBits
							== (XiStatusCodeStatusBits)(_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask))
						|| ((XiStatusCodeAdditionalDetailType.NotUsed
								!= (_statusCode & XiStatusCodeAdditionalDetailType.AdditionalDetailTypeMask))
							&& (XiStatusCodeAdditionalDetailType.VendorSpecificDetailBits
								!= (_statusCode & XiStatusCodeAdditionalDetailType.AdditionalDetailTypeMask)));
			}
		}

		/// <summary>
		/// This property will return "true" when Vendor Specific Detail is present in the Xi Status Code.
		/// </summary>
		public bool HasVendorSpecificDetail
		{
			get
			{
				return (XiStatusCodeStatusBits.StatusCodeStatusGroupServerBadBits
							!= (XiStatusCodeStatusBits)(_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask))
						&& (XiStatusCodeAdditionalDetailType.VendorSpecificDetailBits
								== (_statusCode & XiStatusCodeAdditionalDetailType.AdditionalDetailTypeMask));
			}
		}

		/// <summary>
		/// This property will return "true" when the Xi Status Code indicates that the value is not limited.
		/// </summary>
		public bool IsNotLimited
		{
			get
			{
				return (IsHRESULT) ? false : (XiStatusCodeLimitBits.NotLimitedBits
					== (XiStatusCodeLimitBits)(_statusCode & (uint)XiStatusCodeLimitBits.LimitBitsMask));
			}
		}

		/// <summary>
		/// This property will return "true" when the Xi Status Code indicates that the value is low limited.
		/// </summary>
		public bool IsLowLimited
		{
			get
			{
				return (IsHRESULT) ? false : (XiStatusCodeLimitBits.LowLimitedBits
					== (XiStatusCodeLimitBits)(_statusCode & (uint)XiStatusCodeLimitBits.LimitBitsMask));
			}
		}

		/// <summary>
		/// This property will return "true" when the Xi Status Code indicates that the value is high limited.
		/// </summary>
		public bool IsHighLimited
		{
			get
			{
				return (IsHRESULT) ? false : (XiStatusCodeLimitBits.HighLimitedBits
					== (XiStatusCodeLimitBits)(_statusCode & (uint)XiStatusCodeLimitBits.LimitBitsMask));
			}
		}

		/// <summary>
		/// This property will return "true" when the Xi Status Code indicates that the value is constant;
		/// </summary>
		public bool IsConstant
		{
			get
			{
				return (IsHRESULT) ? false : (XiStatusCodeLimitBits.ConstantBits
					== (XiStatusCodeLimitBits)(_statusCode & (uint)XiStatusCodeLimitBits.LimitBitsMask));
			}
		}

		/// <summary>
		/// This property is used to obtain an HRESULT that may be present in the Xi Status Code.
		/// When this method is not able to extract the HRESULT a value of -1 is returned.
		/// </summary>
		public int HRESULT
		{
			get
			{
				int hr = unchecked((int)XiFaultCodes.S_OK);
				if (HasHRESULT)
				{
					if (IsHRESULT)
					{
						int facilityAndCode = unchecked((int)(_statusCode & (uint)HResultBitCodes.FacilityAndCodeMask));
						if (0x03770000 == (_statusCode & (uint)HResultBitCodes.FacilityMask))
							facilityAndCode |= 0x07770000;
						uint idx = (_statusCode >> ((int)XiStatusCodeStatusBits.StatusCodeShiftCount)) & (uint)XiStatusCodeStatusBits.SubStatusBitsShiftedMask;
						hr = ((int)StatusCodeStatusBits.hresultNTStatusCode[idx]) | facilityAndCode;
					}
					else
					{
						int code = unchecked((int)(_statusCode & (uint)XiStatusCodeAdditionalDetailType.AdditionalDetailMask));
						switch (_statusCode & XiStatusCodeAdditionalDetailType.AdditionalDetailTypeMask)
						{
							case XiStatusCodeAdditionalDetailType.NotUsed:
								break;
							case XiStatusCodeAdditionalDetailType.VendorSpecificDetailBits:
								break;
							case XiStatusCodeAdditionalDetailType.DefaultHResultBits:
								hr = code;
								break;
							case XiStatusCodeAdditionalDetailType.XiHResultBits:
								hr = 0x07770000 | code;
								break;
							case XiStatusCodeAdditionalDetailType.IO_ERROR_CODEBits:
								hr = 0x40040000 | code;
								break;
							case XiStatusCodeAdditionalDetailType.ITF_HResultBits:
								hr = 0x00040000 | code;
								break;
							case XiStatusCodeAdditionalDetailType.Win32HResultBits:
								hr = 0x00070000 | code;
								break;
							case XiStatusCodeAdditionalDetailType.AdditionalErrorCodeBits:
								break;
							default:
								break;
						}
					}
				}
				return hr;
			}
		}

		/// <summary>
		/// This property returns the full Status Byte as defined by OPC .NET.
		/// </summary>
		public byte StatusByte
		{
			get
			{
				return (byte)((_statusCode & (uint)XiStatusCodeStatusBits.StatusByteMask)
					>> (int)XiStatusCodeStatusBits.StatusCodeStatusByteShiftCount);
			}
		}

		/// <summary>
		/// This property returns the Xi (OPC DA) status bits. 
		/// </summary>
		public uint StatusBits
		{
			get
			{
				return (uint)((_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusMask)
					>> (int)XiStatusCodeStatusBits.StatusCodeShiftCount);
			}
		}

		/// <summary>
		/// This property returns just the Quality bits (the two high-order bits) from the Xi (OPC DA) status bits.
		/// </summary>
		[Obsolete]
		public XiStatusCodeStatusBits QualityBits
		{
			get
			{
				return (XiStatusCodeStatusBits)((_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask)
					>> (int)XiStatusCodeStatusBits.StatusCodeStatusGroupShiftCount);
			}
		}

		/// <summary>
		/// This property returns just the Status Code Group; the high order two bits of the Xi StatusCode.
		/// </summary>
		public XiStatusCodeGroups StatusCodeGroup
		{
			get
			{
				return (XiStatusCodeGroups)((_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask)
					>> (int)XiStatusCodeStatusBits.StatusCodeStatusGroupShiftCount);
			}
		}

		/// <summary>
		/// This property returns just the unsigned integer value of the 4 Substatus bits of the Xi Status Code.
		/// The substatus bits are the four bits that follow the Status Group Bits (the two high order bits) 
		/// The valid values are 0 to 15.
		/// </summary>
		public uint StatusCodeSubstatusBits
		{
			get
			{
				return (uint)((_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeSubstatusBitsMask)
					>> (int)XiStatusCodeStatusBits.StatusCodeShiftCount);
			}
		}

		/// <summary>
		/// This property returns the Xi (OPC DA) limit bits.
		/// Note that zero is returned if the Xi Status Code represents an HRESULT.
		/// </summary>
		public XiStatusCodeLimitBits LimitBits
		{
			get
			{
				return (XiStatusCodeLimitBits)((IsHRESULT) ? 0 : ((_statusCode & (uint)XiStatusCodeLimitBits.LimitBitsMask)
					>> (int)XiStatusCodeLimitBits.LimitBitsShiftCount));
			}
		}

		/// <summary>
		/// Returns the equivalent of the OPC DA Quality.
		/// Note: 0x0080 is returned if the Xi Status Code is an HRESULT.
		/// </summary>
		public ushort OpcDaQuality
		{
			get
			{
				return (IsHRESULT) ? ((ushort)0x0080) : (ushort)((_statusCode & (uint)XiStatusCodeStatusBits.StatusByteMask)
					>> (int)XiStatusCodeStatusBits.StatusCodeStatusByteShiftCount);
			}
		}

		/// <summary>
		/// Returns the equivalent of the OPC HDA Quality.
		/// Note: 0x0080 is returned if the Xi Status Code is an HRESULT.
		/// </summary>
		public uint OpcHdaQuality
		{
			get
			{
				if (IsHRESULT)
					return 0x00000080;
				uint opcHdaQuality = 0;
				switch (_statusCode & (uint)XiStatusCodeHistoricalValueType.HistoricalValueTypeMask)
				{
					case (uint)XiStatusCodeHistoricalValueType.NotUsed:
						break;

					case (uint)XiStatusCodeHistoricalValueType.RawValueBits:
						opcHdaQuality |= 0x00040000;
						break;

					case (uint)XiStatusCodeHistoricalValueType.NoValueBits:
						opcHdaQuality |= 0x00200000;
						break;

					case (uint)XiStatusCodeHistoricalValueType.ExtraValueBits:
						opcHdaQuality |= 0x00010000;
						break;

					case (uint)XiStatusCodeHistoricalValueType.LostValueBits:
						opcHdaQuality |= 0x00400000;
						break;

					case (uint)XiStatusCodeHistoricalValueType.InterpolatedValueBits:
						opcHdaQuality |= 0x00020000;
						break;

					case (uint)XiStatusCodeHistoricalValueType.CalculatedValueBits:
						opcHdaQuality |= 0x00080000;
						break;

					case (uint)XiStatusCodeHistoricalValueType.PartialCalculatedValueBits:
						opcHdaQuality |= 0x01000000;
						break;

					default:
						break;
				}
				if (0 != (_statusCode & (uint)XiStatusCodeHistoricalValueType.HistoricalNoBoundingFlg))
					opcHdaQuality |= 0x00100000; // OPCHDA_NOBOUND = 0x00100000
				if (0 != (_statusCode & (uint)XiStatusCodeHistoricalValueType.HistoricalConversionErrorFlg))
					opcHdaQuality |= 0x00800000; // OPCHDA_CONVERSION = 0x00800000
				return opcHdaQuality;
			}
		}

		/// <summary>
		/// This property returns the Xi Historical value type.
		/// Note that zero is returned if the Xi Status Code represents an HRESULT.
		/// </summary>
		public XiStatusCodeHistoricalValueType HistoricalValueType
		{
			get
			{
				return (XiStatusCodeHistoricalValueType)((IsHRESULT) ? 0 : ((_statusCode & (uint)XiStatusCodeHistoricalValueType.HistoricalValueTypeMask)
					>> (int)XiStatusCodeHistoricalValueType.HistoricalValueTypeShiftCount));
			}
		}

		/// <summary>
		/// This property returns the Historical Bounding Value flag.
		/// Note that false is returned if the Xi Status Code represents an HRESULT.
		/// </summary>
		public bool HistoricalBoundingValue
		{
			get
			{
				return (IsHRESULT) ? false : (0 != (_statusCode & (uint)XiStatusCodeHistoricalValueType.HistoricalNoBoundingFlg));
			}
		}

		/// <summary>
		/// This property returns the Historical Conversion Error flag.
		/// Note that false is returned if the Xi Status Code represents an HRESULT.
		/// </summary>
		public bool HistoricalConversionError
		{
			get
			{
				return (IsHRESULT) ? false : (0 != (_statusCode & (uint)XiStatusCodeHistoricalValueType.HistoricalConversionErrorFlg));
			}
		}

		/// <summary>
		/// This method decodes the Xi Status into a string for a 
		/// user friendly presentation of the status code. 
		/// </summary>
		/// <returns>A user readable string representing the Xi Status.</returns>
		public string DecodeValueStatus()
		{
			return DecodeValueStatus(null);
		}

		/// <summary>
		/// This method decodes the Xi Status into a string for a 
		/// user friendly presentation of the status code. 
		/// </summary>
		/// <param name="lookUpResultCode">This is a delegate that provides 
		/// a callback to the context LookupResultCodes this allows the 
		/// Fault Strings class to lookup error codes that are not currently known. </param>
		/// <returns>A user readable string representing the Xi Status.</returns>
		public string DecodeValueStatus(LookupResultCodes lookUpResultCode)
		{
			if (null != lookUpResultCode) _lookUpResultCode = lookUpResultCode;

			if (IsHRESULT)
			{
				int hr = HRESULT;
				return FaultStrings.Get(unchecked((uint)hr), _lookUpResultCode);
			}
			else
			{
				if (null == _statusCodesToStringDictionary)
				{
					LoadCodesIntoStringDictionaries();
				}
				StringBuilder sb = new StringBuilder();
				string tmpStr = null;
				if (_statusCodesToStringDictionary.TryGetValue(this.StatusBits, out tmpStr))
				{
					sb.Append(tmpStr);
					if (XiStatusCodeLimitBits.NotLimited != LimitBits)
					{
						if (_limitedCodeToStringDictionary.TryGetValue(LimitBits, out tmpStr))
						{
							sb.Append(", ");
							sb.Append(tmpStr);
						}
						if (HasHRESULT)
						{
							int hr = HRESULT;
							tmpStr = FaultStrings.Get(unchecked((uint)hr), _lookUpResultCode); ;
							sb.Append(", ");
							sb.Append(tmpStr);
						}
					}
				}
				return sb.ToString();
			}
			//return String.Format("0x{0:X}", _statusCode);
		}

		/// <summary>
		/// This method decodes the Xi Status into a string for a 
		/// user friendly presentation of the status code.  This method 
		/// may be used when the value is a historical value with the 
		/// additional information about historical values.
		/// </summary>
		/// <returns>A user readable string representing the Xi Status.</returns>
		public string DecodeHistoryStatus()
		{
			return DecodeValueStatus(null);
		}

		/// <summary>
		/// This method decodes the Xi Status into a string for a 
		/// user friendly presentation of the status code.  This method 
		/// may be used when the value is a historical value with the 
		/// additional information about historical values.
		/// </summary>
		/// <param name="lookUpResultCode">This is a delegate that provides 
		/// a callback to the context LookupResultCodes this allows the 
		/// Fault Strings class to lookup error codes that are not currently known. </param>
		/// <returns>A user readable string representing the Xi Status.</returns>
		public string DecodeHistoryStatus(LookupResultCodes lookUpResultCode)
		{
			if (null != lookUpResultCode) _lookUpResultCode = lookUpResultCode;

			if (IsHRESULT)
			{
				return DecodeValueStatus(lookUpResultCode);
			}
			else
			{
				StringBuilder sb = new StringBuilder(DecodeValueStatus());
				if (XiStatusCodeHistoricalValueType.NotUsed != HistoricalValueType)
				{
					string tmpStr;
					if (_historyCodestoStringDictionary.TryGetValue(HistoricalValueType, out tmpStr))
					{
						sb.Append(", ");
						sb.Append(tmpStr);
					}
					// TODO: What about the bounding and convestion error flags
				}
				return sb.ToString();
			}
			//return String.Format("0x{0:X}", _statusCode);
		}

		/// <summary>
		/// This method loads dictionaries with various status / quality codes and the coresponding strings.
		/// </summary>
		private void LoadCodesIntoStringDictionaries()
		{
			if (null == _statusCodesToStringDictionary)
			{
				_statusCodesToStringDictionary = new Dictionary<uint, string>();
				_limitedCodeToStringDictionary = new Dictionary<XiStatusCodeLimitBits, string>();
				_historyCodestoStringDictionary = new Dictionary<XiStatusCodeHistoricalValueType, string>();

				System.IO.StreamReader sr = null;
				System.IO.Stream stm = null;
				try
				{
					using (stm = System.Reflection.Assembly.GetAssembly(typeof(FaultStrings))
						.GetManifestResourceStream("Xi.Common.Support.ValueQualityCodes.xml"))
					{
						if (null != stm)
						{
							using (sr = new System.IO.StreamReader(stm))
							{
								string id;
								string text;
								try
								{
									System.Xml.Linq.XElement root = System.Xml.Linq.XElement.Parse(sr.ReadToEnd());
									foreach (var quality in root.Elements("statusbits"))
									{
										id = quality.Attribute("id").Value;
										text = quality.Attribute("text").Value;
										_statusCodesToStringDictionary.Add(
											ParseNumber(quality.Attribute("id").Value),
											quality.Attribute("text").Value);
									}
								}
								catch (Exception ex)
								{
									string msg = ex.Message;
								}
							}
							sr.Dispose();
							sr = null;
						}
					}
					stm.Dispose();
					stm = null;

					using (stm = System.Reflection.Assembly.GetAssembly(typeof(FaultStrings))
						.GetManifestResourceStream("Xi.Common.Support.ValueQualityCodes.xml"))
					{
						if (stm != null)
						{
							using (sr = new System.IO.StreamReader(stm))
							{
								string id;
								string text;
								try
								{
									System.Xml.Linq.XElement root = System.Xml.Linq.XElement.Parse(sr.ReadToEnd());
									foreach (var quality in root.Elements("limit"))
									{
										id = quality.Attribute("id").Value;
										text = quality.Attribute("text").Value;
										_limitedCodeToStringDictionary.Add(
											(XiStatusCodeLimitBits)ParseNumber(quality.Attribute("id").Value),
											quality.Attribute("text").Value);
									}
								}
								catch (Exception ex)
								{
									string msg = ex.Message;
								}
							}
							sr.Dispose();
							sr = null;
						}
					}
					stm.Dispose();
					stm = null;

					using (stm = System.Reflection.Assembly.GetAssembly(typeof(FaultStrings))
						.GetManifestResourceStream("Xi.Common.Support.ValueQualityCodes.xml"))
					{
						if (null != stm)
						{
							using (sr = new System.IO.StreamReader(stm))
							{
								string id;
								string text;
								try
								{
									System.Xml.Linq.XElement root = System.Xml.Linq.XElement.Parse(sr.ReadToEnd());
									foreach (var quality in root.Elements("historyDataType"))
									{
										id = quality.Attribute("id").Value;
										text = quality.Attribute("text").Value;
										_historyCodestoStringDictionary.Add(
											(XiStatusCodeHistoricalValueType)ParseNumber(quality.Attribute("id").Value),
											quality.Attribute("text").Value);
									}
								}
								catch (Exception ex)
								{
									string msg = ex.Message;
								}
							}
							sr.Dispose();
							sr = null;
						}
						stm.Dispose();
						stm = null;
					}
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
		}

		/// <summary>
		/// Parses out error codes and allows for both integer and hexidecimal varieties.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static uint ParseNumber(string value)
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
				if (uint.TryParse(val, ((isHex) ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer),
					System.Globalization.CultureInfo.InvariantCulture, out uiValue))
					return uiValue;
			}
			throw new FormatException(
				string.Format("Improper Error Code Formatting [{0}]. Expected numeric or hex number.", value));
		}

		/// <summary>
		/// This override of the To String method will generally convert most Value Status Codes to a user friendly string.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (IsHRESULT)
			{
				int hr = HRESULT;
				return FaultStrings.Get(unchecked((uint)hr), _lookUpResultCode);
			}
			return DecodeValueStatus();
			//return String.Format("0x{0:X}", _statusCode);
		}

		/// <summary>
		/// This version of the To String method will also convert most Value Status Codes to a user friendly string 
		/// with the additional advantage of calling the server to lookup a status code.
		/// </summary>
		/// <param name="lookUpResultCode">This is a delegate that provides for a callback to the context LookupResultCodes 
		/// this allows the Fault Strings class to lookup error codes that are not currently known.  </param>
		/// <returns></returns>
		public string ToString(LookupResultCodes lookUpResultCode)
		{
			if (null != lookUpResultCode) _lookUpResultCode = lookUpResultCode;

			if (IsHRESULT)
			{
				int hr = HRESULT;
				return FaultStrings.Get(unchecked((uint)hr), lookUpResultCode);
			}
			return DecodeValueStatus();
			//return String.Format("0x{0:X}", _statusCode);
		}

		/// <summary>
		/// The Xi Status Code as defined in Xi Contracts Data.
		/// </summary>
		private uint _statusCode;

		/// <summary>
		/// The callback delegate used to look up result codes.
		/// </summary>
		private LookupResultCodes _lookUpResultCode;

		/// <summary>
		/// Dictionary used to convert from quality codes to strings.
		/// </summary>
		private static Dictionary<uint, string> _statusCodesToStringDictionary;

		/// <summary>
		/// Dictionary used to convert from limited codes to strings.
		/// </summary>
		private static Dictionary<XiStatusCodeLimitBits, string> _limitedCodeToStringDictionary;

		/// <summary>
		/// Dictionary used to convert from history codes to strings.
		/// </summary>
		private static Dictionary<XiStatusCodeHistoricalValueType, string> _historyCodestoStringDictionary;
	}

	public static class XiStatusCodeEncoder
	{
		public static uint EncodeFailedHResultToStatusCode(uint hResult)
		{
			System.Diagnostics.Debug.Assert(0 != (0x80000000 & hResult));

			// The HRESULT is FAIL(hr) so just encode this into the Xi Value Status Code
			// See Xi Common Support Value Status Code for encoding details.
			uint idx = (hResult >> ((int)HResultBitCodes.ShiftEncodingBits)) & (uint)HResultBitCodes.ShiftedEncodingMask;
			uint statusCode = (uint)StatusCodeStatusBits.statusCodeStatusBits[idx];
			if ((uint)XiStatusCodeStatusBits.BadServerAccessNonSpecificBits == statusCode)
			{
				// Not support are always retured as E_FAIL
				hResult = XiFaultCodes.E_FAIL;
				statusCode = (uint)XiStatusCodeStatusBits.BadServerAccessHResultFailNTStatusWarningBits;
			}
			statusCode |= (hResult & (uint)HResultBitCodes.FacilityAndCodeMask);
			return statusCode;
		}
	}

}