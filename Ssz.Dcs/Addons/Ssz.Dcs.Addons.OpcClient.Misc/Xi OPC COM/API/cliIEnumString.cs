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
	public interface cliIEnumString
		: IDisposable
	{
		cliHRESULT Next(uint celt, out List<string> rgelt);
		cliHRESULT Skip(uint celt);
		cliHRESULT Reset();
		cliHRESULT Clone(out cliIEnumString newIEnumString);
	}
}
