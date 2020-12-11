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

using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// <para>Objects of this class are used to associate additional error 
	/// information with a data value transferred in the DataValueArrays. 
	/// It is related to the value by the ClientAlias.  Only one ErrorInfo 
	/// object for a given ClientAlias may be present in the DataValuesArray.</para>
	/// <para>The presence of this object for a data value is indicated by the 
	/// value of the AdditionalDetailDesc property of the StatusCode FlagsByte.  
	/// When the value of the AdditionalDetailDesc property is set to 7, an 
	/// ErrorInfo object with an HResult must be present in the ErrorInfo list 
	/// of the DataValueArrays.</para>
	/// <para>Alternatively, if the Context has been opened with ContextOptions 
	/// set to DebugErrorMessages using the Initiate() or ReInitiate() methods, then 
	/// the server may enter ErrorInfo objects with an ErrorMessage string into the 
	/// ErrorInfo list of the DataValueArrays. In this case, the AdditionalDetailDesc 
	/// property does not have to indicate that the ErrorInfo object is present.  
	/// Further, the ErrorMessage string cannot be used unless ContextOptions is 
	/// set to DebugErrorMessages.</para>
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ErrorInfo
	{
		#region Data Members
		/// <summary>
		/// The ClientAlias of a value transferred in the DataValueArrays.  
		/// </summary>
		[DataMember] public uint ClientAlias { get; private set; }

		/// <summary>
		/// The ServerAlias of the value transfered in the DataValueArrays.
		/// </summary>
		[DataMember] public uint ServerAlias { get; private set; }

		/// <summary>
		/// <para>The HResult associated with the value. This capability is provided 
		/// to support additional HResult Facility Codes not defined by the
		/// XiStatusCodeAdditionalDetailType enumeration.  </para>
		/// <para>When present, and ContextOptions is not set to DebugErrorMessages 
		/// for the Context, the presence of an ErrorInfo object with a valid HResult 
		/// is specified using the AdditionalDetailDesc property (value = 7) of the 
		/// StatusCode FlagsByte.  In this case,this HResult must have a valid non-zero 
		/// HResult value.</para>
		/// <para>When the presence of the ErrorInfo object is not specified 
		/// by the AdditionalDetailDesc property, this HResult may be zero.  This 
		/// case may exist when ContextOptions is set to DebugErrorMessages for 
		/// the Context.</para>
		/// </summary>
		[DataMember] public uint[]? HResult { get; private set; }

		/// <summary>
		/// The description of the error. ErrorMessage may only be used when 
		/// the Context has been opened with ContextOptions set to DebugErrorMessages 
		/// using either the Initiate() or ReInitiate() method.
		/// </summary>
		[DataMember] public string[]? ErrorMessage { get; private set; }
		#endregion // Data Members

		#region Constructors
		/// <summary>
		/// This constructor initializes an ErrorInfo object with 
		/// the ClientAlias and HResult.  
		/// </summary>
		/// <param name="hResult">A valid non-zero HResult value. </param>
		/// <param name="clientAlias">The Client Alias of the associated value.</param>
		/// <param name="serverAlias">The Server Alias of the associated value.</param>
		public ErrorInfo(uint hResult, uint clientAlias, uint serverAlias)
		{
			ClientAlias = clientAlias;
			ServerAlias = serverAlias;
			HResult = new uint[1] { hResult };
			ErrorMessage = null;
		}

		/// <summary>
		/// This constructor initializes an ErrorInfo object with 
		/// the ClientAlias and ErrorMessage.  This constructor
		/// may only be used when when the Context has been opened with 
		/// ContextOptions set to DebugErrorMessages using either the 
		/// Initiate() or ReInitiate() method.
		/// </summary>
		/// <param name="errorMessage">A non-empty text description of the error.</param>
		/// <param name="clientAlias">The Client Alias of the associated value.</param>
		/// <param name="serverAlias">The Server Alias of the associated value.</param>
		public ErrorInfo(string errorMessage, uint clientAlias, uint serverAlias)
		{
			ClientAlias = clientAlias;
			ServerAlias = serverAlias;
			HResult = null;
			ErrorMessage = new string[1] { errorMessage };
		}

		/// <summary>
		/// This constructor initializes an ErrorInfo object with the 
		/// ClientAlias, HResult, and ErrorMessage.  This constructor may 
		/// only be used with a non-empty ErrorMessage when the Context 
		/// was opened with ContextOptions set to DebugErrorMessages.
		/// </summary>
		/// <param name="hResult">A valid non-zero HResult value.</param>
		/// <param name="errorMessage">The text description of the error.</param>
		/// <param name="clientAlias">The Client Alias of the associated value.</param>
		/// <param name="serverAlias">The Server Alias of the associated value.</param>
		public ErrorInfo(uint hResult, string errorMessage, uint clientAlias, uint serverAlias)
			: this(hResult, clientAlias, serverAlias)
		{
			ErrorMessage = new string[1] { errorMessage };
		}
		#endregion // Constructors
	}
}