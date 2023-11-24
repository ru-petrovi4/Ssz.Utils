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

using Xi.Contracts.Data;
using Xi.Contracts.Constants;

namespace Xi.OPC.COM.API
{
	[System.Obsolete("Use XiStatusCodeFromOpcCOM defined in Xi.OPC.COM.API")]
	public class OpcXiHelpers
	{
		public static uint MakeXiStatusCodeFromDaQuality(uint daQuality, uint hResult)
		{
			byte statusByte = (byte)(daQuality & 0xFF);
			uint facilityCodeMask = 0x7FF0000;
			byte additionalDetailDesc = 0;
			ushort additionalDetail = 0;
			if ((daQuality & 0xFF00) > 0)
			{
				additionalDetailDesc = (byte)XiStatusCodeAdditionalDetailType.VendorSpecificDetail;
				additionalDetail = (ushort)((daQuality & 0xFF00) >> 8);
			}
			else if (hResult > 0)
			{
				if ((hResult & facilityCodeMask) == 0)
					additionalDetailDesc = (byte)XiStatusCodeAdditionalDetailType.DefaultHResult;
				else if ((hResult & facilityCodeMask) == 0x00040000)
				{
					// set IO_ERROR_CODE if the top two bits are set
					if ((daQuality & 0xC0000000) > 0)
						additionalDetailDesc = (byte)XiStatusCodeAdditionalDetailType.IO_ERROR_CODE;
					else
						additionalDetailDesc = (byte)XiStatusCodeAdditionalDetailType.ITF_HResult;
				}
				else if ((hResult & facilityCodeMask) == 0x00070000)
					additionalDetailDesc = (byte)XiStatusCodeAdditionalDetailType.Win32HResult;
				additionalDetail = (ushort)(hResult & 0x0000FFFF);
			}
			byte flagsByte = XiStatusCode.MakeFlagsByte(0, false, false, additionalDetailDesc);
			return XiStatusCode.MakeStatusCode(statusByte, flagsByte, additionalDetail);
		}

		public static uint MakeXiStatusCodeFromHdaQuality(uint hdaQuality)
		{
			byte historicalValueType = (byte)XiStatusCodeHistoricalValueType.NotUsed;
			if ((hdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_EXTRADATA) > 0)
				historicalValueType = (byte)XiStatusCodeHistoricalValueType.ExtraValue;
			else if ((hdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_INTERPOLATED) > 0)
				historicalValueType = (byte)XiStatusCodeHistoricalValueType.InterpolatedValue;
			else if ((hdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_RAW) > 0)
				historicalValueType = (byte)XiStatusCodeHistoricalValueType.RawValue;
			else if ((hdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_CALCULATED) > 0)
				historicalValueType = (byte)XiStatusCodeHistoricalValueType.CalculatedValue;
			else if ((hdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_NODATA) > 0)
				historicalValueType = (byte)XiStatusCodeHistoricalValueType.NoValue;
			else if ((hdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_DATALOST) > 0)
				historicalValueType = (byte)XiStatusCodeHistoricalValueType.LostValue;
			else if ((hdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_PARTIAL) > 0)
				historicalValueType = (byte)XiStatusCodeHistoricalValueType.PartialCalculatedValue;

			bool historicalNoBounding = ((hdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_NOBOUND) > 0);
			bool historicalConversionError = ((hdaQuality & (uint)OPCHDA_QUALITY.OPCHDA_CONVERSION) > 0);

			ushort daQuality = (ushort)(hdaQuality & 0xFFFF);
			byte statusByte = (byte)(daQuality & 0xFF);
			ushort additionalDetail = 0;
			byte additionalDetailDesc = 0;
			if ((daQuality & 0xFF00) > 0)
			{
				additionalDetailDesc = (byte)XiStatusCodeAdditionalDetailType.VendorSpecificDetail;
				additionalDetail = (ushort)((daQuality & 0xFF00) >> 8);
			}

			byte flagsByte = XiStatusCode.MakeFlagsByte(historicalValueType, historicalNoBounding,
											historicalConversionError, additionalDetailDesc);

			return XiStatusCode.MakeStatusCode(statusByte, flagsByte, additionalDetail);
		}
	}
}
