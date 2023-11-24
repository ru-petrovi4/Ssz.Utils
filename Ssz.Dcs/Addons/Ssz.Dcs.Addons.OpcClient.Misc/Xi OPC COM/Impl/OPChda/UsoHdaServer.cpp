#include "..\StdAfx.h"
#include <vcclr.h>

#include "..\Helper.h"
#include "UsoHdaServer.h"
#include "..\OPCda\OPCDaServer.h"

#include <vector>

using namespace System::Runtime::InteropServices;
using namespace System::Diagnostics;
using namespace Xi::Contracts;
using namespace Xi::Contracts::Data;
using namespace Xi::Contracts::Constants;
using namespace Xi::Common::Support;

#ifdef USO
namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {
	
	CUsoHdaServer::CUsoHdaServer(
		cliHRESULT %HR,
		uso_interfaces::IUsoOpcAccess * pIOPCHDA_Server,
		IOPCServerCli ^opcDAServer) :
		_handles(gcnew Dictionary<unsigned int, String^>()),
		_usoServer(pIOPCHDA_Server),
		_opcDAServer(opcDAServer)
	{
	}

	CUsoHdaServer::~CUsoHdaServer(void)
	{
		if (DisposeThis(true))
			GC::SuppressFinalize(this);

	}

	CUsoHdaServer::!CUsoHdaServer(void)
	{
		DisposeThis(false);
	}

	bool CUsoHdaServer::DisposeThis(bool isDisposing)
	{
		if (m_bHasBeenDisposed)
			return false;

		if (_usoServer != NULL)
		{
			_usoServer->Release();
			_usoServer = NULL;
		}

		// don't release DA server here!

		return true;
	}

	// IOPCCommon
	cliHRESULT CUsoHdaServer::SetLocaleID(
		/*[in]*/ unsigned int dwLcid )
	{
		_lcid = dwLcid;
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::GetLocaleID(
		/*[out]*/ unsigned int %dwLcid )
	{
		dwLcid = _lcid;

		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::QueryAvailableLocaleIDs(
		/*[out]*/ List<unsigned int>^ %dwLcid )
	{
		dwLcid = gcnew List<unsigned int>();
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::GetErrorString(
		/*[in]*/ cliHRESULT dwError,
		/*[out]*/ String^ %errString )
	{
		errString = "";
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::SetClientName(
		/*[in]*/ String^ zName )
	{
		return cliHRESULT(S_OK);
	}



	// IOPCHDA_Server
	cliHRESULT CUsoHdaServer::GetItemAttributes( 
		/*[out]*/ List<OPCHDAITEMATTR^>^ %HDAItemAttributes)
	{
		HDAItemAttributes = gcnew List<OPCHDAITEMATTR^>();
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::GetAggregates( 
		/*[out]*/ List<OPCHDAAGGREGATES^>^ %HDAAggregates)
	{
		HDAAggregates = gcnew List<OPCHDAAGGREGATES^>();
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::GetHistorianStatus( 
		/*[out]*/ OPCHDA_SERVERSTATUS %wStatus,
		/*[out]*/ DateTime %dtCurrentTime,
		/*[out]*/ DateTime %dtStartTime,
		/*[out]*/ unsigned short %wMajorVersion,
		/*[out]*/ unsigned short %wMinorVersion,
		/*[out]*/ unsigned short %wBuildNumber,
		/*[out]*/ unsigned int %dwMaxReturnValues,
		/*[out]*/ String^ %sStatusString,
		/*[out]*/ String^ %sVendorInfo)
	{
		wStatus = OPCHDA_SERVERSTATUS::OPCHDA_UP;
		dtCurrentTime = DateTime::Now;
		dtStartTime = DateTime::MinValue;
		wMajorVersion = 10;
		wMinorVersion = 123;
		wBuildNumber = 27683;
		dwMaxReturnValues = 10000;
		sStatusString = "Hello world";
		sVendorInfo = "qwerty";


		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::GetItemHandles( 
		/*[in]*/ List<OPCHDA_ITEMDEF^>^ hClientAndItemID,
		/*[out]*/ List<OPCHDAITEMRESULT^>^ %hServerAndHResult)
	{
		hServerAndHResult = gcnew List<OPCHDAITEMRESULT^>();

	    uso_interfaces::IHciDeviceSpecific_HDA *pHda = NULL;
		auto hr = _usoServer->QueryInterface(&pHda);
		if (FAILED(hr))
			return cliHRESULT(hr);

		int iCount = hClientAndItemID->Count;

		unsigned char *data = (unsigned char*)CoTaskMemAlloc(sizeof(uso_interfaces::HCIBYTESTREAM) + iCount * sizeof(DWORD));

		uso_interfaces::HCIBYTESTREAM *itemHandle = (uso_interfaces::HCIBYTESTREAM*)data;

		itemHandle->size = iCount * sizeof(DWORD);
		itemHandle->def = data + sizeof(uso_interfaces::HCIBYTESTREAM);

		ZeroMemory(data, iCount * sizeof(DWORD));


		uso_interfaces::HCIHDAItemDef *rgItemDef = (uso_interfaces::HCIHDAItemDef*)CoTaskMemAlloc(iCount * sizeof(uso_interfaces::HCIHDAItemDef));
		ZeroMemory(rgItemDef, iCount * sizeof(uso_interfaces::HCIHDAItemDef));

		HRESULT *rgError = (HRESULT*)CoTaskMemAlloc(iCount * sizeof(HRESULT));
		ZeroMemory(rgError, iCount * sizeof(HRESULT));

		try
		{
			for (int nItem = 0; nItem < iCount; ++nItem)
			{
				auto item = hClientAndItemID[nItem];
				pin_ptr<const wchar_t> itemName = PtrToStringChars(item->sItemID);

				auto nativeStringLength = (item->sItemID->Length + 1) * 2;
				LPWSTR nativeString = (LPWSTR)CoTaskMemAlloc(nativeStringLength);
				memcpy(nativeString, itemName, nativeStringLength);

				rgItemDef[nItem].szItemID = nativeString;
			}
			
			try
			{
				pHda->HDAGetItemHandles(iCount, itemHandle, rgItemDef, rgError);
			}
			catch (...)
			{
			}
			finally
			{
				for (int nItem = 0; nItem < iCount; ++nItem)
				{
					CoTaskMemFree(rgItemDef[nItem].szItemID);
				}
			}

			for each (OPCHDA_ITEMDEF^ iter in hClientAndItemID)
			{
				auto itemResult = gcnew OPCHDAITEMRESULT();
				itemResult->hServer = iter->hClient;
				itemResult->hClient = iter->hClient;
				itemResult->HResult = cliHRESULT(S_OK);

				hServerAndHResult->Add(itemResult);

				_handles[iter->hClient] = iter->sItemID;
			}
		}
		finally
		{
			CoTaskMemFree(data);
			CoTaskMemFree(rgItemDef);
			CoTaskMemFree(rgError);

			pHda->Release();
		}

		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::ReleaseItemHandles( 
		/*[in]*/ List<unsigned int>^ hServer,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList)
	{
		for each (unsigned int i in hServer)
			_handles->Remove(i);

		ErrorsList = gcnew List<HandleAndHRESULT^>();
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::ValidateItemIDs( 
		/*[in]*/ List<String^>^ sItemID,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList)
	{
		ErrorsList = gcnew List<HandleAndHRESULT^>();
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::CreateBrowse( 
		/*[in]*/ List<OPCHDA_BROWSEFILTER^>^ BrowseFilters,
		/*[out]*/ IOPCHDA_BrowserCli^ %iBrowser,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList)
	{
		return cliHRESULT(S_OK);
	}



	// IOPCHDA_SyncRead
	cliHRESULT CUsoHdaServer::ReadRaw( 
		/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
		/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
		/*[in]*/ unsigned int dwNumValues,
		/*[in]*/ bool bBounds,
		/*[in]*/ List<unsigned int>^ hServer,
		/*[out]*/ array<JournalDataValues^>^ %ItemValues)
	{
		ItemValues = gcnew array<JournalDataValues^>(hServer->Count);

		double currentSimulationTime;
		auto hr = getUnisimSimulationTime(currentSimulationTime);
		auto now = DateTime::UtcNow;

		for (int nItem = 0; nItem < hServer->Count; ++nItem)
		{
			auto hItem = hServer[nItem];
			auto itemName = _handles[hItem];
			
			double startTime, endTime;
			if (hr.Failed)
			{
				return hr;
			}
			else
			{
				startTime = Math::Max(currentSimulationTime - (now - cliStartTime->dtTime).TotalSeconds, 0.0);
				endTime = Math::Max(currentSimulationTime - (now - cliEndTime->dtTime).TotalSeconds, startTime);
			}

			SAFEARRAY *pTimeStamps = NULL;
			SAFEARRAY *pValues = NULL;

			pin_ptr<const wchar_t> bstr = PtrToStringChars(itemName);

			auto bstr_native = _bstr_t(bstr);

			hr = _usoServer->ReadHistoryData(startTime, endTime, bstr_native, &pTimeStamps, &pValues);
			if (hr.Failed)
			{
				return hr;
			}
			 
			if (pTimeStamps->rgsabound->cElements <= 0)
			{
				auto data = gcnew JournalDataValues();
				auto convertedValues = gcnew DataValueArrays(0, 0, 0);
				data->HistoricalValues = convertedValues;

				data->Calculation = gcnew TypeId();
				data->Calculation->LocalId = JournalDataSampleTypes::RawDataSamples.ToString();

				ItemValues[nItem] = data;
			}
			else
			{
				auto numElements = pTimeStamps->rgsabound->cElements;
				auto convertedValues = gcnew DataValueArrays(numElements, 0, 0);

				for (size_t i = 0; i < numElements; ++i)
				{
					auto pointTimeStamp = ((double*)pTimeStamps->pvData)[i];
					auto pointValue = ((double*)pValues->pvData)[i];

					convertedValues->DoubleTimeStamps[i] = now + TimeSpan::FromSeconds(pointTimeStamp - currentSimulationTime);
					convertedValues->DoubleStatusCodes[i] = S_OK;
					convertedValues->DoubleValues[i] = pointValue;
				}

				auto data = gcnew JournalDataValues();
				data->HistoricalValues = convertedValues;

				data->Calculation = gcnew TypeId();
				data->Calculation->LocalId = JournalDataSampleTypes::RawDataSamples.ToString();

				ItemValues[nItem] = data;
			}

			SafeArrayDestroy(pTimeStamps);
			pTimeStamps = NULL;
			SafeArrayDestroy(pValues);
			pValues = NULL;
		}

		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::ReadProcessed( 
		/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
		/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
		/*[in]*/ TimeSpan dtResampleInterval,
		/*[in]*/ List<OPCHDA_HANDLEAGGREGATE^>^ HandleAggregate,
		/*[out]*/ array<JournalDataValues^>^ %ItemValues)
	{
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::ReadAtTime( 
		/*[in]*/ List<DateTime>^ dtTimeStamps,
		/*[in]*/ List<unsigned int>^ hServer,
		/*[out]*/ array<JournalDataValues^>^ %ItemValues)
	{
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::ReadModified( 
		/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
		/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
		/*[in]*/ unsigned int dwNumValues,
		/*[in]*/ List<unsigned int>^ hServer,
		/*[out]*/ array<JournalDataChangedValues^>^ %ItemValues)
	{
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::ReadAttribute( 
		/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
		/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
		/*[in]*/ unsigned int hServer,
		/*[in]*/ List<unsigned int>^ dwAttributeIDs,
		/*[out]*/ array<JournalDataPropertyValue^>^ %AttributeValues)
	{
		AttributeValues = gcnew array<JournalDataPropertyValue^>(dwAttributeIDs->Count);

		for (int i = 0; i < dwAttributeIDs->Count; ++i)
		{
			auto attributeValue = gcnew JournalDataPropertyValue();

			if (dwAttributeIDs[i] == OPCHDA_DATA_TYPE)
			{
				attributeValue->PropertyValues = gcnew DataValueArrays(0, 1, 0);
				attributeValue->PropertyValues->UintValues[0] = VT_R8;
			}

			AttributeValues[i] = attributeValue;
		}
		

		return cliHRESULT(S_OK);
	}



	// IAdviseOPCShutdownCli
	cliHRESULT CUsoHdaServer::AdviseShutdownRequest(
		/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest)
	{
		return cliHRESULT(S_OK);
	}


	cliHRESULT CUsoHdaServer::UnadviseShutdownRequest(
		/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest)
	{
		return cliHRESULT(S_OK);
	}

	cliHRESULT CUsoHdaServer::getUnisimSimulationTime(double %time)
	{
		IOPCItemMgtCli ^itemMgt;
		unsigned int dwRevisedUpdateRate;

		cliHRESULT hr = _opcDAServer->AddGroup(
			"SimulationTime",
			false,
			0,
			123 /* hClientGroup */,
			System::Nullable<int>(),
			System::Nullable<float>(),
			0,
			dwRevisedUpdateRate,
			itemMgt);

		if (hr.Failed)
			return hr;
		try
		{
			OPCITEMDEF ^item = gcnew OPCITEMDEF();
			item->sItemID = "SIMULATIONCONTROL.SIMTIME";

			auto items = gcnew List<OPCITEMDEF^>();
			items->Add(item);

			List<OPCITEMRESULT^> ^itemResults;

			hr = itemMgt->AddItems(items, itemResults);
			if (hr.Failed)
				return hr;

			auto hServerList = gcnew List<unsigned int>();
			for each (OPCITEMRESULT^ item in itemResults)
				hServerList->Add(item->hServer);

			DataValueArraysWithAlias ^itemValues;
			hr = itemMgt->Read(OPCDATASOURCE::OPC_DS_DEVICE, hServerList, itemValues);
			if (hr.Failed)
				return hr;

			time = itemValues->DoubleValues[itemValues->DoubleValues->Length - 1];
		}
		finally
		{
			delete itemMgt;
		}

		return hr;
	}

}}}}
#endif