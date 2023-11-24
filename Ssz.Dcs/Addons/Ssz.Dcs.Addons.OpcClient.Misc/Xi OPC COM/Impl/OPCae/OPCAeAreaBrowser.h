/**********************************************************************
*
 * Copyright © 2009, 2010, 2011, 2012 OPC Foundation, Inc. 
*
* All binaries built with the "OPC .NET 3.0 (WCF Edition)" source code 
* are subject to the terms of the Express Interface Public License (Xi-PL).
* See http://www.opcfoundation.org/License/Xi-PL/
 *
* The source code itself is also covered by the Xi-PL except the source code 
* cannot be redistributed in its original or modified form unless
* it has been incorporated into a product or system sold by an OPC Foundation 
* member that adds value to the codebase. 
*
* You must not remove this notice, or any other, from this software.
*
*********************************************************************/

#pragma once

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;

using namespace Xi::OPC::COM::API;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	public ref class COPCAeAreaBrowser
		: public IDisposable
		, public IOPCEventAreaBrowserCli
	{
	public:
		COPCAeAreaBrowser(IOPCEventAreaBrowser *pIOPCEventAreaBrowser);
		~COPCAeAreaBrowser();

	private:
		!COPCAeAreaBrowser();
		bool DisposeThis(bool isDisposing);

	public:
		// IOPCEventAreaBrowser
		virtual cliHRESULT ChangeBrowsePosition (
			/*[in]*/ cliOPCAEBROWSEDIRECTION dwBrowseDirection,
			/*[in]*/ String^ sString );
		virtual cliHRESULT BrowseOPCAreas (
			/*[in]*/ cliOPCAEBROWSETYPE dwBrowseFilterType,
			/*[in]*/ String^ sFilterCriteria,
			/*[out]*/ cliIEnumString^ %iEnumAreaNames );
		virtual cliHRESULT GetQualifiedAreaName (
			/*[in]*/ String^ sAreaName,
			/*[out]*/ String^ %sQualifiedAreaName );
		virtual cliHRESULT GetQualifiedSourceName (
			/*[in]*/ String^ sSourceName,
			/*[out]*/ String^ %sQualifiedSourceName );

	private:
		bool m_bHasBeenDisposed;
		IOPCEventAreaBrowser *m_pIOPCEventAreaBrowser;
	};

}}}}
