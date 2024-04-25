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
	internal class OpcDa_BrowseFilters
	{
		private static string Branch = FilterOperandValues.Branch.ToUpper();
		private static string Leaf = FilterOperandValues.Leaf.ToUpper();

		/// <summary>
		/// The original filter criteria. 
		/// </summary>
		public FilterSet OriginalFilterSet;

		/// <summary>
		/// The OPC DA data type filter. 
		/// </summary>
		public ushort DataTypeFilter;

		/// <summary>
		/// The CLI data type filter. 
		/// </summary>
		public TypeId cliDataTypeFilter;

		/// <summary>
		/// The access rights filter. 
		/// </summary>
		public uint AccessRightsFilter;

		/// <summary>
		/// The vendor-specific filter. 
		/// </summary>
		public string VendorFilter;

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
		public OpcDa_BrowseFilters(FilterSet filterSet, CurrentBrowseContext currentBrowseContext)
		{
			// TODO: Set this properly if Vendor Specific Filters are supported
			VendorFilter = "";

			OriginalFilterSet = filterSet;
			DataTypeFilter = (ushort)cliVARENUM.VT_EMPTY; // default value 

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
							string comparisonValue;
							switch (filter.OperandName)
							{
								case FilterOperandNames.BranchOrLeaf:
									if (filter.Operator == FilterOperator.Equal)
									{
										comparisonValue = ((string)filter.ComparisonValue).ToUpper();
										if (comparisonValue == Branch)
											currentBrowseContext.BrowseTypesRequested += (int)OPCBROWSETYPE.OPC_BRANCH;
										else if (comparisonValue == Leaf)
											currentBrowseContext.BrowseTypesRequested += (int)OPCBROWSETYPE.OPC_LEAF;
										else
											throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Value for BranchOrLeaf Filter Criterion: " + (string)filter.ComparisonValue);
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for BranchOrLeaf Filter Criterion");
									break;
								case FilterOperandNames.StartingObjectAttributes:
									if (filter.Operator == FilterOperator.Equal)
									{
										currentBrowseContext.StartingObjectAttributesFilterOperand = (int)filter.ComparisonValue;
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for StartingObjects Filter Criterion");
									break;
								case FilterOperandNames.DataType:
									if (DataTypeFilter == 0)
									{
										if (filter.Operator == FilterOperator.Equal)
										{
											DataTypeFilter = cliVARIANT.UshortTypeFromCliString((string)filter.ComparisonValue);
											cliDataTypeFilter = new TypeId(cliVARIANT.CliTypeFrom(DataTypeFilter));
										}
										else
											throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for DataType Filter Criterion");
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Multiple DataType Filter Criteria");
									break;
								case FilterOperandNames.AccessRight:
									if (filter.Operator == FilterOperator.Equal)
									{
										comparisonValue = ((string)filter.ComparisonValue).ToUpper();
										if (comparisonValue == "READ")
											AccessRightsFilter += (uint)OPCACCESSRIGHTS.OPC_READABLE;
										else if (comparisonValue == "WRITE")
											AccessRightsFilter += (uint)OPCACCESSRIGHTS.OPC_WRITABLE;
										else
											throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Value for AccessRight Filter Criterion: " + (string)filter.ComparisonValue);
									}
									else
										throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Operator for AccessRight Filter Criterion");
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

	internal class OpcDa_CurrentBrowseInfo
	{
		/// <summary>
		/// The OPC DA ItemID for the starting object.
		/// </summary>
		public string ItemID;

		/// <summary>
		/// The list of OPC DA Properties of the starting object.
		/// </summary>
		public List<ObjectAttributes> Properties;
	}

	internal class OpcDaBrowser : OpcBrowser
	{
		public const string OpcDaPropertyPrefix = "OpcDaProperty_";
		protected IOPCBrowseServerAddressSpaceCli IOPCBrowseServerAddressSpace;
		protected IOPCItemPropertiesCli IOPCItemProperties;
		protected OpcDa_CurrentBrowseInfo CurrentDaBrowseInfo;
		protected OpcDa_BrowseFilters BrowseFilters;
		public ContextImpl Context { get { return _context; } }
		protected ContextImpl _context;

		public OpcDaBrowser(ContextImpl context)
			: base()
		{
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(_context.IOPCServer_ProgId);

			BrowserType = (uint)ServerType.OPC_DA205_Wrapper;
			_context = context;
			IOPCBrowseServerAddressSpace = context.IOPCBrowseServerAddressSpace;
			IOPCItemProperties = context.IOPCItemProperties;
		}

		protected override bool Dispose(bool isDisposing)
		{
			if (_hasBeenDisposed)
				return false;

			IOPCBrowseServerAddressSpace = null;
			IOPCItemProperties = null;

			return base.Dispose(isDisposing);
		}

		public override void InitBrowser(ObjectPath wrappedServerStart, FilterSet filterSet, bool rootPath)
		{
			// This is a new browse, so create a new browse context and get the properties of the starting object 
			CurrentDaBrowseInfo = new OpcDa_CurrentBrowseInfo();
			CurrentBrowseContext = new CurrentBrowseContext(rootPath);
			BrowseFilters = new OpcDa_BrowseFilters(filterSet, CurrentBrowseContext);
			ChangeBrowsePosition(wrappedServerStart);

			// if the client wants the ObjectAttributes of the starting object
			if (CurrentBrowseContext.StartingObjectAttributesFilterOperand > 0)
			{
				ObjectAttributes startingObjectAttributes = null;
				// if the ItemID was obtained 
				if (CurrentDaBrowseInfo.ItemID != null)
				{
					startingObjectAttributes = new ObjectAttributes();
					startingObjectAttributes.InstanceId = new InstanceId(InstanceIds.ResourceType_DA, null, CurrentDaBrowseInfo.ItemID);
					GetObjectAttributes(ref startingObjectAttributes);

					// if the name is empty 
					if (string.IsNullOrEmpty(startingObjectAttributes.Name))
					{
						// and the path has a leaf element, use it as the name
						if (wrappedServerStart.Elements.Count > 1)
							startingObjectAttributes.Name = wrappedServerStart.Elements[wrappedServerStart.Elements.Count - 1];
						// otherwise there is no way to get the name
					}
				}
				else if (CurrentBrowseContext.ChangeBrowsePositionResults == CurrentBrowseContext.ChangeBrowsePositionPropertyFail)
				{
					// if the current browse position is the item that owns the OPC DA Property 
					// or if the current browse position is parent of the item that owns the OPC DA Property 
					if (   (CurrentBrowsePosition.Elements.Count == wrappedServerStart.Elements.Count - 1)
						|| (CurrentBrowsePosition.Elements.Count == wrappedServerStart.Elements.Count - 2))
					{
						// null name to be used if the current browse position is the item that owns the OPC DA Property
						string propOwnerObjName = null;
						if (CurrentBrowsePosition.Elements.Count == wrappedServerStart.Elements.Count - 2)
						{
							// the current browse position is the parent of the item that owns the OPC DA Property 
							// so use the name of its child that owns the DA Property
							propOwnerObjName = wrappedServerStart.Elements[wrappedServerStart.Elements.Count - 2];
						}
						InstanceId instId = GetItemID(propOwnerObjName);
						if (instId != null)
						{
							List<ObjectAttributes> objAttrList = QueryCustomProperties(instId.LocalId, 0);
							for (int i = 0; i < objAttrList.Count; i++)
							{
								if (objAttrList[i].Name == wrappedServerStart.Elements[wrappedServerStart.Elements.Count - 1])
								{
									startingObjectAttributes = objAttrList[i];
									break;
								}
							}
						}
					}
				}
				if (startingObjectAttributes != null)
				{
					CurrentBrowseContext.SomethingToReturn = true;
					if (CurrentBrowseContext.ListOfObjectAttributes == null)
						CurrentBrowseContext.ListOfObjectAttributes = new List<ObjectAttributes>();
					CurrentBrowseContext.ListOfObjectAttributes.Add(startingObjectAttributes);
				}
			}
		}

		public override bool BrowseBranches()
		{
			// Only Browse for branches if the change browse position succceeded 
			if (CurrentBrowseContext.ChangeBrowsePositionResults == CurrentBrowseContext.ChangeBrowsePositionSuccess)
			{
				if (_context.IsAccessibleDataAccess == false)
					_context.ThrowDisconnectedServerException(_context.IOPCServer_ProgId);

				CurrentBrowseContext.BrowseTypeUsed = OPCBROWSETYPE.OPC_BRANCH;
				cliIEnumString enumString;
				cliHRESULT HR = IOPCBrowseServerAddressSpace.BrowseOPCItemIDs(OPCBROWSETYPE.OPC_BRANCH,
																			  BrowseFilters.VendorFilter,
																			  BrowseFilters.DataTypeFilter,
																			  BrowseFilters.AccessRightsFilter,
																			  out enumString);
				CurrentBrowseContext.EnumString = enumString;
				if (HR.Failed)
				{
					_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);
					// The next lines will not be executed if the call above throws
					CurrentDaBrowseInfo = new OpcDa_CurrentBrowseInfo();
					throw (FaultHelpers.Create((uint)HR.hResult, "OPC DA BrowseItemIds() for branches failed."));
				}
				if (CurrentBrowseContext.EnumString != null && !HR.IsS_FALSE)
					return true;
			}
			return false;
		}

		public override bool BrowseLeaves()
		{
			bool foundSomething = false;
			CurrentBrowseContext.BrowseTypeUsed = OPCBROWSETYPE.OPC_LEAF;

			// if the change browse position succeeded, or if it failed on a leaf, then proceed by first 
			// looking for custom DA Properties
			if (CurrentBrowseContext.ChangeBrowsePositionResults != CurrentBrowseContext.ChangeBrowsePositionAbsoluteFail)
			{
				if (CurrentBrowseContext.StartingObjectAttributesFilterOperand != (int)StartingObjectFilterValues.StartingObjectOnly)
					CurrentDaBrowseInfo.Properties = QueryCustomProperties(CurrentDaBrowseInfo.ItemID, BrowseFilters.DataTypeFilter);

				if (CurrentDaBrowseInfo.Properties != null)
					foundSomething = true;

				// if the browse position was successfully changed to the starting object, then browse for leaves
				if (CurrentBrowseContext.ChangeBrowsePositionResults == CurrentBrowseContext.ChangeBrowsePositionSuccess)
				{
					if (_context.IsAccessibleDataAccess == false)
						_context.ThrowDisconnectedServerException(_context.IOPCServer_ProgId);

					// The current browse position is the starting path
					cliIEnumString enumString;
					cliHRESULT HR = IOPCBrowseServerAddressSpace.BrowseOPCItemIDs(OPCBROWSETYPE.OPC_LEAF,
																				  BrowseFilters.VendorFilter,
																				  BrowseFilters.DataTypeFilter,
																				  BrowseFilters.AccessRightsFilter,
																				  out enumString);
					CurrentBrowseContext.EnumString = enumString;
					if (HR.Failed)
					{
						_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);
						// The next lines will not be executed if the call above throws
						CurrentDaBrowseInfo = new OpcDa_CurrentBrowseInfo();
						throw (FaultHelpers.Create((uint)HR.hResult, "OPC DA BrowseItemIds() for leaves failed."));
					}
					if (CurrentBrowseContext.EnumString != null && !HR.IsS_FALSE)
						foundSomething = true;
				}
			}
			return foundSomething;
		}

		/// <summary>
		/// This method gets the DataTypeId, IsWritable, and FastestScanRate attributes of an object 
		/// from the corresponding OPC DA Properties.  Null if the object is not a data object.
		/// </summary>
		/// <param name="objectAttrs">
		/// This ref parameter contains the name of the object, supplied by the caller, 
		/// and the attributes whose value is to be provided by the method.
		/// </param>
		public override void GetObjectAttributes(ref ObjectAttributes objectAttrs)
		{
			// If the instance id is not present, get it
			if ((objectAttrs.InstanceId == null) || (string.IsNullOrEmpty(objectAttrs.InstanceId.FullyQualifiedId)))
			{
				// if the name is null, then passing in a null will return the item id of the current browse position
				// otherwise, the name must be the name of an item that was returned by browsing from the current 
				// browse position (the name of one of the elements below the current browse position)
				objectAttrs.InstanceId = GetItemID(objectAttrs.Name);
			}
			objectAttrs.IsLeaf = false;

			if (   (objectAttrs.InstanceId != null)
				&& (objectAttrs.InstanceId.LocalId.Length != 0)
			   )
			{
				if (_context.IsAccessibleDataAccess == false)
					_context.ThrowDisconnectedServerException(_context.IOPCServer_ProgId);

				List<ItemProperty> itemProperties = null;
				List<uint> stdPropIds = new List<uint>();
				bool customProperty = false;
				cliHRESULT HR = IOPCItemProperties.QueryAvailableProperties(objectAttrs.InstanceId.LocalId, out itemProperties);
				if (HR.Failed)
				{
					_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);

					if (   (HR.hResult != cliHR.OPC_E_UNKNOWNITEMID)
						&& (HR.hResult != cliHR.OPC_E_INVALIDITEMID)
					   )
					{
						// TODO: Add code here and additional conditions in the if statement above to catch 
						//       real failures of the DA Server.  If a real failure occurs, throw the following:
						//       throw FaultHelpers.Create((uint)HR.hResult, "QueryAvailableProperties failed.");
					}
				}
				else
				{
					if (itemProperties != null)
					{
						foreach (var prop in itemProperties)
						{
							if (prop.PropertyID == (uint)OpcDaPropIDs.DataType)
								stdPropIds.Add((uint)OpcDaPropIDs.DataType);
							else if (prop.PropertyID == (uint)OpcDaPropIDs.ItemAccessRights)
								stdPropIds.Add((uint)OpcDaPropIDs.ItemAccessRights);
							else if (prop.PropertyID == (uint)OpcDaPropIDs.ServerScanRate)
								stdPropIds.Add((uint)OpcDaPropIDs.ServerScanRate);
							else if (prop.PropertyID > (uint)7)
							{
								customProperty = true;
								objectAttrs.IsLeaf = false; // set to false if this item has properties that will become objects below it
							}
						}
					}
				}

				// Add the role of this object and set the IsLeaf attribute
				if (objectAttrs.Roles == null)
					objectAttrs.Roles = new List<TypeId>();

				if (   (CurrentBrowseContext.BrowseTypeUsed == OPCBROWSETYPE.OPC_LEAF)
					|| (CurrentBrowseContext.ChangeBrowsePositionResults == CurrentBrowseContext.ChangeBrowsePositionLeafFail))
				{
					objectAttrs.Roles.Add(ObjectRoleIds.OpcLeafRoleId);
					if (customProperty == false)
					{
						// this object is a leaf if it is a DA Leaf Item with no DA Properties
						objectAttrs.IsLeaf = true;
					}
				}
				else
				{
					objectAttrs.Roles.Add(ObjectRoleIds.OpcBranchRoleId);
				}

				// get the attributes for the standard properties supported by the DA Item/Branch
				List<PropertyValue> propValues = null;
				if (stdPropIds.Count > 0)
				{
					HR = IOPCItemProperties.GetItemProperties(objectAttrs.InstanceId.LocalId, stdPropIds, out propValues);
					if (HR.Succeeded)
					{
						int i = 0;
						foreach (var propId in stdPropIds)
						{
							if ((propValues.Count > i) && (propValues[i].hResult == cliHR.S_OK))
							{
								if (propId == (uint)OpcDaPropIDs.DataType)
									objectAttrs.DataTypeId = new TypeId(cliVARIANT.CliTypeFrom(propValues[i].vDataValue));
								else if (propId == (uint)OpcDaPropIDs.ItemAccessRights)
								{
									int accessRights = (int)propValues[i].vDataValue;
									objectAttrs.IsReadable = ((accessRights & (int)OpcDaAccessRights.Readable) > 0);
									objectAttrs.IsWritable = ((accessRights & (int)OpcDaAccessRights.Writable) > 0);
								}
								else if (propId == (uint)OpcDaPropIDs.ServerScanRate)
								{
									objectAttrs.FastestScanRate = null;
									try
									{
										objectAttrs.FastestScanRate = Convert.ToUInt32(propValues[2].vDataValue.DataValue);
									}
									catch (OverflowException)
									{
										objectAttrs.FastestScanRate = uint.MaxValue;
									}
									catch (InvalidCastException)
									{
										objectAttrs.FastestScanRate = 0;
									}
								}
							}
							i++;
						}
					}
					else
					{
						_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);

						if (   (HR.hResult != cliHR.OPC_E_UNKNOWNITEMID)
							&& (HR.hResult != cliHR.OPC_E_INVALIDITEMID))
						{
							string msg = "OPC DA GetItemProperties() failed for ItemId: " + objectAttrs.InstanceId.LocalId
									   + ", Result Code: " + String.Format("0x{0:X}", HR.hResult);
							throw FaultHelpers.Create((uint)HR.hResult, msg);
						}
					}
				}
			}
		}

		public override bool ApplyFilters(ObjectAttributes oa)
		{
			// if nothing to check, return true
			if (   ((BrowseFilters == null)
				|| (BrowseFilters.OriginalFilterSet == null))
				|| (oa.DataTypeId == null))
				return true;

			bool bPass = true;
			if (BrowseFilters != null)
			{
				if ((BrowseFilters.cliDataTypeFilter != null) && (oa.DataTypeId != null))
				{
					bPass = oa.DataTypeId.Compare(BrowseFilters.cliDataTypeFilter);
				}
				if ((bPass) && (BrowseFilters.AccessRightsFilter > 0))
				{
					bool readableFilter = ((BrowseFilters.AccessRightsFilter & (uint)OPCACCESSRIGHTS.OPC_READABLE) != 0);
					// if checking for readable and oa is not readable
					if ((readableFilter) && (!oa.IsReadable))
						bPass = false;
					bool writableFilter = ((BrowseFilters.AccessRightsFilter & (uint)OPCACCESSRIGHTS.OPC_WRITABLE) != 0);
					// if checking for writable and oa is not writable
					if ((writableFilter) && (!oa.IsWritable))
						bPass = false;
				}
				if ((BrowseFilters.OriginalFilterSet != null) && (BrowseFilters.OriginalFilterSet.Not))
					bPass = !bPass; // complement bPass if Not is true
			}
			return bPass;
		}

		/// <summary>
		/// This method calls the OPC DA ChangeBrowsePosition method repetitively starting from the   
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
		/// <para>Returns CurrentBrowseContext.ChangeBrowsePositionSuccess if the browse down succeeded. </para>
		/// <para>Returns CurrentBrowseContext.ChangeBrowsePositionLeafFail if the browse failed on the leaf of 
		/// the ObjectPath. </para>
		/// <para>Returns CurrentBrowseContext.ChangeBrowsePositionInstanceIdFail if the browse down failed on 
		/// the InstanceId. </para>
		/// <para>Otherwise, returns CurrentBrowseContext.ChangeBrowsePositionAbsoluteFail. </para>
		/// </returns>
		public override int ChangeBrowsePositionDown(int startingPathOffset, ObjectPath startingPath)
		{
			int index = 0;
			int returnCode = CurrentBrowseContext.ChangeBrowsePositionNotSet;

			// validate the request - this should never happen, but check anyway
			if (   (startingPath != null) // if there is a starting path
				&& (startingPath.Elements.Count > 0) // and it has elements
				&& (startingPathOffset >= startingPath.Elements.Count) // but the zero-based offset starts after the last element 
			   )
				throw FaultHelpers.Create("Invalid starting path = " + startingPath.ToString());

			cliHRESULT HR;
			if ( startingPath != null &&    // Check (klocwork detects error here). TODO: testing result
                startingPathOffset == 0 ) // Need to start at the root or at an instance id
			{
				if (CurrentBrowseContext.IsRootPath == false)
				{
					InstanceId instanceId = new InstanceId(startingPath.Elements[0]);
					if ((instanceId.ResourceType != null) && (instanceId.ResourceType != InstanceIds.ResourceType_DA))
						throw FaultHelpers.Create("Invalid Resource Type in Starting Instance Id = " + instanceId.LocalId);
					else if (instanceId.System != null)
						throw FaultHelpers.Create("This server does not support use of System Name in Starting Instance Id = " + instanceId.LocalId);
					else if (_context.IsAccessibleDataAccess == false)
						_context.ThrowDisconnectedServerException(_context.IOPCServer_ProgId);
					else
					{
						string itemID = instanceId.LocalId;
						HR = IOPCBrowseServerAddressSpace.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_TO, itemID);
						if (HR.Failed)
						{
							_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);

							// the new browse position could not be reached - it could be a DA Leaf, so if it is the only 
							// element of the starting path, check to see if leaves are to be browsed, if not, throw an exception
							// This includes throwing an exception if browsing to an ItemId of a branch failed, since this is 
							// supposed to work. Note that the item identified may be a leaf, but if the client is looking for 
							// branches under it, then it will also cause the exception to be thrown.
							if (   ((CurrentBrowseContext.BrowseTypesRequested & (int)OPCBROWSETYPE.OPC_LEAF) != 0)
								&& (startingPath.Elements.Count == 1)
							   )
							{
								// Test to see if this is a good ItemID for a DA Leaf
								List<ItemProperty> itemProperties = null;
								HR = IOPCItemProperties.QueryAvailableProperties(itemID, out itemProperties);
								if (HR.Failed)
								{
									_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);

									// TODO:  Add code here to test HResult values to catch real failures of the DA Server.
									//        If a real failure occurs, throw the following:
									//        throw FaultHelpers.Create("Invalid InstanceId = " + instanceId.FullyQualifiedId);
									if (HR.hResult == cliHR.OPC_E_UNKNOWNITEMID)
										throw FaultHelpers.Create((uint)HR.hResult, "QueryAvailableProperties failed: Unknown ItemID " + itemID);

								}
								CurrentDaBrowseInfo.ItemID = itemID;
								returnCode = CurrentBrowseContext.ChangeBrowsePositionLeafFail;
							}
							else
							{
								throw FaultHelpers.Create("OPC DA ChangeBrowsePosition() to " + startingPath.Elements[0] + " failed.");
							}
						}
						else // only change the CurrentBrowsePosition if the ChangeBrowsePosition() was successful
						{
							CurrentBrowsePosition = new ObjectPath(instanceId.FullyQualifiedId, null);  // current browse position has been set
							CurrentDaBrowseInfo.ItemID = itemID;
						}
						startingPathOffset = 1; // bump to element in the path after the instance id
					}
				}
				else
				{
					if (CurrentBrowsePosition.Elements.Count > 0) // if not already at the root, go to the root
					{
						HR = IOPCBrowseServerAddressSpace.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_TO, null);
						if (HR.Failed)
						{
							_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);
							// The next line will not be executed if the call above throws
							throw FaultHelpers.Create("OPC DA ChangeBrowsePosition() to the ROOT failed.");
						}
						CurrentBrowsePosition = new ObjectPath(true);  // current browse position has been reset
						startingPathOffset = 1;  // bump to element in the path after the root
					}
				}
			}

			// ... and browse to the new position if starting below the root
			if (returnCode == CurrentBrowseContext.ChangeBrowsePositionNotSet)// if not already set
			{
				if (startingPath != null)
				{
					for (index = startingPathOffset; index < startingPath.Elements.Count; index++)
					{
						// if at the leaf
						if (index == startingPath.Elements.Count - 1)
						{
							// Get the item id for the starting object (call GetItemID from the parent and supply the name of the starting object
							HR = IOPCBrowseServerAddressSpace.GetItemID(startingPath.Elements[startingPath.Elements.Count - 1],
																							  out CurrentDaBrowseInfo.ItemID);
							if (HR.Failed)
							{
								_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);
								// The next line will not be executed if the call above throws
								CurrentDaBrowseInfo.ItemID = null;
							}
						}
						HR = IOPCBrowseServerAddressSpace.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_DOWN,
																			   startingPath.Elements[index]);
						if (HR.Succeeded)
						{
							// record the new browse position
							CurrentBrowsePosition.Elements.Add(startingPath.Elements[index]);
						}
						else
						{
							_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);
							// The next lines will not be executed if the call above throws

							if (CurrentDaBrowseInfo.ItemID != null)
								returnCode = CurrentBrowseContext.ChangeBrowsePositionLeafFail;
							else
							{
								// if trying to browse to a DA Property...
								string str = startingPath.Elements[startingPath.Elements.Count - 1];
								if (str.Length > OpcDaBrowser.OpcDaPropertyPrefix.Length)
								{
									string prefix = str.Substring(0, OpcDaBrowser.OpcDaPropertyPrefix.Length);
									if (string.Compare(prefix, OpcDaBrowser.OpcDaPropertyPrefix) == 0)
									{
										returnCode = CurrentBrowseContext.ChangeBrowsePositionPropertyFail;
									}
								}
							}
							break;
						}
					}
				}

				if (returnCode == CurrentBrowseContext.ChangeBrowsePositionNotSet)
				{
					if ((startingPath == null) || (index == startingPath.Elements.Count)) // success
						returnCode = CurrentBrowseContext.ChangeBrowsePositionSuccess;
					else
						returnCode = CurrentBrowseContext.ChangeBrowsePositionAbsoluteFail; // other failure
				}
			}
			return returnCode;
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
			if (_context.IsAccessibleDataAccess == false)
				_context.ThrowDisconnectedServerException(_context.IOPCServer_ProgId);

			InstanceId instanceId = null;
			string itemID;
			cliHRESULT HR = IOPCBrowseServerAddressSpace.GetItemID(objectName, out itemID);
			if (HR.Succeeded)
			{
				if (itemID.Length > 0)
				{
					instanceId = new InstanceId(InstanceIds.ResourceType_DA, null, itemID);
				}
			}
			else
			{
				_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);
				// TODO:  Add code here to catch real failures of the DA Server
				//        If a real failure occurs, throw the following:
				//        throw FaultHelpers.Create((uint)HR.hResult, "OPC DA GetItemId() failed for object name: " + objectName);
			}
			return instanceId;
		}

		/// <summary>
		/// This method returns the list of ObjectAttributes for the custom (server-specific) 
		/// OPC DA Properties supported by an object.
		/// </summary>
		/// <param name="itemID">
		/// The itemID of the item for which properties are requested.
		/// </param>
		/// <param name="DataTypeFilter">
		/// The Data Type Filter to be used to filter properties.
		/// </param>
		/// <returns>
		/// The list of ObjectAttributes created from the properties of the object.
		/// </returns>
		protected List<ObjectAttributes> QueryCustomProperties(string itemID, ushort dataTypeFilter)
		{
			if (_context.IsAccessibleDataAccess == false)
				_context.ThrowDisconnectedServerException(_context.IOPCServer_ProgId);

			if ((itemID == null) || (itemID.Length == 0))
				return null;

			List<ObjectAttributes> listOfObjAttrs = new List<ObjectAttributes>();
			// QueryAvailableProperties, and for those with a propId > 7, add them to the list to return,
			// and get their item ids if they exist
			List<ItemProperty> itemProperties = null;
			cliHRESULT HR = IOPCItemProperties.QueryAvailableProperties(itemID, out itemProperties);
			if (HR.Failed)
			{
				_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);
				// The next line will not be executed if the call above throws
				if (   (HR.hResult != cliHR.OPC_E_UNKNOWNITEMID)
					&& (HR.hResult != cliHR.OPC_E_INVALIDITEMID)
				   )
				{
					// TODO:  Add code here and additional conditions in the if statement above
					//       to catch real failures of the DA Server. If a real failure occurs, 
					//       throw the following:
					//        throw FaultHelpers.Create((uint)HR.hResult, "QueryAvailableProperties failed.");
				}
			}
			else
			{
				if (itemProperties != null)
				{
					List<uint> propIds = new List<uint>();
					foreach (var prop in itemProperties)
					{
						if (prop.PropertyID > 7)
						{
							if ((dataTypeFilter == 0) || (prop.PropDataType == dataTypeFilter))
							{
								// TODO:  Customize any of the ObjectAttributes, especially oa.Name, 
								//        for the property
								ObjectAttributes oa = new ObjectAttributes();
								oa.IsLeaf = true;
								oa.IsWritable = false;
								oa.Name = OpcDaBrowser.OpcDaPropertyPrefix + prop.PropertyID.ToString();
								oa.Description = prop.Description;

								string nameSpace;
								if (prop.PropertyID < 5000)
									nameSpace = XiNamespace.OPCDA205;
								else
									nameSpace = XiOPCWrapperServer.ServerDescription.VendorNamespace;

								oa.ObjectTypeId = new TypeId(XiSchemaType.OPC, nameSpace, prop.PropertyID.ToString());

								oa.DataTypeId = new TypeId(cliVARIANT.CliTypeFrom(prop.PropDataType));

								if (oa.Roles == null)
									oa.Roles = new List<TypeId>();
								oa.Roles.Add(ObjectRoleIds.OpcPropertyRoleId);

								listOfObjAttrs.Add(oa);
								propIds.Add(prop.PropertyID);
							}
						}
					}
					// see if the properties have item ids
					if (propIds.Count > 0)
					{
						List<PropertyItemID> propItemIDs = new List<PropertyItemID>();
						HR = IOPCItemProperties.LookupItemIDs(itemID, propIds, out propItemIDs);
						if (HR.Failed)
						{
							_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);
							// The next line will not be executed if the call above throws
							throw FaultHelpers.Create((uint)HR.hResult, "LookupItemIDs failed for " + itemID + ".");
						}
						else
						{
							for (int i = 0; i < propIds.Count; i++)
							{
								if (propItemIDs[i].hResult == cliHR.S_OK)
								{
									listOfObjAttrs[i].InstanceId = new InstanceId(InstanceIds.ResourceType_DA, null, propItemIDs[i].ItemID);

									// see if the ItemID is writable
									List<uint> accessRightsPropIdList = new List<uint> { (uint)OpcDaPropIDs.ItemAccessRights };
									List<PropertyValue> propValues = null;
									HR = IOPCItemProperties.GetItemProperties(propItemIDs[i].ItemID, accessRightsPropIdList, out propValues);
									if (HR.Succeeded)
									{
										if (propValues[0].hResult == cliHR.S_OK)
										{
											int accessRights = (int)propValues[0].vDataValue;
											listOfObjAttrs[i].IsReadable = ((accessRights & (int)OpcDaAccessRights.Readable) > 0);
											listOfObjAttrs[i].IsWritable = ((accessRights & (int)OpcDaAccessRights.Writable) > 0);
										}
									}
									else
										_context.ThrowOnDisconnectedServer(HR.hResult, _context.IOPCServer_ProgId);
								}
							}
						}
					}
				}
			}
			if (listOfObjAttrs.Count == 0)
				listOfObjAttrs = null;
			return listOfObjAttrs;
		}

		public override List<ObjectAttributes> ConvertPropertiesToObjects(uint numberToReturn, List<ObjectAttributes> objectAttributesList)
		{
			List<ObjectAttributes> listOfObjAttrs = objectAttributesList;
			if (CurrentDaBrowseInfo.Properties != null)
			{
				int i = 0;
				for (i = 0; (i < numberToReturn) && (i < CurrentDaBrowseInfo.Properties.Count); i++)
				{
					listOfObjAttrs.Add(CurrentDaBrowseInfo.Properties[i]);
				}
				CurrentDaBrowseInfo.Properties.RemoveRange(0, i);
			}
			return listOfObjAttrs;
		}

	}

}
