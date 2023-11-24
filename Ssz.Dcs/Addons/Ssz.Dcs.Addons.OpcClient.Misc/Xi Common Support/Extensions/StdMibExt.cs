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

using Xi.Contracts.Data;

namespace Xi.Common.Support.Extensions
{
	public static class StdMibExt
	{
		public static bool SupportsDataLists(this StandardMib stdMib)
		{
			if (null == stdMib) return false;
			return (0 != (stdMib.MethodsSupported 
				& ((ulong)(XiMethods.IRead_ReadData
					| XiMethods.IWrite_WriteValues
					| XiMethods.IPoll_PollDataChanges
					| XiMethods.ICallback_InformationReport))));
		}

		public static bool SupportsDataJournalLists(this StandardMib stdMib)
		{
			if (null == stdMib) return false;
			return (0 != (stdMib.MethodsSupported
				& ((ulong)(XiMethods.IRead_ReadJournalDataForTimeInterval
					| XiMethods.IRead_ReadJournalDataAtSpecificTimes
					| XiMethods.IRead_ReadJournalDataChanges
					| XiMethods.IRead_ReadCalculatedJournalData
					| XiMethods.IRead_ReadJournalDataProperties))));
		}

		public static bool SupportsEventLists(this StandardMib stdMib)
		{
			if (null == stdMib) return false;
			return (0 != (stdMib.MethodsSupported
				& ((ulong)(XiMethods.ICallback_EventNotification
					| XiMethods.IRead_ReadEvents
					| XiMethods.IPoll_PollEventChanges))));
		}

		public static bool SupportsEventJournalLists(this StandardMib stdMib)
		{
			if (null == stdMib) return false;
			return (0 != (stdMib.MethodsSupported
				& ((ulong)(XiMethods.IRead_ReadJournalEvents))));
		}
	}
}
