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

#pragma once

using namespace System;
using namespace Xi::OPC::COM::API;
using namespace Xi::Contracts::Data;
using namespace Xi::Common::Support;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	namespace CHelper
	{
		// General C++ Helper Functions

		// This function is used to convert from a managed string
		// to a LPWSTR for used by unmanaged code.  When using
		// this function be sure to free the allocated string.
		LPWSTR ConvertStringToLPWSTR( String^ pSource );

		// Use this method to convert from an unmanaged GUID to a managed Guid
		Guid ConvertGUIDToGuid(GUID& guid);
		// And this to convert from managed Guid to unmanaged GUID
		GUID ConvertFromGuidToGUID(Guid& guid);

		// Use this method to convert from a COM VARIANT to a cliVARIANT
		cliVARIANT ConvertFromVARIANT( VARIANT *variant );
		cliVARIANT ConvertFromVARIANTdefaultDouble( VARIANT *variant );
		cliVARIANT ConvertFromVARIANTdefaultUint( VARIANT *variant );

		// Use this method to convert from a cliVARIANT to a COM VARIANT
		void ConvertToVARIANT( cliVARIANT cliVariant, VARIANT * pVariant);

		// Used to convert from a .NET OPCHDA_TIME to a C++ tagOPCHDA_TIME
		void cliHdaTimeToHdaTime(OPCHDA_TIME ^ cliHdaTime, tagOPCHDA_TIME * hdaTime);

		// used to convert from a C++ tagOPCHDA_TIME to a .NET OPCHDA_TIME
		void HdaTimeToCliHdaTime(tagOPCHDA_TIME * hdaTime, OPCHDA_TIME ^% cliHdaTime);

		// used to determine the which array a VARIANT should be placed in.
		TransportDataType GetTransportDataType(VARTYPE vt);

		// use this to get the CLSID from a remote machine
		HRESULT CLSIDFromProgID( String^ machine, String^ progID, CLSID &pCLSID );
	}

}}}}
