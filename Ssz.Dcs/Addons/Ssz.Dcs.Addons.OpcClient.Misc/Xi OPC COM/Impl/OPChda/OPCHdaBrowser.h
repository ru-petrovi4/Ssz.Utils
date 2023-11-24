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
using namespace System::Collections::Generic;

using namespace Xi::OPC::COM::API;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	public ref class COPCHdaBrowser
		: public IDisposable
		, public IOPCHDA_BrowserCli
	{
	public:
		COPCHdaBrowser(cliHRESULT %HR, ::IOPCHDA_Browser * pIOPCHDA_Browser);
		~COPCHdaBrowser(void);

	private:
		!COPCHdaBrowser(void);
		bool DisposeThis(bool isDisposing);

	public:
		// IOPCHDA_Browser
		virtual cliHRESULT GetEnum( 
			/*[in]*/  OPCHDA_BROWSETYPE dwBrowseType,
			/*[out]*/ cliIEnumString^ %iEnumStrings);
		virtual cliHRESULT ChangeBrowsePosition( 
			/*[in]*/ OPCHDA_BROWSEDIRECTION dwBrowseDirection,
			/*[in]*/ String^ sString);
		virtual cliHRESULT GetItemID( 
			/*[in]*/ String^ sNode,
			/*[out]*/ String^ %sItemID);
		virtual cliHRESULT GetBranchPosition( 
			/*[out]*/String^ %sBranchPos);

	private:
		bool m_bHasBeenDisposed;
		::IOPCHDA_Browser * m_pIOPCHDA_Browser;
	};

}}}}
