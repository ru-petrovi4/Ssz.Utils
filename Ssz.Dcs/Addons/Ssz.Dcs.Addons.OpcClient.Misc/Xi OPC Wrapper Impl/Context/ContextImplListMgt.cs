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

using System.Diagnostics;

using Xi.Contracts.Data;
using Xi.Contracts.Constants;
using Xi.Server.Base;
using Xi.Common.Support;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// Each client is represented by an instance of a this class.
	/// </summary>
	public partial class ContextImpl
		: ContextBase<ListRoot>
	{
		public override ListAttributes OnDefineList(uint clientId,
			uint listType, uint updateRate, uint bufferingRate, FilterSet filterSet)
		{
			bool newOpcDaGroup = false;
			bool newOpcHdaList = false;
			bool newOpcAeSubscription = false;
			bool newHaeList = false;

			ListAttributes listAttrs = null;
			ListRoot listBase = null;
			uint listKey = NewUniqueListId();
			uint rc = XiFaultCodes.S_OK;

		    var lcid = LocaleId;

			lock (ContextLock)
			{
				switch (listType)
				{
					case (uint)StandardListType.DataList:
						if (IsAccessibleDataAccess == false)
						{
						    OpcRelease();
						    OpcCreateInstance(ref lcid, ServerRoot.ThisServerEntry.ServerDescription);

						    if (IsAccessibleDataAccess == false)
						    {
						        throw FaultHelpers.Create("The wrapped OPC DA server is currently not accessible.");
						    }
						}
						newOpcDaGroup = true;
						break;

					case (uint)StandardListType.DataJournalList:
						if (IsAccessibleJournalDataAccess == false)
						{
                            OpcRelease();
                            OpcCreateInstance(ref lcid, ServerRoot.ThisServerEntry.ServerDescription);

                            if (IsAccessibleJournalDataAccess == false)
                                throw FaultHelpers.Create("The wrapped OPC HDA server is currently not accessible.");
						}
						newOpcHdaList = true;
						break;

					case (uint)StandardListType.EventList:
						if (IsAccessibleAlarmsAndEvents == false)
                        {
                            OpcRelease();
                            OpcCreateInstance(ref lcid, ServerRoot.ThisServerEntry.ServerDescription);
                            
                            if (IsAccessibleAlarmsAndEvents == false)
                                throw FaultHelpers.Create("The wrapped OPC A&E server is currently not accessible.");
						}
						newOpcAeSubscription = true;
						break;

					case (uint)StandardListType.EventJournalList:
						if (IsAccessibleJournalAlarmsAndEvents == false)
						{
                            OpcRelease();
                            OpcCreateInstance(ref lcid, ServerRoot.ThisServerEntry.ServerDescription);

                            if (IsAccessibleJournalAlarmsAndEvents == false)
                                throw FaultHelpers.Create("This server does not support the Journal Event List type.");
						}
						newHaeList = true;
						break;

					default:
						rc = XiFaultCodes.E_FAIL;
						Debug.Assert(listType == (uint)StandardListType.DataList || listType == (uint)StandardListType.DataJournalList ||
									 listType == (uint)StandardListType.EventList || listType == (uint)StandardListType.EventJournalList);
						throw FaultHelpers.Create("This server does not support the type of list specified.");
				}
			} // end Context lock

			if (newOpcDaGroup)
				listBase = new DataList(this, clientId, updateRate, bufferingRate, listType, listKey, filterSet);

			if (newOpcHdaList)
				listBase = new DataJournalList(this, clientId, updateRate, bufferingRate, listType, listKey, filterSet);

			if (newOpcAeSubscription)
				listBase = new EventsList(this, clientId, updateRate, bufferingRate, listType, listKey, filterSet);

			if (newHaeList)
				listBase = new EventListJournal(this, clientId, updateRate, bufferingRate, listType, listKey, filterSet);

			listAttrs = AddXiList(listBase); // this method locks the Context while adding the new list to it
			listAttrs.ResultCode = rc;
			return listAttrs;
		}
	}
}
