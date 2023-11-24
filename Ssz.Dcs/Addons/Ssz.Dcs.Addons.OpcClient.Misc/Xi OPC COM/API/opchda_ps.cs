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

using Xi.Common.Support;
using Xi.Contracts.Data;

namespace Xi.OPC.COM.API
{
	/// <summary>
	/// Duplicate of OpcHdaAttrIDs
	/// </summary>
	public enum OPCHDA_ATTRIBUTES : uint
	{
		OPCHDA_DATA_TYPE = 1,
		OPCHDA_DESCRIPTION = 2,
		OPCHDA_ENG_UNITS = 3,
		OPCHDA_STEPPED = 4,
		OPCHDA_ARCHIVING = 5,
		OPCHDA_DERIVE_EQUATION = 6,
		OPCHDA_NODE_NAME = 7,
		OPCHDA_PROCESS_NAME = 8,
		OPCHDA_SOURCE_NAME = 9,
		OPCHDA_SOURCE_TYPE = 10,
		OPCHDA_NORMAL_MAXIMUM = 11,
		OPCHDA_NORMAL_MINIMUM = 12,
		OPCHDA_ITEMID = 13,
		OPCHDA_MAX_TIME_INT = 14,
		OPCHDA_MIN_TIME_INT = 15,
		OPCHDA_EXCEPTION_DEV = 16,
		OPCHDA_EXCEPTION_DEV_TYPE = 17,
		OPCHDA_HIGH_ENTRY_LIMIT = 18,
		OPCHDA_LOW_ENTRY_LIMIT = 19,
	}

	/// <summary>
	/// Duplicate of OPCHDA_ATTRIBUTES
	/// </summary>
	public enum OpcHdaAttrIDs : uint
	{
		DataType = 1,
		Description = 2,
		EngUnits = 3,
		Stepped = 4,
		Archiving = 5,
		DeriveEquation = 6,
		NodeName = 7,
		ProcessName = 8,
		SourceName = 9,
		SourceType = 10,
		NormalMaximum = 11,
		NormalMinimum = 12,
		ItemID = 13,
		MaxTimeInterval = 14,
		MinTimeInterval = 15,
		ExceptionDeviation = 16,
		ExceptionDevType = 17,
		HighEntryLimit = 18,
		LowEntryLimit = 19,
	}

	/// <summary>
	/// 
	/// </summary>
	public enum OPCHDA_SERVERSTATUS : uint
	{
		OPCHDA_UP = 1,
		OPCHDA_DOWN = 2,
		OPCHDA_INDETERMINATE = 3
	}

	/// <summary>
	/// 
	/// </summary>
	public enum OPCHDA_OPERATORCODES : uint
	{
		OPCHDA_EQUAL = 1,
		OPCHDA_LESS = 2,
		OPCHDA_LESSEQUAL = 3,
		OPCHDA_GREATER = 4,
		OPCHDA_GREATEREQUAL = 5,
		OPCHDA_NOTEQUAL = 6
	}

	/// <summary>
	/// 
	/// </summary>
	public enum OPCHDA_BROWSETYPE : uint
	{
		OPCHDA_BRANCH = 1,
		OPCHDA_LEAF = 2,
		OPCHDA_FLAT = 3,
		OPCHDA_ITEMS = 4
	}

	/// <summary>
	/// 
	/// </summary>
	public enum OPCHDA_BROWSEDIRECTION : uint
	{
		OPCHDA_BROWSE_UP = 1,
		OPCHDA_BROWSE_DOWN = 2,
		OPCHDA_BROWSE_DIRECT = 3
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCHDA_BrowserCli
		: IDisposable
	{
		cliHRESULT GetEnum(
			/*[in]*/  OPCHDA_BROWSETYPE dwBrowseType,
			/*[out]*/ out cliIEnumString iEnumString);
		cliHRESULT ChangeBrowsePosition(
			/*[in]*/ OPCHDA_BROWSEDIRECTION dwBrowseDirection,
			/*[in]*/ string sString);
		cliHRESULT GetItemID(
			/*[in]*/ string sNode,
			/*[out]*/ out string sItemID);
		cliHRESULT GetBranchPosition(
			/*[out]*/ out string sBranchPos);
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDAITEMATTR
	{
		public uint dwAttrID;
		public string sAttrName;
		public string sAttrDesc;
		public ushort vtAttrDataType;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDAAGGREGATES
	{
		public uint dwAggrID;
		public string sAggrName;
		public string sAggrDesc;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDA_ITEMDEF
	{
		public uint hClient;
		public string sItemID;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDAITEMRESULT
	{
		public uint hClient;
		public uint hServer;
		public cliHRESULT HResult { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDA_BROWSEFILTER
	{
		public uint dwAttrID;
		public OPCHDA_OPERATORCODES FilterOperator;
		public cliVARIANT FilterValue;
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCHDA_ServerCli
		: IOPCCommonCli
		, IDisposable
	{
		cliHRESULT GetItemAttributes(
			/*[out]*/ out List<OPCHDAITEMATTR> HDAItemAttributes);
		cliHRESULT GetAggregates(
			/*[out]*/ out List<OPCHDAAGGREGATES> HDAAggregates);
		cliHRESULT GetHistorianStatus(
			/*[out]*/ out OPCHDA_SERVERSTATUS wStatus,
			/*[out]*/ out DateTime dtCurrentTime,
			/*[out]*/ out DateTime dtStartTime,
			/*[out]*/ out ushort wMajorVersion,
			/*[out]*/ out ushort wMinorVersion,
			/*[out]*/ out ushort wBuildNumber,
			/*[out]*/ out uint dwMaxReturnValues,
			/*[out]*/ out string sStatusString,
			/*[out]*/ out string sVendorInfo);
		cliHRESULT GetItemHandles(
			/*[in]*/ List<OPCHDA_ITEMDEF> hClientAndItemID,
			/*[out]*/ out List<OPCHDAITEMRESULT> hServerAndHResult);
		cliHRESULT ReleaseItemHandles(
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out List<HandleAndHRESULT> ErrorsList);
		cliHRESULT ValidateItemIDs(
			/*[in]*/ List<string> sItemID,
			/*[out]*/ out List<HandleAndHRESULT> ErrorsList);
		cliHRESULT CreateBrowse(
			/*[in]*/ List<OPCHDA_BROWSEFILTER> BrowseFilters,
			/*[out]*/ out IOPCHDA_BrowserCli iBrowser,
			/*[out]*/ out List<HandleAndHRESULT> ErrorsList);
	}

	/// <summary>
	/// This class is used to represent a time for use with OPC HDA.
	/// In places this class my be refered to as cliHdaTime.
	/// Note: The DateTime value is always UTC, as are the FILETIME
	/// values for an OPC HDA Server.
	/// </summary>
	public class OPCHDA_TIME
	{
		public OPCHDA_TIME()
		{
			bString = false;
			sTime = null;
			dtTime = DateTime.UtcNow;
		}
		public OPCHDA_TIME(OPCHDA_TIME opcHdaTime)
		{
			bString = opcHdaTime.bString;
			sTime = opcHdaTime.sTime;
			dtTime = opcHdaTime.dtTime;
		}
		public OPCHDA_TIME(string strTime)
		{
			bString = true;
			sTime = strTime;
			dtTime = DateTime.UtcNow;
		}
		public OPCHDA_TIME(DateTime dateTime)
		{
			bString = false;
			sTime = null;
			dtTime = dateTime;
		}
		/// <summary>
		/// When true the sTime property holds a string as defined by the 
		/// OPC HDA specification representing a time offset.
		/// </summary>
		public bool bString { get; set; }
		/// <summary>
		/// A time offset string as defined by the OPC HDA 1.2 specification.
		/// </summary>
		public string sTime { get; set; }
		/// <summary>
		/// The property when the bString property is false holds a .NET UTC time value.
		/// </summary>
		public DateTime dtTime
		{
			get { return _dtTime; }
			set
			{
				switch (value.Kind)
				{
					case DateTimeKind.Unspecified:
						_dtTime = new DateTime((value.Ticks), DateTimeKind.Utc);
						break;
					case DateTimeKind.Local:
						_dtTime = value.ToUniversalTime();
						break;
					case DateTimeKind.Utc:
						_dtTime = value;
						break;
					default:
						throw FaultHelpers.Create("Invalid time in OPCHDA_TIME");
				}
			}
		}
		private DateTime _dtTime;
	}

	/// <summary>
	/// 
	/// </summary>
	public enum OPCHDA_EDITTYPE : uint
	{
		OPCHDA_INSERT = 1,
		OPCHDA_REPLACE = 2,
		OPCHDA_INSERTREPLACE = 3,
		OPCHDA_DELETE = 4
	}

	/// <summary>
	/// 
	/// </summary>
	public enum OPCHDA_AGGREGATE : uint
	{
		OPCHDA_NOAGGREGATE = 0,
		OPCHDA_INTERPOLATIVE = 1,
		OPCHDA_TOTAL = 2,
		OPCHDA_AVERAGE = 3,
		OPCHDA_TIMEAVERAGE = 4,
		OPCHDA_COUNT = 5,
		OPCHDA_STDEV = 6,
		OPCHDA_MINIMUMACTUALTIME = 7,
		OPCHDA_MINIMUM = 8,
		OPCHDA_MAXIMUMACTUALTIME = 9,
		OPCHDA_MAXIMUM = 10,
		OPCHDA_START = 11,
		OPCHDA_END = 12,
		OPCHDA_DELTA = 13,
		OPCHDA_REGSLOPE = 14,
		OPCHDA_REGCONST = 15,
		OPCHDA_REGDEV = 16,
		OPCHDA_VARIANCE = 17,
		OPCHDA_RANGE = 18,
		OPCHDA_DURATIONGOOD = 19,
		OPCHDA_DURATIONBAD = 20,
		OPCHDA_PERCENTGOOD = 21,
		OPCHDA_PERCENTBAD = 22,
		OPCHDA_WORSTQUALITY = 23,
		OPCHDA_ANNOTATIONS= 24,
	}

	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OPCHDA_QUALITY : uint
	{
		OPCHDA_EXTRADATA    = 0x00010000,
		OPCHDA_INTERPOLATED = 0x00020000,
		OPCHDA_RAW          = 0x00040000,
		OPCHDA_CALCULATED   = 0x00080000,
		OPCHDA_NOBOUND      = 0x00100000,
		OPCHDA_NODATA       = 0x00200000,
		OPCHDA_DATALOST     = 0x00400000,
		OPCHDA_CONVERSION   = 0x00800000,
		OPCHDA_PARTIAL      = 0x01000000,
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDA_MODIFYINFO
	{
		public DateTime dtTimeStamp;
		public uint dwQuality;
		public cliVARIANT DataValue;
		public DateTime dtModificationTime;
		public OPCHDA_EDITTYPE EditType;
		public string sUser;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDA_HANDLEAGGREGATE
	{
		public uint hServer;
		public uint haAggregate;
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCHDA_SyncReadCli
	{
		cliHRESULT ReadRaw(
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ uint dwNumValues,
			/*[in]*/ bool bBounds,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out JournalDataValues[] ItemValues);
		cliHRESULT ReadProcessed(
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ TimeSpan dtResampleInterval,
			/*[in]*/ List<OPCHDA_HANDLEAGGREGATE> HandleAggregate,
			/*[out]*/ out JournalDataValues[] ItemValues);
		cliHRESULT ReadAtTime(
			/*[in]*/ List<DateTime> dtTimeStamps,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out JournalDataValues[] ItemValues);
		cliHRESULT ReadModified(
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ uint dwNumValues,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out JournalDataChangedValues[] ItemValues);
		cliHRESULT ReadAttribute(
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ uint hServer,
			/*[in]*/ List<uint> dwAttributeIDs,
			/*[out]*/ out JournalDataPropertyValue[] AttributeValues);
	}

	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OPCHDA_UPDATECAPABILITIES
	{
		OPCHDA_INSERTCAP = 1,
		OPCHDA_REPLACECAP = 2,
		OPCHDA_INSERTREPLACECAP = 4,
		OPCHDA_DELETERAWCAP = 8,
		OPCHDA_DELETEATTIMECAP = 16
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDA_VALUEUPDATE
	{
		public uint hServer;
		public DateTime dtTimeStamp;
		public cliVARIANT DataValue;
		public uint dwQuality;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDA_HANDLETIME
	{
		public uint hServer;
		public DateTime dtTimeStamp;
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCHDA_SyncUpdateCli
	{
		cliHRESULT QueryCapabilities(
			/*[out]*/ out OPCHDA_UPDATECAPABILITIES Capabilities);
		cliHRESULT Insert(
			/*[in]*/ List<OPCHDA_VALUEUPDATE> ValueInserts,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT Replace(
			/*[in]*/ List<OPCHDA_VALUEUPDATE> ValueReplaces,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT InsertReplace(
			/*[in]*/ List<OPCHDA_VALUEUPDATE> ValueInsertReplaces,
			/*[out]*/ List<cliHRESULT> ErrorList);
		cliHRESULT DeleteRaw(
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT DeleteAtTime(
			/*[in]*/ List<OPCHDA_HANDLETIME> DeleteList,
			/*[out]*/ out List<cliHRESULT> ErrorList);
	}

	/// <summary>
	/// 
	/// </summary>
	public enum OPCHDA_ANNOTATIONCAPABILITIES : uint
	{
		OPCHDA_READANNOTATIONCAP = 1,
		OPCHDA_INSERTANNOTATIONCAP = 2
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDA_ANNOTATIONENTRY
	{
		public DateTime dtTimeStamp;
		public string sAnnotation;
		public DateTime dtAnnotationTime;
		public string sUser;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDA_ANNOTATION1
	{
		public uint hClient;
		public List<OPCHDA_ANNOTATIONENTRY> Annotations;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDA_ANNOTATION2
	{
		public uint hClient;
		public uint hServer;
		public DateTime dtTimeStamps;
		public List<OPCHDA_ANNOTATIONENTRY> Annotations;
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCHDA_SyncAnnotationsCli
	{
		cliHRESULT QueryCapabilities(
			/*[out]*/ out OPCHDA_ANNOTATIONCAPABILITIES Capabilities);
		cliHRESULT Read(
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ List<uint> hServer,
			/*[in]*/ List<OPCHDA_ANNOTATION1> TheAnnotations,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT Insert(
			/*[in]*/ List<OPCHDA_ANNOTATION2> TheAnnotations,
			/*[out]*/ out List<cliHRESULT> ErrorList);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCHDA_AsyncReadCli
	{
		cliHRESULT ReadRaw(
			/*[in]*/ uint dwTransactionID,
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ uint dwNumValues,
			/*[in]*/ bool bBounds,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT AdviseRaw(
			/*[in]*/ uint dwTransactionID,
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in]*/ DateTime dtUpdateInterval,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT ReadProcessed(
			/*[in]*/ uint dwTransactionID,
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ DateTime dtResampleInterval,
			/*[in]*/ List<OPCHDA_HANDLEAGGREGATE> HandleAggregate,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT AdviseProcessed(
			/*[in]*/ uint dwTransactionID,
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in]*/ DateTime dtResampleInterval,
			/*[in]*/ List<OPCHDA_HANDLEAGGREGATE> HandleAggregate,
			/*[in]*/ uint dwNumIntervals,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT ReadAtTime(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ List<DateTime> dtTimeStamps,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT ReadModified(
			/*[in]*/ uint dwTransactionID,
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ uint dwNumValues,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT ReadAttribute(
			/*[in]*/ uint dwTransactionID,
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ uint hServer,
			/*[in]*/ List<uint> dwAttributeIDs,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT Cancel(
			/*[in]*/ uint dwCancelID);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCHDA_AsyncUpdateCli
	{
		cliHRESULT QueryCapabilities(
			/*[out]*/ out OPCHDA_UPDATECAPABILITIES Capabilities);
		cliHRESULT Insert(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ List<OPCHDA_VALUEUPDATE> ValueInserts,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT Replace(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ List<OPCHDA_VALUEUPDATE> ValueReplaces,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT InsertReplace(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ List<OPCHDA_VALUEUPDATE> ValueInsertReplaces,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT DeleteRaw(
			/*[in]*/ uint dwTransactionID,
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT DeleteAtTime(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ List<OPCHDA_HANDLETIME> DeleteList,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT Cancel(
			/*[in]*/ uint dwCancelID);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCHDA_AsyncAnnotationsCli
	{
		cliHRESULT QueryCapabilities(
			/*[out]*/ out OPCHDA_ANNOTATIONCAPABILITIES Capabilities);
		cliHRESULT Read(
			/*[in]*/ uint dwTransactionID,
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT Insert(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ List<OPCHDA_ANNOTATION2> TheAnnotations,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT Cancel(
			/*[in]*/ uint dwCancelID);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCHDA_PlaybackCli
	{
		cliHRESULT ReadRawWithUpdate(
			/*[in]*/ uint dwTransactionID,
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ uint dwNumValues,
			/*[in]*/ DateTime dtUpdateDuration,
			/*[in]*/ DateTime dtUpdateInterval,
			/*[in]*/ List<uint> hServer,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT ReadProcessedWithUpdate(
			/*[in]*/ uint dwTransactionID,
			/*[in,out]*/ ref OPCHDA_TIME htStartTime,
			/*[in,out]*/ ref OPCHDA_TIME htEndTime,
			/*[in]*/ DateTime dtResampleInterval,
			/*[in]*/ uint dwNumIntervals,
			/*[in]*/ DateTime dtUpdateInterval,
			/*[in]*/ List<OPCHDA_HANDLEAGGREGATE> HandleAggregate,
			/*[out]*/ out uint dwCancelID,
			/*[out]*/ out List<cliHRESULT> ErrorList);
		cliHRESULT Cancel(
			/*[in]*/ uint dwCancelID);
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCHDA_DataCallback
	{
		public delegate void OnDataChange(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ cliHRESULT hrStatus,
			/*[in]*/ JournalDataValues[] ItemValues);
		public delegate void OnReadComplete(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ cliHRESULT hrStatus,
			/*[in]*/ JournalDataValues[] ItemValues);
		public delegate void OnReadModifiedComplete(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ cliHRESULT hrStatus,
			/*[in]*/ JournalDataChangedValues[] ItemValues);
		public delegate void OnReadAttributeComplete(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ cliHRESULT hrStatus,
			/*[in]*/ uint hClient,
			/*[in]*/ JournalDataPropertyValue[] AttributeValues);
		public delegate void OnReadAnnotations(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ cliHRESULT hrStatus,
			/*[in]*/ OPCHDA_ANNOTATION1[] AnnotationValues);
		public delegate void OnInsertAnnotations(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ cliHRESULT hrStatus,
			/*[in]*/ OPCHDA_ANNOTATION2[] AnnotationValues);
		public delegate void OnPlayback(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ cliHRESULT hrStatus,
			/*[in]*/ JournalDataValues[] ItemValues);
		public delegate void OnUpdateComplete(
			/*[in]*/ uint dwTransactionID,
			/*[in]*/ cliHRESULT hrStatus,
			/*[in]*/ HandleAndHRESULT[] hServerAndHResult);
		public delegate void OnCancelComplete(
			/*[in]*/ uint dwCancelID);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IAdviseOPCHDADataCallbackCli
	{
		cliHRESULT AdviseOnDataChange(
			/*[in]*/ OPCHDA_DataCallback.OnDataChange onDataChange);

		cliHRESULT AdviseOnReadComplete(
			/*[in]*/ OPCHDA_DataCallback.OnReadComplete onReadComplete);

		cliHRESULT AdviseOnReadModifiedComplete(
			/*[in]*/ OPCHDA_DataCallback.OnReadModifiedComplete onReadModifiedComplete);

		cliHRESULT AdviseOnReadAttributeComplete(
			/*[in]*/ OPCHDA_DataCallback.OnReadAttributeComplete onReadAttributeComplete);

		cliHRESULT AdviseOnReadAnnotations(
			/*[in]*/ OPCHDA_DataCallback.OnReadAnnotations onReadAnnotations);

		cliHRESULT AdviseOnInsertAnnotations(
			/*[in]*/ OPCHDA_DataCallback.OnInsertAnnotations onInsertAnnotations);

		cliHRESULT AdviseOnPlayback(
			/*[in]*/ OPCHDA_DataCallback.OnPlayback onPlayback);

		cliHRESULT AdviseOnUpdateComplete(
			/*[in]*/ OPCHDA_DataCallback.OnUpdateComplete onUpdateComplete);

		cliHRESULT AdviseOnCancelComplete(
			/*[in]*/ OPCHDA_DataCallback.OnCancelComplete onCancelComplete);

		cliHRESULT UnadviseOnDataChange(
			/*[in]*/ OPCHDA_DataCallback.OnDataChange onDataChange);

		cliHRESULT UnadviseOnReadComplete(
			/*[in]*/ OPCHDA_DataCallback.OnReadComplete onReadComplete);

		cliHRESULT UnadviseOnReadModifiedComplete(
			/*[in]*/ OPCHDA_DataCallback.OnReadModifiedComplete onReadModifiedComplete);

		cliHRESULT UnadviseOnReadAttributeComplete(
			/*[in]*/ OPCHDA_DataCallback.OnReadAttributeComplete onReadAttributeComplete);

		cliHRESULT UnadviseOnReadAnnotations(
			/*[in]*/ OPCHDA_DataCallback.OnReadAnnotations onReadAnnotations);

		cliHRESULT UnadviseOnInsertAnnotations(
			/*[in]*/ OPCHDA_DataCallback.OnInsertAnnotations onInsertAnnotations);

		cliHRESULT UnadviseOnPlayback(
			/*[in]*/ OPCHDA_DataCallback.OnPlayback onPlayback);

		cliHRESULT UnadviseOnUpdateComplete(
			/*[in]*/ OPCHDA_DataCallback.OnUpdateComplete onUpdateComplete);

		cliHRESULT UnadviseOnCancelComplete(
			/*[in]*/ OPCHDA_DataCallback.OnCancelComplete onCancelComplete);
	}
}
