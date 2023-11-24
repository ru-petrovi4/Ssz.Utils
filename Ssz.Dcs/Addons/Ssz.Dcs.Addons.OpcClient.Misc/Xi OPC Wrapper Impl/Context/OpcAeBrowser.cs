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
	/// The Browse filters used by the OpcDaBrowser. Although the DA and AE Browsers 
	/// use a new filter for each browse request, the HDA Browser uses the same 
	/// filter for all browse requests. Therefore, if the HDA browse filter changes, 
	/// a new HDA Browser must be instantiated. As a result, the Browse Filters are 
	/// not included in the CurrentBrowseContext because the CurrentBrowseContext is 
	/// newly instantiated for each new browse request (but not for continuation 
	/// requests).
	/// </summary>
	internal class OpcAe_BrowseFilters
	{
		/// <summary>
		/// The original filter criteria. 
		/// </summary>
		public FilterSet OriginalFilterSet;

		/// <summary>
		/// The AE server-specific filter. 
		/// </summary>
		public string FilterString;

		/// <summary>
		/// This constructor parses the list of filter criteria and converts it into OPC DA Browse 
		/// filter criteria.  The current browse context is passed in as a parameter because the 
		/// BrowseType can be included in the filter, and is defaulted if it is not. 
		/// </summary>
		/// <param name="filterSet">
		/// The FilterSet that contains the filter parameters.
		/// </param>
		/// <param name="currentBrowseContext">
		/// The current context for the browse.
		/// </param>
		public OpcAe_BrowseFilters(FilterSet filterSet, CurrentBrowseContext currentBrowseContext)
		{
			OriginalFilterSet = filterSet;
			FilterString = null;

			currentBrowseContext.BrowseTypesRequested = 0;
			if ((filterSet != null) && (filterSet.Filters != null))
			{
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
								case FilterOperandNames.Name:
									if (filter.Operator == FilterOperator.Equal)
									{
										FilterString = (string)filter.ComparisonValue;
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for Name Filter Criterion");
									break;
								default: break;
							}
						}
						else
							throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Filter Criterion = " + filter);
					}
				}
				// default the browse filter type to look for branches and leaves, if it wasn't set
				if (currentBrowseContext.BrowseTypesRequested == 0)
					currentBrowseContext.BrowseTypesRequested = (int)OPCBROWSETYPE.OPC_BRANCH | (int)OPCBROWSETYPE.OPC_LEAF;
			}
			else // the filter set is null or empty
				currentBrowseContext.BrowseTypesRequested = (int)OPCBROWSETYPE.OPC_BRANCH | (int)OPCBROWSETYPE.OPC_LEAF;
		}
	}

	internal class OpcAeBrowser : OpcBrowser
	{
		protected IOPCEventServerCli IOPCEventServer;
		protected IOPCEventAreaBrowserCli IOPCEventAreaBrowser;
		protected OpcAe_BrowseFilters OpcAe_BrowseFilters;
		protected ContextImpl _context;

		public OpcAeBrowser(ContextImpl context)
			: base()
		{
			if (context.IsAccessibleAlarmsAndEvents == false)
				context.ThrowDisconnectedServerException(_context.IOPCEventServer_ProgId);

			_context = context;
			cliHRESULT HR = context.IOPCEventServer.CreateAreaBrowser(out IOPCEventAreaBrowser);
			if (HR.Succeeded)
			{
				BrowserType = (uint)ServerType.OPC_AE11_Wrapper;
				IOPCEventServer = context.IOPCEventServer;
			}
			else
			{
				_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCEventServer_ProgId);
				// The next line will not be executed if the call above throws
				throw FaultHelpers.Create("OPC AE CreateAreaBrowser() failed.");
			}
		}

		protected override bool Dispose(bool isDisposing)
		{
			if (_hasBeenDisposed)
				return false;

			IOPCEventServer = null;
			if (isDisposing && null != IOPCEventAreaBrowser)
			{
				IOPCEventAreaBrowser.Dispose();
			}
			IOPCEventAreaBrowser = null;

			return base.Dispose(isDisposing);
		}

		public override void InitBrowser(ObjectPath wrappedServerStart, FilterSet filterSet, bool rootPath)
		{
			// This is a new browse, so create a new browse context and get the properties of the starting object 
			CurrentBrowseContext = new CurrentBrowseContext(rootPath);
			OpcAe_BrowseFilters = new OpcAe_BrowseFilters(filterSet, CurrentBrowseContext);
			ChangeBrowsePosition(wrappedServerStart);
		}

		/// <summary>
		/// This method calls the OPC AE ChangeBrowsePosition method repetitively starting from the   
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
				throw FaultHelpers.Create("OPC AE ChangeBrowsePosition() Down failed.");

			cliHRESULT HR;
			if (startingPathOffset == 0) // Need to start at the root 
			{
				if (CurrentBrowsePosition.Elements.Count > 0) // if not already at the root, go to the root
				{
					HR = IOPCEventAreaBrowser.ChangeBrowsePosition(cliOPCAEBROWSEDIRECTION.OPCAE_BROWSE_TO, null);
					if (HR.Failed)
						throw FaultHelpers.Create("OPC AE ChangeBrowsePosition() to the ROOT failed.");
					CurrentBrowsePosition = new ObjectPath(true);  // current browse position has been reset
				}
			}

			// ... and browse to the new position if starting below the root
			if (startingPath != null)
			{
				for (index = startingPathOffset; index < startingPath.Elements.Count; index++)
				{
					HR = IOPCEventAreaBrowser.ChangeBrowsePosition(cliOPCAEBROWSEDIRECTION.OPCAE_BROWSE_DOWN,
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
			cliOPCAEBROWSETYPE aeBrowseType = cliOPCAEBROWSETYPE.OPC_AREA;
			cliIEnumString enumString;
			cliHRESULT HR = IOPCEventAreaBrowser.BrowseOPCAreas(aeBrowseType,
														   OpcAe_BrowseFilters.FilterString,
														   out enumString);
			CurrentBrowseContext.EnumString = enumString;
			if (HR.Failed)
			{
				throw FaultHelpers.Create((uint)HR.hResult, "OPC AE BrowseOPCAreas() for Areas failed.");
			}
			if (CurrentBrowseContext.EnumString != null && !HR.IsS_FALSE)
				return true;
			return false;
		}

		public override bool BrowseLeaves()
		{
			CurrentBrowseContext.BrowseTypeUsed = OPCBROWSETYPE.OPC_LEAF;
			cliOPCAEBROWSETYPE aeBrowseType = cliOPCAEBROWSETYPE.OPC_SOURCE;
			cliIEnumString enumString;
			cliHRESULT HR = IOPCEventAreaBrowser.BrowseOPCAreas(aeBrowseType,
														   OpcAe_BrowseFilters.FilterString,
														   out enumString);
			CurrentBrowseContext.EnumString = enumString;
			if (HR.Failed)
			{
				throw FaultHelpers.Create((uint)HR.hResult, "OPC AE BrowseOPCAreas() for Areas failed.");
			}
			if (CurrentBrowseContext.EnumString != null && !HR.IsS_FALSE)
				return true;
			return false;
		}

		/// <summary>
		/// This method parses the list of filter criteria and converts it into 
		/// an OPC AE Browse filter string.
		/// </summary>
		/// <param name="criteria">
		/// The list of filter criteria.
		/// </param>
		/// <returns>
		/// The AE filter string.
		/// </returns>
		public override void GetObjectAttributes(ref ObjectAttributes objectAttrs)
		{
			if (CurrentBrowseContext.BrowseTypeUsed == OPCBROWSETYPE.OPC_BRANCH)
			{
				objectAttrs.InstanceId = GetQualifiedName(true, objectAttrs.Name);
				objectAttrs.IsLeaf = false;
				objectAttrs.Roles = new List<TypeId>();
				objectAttrs.Roles.Add(ObjectRoleIds.AreaRoleId);
			}
			else
			{
				objectAttrs.InstanceId = GetQualifiedName(false, objectAttrs.Name);
				objectAttrs.IsLeaf = true;
				objectAttrs.Roles = new List<TypeId>();
				objectAttrs.Roles.Add(ObjectRoleIds.EventSourceRoleId);
			}
		}

		public override bool ApplyFilters(ObjectAttributes oa)
		{
			return true;
		}

		/// <summary>
		/// This method gets the OPC AE Qualified Area Name or Source Name from the AE 
		/// server and converts it to an InstanceId.
		/// </summary>
		/// <param name="bGetAreaName">
		/// Indicates, when TRUE, that the Qualified Area Name is to be retrieved.
		/// When FALSE, the Qualified Source Name is to be retrieved. 
		/// </param>
		/// <param name="name">
		/// The name of the Area or the Event Source for which the InstanceId is to be provided.
		/// </param>
		/// <returns>
		/// The Object LocalId converted from the AE Qualified Area or Source Name. </returns>
		protected InstanceId GetQualifiedName(bool bGetAreaName, string name)
		{
			InstanceId instanceId = null;
			string opcQualifiedName;
			cliHRESULT HR = (bGetAreaName)
						  ? IOPCEventAreaBrowser.GetQualifiedAreaName(name, out opcQualifiedName)
						  : IOPCEventAreaBrowser.GetQualifiedSourceName(name, out opcQualifiedName);
			if (HR.Succeeded)
			{
				if (opcQualifiedName.Length > 0)
					instanceId = new InstanceId(InstanceIds.ResourceType_AE, null, opcQualifiedName);
			}
			else
			{
				string msg = "OPC AE GetQualifiedAreaName() failed for area name: " + name;
				throw FaultHelpers.Create((uint)HR.hResult, msg);
			}
			return instanceId;
		}

	}

}
