#pragma once

using namespace System::Collections::Generic;

#ifdef USO
namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	public ref class CUsoHdaServer
		: public IDisposable
		, public IOPCCommonCli
		, public IOPCHDA_ServerCli
		, public IOPCHDA_SyncReadCli
		, public IAdviseOPCShutdownCli
	{
	public:
		CUsoHdaServer(
			cliHRESULT %HR, 
			uso_interfaces::IUsoOpcAccess * pIOPCHDA_Server,
			IOPCServerCli ^opcDAServer);

		~CUsoHdaServer(void);

		// IOPCCommon
		virtual cliHRESULT SetLocaleID(
			/*[in]*/ unsigned int dwLcid );
		virtual cliHRESULT GetLocaleID(
			/*[out]*/ unsigned int %dwLcid );
		virtual cliHRESULT QueryAvailableLocaleIDs(
			/*[out]*/ List<unsigned int>^ %dwLcid );
		virtual cliHRESULT GetErrorString(
			/*[in]*/ cliHRESULT dwError,
			/*[out]*/ String^ %errString );
		virtual cliHRESULT SetClientName(
			/*[in]*/ String^ zName );

		// IOPCHDA_Server
		virtual cliHRESULT GetItemAttributes( 
			/*[out]*/ List<OPCHDAITEMATTR^>^ %HDAItemAttributes);
		virtual cliHRESULT GetAggregates( 
			/*[out]*/ List<OPCHDAAGGREGATES^>^ %HDAAggregates);
		virtual cliHRESULT GetHistorianStatus( 
			/*[out]*/ OPCHDA_SERVERSTATUS %wStatus,
			/*[out]*/ DateTime %dtCurrentTime,
			/*[out]*/ DateTime %dtStartTime,
			/*[out]*/ unsigned short %wMajorVersion,
			/*[out]*/ unsigned short %wMinorVersion,
			/*[out]*/ unsigned short %wBuildNumber,
			/*[out]*/ unsigned int %dwMaxReturnValues,
			/*[out]*/ String^ %sStatusString,
			/*[out]*/ String^ %sVendorInfo);
		virtual cliHRESULT GetItemHandles( 
			/*[in]*/ List<OPCHDA_ITEMDEF^>^ hClientAndItemID,
			/*[out]*/ List<OPCHDAITEMRESULT^>^ %hServerAndHResult);
		virtual cliHRESULT ReleaseItemHandles( 
			/*[in]*/ List<unsigned int>^ hServer,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList);
		virtual cliHRESULT ValidateItemIDs( 
			/*[in]*/ List<String^>^ sItemID,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList);
		virtual cliHRESULT CreateBrowse( 
			/*[in]*/ List<OPCHDA_BROWSEFILTER^>^ BrowseFilters,
			/*[out]*/ IOPCHDA_BrowserCli^ %iBrowser,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList);

		// IOPCHDA_SyncRead
		virtual cliHRESULT ReadRaw( 
			/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
			/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
			/*[in]*/ unsigned int dwNumValues,
			/*[in]*/ bool bBounds,
			/*[in]*/ List<unsigned int>^ hServer,
			/*[out]*/ array<JournalDataValues^>^ %ItemValues);
		virtual cliHRESULT ReadProcessed( 
			/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
			/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
			/*[in]*/ TimeSpan dtResampleInterval,
			/*[in]*/ List<OPCHDA_HANDLEAGGREGATE^>^ HandleAggregate,
			/*[out]*/ array<JournalDataValues^>^ %ItemValues);
		virtual cliHRESULT ReadAtTime( 
			/*[in]*/ List<DateTime>^ dtTimeStamps,
			/*[in]*/ List<unsigned int>^ hServer,
			/*[out]*/ array<JournalDataValues^>^ %ItemValues);
		virtual cliHRESULT ReadModified( 
			/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
			/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
			/*[in]*/ unsigned int dwNumValues,
			/*[in]*/ List<unsigned int>^ hServer,
			/*[out]*/ array<JournalDataChangedValues^>^ %ItemValues);
		virtual cliHRESULT ReadAttribute( 
			/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
			/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
			/*[in]*/ unsigned int hServer,
			/*[in]*/ List<unsigned int>^ dwAttributeIDs,
			/*[out]*/ array<JournalDataPropertyValue^>^ %AttributeValues);

		// IAdviseOPCShutdownCli
		virtual cliHRESULT AdviseShutdownRequest(
			/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest);
		virtual cliHRESULT UnadviseShutdownRequest(
			/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest);

	private:
		cliHRESULT getUnisimSimulationTime(double %time);

		!CUsoHdaServer(void);
		bool DisposeThis(bool isDisposing);

		bool m_bHasBeenDisposed;

		DWORD _lcid;
		Dictionary<unsigned int, String^> ^_handles;
		uso_interfaces::IUsoOpcAccess *_usoServer;

		IOPCServerCli ^_opcDAServer;
	};
}}}}

#endif