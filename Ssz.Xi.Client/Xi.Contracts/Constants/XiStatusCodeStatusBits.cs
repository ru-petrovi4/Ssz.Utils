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

namespace Xi.Contracts.Constants
{
	/// <summary>
	/// <para>The Status Bits are the high-order 6 bits of the high-order byte (the StatusByte) of the XiStatus 
	/// code as defined in Xi.Contracts.Data.XiStatusCode.  The high-order two bits of the Status Bits are the 
	/// Status Group bits. They indicate whether a value is good, bad, or uncertain and when bad, whether or 
	/// not the bad status was assigned by the server. Server assigned bad status codes are typically assigned 
	/// when the server is unable to retrieve the value from the underlying system.</para>  
	/// <para>The four bits following the two Status Group bits indicate the reason associated with the Status 
	/// Group bits value.  The final two bits of the StatusByte are the limit bits. Their values are defined 
	/// in Xi.Contracts.Contstants.XiStatusCodeLimitBits.  This layout is summarized as follows.</para>
	/// <para>GGRRRRLL, where </para>
	/// <para>    GG     = Status Group Bits</para>
	/// <para>    RRRR   = Reason Bits </para>
	/// <para>    LL     = Limit Bits </para>
	/// <para>In the value definitions for the StatusBits, GG values are individually defined, but the 
	/// Reason Bit values are defined in combination with the Group Bits as GGRRRR. The Limit Bits are 
	/// separately defined in Xi.Contracts.Contstants.XiStatusCodeLimitBits.</para>
	/// <para>This class defines values for the 2-bit Status Group Bits, the 6-bit Status Bits, and 
	/// additional values for their manipulation, such as bit masks and shift values.</para>
	/// <para>In general there are two definitions for each 2-bit Status Group Bits value and each 6-bit 
	/// Status Bits value.  One value is the hex representation of the two-bit value or the six-bit value. 
	/// The other is the hex representation in the context of the full 32-bit Xi Status Code. For example,
	/// the GG bits for the Uncertain value are 01. Therefore, the two definitions for it are: </para>
	/// <para>StatusCodeStatusGroupUncertain = 0x1,  // two-bit value</para>
	/// <para>StatusCodeStatusGroupUncertainBits = 0x40000000, // 32-bit value</para>
	/// <para>The BadServerAccess values are used to indicate that the Xi server was unable to access 
	/// the data object value from the underlying OPC Classic server or from the underlying system.</para>
	/// <para>In general an HRESULT value of SUCCEEDED(hr) returned by the OPC Classic server indicates 
	/// successful access of the data object. In these cases, the Xi server would normally use the OPC 
	/// Quality returned with the data object value to construct the Xi Status Code.</para>
	/// <para>However, there can be HRESULT values that are SUCCEEDED(hr), but that indicates that the OPC 
	/// Classic server was unable to access the data object, and where the OPC Quality returned is not useful. 
	/// In these cases, the Xi server can construct an Xi Status Code that indicates BAD SERVER ACCESS. 
	/// BAD SERVER ACCESS can also be constructed by the Xi Server when the returned HRESULT indicates 
	/// failure.</para>
	/// <para>See StatusCode in both Xi Common Support and Xi OPC Com API for additional 
	/// details on the encoding of HRESULT values.</para>
	/// </summary>
	public enum XiStatusCodeStatusBits : uint
	{
		#region Masks and ShiftCounts
		/// <summary>
		/// Mask used to obtain the full Status Byte
		/// </summary>
		StatusByteMask = 0xFF000000,

		/// <summary>
		/// The mask for the Status Bits (the high-order six bits) of the Xi Status Code.
		/// </summary>
		StatusCodeStatusMask = 0xFC000000,

		/// <summary>
		/// The StatusCodeShiftCount is used to shift the Status Bits (the high-order six bits) 
		/// of the Xi Status Code uint to its low-order six bits, or to initially set the Status 
		/// Bits value into a uint that is to become the Xi Status Code and then shift them to 
		/// the high-order six bits.
		/// </summary>
		StatusCodeShiftCount = 26,

		/// <summary>
		/// Mask used to keep the shifted sub status code bits
		/// </summary>
		SubStatusBitsShiftedMask = 0x0000000F,

		/// <summary>
		/// The StatusCodeStatusByteShiftCount is used to shift the Status Byte (the high-order 
		/// byte) of the Xi Status Code uint to its low-order byte, or to initially set the 
		/// Status Byte value into a uint that is to become the Xi Status Code and then shift 
		/// them to the high-order byte. Note that the Xi Status Byte uses the same bit pattern 
		/// as the low order byte of the OPC DA Quality so it can be copied the Xi Status Code 
		/// uint and then shifted to the proper byte position.
		/// </summary>
		StatusCodeStatusByteShiftCount = 24,

		/// <summary>
		/// The mask for the Status Group Bits (the high order two bits) of the Xi Status Code.
		/// </summary>
		StatusCodeStatusGroupMask = 0xC0000000,

		/// <summary>
		/// The mask for the SubStatus bits of the Xi Status Code. The substatus bits are the 
		/// four bits that follow the Status Group Bits (the two high order bits).
		/// </summary>
		StatusCodeSubstatusBitsMask = 0x3C000000,

		/// <summary>
		/// The StatusCodeStatusGroupShiftCount is used to shift the Status Group Bits (the high-order 
		/// two bits) of the Xi Status Code uint to its low-order two bits, or to initially set the Status 
		/// Group Bits value into a uint that is to become the Xi Status Code and then shift them to 
		/// the high-order two bits.
		/// </summary>
		StatusCodeStatusGroupShiftCount = 30,

		#endregion // Masks and ShiftCounts

		#region Status Group Bits

		/// <summary>
		/// The 2-bit value for bad status. This value can be used to test for bad 
		/// status after shifting the Status Group bits of a Xi Status Code uint to its 
		/// low-order two bits. It can also be used to set the high-order two bits 
		/// to indicate bad status by setting the Xi Status Code uint to this value and 
		/// then shifting it to the high-order two bits.
		/// </summary>
		StatusCodeStatusGroupBad = 0x0,

		/// <summary>
		/// The 32-bit value for bad status. This value can be used to test for 
		/// bad status after masking off the lower 30 bits of the Xi Status Code. 
		/// It can also be used when constructing a bad status code by setting the 
		/// Xi Status Code uint to this value.
		/// </summary>
		StatusCodeStatusGroupBadBits = 0x00000000,

		/// <summary>
		/// The 2-bit value for uncertain status. This value can be used to test for uncertain 
		/// status after shifting the Status Group bits of a Xi Status Code uint to its 
		/// low-order two bits. It can also be used to set the high-order two bits 
		/// to indicate uncertain status by setting the Xi Status Code uint to this value and 
		/// then shifting it to the high-order two bits.
		/// </summary>
		StatusCodeStatusGroupUncertain = 0x1,

		/// <summary>
		/// The 32-bit value for uncertain status. This value can be used to test for 
		/// uncertain status after masking off the lower 30 bits of the Xi Status Code. 
		/// It can also be used when constructing an uncertain status code by setting the 
		/// Xi Status Code uint to this value.
		/// </summary>
		StatusCodeStatusGroupUncertainBits = 0x40000000,

		/// <summary>
		/// The 2-bit value for bad server access status. This value can be used to test for 
		/// bad server access status after shifting the Status Group bits of a Xi Status Code 
		/// uint to its low-order two bits. It can also be used to set the high-order two bits 
		/// to indicate bad server access status by setting the Xi Status Code uint to this 
		/// value and then shifting it to the high-order two bits.  Bad server access indicates 
		/// that the Xi server was unable to access the underlying data source (e.g. OPC DA server) 
		/// for the value.
		/// </summary>
		StatusCodeStatusGroupServerBad = 0x2,

		/// <summary>
		/// The 32-bit value for bad server access status. This value can be used to test for 
		/// bad server access status after masking off the lower 30 bits of the Xi Status Code. 
		/// It can also be used when constructing a bad server access status code by setting the 
		/// Xi Status Code uint to this value.  Bad server access indicates that the Xi server 
		/// was unable to access the underlying data source (e.g. OPC DA server) for the value.
		/// </summary>
		StatusCodeStatusGroupServerBadBits = 0x80000000,

		/// <summary>
		/// The 2-bit value for good status. This value can be used to test for good status 
		/// after shifting the Status Group bits of a Xi Status Code uint to its low-order two bits. 
		/// It can also be used to set the high-order two bits to indicate good status by setting 
		/// the Xi Status Code uint to this value and then shifting it to the high-order two bits.  
		/// </summary>
		StatusCodeStatusGroupGood = 0x3,

		/// <summary>
		/// The 32-bit value for good status. This value can be used to test for good status after
		/// masking off the lower 30 bits of the Xi Status Code. 
		/// It can also be used when constructing a good status code by setting the Xi Status Code 
		/// uint to this value.  
		/// </summary>
		StatusCodeStatusGroupGoodBits = 0xC0000000,

		#endregion // Status Group Bits

		#region Bad Status Bits

		/// <summary>
		/// The value is bad but no specific reason is known.
		/// Use Bad Non Specific when the value is in the low order bits.
		/// </summary>
		BadNonSpecific = 0x00,

		/// <summary>
		/// The value is bad but no specific reason is known.
		/// Use Bad Non Specific Bits when the value is in the Xi defined high order bits.
		/// </summary>
		BadNonSpecificBits = 0x00000000,

		/// <summary>
		/// There is some server specific problem with the 
		/// configuration. For example the item in question has 
		/// been deleted from the configuration.
		/// Use Bad Config Error when the value is in the low order bits.
		/// </summary>
		BadConfigError = 0x01,

		/// <summary>
		/// There is some server specific problem with the 
		/// configuration. For example the item in question has 
		/// been deleted from the configuration.
		/// Use Bad Config Error Bits when the value is in the Xi define high order bits.
		/// </summary>
		BadConfigErrorBits = 0x04000000,

		/// <summary>
		/// The input is required to be logically connected to 
		/// something but is not. This quality may reflect that no 
		/// value is available at this time, for reasons like the 
		/// value may have not been provided by the data source.
		/// Use Bad Not Connected when the value is in the low order bits.
		/// </summary>
		BadNotConnected = 0x02,

		/// <summary>
		/// The input is required to be logically connected to 
		/// something but is not. This quality may reflect that no 
		/// value is available at this time, for reasons like the 
		/// value may have not been provided by the data source.
		/// Use Bad Not Connected Bits when the value is in the Xi define high order bits.
		/// </summary>
		BadNotConnectedBits = 0x08000000,

		/// <summary>
		/// A device failure has been detected.
		/// Use Bad Device Fallure when the value is in the low order bits.
		/// </summary>
		BadDeviceFailure = 0x03,

		/// <summary>
		/// A device failure has been detected.
		/// Use Bad Device Failure Bits when the value is in the Xi define high order bits.
		/// </summary>
		BadDeviceFailureBits = 0x0C000000,

		/// <summary>
		/// A sensor failure had been detected (the ’Limits’ field 
		/// can provide additional diagnostic information in some 
		/// situations).
		/// Use Bad Sensor Fallure when the value is in the low order bits.
		/// </summary>
		BadSensorFailure = 0x04,

		/// <summary>
		/// A sensor failure had been detected (the ’Limits’ field 
		/// can provide additional diagnostic information in some 
		/// situations).
		/// Use Bad Sensor Failure Bits when the value is in the Xi define high order bits.
		/// </summary>
		BadSensorFailureBits = 0x10000000,

		/// <summary>
		/// Communications have failed. However, the last known value 
		/// is available. Note that the ‘age’ of the value may be 
		/// determined from its timestamp.
		/// Use Bad Last Known Value when the value is in the low order bits.
		/// </summary>
		BadLastKnownValue = 0x05,

		/// <summary>
		/// Communications have failed. However, the last known value 
		/// is available. Note that the ‘age’ of the value may be 
		/// determined from its timestamp.
		/// Use Bad Last Known Value Bits when the value is in the Xi define high order bits.
		/// </summary>
		BadLastKnownValueBits = 0x14000000,

		/// <summary>
		/// Communications have failed. There is no last known 
		/// value available.
		/// Use Bad Comm Fallure when the value is in the low order bits.
		/// </summary>
		BadCommFailure = 0x06,

		/// <summary>
		/// Communications have failed. There is no last known 
		/// value available.
		/// Use Bad Comm Failure Bits when the value is in the Xi define high order bits.
		/// </summary>
		BadCommFailureBits = 0x18000000,

		/// <summary>
		/// The block is off scan or otherwise locked. This code 
		/// is also used when the Monitored Item or Subscription
		/// is disabled.
		/// Use Bad Out of Service when the value is in the low order bits.
		/// </summary>
		BadOutOfService = 0x07,

		/// <summary>
		/// The block is off scan or otherwise locked. This code 
		/// is also used when the Monitored Item or Subscription
		/// is disabled.
		/// Use Bad Out of Service Bits when the value is in the Xi define high order bits.
		/// </summary>
		BadOutOfServiceBits = 0x1C000000,

		/// <summary>
		/// After Items are added to a group, it may take some time 
		/// for the server to actually obtain values for these items. 
		/// In such cases the client might perform a read (from cache), 
		/// or establish a ConnectionPoint based subscription and/or 
		/// execute a Refresh on such a subscription before the values 
		/// are available. This substatus is only available from 
		/// OPC DA 3.0 or newer servers.
		/// Use Bad Waiting For Initial Data when the value is in the low order bits.
		/// </summary>
		BadWaitingForInitialData = 0x08,

		/// <summary>
		/// After Items are added to a group, it may take some time 
		/// for the server to actually obtain values for these items. 
		/// In such cases the client might perform a read (from cache), 
		/// or establish a ConnectionPoint based subscription and/or 
		/// execute a Refresh on such a subscription before the values 
		/// are available. This substatus is only available from 
		/// OPC DA 3.0 or newer servers.
		/// Use Bad Waiting For Initial Data when the value is in the Xi define high order bits.
		/// </summary>
		BadWaitingForInitialDataBits = 0x20000000,

		#endregion // Bad Status Bits

		#region Uncertain Status Bits

		/// <summary>
		/// There is no specific reason why the value is uncertain.
		/// Use Uncertain Non Specific when the value is in the low order bits.
		/// </summary>
		UncertainNonSpecific = 0x10,

		/// <summary>
		/// There is no specific reason why the value is uncertain.
		/// Use Uncertain Non Specific Bits when the value is in the Xi define high order bits.
		/// </summary>
		UncertainNonSpecificBits = 0x40000000,

		/// <summary>
		/// Whatever was writing this value has stopped doing so. The 
		/// returned value should be regarded as ‘stale’. Note that this 
		/// differs from a BAD value with Substatus = Last Known Value. 
		/// That status is associated specifically with a detectable 
		/// communications error on a ‘fetched’ value. This error is 
		/// associated with the failure of some external source to ‘put’ 
		/// something into the value within an acceptable period of time. 
		/// Note that the ‘age’ of the value can be determined from 
		/// the timestamp. 
		/// Use Uncertain Last Usable Value when the value is in the low order bits.
		/// </summary>
		UncertainLastUsableValue = 0x11,

		/// <summary>
		/// Whatever was writing this value has stopped doing so. The 
		/// returned value should be regarded as ‘stale’. Note that this 
		/// differs from a BAD value with Substatus = Last Known Value. 
		/// That status is associated specifically with a detectable 
		/// communications error on a ‘fetched’ value. This error is 
		/// associated with the failure of some external source to ‘put’ 
		/// something into the value within an acceptable period of time. 
		/// Note that the ‘age’ of the value can be determined from 
		/// the timestamp. 
		/// Use Uncertain Last Usable Value Bits when the value is in the Xi define high order bits.
		/// </summary>
		UncertainLastUsableValueBits = 0x44000000,

		/// <summary>
		/// Either the value has ‘pegged’ at one of the sensor limits 
		/// (in which case the limit field should be set to LowLimited 
		/// or HighLimited) or the sensor is otherwise known to be out 
		/// of calibration via some form of internal diagnostics (in 
		/// which case the limit field should be NotLimited). 
		/// Use Uncertain Sensor Not Accurate when the value is in the low order bits.
		/// </summary>
		UncertainSensorNotAccurate = 0x14,

		/// <summary>
		/// Either the value has ‘pegged’ at one of the sensor limits 
		/// (in which case the limit field should be set to LowLimited 
		/// or HighLimited) or the sensor is otherwise known to be out 
		/// of calibration via some form of internal diagnostics (in 
		/// which case the limit field should be NotLimited). 
		/// Use Uncertain Sensor Not Accurate Bits when the value is in the Xi define high order bits.
		/// </summary>
		UncertainSensorNotAccurateBits = 0x50000000,

		/// <summary>
		/// The returned value is outside the limits defined for this 
		/// parameter. Note that in this case (per the Fieldbus 
		/// Specification) the ‘Limits’ field indicates which limit 
		/// has been exceeded but does NOT necessarily imply that the 
		/// value cannot move farther out of range. 
		/// Use Uncertain Engineering Units Exceeded when the value is in the low order bits.
		/// </summary>
		UncertainEngineeringUnitsExceeded = 0x15,

		/// <summary>
		/// The returned value is outside the limits defined for this 
		/// parameter. Note that in this case (per the Fieldbus 
		/// Specification) the ‘Limits’ field indicates which limit 
		/// has been exceeded but does NOT necessarily imply that the 
		/// value cannot move farther out of range. 
		/// Use Uncertain Engineering Units Exceeded Bits when the value is in the Xi define high order bits.
		/// </summary>
		UncertainEngineeringUnitsExceededBits = 0x54000000,

		/// <summary>
		/// The value is derived from multiple sources and has less 
		/// than the required number of Good sources.
		/// Use Uncertain Sub Normal when the value is in the low order bits.
		/// </summary>
		UncertainSubNormal = 0x16,

		/// <summary>
		/// The value is derived from multiple sources and has less 
		/// than the required number of Good sources.
		/// Use Uncertain Sub Normal Bits when the value is in the Xi define high order bits.
		/// </summary>
		UncertainSubNormalBits = 0x58000000,

		#endregion // Uncertain Status Bits

		#region Bad Server Access Status Bits

		/// <summary>
		/// Deprecated.
		/// The value is bad but no specific reason is known.
		/// </summary>
		BadServerAccessNonSpecific = 0x20,

		/// <summary>
		/// Deprecated.
		/// The format of the InstanceId is not valid. 
		/// </summary>
		BadServerAccessInstanceIdInvalid = 0x21,

		/// <summary>
		/// Deprecated.
		/// The InstanceId refers to a object that could not be found.
		/// </summary>
		BadServerAccessObjectUnknown = 0x22,

		/// <summary>
		/// Deprecated.
		/// The InstanceId refers to element of an object and that 
		/// element could not be found.         
		/// </summary>
		BadServerAccessObjectElementUnknown = 0x23,

		/// <summary>
		/// Deprecated.
		/// Access to the value was denied.
		/// {The Additional Detail Value must be zero.}
		/// <para>*** Encode Win32 Access Denied as Xi Status Code 0x98000005 ***</para>
		/// </summary>
		BadServerAccessAccessDenied = 0x24,

		/// <summary>
		/// This bit pattern is used for not transformed HRESULT / NTSTATUS / Win32 codes.
		/// </summary>
		BadServerAccessNonSpecificBits = 0x80000000,

		/// <summary>
		/// In general HRESULT value that are SUCCEEDED(hr) are 
		/// not encoded as Bad Server Access.  It is assumed that 
		/// such values are usable and should be encoded using 
		/// one either a good or uncertain quality.  Allowing 
		/// these to be encoded here is done for completeness only.  
		/// Any use a SUCCEEDED(hr) is considered a deviation from 
		/// the Xi Specification.
		/// This value represents a SUCCEEDED(hr) S R C N bits clear.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAccessWithSuccessHResult = 0x24,

		/// <summary>
		/// In general HRESULT value that are SUCCEEDED(hr) are 
		/// not encoded as Bad Server Access.  It is assumed that 
		/// such values are usable and should be encoded using 
		/// one either a good or uncertain quality.  Allowing 
		/// these to be encoded here is done for completeness only.  
		/// Any use a SUCCEEDED(hr) is considered a deviation from 
		/// the Xi Specification.
		/// This value represents a SUCCEEDED(hr) S R C N bits clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessWithSuccessHResultBits = 0x90000000,

		/// <summary>
		/// This value represents a FAILED(hr) S bit set with the R C N bits clear.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAccessHResultFailNTStatusWarning = 0x25,

		/// <summary>
		/// This value represents a FAILED(hr) S bit set with the R C N bits clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessHResultFailNTStatusWarningBits = 0x94000000,

		/// <summary>
		/// In general Win32 NTSTATUS value with a severity of 
		/// Success are not encoded as Bad Server Access.  It is 
		/// assumed that such values are usable and should be 
		/// encoded using one either a good or uncertain quality.  
		/// Allowing these to be encoded here is done for completeness 
		/// only.  Any use a Success severity is considered a deviation 
		/// from the Xi Specification.
		/// This value represents a Win32 status with N bit set and S R C bits clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessNTStatusInformational = 0x26,

		/// <summary>
		/// In general Win32 NTSTATUS value with a severity of 
		/// Success are not encoded as Bad Server Access.  It is 
		/// assumed that such values are usable and should be 
		/// encoded using one either a good or uncertain quality.  
		/// Allowing these to be encoded here is done for completeness 
		/// only.  Any use a Success severity is considered a deviation 
		/// from the Xi Specification.
		/// This value represents a Win32 status with N bit set and S R C bits clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessSuccessNTStatusInfoBits = 0x98000000,

		/// <summary>
		/// In general Win32 NTSTATUS value with a severity of 
		/// Informational are not encoded as Bad Server Access.  It is 
		/// assumed that such values are usable and should be 
		/// encoded using one either a good or uncertain quality.  
		/// Allowing these to be encoded here is done for completeness 
		/// only.  Any use a Informational severity is considered a deviation 
		/// from the Xi Specification.
		/// This value represents a Win32 status with N R bits set and S C bits clear.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAccessNTStatusError = 0x27,

		/// <summary>
		/// In general Win32 NTSTATUS value with a severity of 
		/// Informational are not encoded as Bad Server Access.  It is 
		/// assumed that such values are usable and should be 
		/// encoded using one either a good or uncertain quality.  
		/// Allowing these to be encoded here is done for completeness 
		/// only.  Any use a Informational severity is considered a deviation 
		/// from the Xi Specification.
		/// This value represents a Win32 status with N R bits set and S C bits clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessNTStatusErrorBits = 0x9C000000,

		/// <summary>
		/// This value represents a Win32 status with S N bits set and R C bits clear.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAccessHResultNTStatusSuccess = 0x28,

		/// <summary>
		/// This value represents a Win32 status with S N bits set and R C bits clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessHResultNTStatusSuccessBits = 0xA0000000,

		/// <summary>
		/// This value represents a Win32 status with S R N bits set and C bit clear.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAccessHResultNTStatusInfo = 0x29,

		/// <summary>
		/// This value represents a Win32 status with S R N bits set and C bit clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessHResultNTStatusInfoBits = 0xA4000000,

		/// <summary>
		/// This value represents a SUCCEEDED(hr) C bit set and S R N bits clear.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAccessHResultNTStatusWarning = 0x2A,

		/// <summary>
		/// This value represents a SUCCEEDED(hr) C bit set and S R N bits clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessHResultNTStatusWarningBits = 0xA8000000,

		/// <summary>
		/// This value represents a FAILED(hr) S C bits set with the R N bits clear.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAccessHResultNTStatusError = 0x2B,

		/// <summary>
		/// This value represents a FAILED(hr) S C bits set with the R N bits clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessHResultNTStatusErrorBits = 0xAC000000,

		/// <summary>
		/// This value represents a Win32 status with N C bits set and S R bits clear.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAccessCustNTStatusSuccess = 0x2C,

		/// <summary>
		/// This value represents a Win32 status with N C bits set and S R bits clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessCustNTStatusSuccessBits = 0xB0000000,

		/// <summary>
		/// This value represents a Win32 status with R C N bits set and S bit clear.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAccessCustNTStatusInfo = 0x2D,

		/// <summary>
		/// This value represents a Win32 status with R C N bits set and S bit clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessCustNTStatusInfoBits = 0xB4000000,

		/// <summary>
		/// This value represents a Win32 status with S N C bits set and R bit clear.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAcccessCustNTStatusWarning = 0x2E,

		/// <summary>
		/// This value represents a Win32 status with S N C bits set and R bit clear.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAcccessCustNTStatusWarningBits = 0xB8000000,

		/// <summary>
		/// This value represents a Win32 status with S R N C bits set.
		/// See Xi Common Support StatusCode class.
		/// </summary>
		BadServerAccessCustNTStatusError = 0x2F,

		/// <summary>
		/// This value represents a Win32 status with S R N C bits set.
		/// See Xi Common Support StatusCode class.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		BadServerAccessCustNTStatusErrorBits = 0xBC000000,

		#endregion // Bad Server Status Bits

		#region Good Status Bits

		/// <summary>
		/// The value is good. This is the general mask for all good values.
		/// Use Good Non Specific when the value is in the low order bits.
		/// </summary>
		GoodNonSpecific = 0x30,

		/// <summary>
		/// The value is good. This is the general mask for all good values.
		/// Use Good Non Specific Bits when the value is in the Xi define high order bits.
		/// </summary>
		GoodNonSpecificBits = 0xC0000000,

		/// <summary>
		/// The value has been Overridden. Typically this is means the 
		/// input has been disconnected and a manually entered value has 
		/// been written to data object.
		/// Use Good Local Overrid when the value is in the low order bits.
		/// </summary>
		GoodLocalOverride = 0x36,

		/// <summary>
		/// The value has been Overridden. Typically this is means the 
		/// input has been disconnected and a manually entered value has 
		/// been written to data object.
		/// Use Good Local Overrid Bits when the value is in the Xi define high order bits.
		/// </summary>
		GoodLocalOverrideBits = 0xD8000000,

		#endregion // Good Status Bits

	}

	/// <summary>
	/// This enumeration defines the unsigned integer values for the high order two bits of the 
	/// Xi Status Code.  These two bits are referred to as the StatusCodeStatusGroup.
	/// The values are given as two bit values from 0 to 3.
	/// </summary>
	public enum XiStatusCodeGroups : uint
	{
		/// <summary>
		/// The 2-bit value for bad status. This value can be used to test for bad 
		/// status after shifting the Status Group bits of a Xi Status Code uint to its 
		/// low-order two bits. It can also be used to set the high-order two bits 
		/// to indicate bad status by setting the Xi Status Code uint to this value and 
		/// then shifting it to the high-order two bits.
		/// </summary>
		StatusCodeStatusGroupBad = 0x0,

		/// <summary>
		/// The 2-bit value for uncertain status. This value can be used to test for uncertain 
		/// status after shifting the Status Group bits of a Xi Status Code uint to its 
		/// low-order two bits. It can also be used to set the high-order two bits 
		/// to indicate uncertain status by setting the Xi Status Code uint to this value and 
		/// then shifting it to the high-order two bits.
		/// </summary>
		StatusCodeStatusGroupUncertain = 0x1,

		/// <summary>
		/// The 2-bit value for bad server access status. This value can be used to test for 
		/// bad server access status after shifting the Status Group bits of a Xi Status Code 
		/// uint to its low-order two bits. It can also be used to set the high-order two bits 
		/// to indicate bad server access status by setting the Xi Status Code uint to this 
		/// value and then shifting it to the high-order two bits.  Bad server access indicates 
		/// that the Xi server was unable to access the underlying data source (e.g. OPC DA server) 
		/// for the value.
		/// </summary>
		StatusCodeStatusGroupServerBad = 0x2,

		/// <summary>
		/// The 2-bit value for good status. This value can be used to test for good status 
		/// after shifting the Status Group bits of a Xi Status Code uint to its low-order two bits. 
		/// It can also be used to set the high-order two bits to indicate good status by setting 
		/// the Xi Status Code uint to this value and then shifting it to the high-order two bits.  
		/// </summary>
		StatusCodeStatusGroupGood = 0x3,
	}

	/// <summary>
	/// This enumeration defines value that are helpful 
	/// while encoding and decoding HRESULT codes and
	/// Win32 error codes.
	/// See Xi Common Support StatusCode class.
	/// See http://msdn.microsoft.com/en-us/library/cc231196(v=PROT.10).aspx 
	/// </summary>
	public enum HResultBitCodes : uint
	{
		/// <summary>
		/// Number of bit positions to shift the error type encoding
		/// </summary>
		ShiftEncodingBits = 28,

		/// <summary>
		/// Mask for the four encoding bits
		/// </summary>
		ShiftedEncodingMask = 0x0000000F,

		/// <summary>
		/// HRESULT Failed bit
		/// </summary>
		Failed = 0x80000000,

		/// <summary>
		/// HRESULT Reserved bit - Used in encoding Win32 error into an HRESULT
		/// </summary>
		Reserved = 0x40000000,

		/// <summary>
		/// HRESULT Customer bit - Used to indicate that this is an application defined error code
		/// </summary>
		Customer = 0x20000000,

		/// <summary>
		/// HRESULT NTStatus bit - Used to indicate that this HRESULT represents a Win32 error code
		/// </summary>
		NTStatus = 0x10000000,

		/// <summary>
		/// Win32 Severity Code mask - The top two bits of a Win32 error code generally represents the sevrity
		/// </summary>
		NTStatusSeverityMask = 0xC0000000,

		/// <summary>
		/// Win32 Severity Code low order bit
		/// </summary>
		NTStatusSeverityBit0 = 0x40000000,

		/// <summary>
		/// Win32 Severity Code high order bit
		/// </summary>
		NTStatusSeverityBit2 = 0x80000000,

		/// <summary>
		/// The mask for the Facility code of an HRESULT
		/// NOTE: Two bits take from Facilty Codes.
		/// </summary>
		FacilityMask = 0x03FF0000,

		/// <summary>
		/// The mask for the low 16 bits of the HRESULT that provides the specifics of the error
		/// </summary>
		CodeMask = 0x0000FFFF,

		/// <summary>
		/// This maks is used to keep the combined Facility and Code
		/// NOTE: Two bits take from Facilty Codes.
		/// </summary>
		FacilityAndCodeMask = 0x03FFFFFF,

		/// <summary>
		/// The mask for the high-order four bits of an HRESULT
		/// </summary>
		EncodingMask = 0xF0000000,

		/// <summary>
		/// HRESULT represents a SUCCEEDED(hr) after applying the EncodingMask
		/// </summary>
		EncodingHResultSuccessNTStatusSuccess = 0x00000000,

		/// <summary>
		/// HRESULT represents a FAILED(hr) after applying the EncodingMask
		/// </summary>
		EncodingHResultFailNTStatusInfo = 0x80000000,

		/// <summary>
		/// HRESULT represents a severity Informational Win32 error code after applying the EncodingMask
		/// </summary>
		EncodingNTStatusInformational = 0x40000000,

		/// <summary>
		/// HRESULT represents a severity Warning Win32 error code after applying the EncodingMask
		/// </summary>
		EncodingNTStatusWarning = 0x80000000,

		/// <summary>
		/// HRESULT represents a severity Error Win32 error code after applying the EncodingMask
		/// </summary>
		EncodingNTStatusError = 0xC0000000,

		/// <summary>
		/// HRESULT represents a Customer SUCCEEDED(hr) after applying the EncodingMask
		/// </summary>
		EncodingCustSuccessHResult = 0x20000000,
	}

}
