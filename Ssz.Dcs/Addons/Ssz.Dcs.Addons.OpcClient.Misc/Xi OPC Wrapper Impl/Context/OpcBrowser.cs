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
using System.Linq;

using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.OPC.COM.API;
using Xi.Server.Base;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// The browse info used for a new browse request, and reused for continuations.  
	/// For each new browse request, a new CurrentBrowseContext is instantiated.
	/// </summary>
	internal class CurrentBrowseContext
		: IDisposable
	{
		public const int ChangeBrowsePositionNotSet       = -1;
		public const int ChangeBrowsePositionSuccess      = 0;
		public const int ChangeBrowsePositionLeafFail     = 1;
		public const int ChangeBrowsePositionPropertyFail = 2;
		public const int ChangeBrowsePositionAbsoluteFail = 3;

		public const bool RootPathTrue  = true;
		public const bool RootPathFalse = false;

		public CurrentBrowseContext(bool isRootPath)
		{
			IsRootPath = isRootPath;
		}

		~CurrentBrowseContext()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			if (Dispose(true))
				GC.SuppressFinalize(this);
		}

		public bool Dispose(bool isDisposing)
		{
			if (_hasBeenDisposed)
				return false;

			if (isDisposing && null != _enumString)
			{
				_enumString.Dispose();
			}
			_enumString = null;
			ListOfObjectAttributes = null;

			_hasBeenDisposed = true;
			return true;
		}
		private bool _hasBeenDisposed = false;

		/// <summary>
		/// The StartingObjectAttributes FilterOperand. See the FilterOperand class 
		/// for the definition of StartingObjectAttributes.
		/// </summary>
		public int StartingObjectAttributesFilterOperand;

		/// <summary>
		/// When true, the StartingPath is a relative path. 
		/// </summary>
		public bool IsRootPath;

		/// <summary>
		/// The browser found something to return. 
		/// </summary>
		public bool SomethingToReturn;

		/// <summary>
		/// The results of the attempt to change the browse position while setting 
		/// the browse context. 
		/// 0 if the starting path is the new browse position. 
		/// 1 if the parent of the leaf of the starting path is the new browse position 
		///		=>the browse failed when moving to the leaf, 
		/// -1 for all other cases (the change browse position failed).
		/// </summary>
		public int ChangeBrowsePositionResults;

		/// <summary>
		/// The browse types requested. 
		/// </summary>
		public int BrowseTypesRequested;

		/// <summary>
		/// The browse type used. 
		/// </summary>
		public OPCBROWSETYPE BrowseTypeUsed;

		/// <summary>
		/// The EnumString enumerator returned by any of the wrapped OPC servers.
		/// </summary>
		public cliIEnumString EnumString
		{
			get { return _enumString; }
			set
			{
				if (null != _enumString)
				{
					_enumString.Dispose();
					_enumString = null;
				}
				_enumString = value;
			}
		}
		private cliIEnumString _enumString;

		/// <summary>
		/// The EnumString enumerator returned by any of the wrapped OPC servers or if 
		/// the client requests the attributes of the starting object.
		/// </summary>
		public List<ObjectAttributes> ListOfObjectAttributes;

		public List<ObjectAttributes> CopyDestructiveListOfObjectAttributes(int numberToCopy)
		{
			List<ObjectAttributes> listOfObjAttrs = new List<ObjectAttributes>();
			numberToCopy = (ListOfObjectAttributes.Count < numberToCopy)
						 ? ListOfObjectAttributes.Count
						 : numberToCopy;

			if (ListOfObjectAttributes.Count == numberToCopy)
			{
				listOfObjAttrs = ListOfObjectAttributes;
				ListOfObjectAttributes = new List<ObjectAttributes>();
			}
			else
			{
				for (int i = 0; i < numberToCopy; i++)
				{
					listOfObjAttrs.Add(ListOfObjectAttributes[i]);
				}
				ListOfObjectAttributes.RemoveRange(0, numberToCopy);
			}
			return listOfObjAttrs;
		}
	}

	internal class OpcBrowser
		: IDisposable
	{
		public OpcBrowser()
		{
			CurrentBrowseContext = new CurrentBrowseContext(CurrentBrowseContext.RootPathTrue);
		}

		~OpcBrowser()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			if (Dispose(true))
				GC.SuppressFinalize(this);
		}

		protected virtual bool Dispose(bool isDisposing)
		{
			if (_hasBeenDisposed)
				return false;

			if (isDisposing && null != CurrentBrowseContext)
			{
				CurrentBrowseContext.Dispose();
			}
			CurrentBrowseContext = null;

			_hasBeenDisposed = true;
			return true;
		}
		protected bool _hasBeenDisposed = false;

		/// <summary>
		/// The browse info used in the FindObjects() call.  It is instantiated 
		/// for each new FindObjects call, but not for continuation calls (those 
		/// whose FindCriteria is null).
		/// </summary>
		public CurrentBrowseContext CurrentBrowseContext
		{
			get { return _currentBrowseContext; }
			set
			{
				if (null != _currentBrowseContext)
				{
					_currentBrowseContext.Dispose();
					_currentBrowseContext = null;
				}
				_currentBrowseContext = value;
			}
		}
		private CurrentBrowseContext _currentBrowseContext;

		/// <summary>
		/// The type of browser server called. The type is uint to allow for 
		/// future expansion.
		/// </summary>
		public uint BrowserType;

		protected ObjectPath CurrentBrowsePosition;

		public virtual void InitBrowser(ObjectPath wrappedServerStart, FilterSet filterSet, bool rootPath) { }

		public virtual bool BrowseBranches() { return false; }
		public virtual bool BrowseLeaves() { return false; }
		public virtual int ChangeBrowsePositionDown(int startingPathOffset, ObjectPath startingPath) { return 0; }

		public virtual List<ObjectAttributes> ConvertPropertiesToObjects(uint numberToReturn, List<ObjectAttributes> objectAttributesList) { return objectAttributesList; }

		public virtual void GetObjectAttributes(ref ObjectAttributes oa) { }

		// returns TRUE if the filters pass
		public virtual bool ApplyFilters(ObjectAttributes oa)
		{
			return false;
		}

		/// <summary>
		/// This method calls the OPC DA ChangeBrowsePosition method to set the 
		/// starting position.
		/// </summary>
		/// <param name="startingPath">
		/// The new Browse position.
		/// </param>
		/// <returns>
		/// Returns 0 if the browse down succeeded. Positive if the browse down failed for 
		/// the leaf of the starting path. Otherwise negative.
		/// </returns>
		protected void ChangeBrowsePosition(ObjectPath startingPath)
		{
			// At this point, the root elements have been stripped off the starting path - the first 
			// element of the starting path is the element directly below the DA Root.

			CurrentBrowseContext.ChangeBrowsePositionResults = CurrentBrowseContext.ChangeBrowsePositionSuccess;

			// set the offset if the starting object is a child and the previous object was not the root
			// returns:
			//   0 if the paths are identical
			//  >0 if the object identified by the starting path is a child of the previous object, 
			//  <0 otherwise
			int pathCompare = ObjectPath.IsEqualToOrChildOf(startingPath, CurrentBrowsePosition);

			// start from the root or an instance id and browse down - the starting path is not below the current position
			if (pathCompare < 0)
				CurrentBrowseContext.ChangeBrowsePositionResults = ChangeBrowsePositionDown(0, startingPath);

			else if (pathCompare > 0) // start from the current position and browse down
			{
				if (CurrentBrowsePosition == null) // if first time through (at the root)
					CurrentBrowsePosition = new ObjectPath(true);
				CurrentBrowseContext.ChangeBrowsePositionResults = ChangeBrowsePositionDown(CurrentBrowsePosition.Elements.Count, startingPath);
			}
			// else the browse position isn't changing, so start from the current position
			else
			{
				if (CurrentBrowsePosition == null) // if first time through (at the root)
					CurrentBrowsePosition = new ObjectPath(true);
			}
		}
	}

	/// <summary>
	/// This partial class defines the Discovery Methods of the server 
	/// implementation that override the virtual methods defined in the 
	/// Context folder of the ServerBase project.
	/// </summary>
	public partial class ContextImpl : ContextBase<ListRoot>
	{
		/// <summary>
		/// The OPC browser.  This object is used to for an instance of a browse, 
		/// including continuations that call the wrapped server IEnumString.
		/// </summary>
		internal OpcBrowser OpcBrowser
		{
			get { return _opcBrowser; }
			set
			{
				if (!(object.ReferenceEquals(_opcBrowser, OpcDaBrowser)
					|| object.ReferenceEquals(_opcBrowser, OpcAeBrowser)
					|| object.ReferenceEquals(_opcBrowser, OpcHdaBrowser)))
				{
					_opcBrowser.Dispose();
					_opcBrowser = null;
				}
				_opcBrowser = value;
			}
		}
		private OpcBrowser _opcBrowser;

		/// <summary>
		/// The OPC DA browser.  This object should be instantiated once per 
		/// Client Context and used for all browses to the wrapped DA server.
		/// </summary>
		internal OpcDaBrowser OpcDaBrowser;

		/// <summary>
		/// The OPC AE browser.  This object should be instantiated once per 
		/// Client Context and used for all browses to the wrapped HDA server.
		/// </summary>
		internal OpcAeBrowser OpcAeBrowser;

		/// <summary>
		/// The OPC HDA browser.  This object should be instantiated once per 
		/// Client Context and used for all browses to the wrapped HDA server.
		/// </summary>
		internal OpcHdaBrowser OpcHdaBrowser;

		/// <summary>
		/// This method detemines if the starting path identifies the root.  
		/// Throws a fault if the starting path is invalid.
		/// </summary>
		/// <param name="startingPath">
		/// The starting path.
		/// </param>
		/// <returns>
		///  TRUE = Valid && Root
		///  FALSE = Valid && Not Root
		///  Throws a fault if the starting path is invalid.
		/// </returns>
		internal bool IsStartingPathTheRoot(ObjectPath startingPath)
		{
			// If the starting path is null or its elements are null, then it specifies the root
			if (   (startingPath == null)
				|| (startingPath.Elements == null)
				|| (startingPath.Elements.Count == 0)
			   )
			{
				return true;  // Root
			}
			else // (startingPath.Elements.Count > 0)
			{
				// if the first element of the starting path is present, then it must be one of the following
				string rootElement = startingPath.Elements.ElementAt(0);
				if (   (rootElement == "")
					|| (0 == string.Compare(rootElement, "Root", true))
					|| (rootElement == "//")
				   )
				{
					return (startingPath.Elements.Count == 1)
							? true   // Valid && Root
							: false;  // Valid && Not Root
				}

				return false; // not the root
			}
		}

		/// <summary>
		/// This method prepares the server to browse. It strips the top-level elements from the 
		/// path, changes the browse position, and converts the filter strings for use by the 
		/// server to be called. 
		/// </summary>
		/// <param name="findCriteria">
		/// The starting path and filter strings.
		/// </param>
		/// <returns>
		/// The results of attempting to change the browse position.
		/// Returns 0 if the browse down succeeded. 
		/// Returns positive if the browse down failed for 
		/// the leaf of the starting path. Otherwise negative.
		/// </returns>
		internal CurrentBrowseContext PrepareToBrowse(FindCriteria findCriteria)
		{
			// If StartingPath is not present return null
			if (   (findCriteria.StartingPath == null)
				|| (findCriteria.StartingPath.Elements == null)
			   )
			{
				return null; // the starting path is the root - this should never happen
			}
			bool rootPath = (findCriteria.StartingPath.Elements[0] == InstanceId.RootId);
			ObjectPath wrappedStartingServerPath = findCriteria.StartingPath;

			// Set the new browse position
			// If there is only one server type supported, then use the server type to determine which one to call.
			// Otherwise, use the first element of the starting path (the wrapped server root) to determine which one to call
			uint serverTypeOfThisBrowse = 0;
			if (XiOPCWrapperServer.OpcWrappedServers.Count == 1)
			{
				// TODO: Add code here for alternate wrapped server types
				if ((XiOPCWrapperServer.ThisServerEntry.ServerDescription.ServerTypes & ServerType.OPC_DA205_Wrapper) > 0)
					serverTypeOfThisBrowse = ServerType.OPC_DA205_Wrapper;
				else if ((XiOPCWrapperServer.ThisServerEntry.ServerDescription.ServerTypes & ServerType.OPC_AE11_Wrapper) > 0)
					serverTypeOfThisBrowse = ServerType.OPC_AE11_Wrapper;
				else if ((XiOPCWrapperServer.ThisServerEntry.ServerDescription.ServerTypes & ServerType.OPC_HDA12_Wrapper) > 0)
					serverTypeOfThisBrowse = ServerType.OPC_HDA12_Wrapper;
			}
			else // (XiOPCWrapper.OpcWrappedServers.Count > 1)
			{
				if (rootPath)
				{
					if (findCriteria.StartingPath.Elements[1] == XiOPCWrapperServer.DA205_RootName)
						serverTypeOfThisBrowse = ServerType.OPC_DA205_Wrapper;

					else if (findCriteria.StartingPath.Elements[1] == XiOPCWrapperServer.AE_RootName)
						serverTypeOfThisBrowse = ServerType.OPC_AE11_Wrapper;

					else if (findCriteria.StartingPath.Elements[1] == XiOPCWrapperServer.HDA_RootName)
						serverTypeOfThisBrowse = ServerType.OPC_HDA12_Wrapper;

					else
						FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT,
											"Element #2 of the ObjectPath does not identify one of the wrapped servers.");

					// remove the wrapped server root (the second element) from the path
					wrappedStartingServerPath.Elements.RemoveAt(1);
				}
				else // the first element of the wrapped server path must be an instance id
				{
					InstanceId instId = new InstanceId(findCriteria.StartingPath.Elements[0]);
					if (instId.IsValid())
					{
						if (instId.ResourceType != null)
						{
							if (string.Compare(instId.ResourceType, InstanceIds.ResourceType_DA) == 0)
								serverTypeOfThisBrowse = ServerType.OPC_DA205_Wrapper;
							else if (string.Compare(instId.ResourceType, InstanceIds.ResourceType_AE) == 0)
								serverTypeOfThisBrowse = ServerType.OPC_AE11_Wrapper;
							else if (string.Compare(instId.ResourceType, InstanceIds.ResourceType_HDA) == 0)
								serverTypeOfThisBrowse = ServerType.OPC_HDA12_Wrapper;
							else
								throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Resource Type in Starting Path InstanceId = " + findCriteria.StartingPath.Elements[0]);
						}
						else
							throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Resource Type Missing in Starting Path InstanceId = " + findCriteria.StartingPath.Elements[0]);
					}
					else
						throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Starting Path InstanceId = " + findCriteria.StartingPath.ToString());
				}
			}

			// now check to see if the desired server type is accessible for this context
			bool isServerTypeAccessible = false;
			if (   (serverTypeOfThisBrowse == ServerType.OPC_DA205_Wrapper)
				&& (IsAccessibleDataAccess)
			   )
				isServerTypeAccessible = true;

			else if (   (serverTypeOfThisBrowse == ServerType.OPC_AE11_Wrapper)
					 && (IsAccessibleAlarmsAndEvents)
					)
				isServerTypeAccessible = true;

			else if (   (serverTypeOfThisBrowse == ServerType.OPC_HDA12_Wrapper)
					 && (IsAccessibleJournalDataAccess)
					)
				isServerTypeAccessible = true;

			if (isServerTypeAccessible == false)
			{
				OpcServerInfo server = XiOPCWrapperServer.OpcWrappedServers.Find(ws => (ws.ServerType & serverTypeOfThisBrowse) > 0);
				if (server != null)
					throw FaultHelpers.Create(XiFaultCodes.E_WRAPPEDSERVER_NOT_ACCESSIBLE,
									"The " + server.ProgId + " wrapped server is not currently accessible.");
				else
					throw FaultHelpers.Create(XiFaultCodes.E_WRAPPEDSERVER_NOT_ACCESSIBLE,
									"The " + ServerType.ToString(serverTypeOfThisBrowse) + " wrapped server is not currently accessible.");
			}

			// Get the browser for this starting path
			switch (serverTypeOfThisBrowse)
			{
				case (uint)ServerType.OPC_DA205_Wrapper:
					if (OpcDaBrowser == null)
					{
						// create this only once
						OpcDaBrowser = new OpcDaBrowser(this);
					}
					OpcBrowser = OpcDaBrowser; // set the OPC Browser for this Find to the DA Browser
					break;
				case (uint)ServerType.OPC_AE11_Wrapper:
					if (OpcAeBrowser == null)
					{
						// create this only once
						OpcAeBrowser = new OpcAeBrowser(this);
					}
					OpcBrowser = OpcAeBrowser; // set the OPC Browser for this Find to the DA Browser
					break;
				case (uint)ServerType.OPC_HDA12_Wrapper:
					if (OpcHdaBrowser == null)
					{
						// create this only once
						OpcHdaBrowser = new OpcHdaBrowser(this);
					}
					OpcBrowser = OpcHdaBrowser; // set the OPC Browser for this Find to the DA Browser
					break;
				default:
					break;
			}

			if (OpcBrowser != null)
			{
				OpcBrowser.InitBrowser(wrappedStartingServerPath, findCriteria.FilterSet, rootPath);
			}

			return (OpcBrowser == null)
				   ? null
				   : OpcBrowser.CurrentBrowseContext;
		}

		internal List<ObjectAttributes> GetEnumeratedBrowseResults(uint numberToReturn, List<ObjectAttributes> objectAttributesList)
		{
			List<ObjectAttributes> listOfObjAttrs = (objectAttributesList != null)
												  ? objectAttributesList
												  : new List<ObjectAttributes>();
			uint numberLeft = 0;

			if (OpcBrowser.CurrentBrowseContext.SomethingToReturn)
			{
				listOfObjAttrs = GetEnumerations(numberToReturn, listOfObjAttrs);
				if ((listOfObjAttrs == null) || (listOfObjAttrs.Count == 0))
				{
					// if there were no more left in the enumerated list
					OpcBrowser.CurrentBrowseContext.SomethingToReturn = false;
					numberLeft = numberToReturn;
				}
				else if (numberToReturn > (uint)listOfObjAttrs.Count)
					numberLeft = (uint)(numberToReturn - listOfObjAttrs.Count);
			}
			else
			{
				numberLeft = numberToReturn;
			}

			if (numberLeft > 0)
			{
				if (OpcBrowser.CurrentBrowseContext.BrowseTypeUsed == OPCBROWSETYPE.OPC_BRANCH)
				{
					// if leaves were also requested, browse for leaves
					if ((OpcBrowser.CurrentBrowseContext.BrowseTypesRequested & (int)OPCBROWSETYPE.OPC_LEAF) > 0)
					{
						OpcBrowser.CurrentBrowseContext.SomethingToReturn = OpcBrowser.BrowseLeaves();
						if (OpcBrowser.CurrentBrowseContext.SomethingToReturn == true)
						{
							GetEnumeratedBrowseResults(numberLeft, listOfObjAttrs);
						}
					}
				}
			}
			return listOfObjAttrs;
		}

		/// <summary>
		/// This method gets the requested number of enumerations returned from the 
		/// browse and converts each into an ObjectAttributes object.
		/// </summary>
		/// <param name="numberToReturn">
		/// The number of enumerations to get.
		/// </param>
		/// <param name="objectAttributesList">
		/// The list of object attributes to which the new object attributes will be appended.
		/// </param>
		/// <returns>
		/// The list of ObjectAttributes created from the enumerations.
		/// </returns>
		internal List<ObjectAttributes> GetEnumerations(uint numberToReturn, List<ObjectAttributes> objectAttributesList)
		{
			List<ObjectAttributes> listOfObjAttrs = objectAttributesList;
			if (OpcBrowser.CurrentBrowseContext.EnumString != null)
			{
				List<string> names = null;
				cliHRESULT HR = OpcBrowser.CurrentBrowseContext.EnumString.Next(numberToReturn, out names);
				if (HR.Succeeded)
				{
					if (names != null)
					{
						foreach (var name in names)
						{
							ObjectAttributes oa = new ObjectAttributes();
							oa.Name = name;
							OpcBrowser.GetObjectAttributes(ref oa);
							// if the filters pass, then return the object
							if (OpcBrowser.ApplyFilters(oa))
								listOfObjAttrs.Add(oa);
						}
					}
				}
			}
			// convert any properties that were retrieved to object attributes
			listOfObjAttrs = OpcBrowser.ConvertPropertiesToObjects(numberToReturn, listOfObjAttrs);
			return listOfObjAttrs;
		}

	}
}
