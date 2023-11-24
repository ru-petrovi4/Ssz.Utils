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

namespace Xi.OPC.COM.API
{
	/// <summary>
	/// 
	/// </summary>
	public interface IOPCCommonCli
		: IDisposable
	{
		cliHRESULT SetLocaleID(
			/*[in]*/ uint dwLcid);
		cliHRESULT GetLocaleID(
			/*[out]*/ out uint dwLcid);
		cliHRESULT QueryAvailableLocaleIDs(
			/*[out]*/ out List<uint> dwLcid);
		cliHRESULT GetErrorString(
			/*[in]*/ cliHRESULT dwError,
			/*[out]*/ out string errString);
		cliHRESULT SetClientName(
			/*[in]*/ string zName);
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCShutdown
	{
		public delegate void ShutdownRequest(
			/*[in]*/ string sReason);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IAdviseOPCShutdownCli
	{
		cliHRESULT AdviseShutdownRequest(
			/*[in]*/ OPCShutdown.ShutdownRequest shutdownRequest);

		cliHRESULT UnadviseShutdownRequest(
			/*[in]*/ OPCShutdown.ShutdownRequest shutdownRequest);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCServerListCli
	{
		cliHRESULT EnumClassesOfCategories(
			/*[in]*/ uint cImplemented,
			/*[in]*/ Guid rgcatidImpl,
			/*[in]*/ uint cRequired,
			/*[in]*/ Guid rgcatidReq,
			/*[out]*/ out List<Guid> Clsids);
		cliHRESULT GetClassDetails(
			/*[in]*/ Guid clsid,
			/*[out]*/ out string sProgID,
			/*[out]*/ out string sUserType);
		cliHRESULT CLSIDFromProgID(
			/*[in]*/ string sProgId,
			/*[out]*/ out Guid clsid);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCServerList2Cli
	{
		cliHRESULT EnumClassesOfCategories(
			/*[in]*/ uint cImplemented,
			/*[in]*/ Guid rgcatidImpl,
			/*[in]*/ uint cRequired,
			/*[in]*/ Guid rgcatidReq,
			/*[out]*/ out List<Guid> Clsids);
		cliHRESULT GetClassDetails(
			/*[in]*/ Guid clsid,
			/*[out]*/ out string sProgID,
			/*[out]*/ out string sUserType,
			/*[out]*/ out string sVerIndProgID);
		cliHRESULT CLSIDFromProgID(
			/*[in]*/ string sProgId,
			/*[out]*/ out Guid clsid);
	}
}
