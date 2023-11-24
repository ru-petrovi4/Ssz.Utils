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

namespace Xi.OPC.COM.API
{

	public struct XiStatusCodeFromOpcCOM
	{
		public XiStatusCodeFromOpcCOM(ushort opcDaQuality, uint hResult)
		{
			_statusCode = (uint)XiStatusCodeStatusBits.BadServerAccessHResultFailNTStatusWarningBits
								| ((uint)HResultBitCodes.FacilityAndCodeMask & XiFaultCodes.E_FAIL);

			if (0 == (hResult & (uint)HResultBitCodes.Failed))
			{
				// The HResult is not failed so this is a usable value
				// Shift the Quality/Substatus and Limit bits into the proper bit position.
				_statusCode = ((uint)opcDaQuality) << (int)XiStatusCodeStatusBits.StatusCodeStatusByteShiftCount;
				if (((uint)cliHR.S_OK) != hResult)
				{
					// The HRESUL was not S_OK so determine if it can be encoded
					switch (hResult & (uint)HResultBitCodes.FacilityMask)
					{
						case 0x00000000:	// FACILITY_NULL
							_statusCode |= XiStatusCodeAdditionalDetailType.DefaultHResultBits;
							_statusCode |= (hResult & (uint)HResultBitCodes.CodeMask);
							break;

						case 0x00040000:	// FACILITY_ITF
							if (0 == (hResult & (uint)HResultBitCodes.NTStatusSeverityBit0))
							{
								// The Win32 severity code is Warning (COM Error)
								_statusCode |= XiStatusCodeAdditionalDetailType.ITF_HResultBits;
								_statusCode |= (hResult & (uint)HResultBitCodes.CodeMask);
							}
							else
							{
								// The Win32 severity code is Error
								_statusCode |= XiStatusCodeAdditionalDetailType.IO_ERROR_CODEBits;
								_statusCode |= (hResult & (uint)HResultBitCodes.CodeMask);
							}
							break;

						case 0x00070000:	// FACILITY_WIN32
							_statusCode |= XiStatusCodeAdditionalDetailType.Win32HResultBits;
							_statusCode |= (hResult & (uint)HResultBitCodes.CodeMask);
							break;

						case 0x03770000:	// Compensate for the lost Facility Bits!
						case 0x07770000:	// Facility Xi
							_statusCode |= XiStatusCodeAdditionalDetailType.XiHResultBits;
							_statusCode |= (hResult & (uint)HResultBitCodes.CodeMask);
							break;

						default:
							_statusCode |= (hResult & (uint)HResultBitCodes.CodeMask);
							break;
					}
				}
			}
			else
			{
				// The HRESULT is FAIL(hr) so just encode this into the Xi Value Status Code
				// See Xi Common Support Value Status Code for encoding details.
				_statusCode = Xi.Common.Support.XiStatusCodeEncoder.EncodeFailedHResultToStatusCode(hResult);
			}
		}


		public XiStatusCodeFromOpcCOM(uint opcHdaQuality, uint hResult)
			: this(((ushort)(opcHdaQuality & 0x0000FFFF)), hResult)
		{
			if (XiStatusCodeStatusBits.StatusCodeStatusGroupServerBadBits != (XiStatusCodeStatusBits)(_statusCode & (uint)XiStatusCodeStatusBits.StatusCodeStatusGroupMask))
			{
				if (0 != (opcHdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_NOBOUND))
				{
					_statusCode |= (uint)XiStatusCodeHistoricalValueType.HistoricalNoBoundingFlg;
				}
				if (0 != (opcHdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_CONVERSION))
				{
					_statusCode |= (uint)XiStatusCodeHistoricalValueType.HistoricalConversionErrorFlg;
				}
				if (0 != (opcHdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_EXTRADATA))
				{
				}
				else if (0 != (opcHdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_INTERPOLATED))
				{
					_statusCode |= (uint)XiStatusCodeHistoricalValueType.InterpolatedValueBits;
				}
				else if (0 != (opcHdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_RAW))
				{
					_statusCode |= (uint)XiStatusCodeHistoricalValueType.RawValueBits;
				}
				else if (0 != (opcHdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_CALCULATED))
				{
				}
				else if (0 != (opcHdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_NODATA))
				{
					_statusCode |= (uint)XiStatusCodeHistoricalValueType.NoValueBits;
				}
				else if (0 != (opcHdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_DATALOST))
				{
					_statusCode |= (uint)XiStatusCodeHistoricalValueType.LostValueBits;
				}
				else if (0 != (opcHdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_PARTIAL))
				{
					_statusCode |= (uint)XiStatusCodeHistoricalValueType.PartialCalculatedValueBits;
				}
			}
		}

		/// <summary>
		/// Property to obtain the status code as a uint as transported by the Xi Contracts.
		/// </summary>
		public uint StatusCode { get { return _statusCode; } }

		/// <summary>
		/// Property to obtain the Status Byte as defined in Xi.Contracts.Data XiStatusCode.
		/// </summary>
		public byte StatusByte { get { return unchecked((byte)((_statusCode >> 24) & 0xFF)); } }

		/// <summary>
		/// Property to obtain the Flags Byte as defined in Xi.Contracts.Data XiStatusCode.
		/// </summary>
		public byte FlagsByte { get { return unchecked((byte)((_statusCode >> 16) & 0xFF)); } }

		/// <summary>
		/// Property to obtain the Additional Detail as defined in Xi.Contracts.Data XiStatusCode.
		/// </summary>
		public ushort AdditionalDetail { get { return unchecked((byte)(_statusCode & 0xFFFF)); } }

		/// <summary>
		/// Holds the 32 Bit Xi Value Status Code -- See Xi Contracts Data Xi Status Code.
		/// </summary>
		private uint _statusCode;

	}
}
