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

// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once

#ifndef STRICT
#define STRICT
#endif

#include "targetver.h"

#define _ATL_APARTMENT_THREADED
#define _ATL_NO_AUTOMATIC_NAMESPACE

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit

#include "resource.h"
#include <atlbase.h>
#include <atlcom.h>
#include <atlctl.h>

#include <vector>

using namespace ATL;

/* ############################################################################
#import "opccomn_ps.dll" exclude("IEnumGUID", "IEnumString", "IEnumUnknown", \
         "IConnectionPointContainer", "IEnumConnectionPoints", \
         "IConnectionPoint", "IEnumConnections", "tagCONNECTDATA")\
         raw_interfaces_only, raw_native_types, no_namespace, named_guids 
############################################################################ */
#include "opccomn_ps.tlh"

/* ############################################################################
#import "opcproxy.dll" exclude("_FILETIME", "IEnumString")\
         raw_interfaces_only, raw_native_types, no_namespace, named_guids 
############################################################################ */
#include "opcproxy.tlh"

/* ############################################################################
#import "opchda_ps.dll" exclude("_FILETIME", "IEnumString")\
         raw_interfaces_only, raw_native_types, no_namespace, named_guids 
############################################################################ */
#include "opchda_ps.tlh"

/* ############################################################################
#import "opc_aeps.dll" exclude("_FILETIME", "IEnumString")\
         raw_interfaces_only, raw_native_types, no_namespace, named_guids 
############################################################################ */
#include "opc_aeps.tlh"

#ifdef USO
#import "uso_opc_access.tlb" exclude ("_FILETIME")
#endif // USO