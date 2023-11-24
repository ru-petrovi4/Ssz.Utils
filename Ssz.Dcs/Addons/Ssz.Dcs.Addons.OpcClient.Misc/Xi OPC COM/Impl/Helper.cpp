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

#include "StdAfx.h"
#include "Helper.h"

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Generic;
using namespace Xi::Contracts::Data;
using namespace Xi::OPC::COM::API;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	// ####################################################################
	LPWSTR CHelper::ConvertStringToLPWSTR( String^ pSource )
	{
		IntPtr p = Marshal::StringToHGlobalUni(pSource);
		LPWSTR szSource = static_cast<wchar_t*>(p.ToPointer());

		return szSource;
	}

	// ####################################################################
	Guid CHelper::ConvertGUIDToGuid( GUID& guid )
	{
		return Guid( guid.Data1, guid.Data2, guid.Data3,
			guid.Data4[0], guid.Data4[1],
			guid.Data4[2], guid.Data4[3],
			guid.Data4[4], guid.Data4[5],
			guid.Data4[6], guid.Data4[7]);
	}

	// ####################################################################
	GUID CHelper::ConvertFromGuidToGUID( Guid& guid )
	{
		array<Byte>^ guidData = guid.ToByteArray();
		pin_ptr<Byte> data = &(guidData[0]);

		return *(GUID *)data;
	}

	// ####################################################################
		// Most of the OPC conversions from VARIANT to cliVARIANT are simple
		// data values.  This method provides minimal overhead for the simple
		// conversions.  The default case uses the standard Microsoft
		// marshalling function for all non simple cases.  If additional
		// conversion are needed they may be added to this switch statement.
		//
		// TODO - Add any needed complex or custom conversions from VARIANT
		// to cliVARIANT (a managed Object).
	cliVARIANT CHelper::ConvertFromVARIANT( VARIANT *variant )
	{
		switch (variant->vt)
		{
		case VT_EMPTY :
			return nullptr;
			break;

		case VT_I2 :
			return cliVARIANT( variant->iVal );
			break;

		case VT_I4 :
			return cliVARIANT( variant->lVal );
			break;

		case VT_R4 :
			return cliVARIANT( variant->fltVal );
			break;

		case VT_R8 :
			return cliVARIANT( variant->dblVal );
			break;

		case VT_CY :
			{
				// Values less than 200 years are assumed to be TimeSpans.
				// These are dates prior to January 1, 1801, 12:00 AM.
				if (0xE0395E4F1A0000L > variant->cyVal.int64)
				{
					TimeSpan timeSpan = TimeSpan::FromTicks(variant->cyVal.int64);
					return cliVARIANT( timeSpan );
				}
				DateTime dateTime = DateTime::FromFileTimeUtc(variant->cyVal.int64);
				return cliVARIANT( dateTime );
			} break;

		case VT_DATE :
			{
				SYSTEMTIME systemTime;
				int sCode = ::VariantTimeToSystemTime(variant->date, &systemTime);
				_FILETIME fileTime;
				BOOL bCode = ::SystemTimeToFileTime(&systemTime, &fileTime);
				DateTime dateTime = DateTime::FromFileTimeUtc(*((_int64*)&fileTime));
				return cliVARIANT( dateTime );
			} break;

		case VT_BSTR :
			{
				_bstr_t bStr(variant);
				wchar_t * szStr = (wchar_t*)bStr;
				String^ str = gcnew String(szStr);
				return cliVARIANT(str);
			} break;

		case VT_BOOL :
			return cliVARIANT( (variant->boolVal != 0) ? true : false );
			break;

		case VT_I1 :
			return cliVARIANT( variant->cVal );
			break;

		case VT_UI1 :
			return cliVARIANT( variant->bVal );
			break;

		case VT_UI2 :
			return cliVARIANT( variant->uiVal );
			break;

		case VT_UI4 :
			return cliVARIANT( variant->uintVal );
			break;

		case VT_I8 :
			return cliVARIANT( variant->llVal );
			break;

		case VT_UI8 :
			return cliVARIANT( variant->ullVal );
			break;

		case VT_INT :
			return cliVARIANT( variant->intVal );
			break;

		case VT_UINT :
			return cliVARIANT( variant->uintVal );
			break;

		default :
			System::IntPtr vP = (System::IntPtr)variant;
			System::Object^ objVal = Marshal::GetObjectForNativeVariant(vP);
			return cliVARIANT( objVal );
		}
		return cliVARIANT();
	}

	cliVARIANT CHelper::ConvertFromVARIANTdefaultDouble( VARIANT *variant )
	{
		cliVARIANT rtnValue = ConvertFromVARIANT(variant);
		if (nullptr == rtnValue.DataValue)
		{
			double dbl = 0.0;
			return cliVARIANT( dbl );
		}
		return rtnValue;
	}

	cliVARIANT CHelper::ConvertFromVARIANTdefaultUint( VARIANT *variant )
	{
		cliVARIANT rtnValue = ConvertFromVARIANT(variant);
		if (nullptr == rtnValue.DataValue)
		{
			UINT ulng = 0;
			return cliVARIANT( ulng );
		}
		return rtnValue;
	}


	// ####################################################################
	// bool, byte, sbyte, short, ushort, int, uint, long, ulong, System.IntPtr, char, double, float.
	void CHelper::ConvertToVARIANT( cliVARIANT cliVariant, VARIANT * pVariant)
	{
		::VariantClear(pVariant);
		System::Type^ dataType = cliVariant.GetDataType();
		if (bool::typeid == dataType)
		{
			_variant_t val((bool)cliVariant);
			*pVariant = val.Detach();
			return;
		}
		if (unsigned char::typeid == dataType)
		{
			_variant_t val((unsigned char)cliVariant);
			*pVariant = val.Detach();
			return;
		}
		if (short::typeid == dataType)
		{
			_variant_t val((short)cliVariant);
			*pVariant = val.Detach();
			return;
		}
		if (unsigned short::typeid == dataType)
		{
			_variant_t val((unsigned short)cliVariant);
			*pVariant = val.Detach();
			return;
		}
		if (int::typeid == dataType)
		{
			_variant_t val((int)cliVariant);
			*pVariant = val.Detach();
			return;
		}
		if (unsigned int::typeid == dataType)
		{
			_variant_t val((unsigned int)cliVariant);
			*pVariant = val.Detach();
			return;
		}
		if (long long::typeid == dataType)
		{
			_variant_t val((long long)cliVariant);
			*pVariant = val.Detach();
			return;
		}
		if (unsigned long long::typeid == dataType)
		{
			_variant_t val((unsigned long long)cliVariant);
			*pVariant = val.Detach();
			return;
		}
		if (double::typeid == dataType)
		{
			_variant_t val((double)cliVariant);
			*pVariant = val.Detach();
			return;
		}
		if (float::typeid == dataType)
		{
			_variant_t val((float)cliVariant);
			*pVariant = val.Detach();
			return;
		}
		if (String::typeid == dataType)
		{
			IntPtr p = Marshal::StringToHGlobalUni((System::String^)cliVariant);
			LPWSTR szSource = static_cast<wchar_t*>(p.ToPointer());
			_variant_t vStr(szSource);
			*pVariant = vStr.Detach();
			Marshal::FreeHGlobal((IntPtr)szSource);
			return;
		}

		if (System::DateTime::typeid == dataType)
		{
			pVariant->vt = VT_CY;
			pVariant->cyVal.int64 = ((DateTime)cliVariant).ToFileTimeUtc();
			return;
		}
		if (System::TimeSpan::typeid == dataType)
		{
			pVariant->vt = VT_CY;
			pVariant->cyVal.int64 = ((TimeSpan)cliVariant).Ticks;
			return;
		}

		System::IntPtr vp = (System::IntPtr)pVariant;
		Marshal::GetNativeVariantForObject(cliVariant.DataValue, vp);
		return;
	}

	// ####################################################################
	void CHelper::cliHdaTimeToHdaTime(OPCHDA_TIME ^ cliHdaTime, tagOPCHDA_TIME * hdaTime)
	{
		if (cliHdaTime->bString)
		{
			hdaTime->bString = 1;
			if (nullptr != cliHdaTime->sTime)
			{
				hdaTime->szTime = ConvertStringToLPWSTR(cliHdaTime->sTime);
			}
			else
			{
				hdaTime->szTime = nullptr;
			}
			hdaTime->ftTime.dwHighDateTime = 0;
			hdaTime->ftTime.dwLowDateTime  = 0;
		}
		else
		{
			hdaTime->bString = 0;
			hdaTime->szTime = nullptr;
			*((__int64*)(&hdaTime->ftTime)) = cliHdaTime->dtTime.ToFileTimeUtc();
		}
		return;
	}

	// ####################################################################
	void CHelper::HdaTimeToCliHdaTime(tagOPCHDA_TIME * hdaTime, OPCHDA_TIME ^% cliHdaTime)
	{
		if (0 != hdaTime->bString)
		{
			cliHdaTime->bString = true;
			cliHdaTime->sTime = gcnew String(hdaTime->szTime);
		}
		else
		{
			cliHdaTime->bString = false;
		}
		cliHdaTime->dtTime = DateTime::FromFileTimeUtc(*((__int64*)(&hdaTime->ftTime)));
	}

	static const TransportDataType DataValueTransports[] = {
		TransportDataType::Unknown,	// VT_EMPTY		= 0
		TransportDataType::Object,		// VT_NULL		= 1
		TransportDataType::Uint,		// VT_I2		= 2
		TransportDataType::Uint,		// VT_I4		= 3
		TransportDataType::Double,		// VT_R4		= 4
		TransportDataType::Double,		// VT_R8		= 5
		TransportDataType::Object,		// VT_CY		= 6
		TransportDataType::Object,		// VT_DATE		= 7
		TransportDataType::Object,		// VT_BSTR		= 8
		TransportDataType::Object,		// VT_DISPATCH	= 9
		TransportDataType::Uint,		// VT_ERROR		= 10
		TransportDataType::Uint,		// VT_BOOL		= 11 {This works because cliVARIANT}
		TransportDataType::Object,		// VT_VARIANT	= 12
		TransportDataType::Object,		// VT_UNKNOWN	= 13
		TransportDataType::Object,		// VT_DECIMAL	= 14
		TransportDataType::Object,		// 15
		TransportDataType::Uint,		// VT_I1		= 16
		TransportDataType::Uint,		// VT_UI1		= 17
		TransportDataType::Uint,		// VT_UI2		= 18
		TransportDataType::Uint,		// VT_UI4		= 19
		TransportDataType::Object,		// VT_I8		= 20
		TransportDataType::Object,		// VT_UI8		= 21
		TransportDataType::Uint,		// VT_INT		= 22
		TransportDataType::Uint,		// VT_UINT		= 23
		TransportDataType::Object,		// VT_VOID		= 24
		TransportDataType::Uint,		// VT_HRESULT	= 25
		TransportDataType::Object,		// VT_PTR		= 26
		TransportDataType::Object,		// VT_SAFEARRAY	= 27
		TransportDataType::Object,		// VT_CARRAY	= 28
		TransportDataType::Object,		// VT_USERDEFINED=29
		TransportDataType::Object,		// VT_LPSTR		= 30
		TransportDataType::Object,		// VT_LPWSTR	= 31
		TransportDataType::Object,		// 32
		TransportDataType::Object,		// 33
		TransportDataType::Object,		// 34
		TransportDataType::Object,		// 35
		TransportDataType::Object,		// VT_RECORD	= 36
		TransportDataType::Object,		// VT_INT_PTR	= 37
		TransportDataType::Object,		// VT_UINT_PTR	= 38
		TransportDataType::Object,		// **Default**	= 39
	};

	// ####################################################################
	TransportDataType CHelper::GetTransportDataType(VARTYPE vt)
	{
		if (VT_EMPTY <= vt && VT_UINT_PTR >= vt)
		{
			return DataValueTransports[vt];
		}
		return TransportDataType::Object;
	}


	#define MAX_KEYLEN 2048
	const CLSID CLSID_OPCServerList = {0x13486D51,0x4821,0x11D2,{0xA4,0x94,0x3C,0xB3,0x06,0xC1,0x00,0x00}};

	HRESULT CHelper::CLSIDFromProgID( String^ machine, String^ progID, CLSID &pCLSID )
	{
		cliHRESULT HR = E_FAIL;

		LPWSTR szMachine = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(machine)).ToPointer());
		LPWSTR szProgID = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(progID)).ToPointer());

		// search locally first. save network overhead
		HR = ::CLSIDFromProgID(szProgID, &pCLSID);

		// if that fails, try the OPC server list object
		if(HR.Failed && szMachine != NULL)
		{
			IOPCServerList* pServers = nullptr;
			COSERVERINFO si;
			MULTI_QI qi;

			si.dwReserved1 = 0;
			si.dwReserved2 = 0;
			si.pwszName = (LPOLESTR)szMachine;
			si.pAuthInfo = NULL;
			
			qi.pIID = &IID_IOPCServerList;
			qi.pItf = NULL;
			qi.hr = 0;

			HR = ::CoCreateInstanceEx(CLSID_OPCServerList, NULL, CLSCTX_ALL, &si, 1, &qi);
			if( HR.Succeeded && SUCCEEDED(qi.hr) )
			{
				pServers = (IOPCServerList *)qi.pItf;
				HR = pServers->CLSIDFromProgID(szProgID, &pCLSID);
				pServers->Release();
			}

			// last resort - search the remote machine registry
			if( HR.Failed )
			{
				HKEY hKey = HKEY_CLASSES_ROOT;
				HKEY hkCLSID;
				
				DWORD dwReg = ::RegConnectRegistry(szMachine, HKEY_CLASSES_ROOT, &hKey);
				if(dwReg == ERROR_SUCCESS)
				{
					dwReg = ::RegOpenKey(hKey, szProgID, &hkCLSID);
					if(dwReg == ERROR_SUCCESS)
					{
						LONG size = MAX_KEYLEN;
						TCHAR strClsId[MAX_KEYLEN];

						dwReg = ::RegQueryValue(hkCLSID, _T("CLSID"), strClsId, &size);
						if(dwReg == ERROR_SUCCESS)
						{
							HR = ::CLSIDFromString(T2OLE(strClsId), &pCLSID);
						}
						dwReg = ::RegCloseKey(hkCLSID);

					}
					dwReg = ::RegCloseKey(hKey);

				}
			}
		}

		// release the unmanaged strings
		Marshal::FreeHGlobal((IntPtr)szProgID);
		Marshal::FreeHGlobal((IntPtr)szMachine);

		return HR.hResult;
	}

}}}}
