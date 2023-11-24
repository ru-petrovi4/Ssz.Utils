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
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.OPC.COM.API;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// The Browse filters used by the OpcHdaBrowser. Although the DA and AE Browsers 
	/// use a new filter for each browse request, the HDA Browser uses the same 
	/// filter for all browse requests. Therefore, if the HDA browse filter changes, 
	/// a new HDA Browser must be instantiated. As a result, the Browse Filters are 
	/// not included in the CurrentBrowseContext because the CurrentBrowseContext is 
	/// newly instantiated for each new browse request (but not for continuation 
	/// requests).
	/// </summary>
	internal class OpcHda_BrowseFilters
	{
		/// <summary>
		/// The original filter criteria. 
		/// </summary>
		public FilterSet OriginalFilterSet;

		/// <summary>
		/// The HDA filter 
		/// </summary>
		public List<OPCHDA_BROWSEFILTER> HdaFilterList;

		/// <summary>
		/// This constructor validates the list of filter criteria and adds each 
		/// valid FilterCriterion to HdaFilterList.  The current 
		/// browse context is passed in as a parameter because the BrowseType can be 
		/// included in the filter, and is defaulted if it is not. 
		/// </summary>
		/// <param name="filterSet">
		/// The FilterSet that contains the filter parameters.
		/// </param>
		/// <param name="currentBrowseContext">
		/// The current context for the browse.
		/// </param>
		public OpcHda_BrowseFilters(FilterSet filterSet, CurrentBrowseContext currentBrowseContext)
		{
			OriginalFilterSet = filterSet;
			currentBrowseContext.BrowseTypesRequested = 0;
			HdaFilterList = new List<OPCHDA_BROWSEFILTER>();

			if ((filterSet != null) && (filterSet.Filters != null))
			{
				bool bStepped = false;
				bool bArchiving = false;
				foreach (var oredFilters in filterSet.Filters)
				{
					// only one FilterCriterion for each ORedFilters list is supported for OPC filtering
					// so use each ORed filter only if it has one FilterCriterion
					if (oredFilters.FilterCriteria.Count == 1)
					{
						FilterCriterion filter = oredFilters.FilterCriteria[0];
						if ((filter.OperandName.Length > 0)
							&& (filter.Operator > 0)
							&& (filter.Operator <= UInt16.MaxValue)
							&& (filter.ComparisonValue != null)
						   )
						{
							switch (filter.OperandName)
							{
								case FilterOperandNames.BranchOrLeaf:
									if (filter.Operator == FilterOperator.Equal)
									{
										if ((string)filter.ComparisonValue == "BRANCH")
											currentBrowseContext.BrowseTypesRequested += (int)OPCBROWSETYPE.OPC_BRANCH;
										else if ((string)filter.ComparisonValue == "LEAF")
											currentBrowseContext.BrowseTypesRequested += (int)OPCBROWSETYPE.OPC_LEAF;
										else
											throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Value for BranchOrLeaf Filter Criterion: " + (string)filter.ComparisonValue);
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for BranchOrLeaf Filter Criterion");
									break;
								case FilterOperandNames.DataType:
									if (filter.Operator == FilterOperator.Equal)
									{
										HdaFilterList.Add(new OPCHDA_BROWSEFILTER()
										{
											dwAttrID = (uint)OpcHdaAttrIDs.DataType,
											FilterOperator = OPCHDA_OPERATORCODES.OPCHDA_EQUAL,
											FilterValue = cliVARIANT.UshortTypeFromCliString((string)filter.ComparisonValue)
										});
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for DataType Filter Criterion");
									break;
								case FilterOperandNames.EngineeringUnits:
									if (filter.Operator == FilterOperator.Equal)
									{
										HdaFilterList.Add(new OPCHDA_BROWSEFILTER()
										{
											dwAttrID = (uint)OpcHdaAttrIDs.EngUnits,
											FilterOperator = OPCHDA_OPERATORCODES.OPCHDA_EQUAL,
											FilterValue = (string)filter.ComparisonValue
										});
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for EngineeringUnits Filter Criterion");
									break;
								case FilterOperandNames.Stepped:
									if (bStepped)
									{
										if (filter.Operator == FilterOperator.Equal)
										{
											HdaFilterList.Add(new OPCHDA_BROWSEFILTER()
											{
												dwAttrID = (uint)OpcHdaAttrIDs.Stepped,
												FilterOperator = OPCHDA_OPERATORCODES.OPCHDA_EQUAL,
												FilterValue = (bool)filter.ComparisonValue
											});
											bStepped = true;
										}
										else
											throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for Stepped Filter Criterion");
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Multiple Stepped Filter Criteria");
									break;
								case FilterOperandNames.Archiving:
									if (bArchiving)
									{
										if (filter.Operator == FilterOperator.Equal)
										{
											HdaFilterList.Add(new OPCHDA_BROWSEFILTER()
											{
												dwAttrID = (uint)OpcHdaAttrIDs.Archiving,
												FilterOperator = OPCHDA_OPERATORCODES.OPCHDA_EQUAL,
												FilterValue = (bool)filter.ComparisonValue
											});
											bArchiving = true;
										}
										else
											throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for Archiving Filter Criterion");
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Multiple Archiving Filter Criteria");
									break;
								case FilterOperandNames.DerivingEquation:
									if (filter.Operator == FilterOperator.Equal)
									{
										HdaFilterList.Add(new OPCHDA_BROWSEFILTER()
										{
											dwAttrID = (uint)OpcHdaAttrIDs.DeriveEquation,
											FilterOperator = OPCHDA_OPERATORCODES.OPCHDA_EQUAL,
											FilterValue = (string)filter.ComparisonValue
										});
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for DerivingEquation Filter Criterion");
									break;
								case FilterOperandNames.ServerMachineName:
									if (filter.Operator == FilterOperator.Equal)
									{
										HdaFilterList.Add(new OPCHDA_BROWSEFILTER()
										{
											dwAttrID = (uint)OpcHdaAttrIDs.NodeName,
											FilterOperator = OPCHDA_OPERATORCODES.OPCHDA_EQUAL,
											FilterValue = (string)filter.ComparisonValue
										});
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for ServerMachineName Filter Criterion");
									break;
								case FilterOperandNames.ServerName:
									if (filter.Operator == FilterOperator.Equal)
									{
										HdaFilterList.Add(new OPCHDA_BROWSEFILTER()
										{
											dwAttrID = (uint)OpcHdaAttrIDs.ProcessName,
											FilterOperator = OPCHDA_OPERATORCODES.OPCHDA_EQUAL,
											FilterValue = (string)filter.ComparisonValue
										});
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for ServerName Filter Criterion");
									break;
								case FilterOperandNames.ServerType:
									if (filter.Operator == FilterOperator.Equal)
									{
										HdaFilterList.Add(new OPCHDA_BROWSEFILTER()
										{
											dwAttrID = (uint)OpcHdaAttrIDs.SourceType,
											FilterOperator = OPCHDA_OPERATORCODES.OPCHDA_EQUAL,
											FilterValue = (string)filter.ComparisonValue
										});
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for ServerType Filter Criterion");
									break;
								case FilterOperandNames.Name:
									if (filter.Operator == FilterOperator.Equal)
									{
										HdaFilterList.Add(new OPCHDA_BROWSEFILTER()
										{
											dwAttrID = (uint)OpcHdaAttrIDs.ItemID,
											FilterOperator = OPCHDA_OPERATORCODES.OPCHDA_EQUAL,
											FilterValue = (string)filter.ComparisonValue
										});
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for Name Filter Criterion");
									break;
								default:
									break;
							}
						}
						else
							throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Filter Criterion = " + filter);
					}
				}
				// default the browse filter type to look for branches and leaves
				if (currentBrowseContext.BrowseTypesRequested == 0)
					currentBrowseContext.BrowseTypesRequested = (int)OPCBROWSETYPE.OPC_BRANCH | (int)OPCBROWSETYPE.OPC_LEAF;

			}
			else // the filter set is null or empty
				currentBrowseContext.BrowseTypesRequested = (int)OPCBROWSETYPE.OPC_BRANCH | (int)OPCBROWSETYPE.OPC_LEAF;
		}
	}

	internal class OpcHdaBrowser : OpcBrowser
	{
		protected IOPCHDA_ServerCli IOPCHDAServer;
		protected IOPCHDA_BrowserCli IOPCHDABrowser;
		protected OpcHda_BrowseFilters OpcHda_BrowseFilters;
		protected ContextImpl _context;

		public OpcHdaBrowser(ContextImpl context)
			: base()
		{
			if (context.IsAccessibleJournalDataAccess == false)
				context.ThrowDisconnectedServerException(_context.IOPCHDAServer_ProgId);

			BrowserType = (uint)ServerType.OPC_HDA12_Wrapper;
			_context = context;
			IOPCHDAServer = context.IOPCHDA_Server;
		}

		protected override bool Dispose(bool isDisposing)
		{
			if (_hasBeenDisposed)
				return false;

			IOPCHDAServer = null;
			if (isDisposing && null != IOPCHDABrowser)
			{
				IOPCHDABrowser.Dispose();
			}
			IOPCHDABrowser = null;

			return base.Dispose(isDisposing);
		}

		public override void InitBrowser(ObjectPath wrappedServerStart, FilterSet filterSet, bool rootPath)
		{
			// see if the browser filter has changed
			bool CreateNew = true;
			if (IOPCHDABrowser != null)
			{
				if (OpcHda_BrowseFilters.OriginalFilterSet == null)
				{
					if (filterSet == null)
						CreateNew = false;
				}
				else if (filterSet != null)
				{
					if (OpcHda_BrowseFilters.OriginalFilterSet.CompareIdentical(filterSet))
					{
						CreateNew = false;
					}
				}
			}
			if (CreateNew)
			{
				// This is a new browse, so create a new browse context and get the properties of the starting object 
				CurrentBrowseContext = new CurrentBrowseContext(rootPath);
				OpcHda_BrowseFilters = new OpcHda_BrowseFilters(filterSet, CurrentBrowseContext);
				List<HandleAndHRESULT> ErrorsList = null;
				cliHRESULT HR = IOPCHDAServer.CreateBrowse(OpcHda_BrowseFilters.HdaFilterList, out IOPCHDABrowser, out ErrorsList);
				if (HR.Succeeded)
				{
					BrowserType = (uint)ServerType.OPC_HDA12_Wrapper;
				}
				else
				{
					_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCHDAServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create("OPC HDA CreateBrowse() failed.");
				}
			}
			ChangeBrowsePosition(wrappedServerStart);
		}

		/// <summary>
		/// This method calls the OPC HDA ChangeBrowsePosition method repetitively starting from the   
		/// specified element in the starting path to reach the object identified by the starting path.
		/// It also sets the InstanceId in the CurrentDaBrowseInfo.
		/// </summary>
		/// <param name="startingPathOffset">
		/// The index of the element in the start path to begin browsing down.
		/// </param>
		/// <param name="startingPath">
		/// The new Browse position.
		/// </param>
		/// <returns>
		/// Returns 0 if the browse down succeeded. Positive if the browse down failed for 
		/// the leaf of the starting path. Otherwise negative.
		/// </returns>
		public override int ChangeBrowsePositionDown(int startingPathOffset, ObjectPath startingPath)
		{
			int index = 0;

			// validate the request - this should never happen, but check anyway
			if (   (startingPathOffset > 0)
				&& (startingPath != null)
				&& (startingPathOffset >= startingPath.Elements.Count)
			   )
				throw FaultHelpers.Create("OPC HDA ChangeBrowsePosition() Down failed.");

			cliHRESULT HR;
			if (startingPathOffset == 0) // Need to start at the root 
			{
				if (CurrentBrowsePosition.Elements.Count > 0) // if not already at the root, go to the root
				{
					HR = IOPCHDABrowser.ChangeBrowsePosition(OPCHDA_BROWSEDIRECTION.OPCHDA_BROWSE_DIRECT, null);
					if (HR.Failed)
						throw FaultHelpers.Create("OPC HDA ChangeBrowsePosition() to the ROOT failed.");
					CurrentBrowsePosition = new ObjectPath(true);  // current browse position has been reset
				}
			}

			// ... and browse to the new position if starting below the root
			if (startingPath != null)
			{
				for (index = startingPathOffset; index < startingPath.Elements.Count; index++)
				{
					HR = IOPCHDABrowser.ChangeBrowsePosition(OPCHDA_BROWSEDIRECTION.OPCHDA_BROWSE_DOWN,
															 startingPath.Elements[index]);
					if (HR.Succeeded)
					{
						// record the new browse position
						CurrentBrowsePosition.Elements.Add(startingPath.Elements[index]);
					}
					else
					{
						break;
					}
				}
			}

			if ((startingPath == null) || (index == startingPath.Elements.Count)) // success
				return 0;
			else if (index == startingPath.Elements.Count - 1) // failed on the leaf
				return 1;
			return -1; // other failure
		} // end ChangeBrowsePositionDown()

		public override bool BrowseBranches()
		{
			CurrentBrowseContext.BrowseTypeUsed = OPCBROWSETYPE.OPC_BRANCH;
			OPCHDA_BROWSETYPE hdaBrowseType = OPCHDA_BROWSETYPE.OPCHDA_BRANCH;
			cliIEnumString enumString;
			cliHRESULT HR = IOPCHDABrowser.GetEnum(hdaBrowseType,
												   out enumString);
			CurrentBrowseContext.EnumString = enumString;
			if (HR.Failed)
			{
				throw (FaultHelpers.Create((uint)HR.hResult, "OPC HDA GetEnum() for Branches failed."));
			}
			if (CurrentBrowseContext.EnumString != null && !HR.IsS_FALSE)
				return true;
			return false;
		}

		public override bool BrowseLeaves()
		{
			CurrentBrowseContext.BrowseTypeUsed = OPCBROWSETYPE.OPC_LEAF;
			OPCHDA_BROWSETYPE hdaBrowseType = OPCHDA_BROWSETYPE.OPCHDA_LEAF;
			cliIEnumString enumString;
			cliHRESULT HR = IOPCHDABrowser.GetEnum(hdaBrowseType,
												   out enumString);
			CurrentBrowseContext.EnumString = enumString;
			if (HR.Failed)
			{
				throw (FaultHelpers.Create((uint)HR.hResult, "OPC HDA GetEnum() for Leaves failed."));
			}
			if (CurrentBrowseContext.EnumString != null && !HR.IsS_FALSE)
				return true;
			return false;
		}

		public override void GetObjectAttributes(ref ObjectAttributes objectAttrs)
		{
			if (CurrentBrowseContext.BrowseTypeUsed == OPCBROWSETYPE.OPC_BRANCH)
			{
				objectAttrs.InstanceId = GetItemID(objectAttrs.Name);
				objectAttrs.IsLeaf = false;
			}
			else
			{
				objectAttrs.InstanceId = GetItemID(objectAttrs.Name);
				objectAttrs.IsLeaf = true;
			}
		}

		public override bool ApplyFilters(ObjectAttributes oa)
		{
			return true;
		}

		/// <summary>
		/// This method gets the OPC DA Item LocalId and converts it to an InstanceId.
		/// </summary>
		/// <param name="objectName">
		/// The name of the object for which the InstanceId is to be provided.
		/// </param>
		/// <returns>
		/// The Object LocalId converted from the DA ItemId of the object. Null if the 
		/// object does not have an ItemId. </returns>
		protected InstanceId GetItemID(string objectName)
		{
			InstanceId instanceId = null;
			string itemID;
			cliHRESULT HR = IOPCHDABrowser.GetItemID(objectName, out itemID);
			if (HR.Succeeded && itemID.Length > 0)
			{
				instanceId = new InstanceId(InstanceIds.ResourceType_HDA, null, itemID);
			}
			else
			{
				instanceId = new InstanceId(InstanceIds.ResourceType_HDA, null, String.Empty);
			}
			return instanceId;
		}

	}

}
