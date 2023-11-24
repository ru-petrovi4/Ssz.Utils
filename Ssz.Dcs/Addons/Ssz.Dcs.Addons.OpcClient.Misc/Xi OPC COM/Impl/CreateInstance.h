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
using namespace System::Runtime::InteropServices;

using namespace Xi::OPC::COM::API;
using namespace Xi::Contracts::Data;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	// This class provides a set of static methods used initialize COM
	// (if not already done) and to create the OPC COM servers as needed.
	public ref class CCreateInstance
	{
	public:
		CCreateInstance(void);
		// Call this method to initialize COM prior to using any COM components!
		static cliHRESULT InitializeCOM();

		// Call this method to create an instance of an OPC A&E COM Server.
		static cliHRESULT CreateInstanceAE( String^ progID, 
			IOPCEventServerCli^ %iOpcEventServer, ServerDescription^ serverDescription );

		static cliHRESULT CreateInstanceAE_Clsid(
			String^ clsid,
			IOPCEventServerCli^ %iOpcEventServer,
			ServerDescription^ serverDescription);

		// Call this method to create an instance of an OPC A&E COM Server.
		static cliHRESULT CreateInstanceAE( String^ hostName, String^ progID, 
			IOPCEventServerCli^ %iOpcEventServer, ServerDescription^ serverDescription );

		static cliHRESULT CreateInstanceAE_Clsid(String^ hostName, String^ clsid,
			IOPCEventServerCli^ %iOpcEventServer, ServerDescription^ serverDescription);

		// Call this method to create an instance of an OPC DA COM Server.
		static cliHRESULT CreateInstanceDA( String^ progID, 
			IOPCServerCli^ %iOpcServer, ServerDescription^ serverDescription );
		static cliHRESULT CreateInstanceDA_Clsid(String^ clsid,
			IOPCServerCli^ %iOpcServer, ServerDescription^ serverDescription);

		// Call this method to create an instance of an OPC DA COM Server.
		static cliHRESULT CreateInstanceDA( String^ hostName, String^ progID, 
			IOPCServerCli^ %iOpcServer, ServerDescription^ serverDescription );
		static cliHRESULT CreateInstanceDA_Clsid(String^ hostName, String^ clsid,
			IOPCServerCli^ %iOpcServer, ServerDescription^ serverDescription);

		// Call this method to create an instance of an OPC HDA COM Server.
		static cliHRESULT CreateInstanceHDA( String^ progID, 
			IOPCHDA_ServerCli^ %iOpcHda_Server, ServerDescription^ serverDescription );
		static cliHRESULT CreateInstanceHDA_Clsid(String^ clsid,
			IOPCHDA_ServerCli^ %iOpcHda_Server, ServerDescription^ serverDescription);

		// Call this method to create an instance of an OPC HDA COM Server.
		static cliHRESULT CreateInstanceHDA( String^ hostName, String^ progID, 
			IOPCHDA_ServerCli^ %iOpcHda_Server, ServerDescription^ serverDescription );
		static cliHRESULT CreateInstanceHDA_Clsid(String^ hostName, String^ clsid,
			IOPCHDA_ServerCli^ %iOpcHda_Server, ServerDescription^ serverDescription);

#ifdef USO
		// Call this method to create an instance of an Unisim HDA COM Server.
		static cliHRESULT CreateInstanceUsoHDA(String^ progID,
			IOPCServerCli ^opcDAServer,
			IOPCHDA_ServerCli^ %iOpcHda_Server, ServerDescription^ serverDescription);
		static cliHRESULT CreateInstanceUsoHDA_Clsid(String^ clsid,
			IOPCServerCli ^opcDAServer,
			IOPCHDA_ServerCli^ %iOpcHda_Server, ServerDescription^ serverDescription);

		// Call this method to create an instance of an Unisim HDA COM Server.
		static cliHRESULT CreateInstanceUsoHDA(String^ hostName, String^ progID,
			IOPCServerCli ^opcDAServer,
			IOPCHDA_ServerCli^ %iOpcHda_Server, ServerDescription^ serverDescription);
		static cliHRESULT CreateInstanceUsoHDA_Clsid(String^ hostName, String^ clsid,
			IOPCServerCli ^opcDAServer,
			IOPCHDA_ServerCli^ %iOpcHda_Server, ServerDescription^ serverDescription);
#endif

	private:
		static cliHRESULT M_HR = E_FAIL;
		static bool M_bComSetupDone = false;
	};

}}}}
