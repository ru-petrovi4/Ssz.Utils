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
using System.ServiceModel;
using System.Threading;

using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	/// <summary>
	/// This class is intended to be used as the root or base class for 
	/// all list types.  The attribute, properties and methods defined 
	/// in this class are common to all list types.
	/// </summary>
	public abstract class ListRoot
		: Dictionary<uint, ValueRoot>
		, IDisposable
	{
		/// <summary>
		/// This constructor is provides the construction of the List Root object.  
		/// Most of the values supplied to this constructor are declared as readonly 
		/// and cannot be changed once the list is constructed.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="clientId"></param>
		/// <param name="updateRate"></param>
		/// <param name="listType"></param>
		/// <param name="listKey"></param>
		public ListRoot(ContextBase<ListRoot> context, uint clientId, uint updateRate, uint bufferingRate,
						uint listType, uint listKey, StandardMib mib)
		{
			_context = context;
			_serverId = listKey;
			_clientId = clientId;
			_listType = listType;
			UpdateRate = updateRate; // to be negotiated later
			BufferingRate = NegotiateBufferingRate(mib, bufferingRate);
		}

		/// <summary>
		/// The destructor clears out the references to Entry Root 
		/// objects that may still be attached to this list.
		/// </summary>
		~ListRoot()
		{
			bool BADlist = (this is ListRoot);
			if (!_hasBeenDisposed && (!BADlist))
				Dispose(false);
		}

		/// <summary>
		/// This is the IDisposable.Dispose implementation.  
		/// This method is used to invoke the Dispose method on 
		/// each of the Entry Root objects held by this list.
		/// </summary>
		public void Dispose()
		{
			if (Dispose(true))
				GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Subclasses should override this method to take care of 
		/// any needed cleanup as dispose takes place.
		/// </summary>
		/// <param name="isDisposing"></param>
		/// <returns></returns>
		protected abstract bool Dispose(bool isDisposing);
		protected bool _hasBeenDisposed = false;

		/// <summary>
		/// This lock is used to single thread calls from the client that change the 
		/// state of the list (Enabling or Modifying List Attributes) or that change 
		/// the state of an element of the list (EnableListElementUpdating).
		/// It is held from the time a call is received until the response is returned.
		/// </summary>
		protected object _ListTransactionLock = new Nullable<long>(System.Environment.TickCount);

		/// <summary>
		/// This lock is used to single thread changes to the membership of the list's dictionary of elements.
		/// It is used when adding and deleting elements of the list, and when getting an element 
		/// from the dictionary.
		/// It is only held while the dictionary is being updated or searched.
		/// </summary>
		protected object _DictionaryIntegrityLock = new Nullable<long>(System.Environment.TickCount);

		private readonly ContextBase<ListRoot> _context;
		private readonly uint _clientId;
		private readonly uint _serverId;
		private readonly uint _listType;
		private bool _enabled;

		protected uint _bufferingRate;
		protected FilterSet _filterSet;

		/// <summary>
		/// Each Xi List belongs to one context.  This property returns the 
		/// context to which this list belongs.
		/// </summary>
		public ContextBase<ListRoot> OwnerContext { get { return _context; } }

		/// <summary>
		/// Unique per Server List ID (context based).  Cannot change after assignment.
		/// ServerId is used as the key in Server List Dictionaries.
		/// </summary>
		public uint ServerId { get { return _serverId; } }

		/// <summary>
		/// Unique per Client List ID (context based).  Cannot change after assignment.
		/// ClientId is used by the client to identify the list.
		/// </summary>
		public uint ClientId { get { return _clientId; } }

		/// <summary>
		/// This is the List Type as defined in List Attributes.
		/// </summary>
		public uint ListType { get { return _listType; } }

		/// <summary>
		/// Indicates, when TRUE, that the list is being deleted and should not be used any further.
		/// </summary>
		public bool BeingDeleted
		{
			get { return _beingDeleted; }
			set { _beingDeleted = value; }
		}
		private bool _beingDeleted = false;

		/// <summary>
		/// Only enabled lists may be actively used for read, write, poll and callbacks.
		/// </summary>
		public bool Enabled
		{
			get { return _enabled; }
			protected set { _enabled = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public DateTime LastFetch { get; set; }

		/// <summary>
		/// Negotiated update rate for the list.
		/// Updating the UpdateRate causes the _missedPollIntervalMsecs to be updated
		/// </summary>
		public uint UpdateRate
		{
			get { return _updateRate; }
			set
			{
				_updateRate = value;
				_missedPollIntervalMsecs = _updateRate * _numberOfUpdateCyclesToQueue;
			}
		}

		/// <summary>
		/// The private value for the update rate.
		/// The update rate should always be updated through the UpdateRate property 
		/// since updating the UpdateRate causes the _missedPollIntervalMsecs to be updated
		/// </summary>
		private uint _updateRate;

		/// <summary>
		/// The configured number of update cycles to queue data and event notifications 
		/// that can subsequently be polled. If the value is 1, then each update cycle 
		/// causes the previous update cycle entries to be discarded if they were 
		/// not yet polled.
		/// </summary>
		protected uint _numberOfUpdateCyclesToQueue = 2;

		/// <summary>
		/// The time interval in which a Poll request should have been received.
		/// Set to _numberOfUpdateCyclesToQueue times the UpdateRate.  If a poll 
		/// has been missed, the oldest queued entry will be deleted.
		/// </summary>
		protected uint _missedPollIntervalMsecs;

		/// <summary>
		/// The number of queue entries that have been discarded since the last poll.
		/// </summary>
		public uint DiscardedQueueEntries { get { return _discardedQueueEntries; } }

		/// <summary>
		/// The number of queue entries that have been discarded since the last poll.
		/// </summary>
		protected uint _discardedQueueEntries;

		/// <summary>
		/// The number of queue entries that have been discarded since the last poll.
		/// </summary>
		public DateTime LastPollTime { get { return _lastPollTime; } }

		/// <summary>
		/// The time that the last poll was received for the list.
		/// </summary>
		private DateTime _lastPollTime;

		/// <summary>
		/// Sets the last poll time to the current time.
		/// </summary>
		public void SetLastPollTime() { lock (_ListTransactionLock) { _lastPollTime = DateTime.UtcNow; } }

		/// <summary>
		/// Negotiated buffering rate for the list.
		/// </summary>
		public uint BufferingRate
		{
			get { return _bufferingRate; }
			set { _bufferingRate = value; }
		}

		/// <summary>
		/// This property returns a copy of the Xi List Attributes from the server.
		/// The ModifyListAttributes method is used oo change the List Attribute.
		/// </summary>
		public ListAttributes ListAttributes
		{
			get
			{
				lock (_ListTransactionLock)
				{
					return new ListAttributes()
					{
						ResultCode = XiFaultCodes.S_OK,
						ClientId = this.ClientId,
						ServerId = this.ServerId,
						ListType = this.ListType,
						Enabled = this.Enabled,
						UpdateRate = this.UpdateRate,
						CurrentCount = this.Count,
						HowSorted = 0,
						SortKeys = null,
						FilterSet = this._filterSet,
					};
				}
			}
		}

		/// <summary>
		/// Use this property to determine if this list may be polled for data values.
		/// </summary>
		public bool PollingActivated { get { return (null != _iPollEndpointEntry); } }

		/// <summary>
		/// Use this property to determine if this list is activily reporting data value changes
		/// </summary>
		public bool CallbackActivated { get { return (null != _iRegisterForCallbackEndpointEntry); } }

		protected EndpointEntry<ListRoot> _iReadEndpointEntry = null;
		protected EndpointEntry<ListRoot> _iPollEndpointEntry = null;
		protected EndpointEntry<ListRoot> _iRestReadEndpointEntry = null;
		protected EndpointEntry<ListRoot> _iWriteEndpointEntry = null;
		protected EndpointEntry<ListRoot> _iRegisterForCallbackEndpointEntry = null;

		/// <summary>
		/// This queue contains the values {always a subclass of Value Root}
		/// that have changed since the last poll request.
		/// </summary>
		protected Queue<ValueRoot> _queueOfChangedValues = new Queue<ValueRoot>();

		/// <summary>
		/// This lock is used to single thread changes to the membership of _queueOfChangedValues.
		/// It is used when adding and deleting to/from the _queueOfChangedValues, and when getting a 
		/// value from _queueOfChangedValues.
		/// It is only held while the _queueOfChangedValues is being updated or searched.
		/// </summary>
		protected object _QueueOfChangedValuesLock = new Nullable<long>(System.Environment.TickCount);

		/// <summary>
		/// This dictionary holds the collection of all Xi Values 
		/// {always a subclass of Value Root} that make up this Xi List.
		/// </summary>
		//protected Dictionary<uint, ValueRoot> _dictValueRoot = new Dictionary<uint, ValueRoot>();

		/// <summary>
		/// This method is used to obtain a unique server alias for each List Entry.
		/// </summary>
		/// <returns></returns>
		protected uint NewUniqueServerAlias()
		{
			lock (_DictionaryIntegrityLock)
			{
				uint key = 0;
				do
				{
					key = (uint)_rand.Next(1, 0x3FFFFFFF);
				}
				while (this.ContainsKey(key));
				return key;
			}
		}

		/// <summary>
		/// Instance of Random used to obtain the unique server alias for each List Entry,
		/// </summary>
		protected Random _rand = new Random(unchecked((int)(DateTime.UtcNow.Ticks & 0x000000007FFFFFFFFL)));

		/// <summary>
		/// This method is used to obtain a reference to a List Entry given the server alias.
		/// </summary>
		/// <param name="serverAlias"></param>
		/// <returns></returns>
		protected ValueRoot FindEntryRoot(uint serverAlias)
		{
			lock (_DictionaryIntegrityLock)
			{
				ValueRoot valueRoot = null;
				this.TryGetValue(serverAlias, out valueRoot);
				return valueRoot;
			}
		}

		/// <summary>
		/// Use this method to add an Entry Root or subclass to this list.
		/// </summary>
		/// <param name="valueRoot"></param>
		protected void AddAValue(ValueRoot valueRoot)
		{
			lock (_DictionaryIntegrityLock)
			{
				this.Add(valueRoot.ServerAlias, valueRoot);
			}
		}

		/// <summary>
		/// Use this method to remove an Entry Root or subclass from this list.
		/// </summary>
		/// <param name="valueRoot"></param>
		/// <returns></returns>
		protected void RemoveAValue(ValueRoot valueRoot)
		{
			lock (_DictionaryIntegrityLock)
			{
				this.Remove(valueRoot.ServerAlias);
				valueRoot.Dispose();
			}
		}

		/// <summary>
		/// This method is invoked to queue an entry to the list that is being polled.
		/// </summary>
		/// <param name="entries">The list of entries to be queued</param>
		public void QueueChangedValues(List<ValueRoot> entries)
		{
			if ((entries != null) && (entries.Count > 0))
			{
				lock (_QueueOfChangedValuesLock)
				{
					TimeSpan timeDiff = DateTime.UtcNow - _lastPollTime;
					if (timeDiff.TotalMilliseconds > _missedPollIntervalMsecs)
						DiscardQueuedEntries();

					entries.Add(new QueueMarker()); // add a queue marker as the last entry to queue
					foreach (var entry in entries)
					{
						_queueOfChangedValues.Enqueue(entry);
					}
				}
			}
		}

		/// <summary>
		/// This method is used to delete the oldest entries from the queue when 
		/// the queue becomes full.  If the queue is not full, no entries are 
		/// deleted. Implementations may override this method.
		/// </summary>
		/// <param name="entry"></param>
		public virtual void DiscardQueuedEntries()
		{
			lock (_QueueOfChangedValuesLock)
			{
				// dequeue entries to the next marker
				if (_queueOfChangedValues.Count > 0)
				{
					ValueRoot entry = _queueOfChangedValues.Dequeue();
					while (entry.GetType() != typeof(QueueMarker))
					{
						entry = _queueOfChangedValues.Dequeue();
						_discardedQueueEntries = (_discardedQueueEntries == uint.MaxValue)
											   ? _discardedQueueEntries // don't bump if max value reached (should be impossible)
											   : _discardedQueueEntries + 1;
					}
				}
			}
		}

		// ********************************************************************************
		#region Endpoint Management

		public virtual AliasResult OnAddListToEndpoint(EndpointEntry<ListRoot> endpointEntry)
		{
			lock (_ListTransactionLock)
			{
				bool okayToAddToEndpoint = false;
				if (endpointEntry.EndpointDefinition.ContractType == typeof(IRead).Name)
				{
					if (null == _iReadEndpointEntry)
					{
						okayToAddToEndpoint = true;
						_iReadEndpointEntry = endpointEntry;
					}
				}
				else if (endpointEntry.EndpointDefinition.ContractType == typeof(IWrite).Name)
				{
					if (null == _iWriteEndpointEntry)
					{
						okayToAddToEndpoint = true;
						_iWriteEndpointEntry = endpointEntry;
					}
				}
				else if (endpointEntry.EndpointDefinition.ContractType == typeof(IPoll).Name)
				{
					if (null == _iPollEndpointEntry && null == _iRegisterForCallbackEndpointEntry)
					{
						okayToAddToEndpoint = true;
						_iPollEndpointEntry = endpointEntry;
					}
				}
				else if (endpointEntry.EndpointDefinition.ContractType == typeof(IRegisterForCallback).Name)
				{
					if (null == _iRegisterForCallbackEndpointEntry && null == _iPollEndpointEntry)
					{
						okayToAddToEndpoint = true;
						_iRegisterForCallbackEndpointEntry = endpointEntry;
					}
				}
				else if (endpointEntry.EndpointDefinition.ContractType == typeof(IRestRead).Name)
				{
					if (null == _iRestReadEndpointEntry)
					{
						okayToAddToEndpoint = true;
						_iRestReadEndpointEntry = endpointEntry;
					}
				}
				if (okayToAddToEndpoint)
				{
					endpointEntry.OnAddListToEndpoint(this);
					return null; // return null if the add succeeded
				}
				return new AliasResult(XiFaultCodes.E_ENDPOINTERROR, this.ClientId, this.ServerId);
			}
		}

		/// <summary>
		/// Removes this list from the specified endpoint
		/// </summary>
		/// <param name="endpointEntry">The specified endpoint</param>
		/// <returns>Returns null if successful, otherwise, the reason for failure is returned.</returns>
		public virtual AliasResult OnRemoveListFromEndpoint(EndpointEntry<ListRoot> endpointEntry)
		{
			lock (_ListTransactionLock)
			{
				bool okayToRemoveFromEndpoint = RemoveEndpointReference(endpointEntry);
				if (okayToRemoveFromEndpoint)
				{
					endpointEntry.OnRemoveListFromEndpoint(this);
					return null;  // return null if the removal succeeded
				}
				return new AliasResult(XiFaultCodes.E_ENDPOINTERROR, this.ClientId, this.ServerId);
			}
		}

		public virtual bool RemoveEndpointReference(EndpointEntry<ListRoot> endpointEntry)
		{
			bool okayToRemoveFromEndpoint = false;
			if (endpointEntry.EndpointDefinition.ContractType == typeof(IRead).Name)
			{
				if (null != _iReadEndpointEntry)
				{
					okayToRemoveFromEndpoint = true;
					_iReadEndpointEntry = null;
				}
			}
			else if (endpointEntry.EndpointDefinition.ContractType == typeof(IRegisterForCallback).Name)
			{
				if (null != _iRegisterForCallbackEndpointEntry)
				{
					okayToRemoveFromEndpoint = true;
					_iRegisterForCallbackEndpointEntry = null;
				}
			}
			else if (endpointEntry.EndpointDefinition.ContractType == typeof(IWrite).Name)
			{
				if (null != _iWriteEndpointEntry)
				{
					okayToRemoveFromEndpoint = true;
					_iWriteEndpointEntry = null;
				}
			}
			else if (endpointEntry.EndpointDefinition.ContractType == typeof(IPoll).Name)
			{
				if (null != _iPollEndpointEntry)
				{
					okayToRemoveFromEndpoint = true;
					_iPollEndpointEntry = null;
				}
			}
			else if (endpointEntry.EndpointDefinition.ContractType == typeof(IRestRead).Name)
			{
				if (null != _iRestReadEndpointEntry)
				{
					okayToRemoveFromEndpoint = true;
					_iRestReadEndpointEntry = null;
				}
			}
			return okayToRemoveFromEndpoint;
		}

		public void AuthorizeEndpointUse(Type contractType)
		{
			OperationContext operationContext = OperationContext.Current;
			EndpointEntry<ListRoot> endpointEntry = null;

			if (contractType == typeof(IRead))
				endpointEntry = _iReadEndpointEntry;

			else if (contractType == typeof(IPoll))
				endpointEntry = _iPollEndpointEntry;

			else if (contractType == typeof(IWrite))
				endpointEntry = _iWriteEndpointEntry;

			else if (contractType == typeof(IRegisterForCallback))
				endpointEntry = _iRegisterForCallbackEndpointEntry;

			else if (contractType == typeof(IRestRead))
				endpointEntry = _iRestReadEndpointEntry;

			if (endpointEntry != null)
			{
				_context.AuthorizeEndpointUse(endpointEntry);
			}
		}

		#endregion

		// ********************************************************************************
		#region List Management

		public virtual List<AliasResult> OnRenewAliases(List<AliasUpdate> newAliases)
		{
			List<AliasResult> results = new List<AliasResult>();
			lock (_ListTransactionLock)
			{
				foreach (var aliasUpdate in newAliases)
				{
					ValueRoot valueRoot;
					if (TryGetValue(aliasUpdate.ExistingServerAlias, out valueRoot))
					{
						valueRoot.ClientAlias = aliasUpdate.NewClientAlias;
						valueRoot.ServerAlias = NewUniqueServerAlias();
					}
					else
					{
						AliasResult aResult = new AliasResult(XiFaultCodes.E_ALIASNOTFOUND, aliasUpdate.NewClientAlias, aliasUpdate.ExistingServerAlias);
						results.Add(aResult);
					}
				}
			}
			if (results.Count > 0)
				return results;
			return null;
		}

		public virtual List<AddDataObjectResult> OnAddDataObjectsToList(List<ListInstanceId> dataObjectsToAdd)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		public virtual List<AliasResult> OnRemoveDataObjectsFromList(List<uint> serverAliasesToDelete)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		public virtual ModifyListAttrsResult OnModifyListAttributes(
			Nullable<uint> updateRate, Nullable<uint> bufferingRate, FilterSet filterSet)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		/// <summary>
		/// This method calls the implementation override to set the buffering rate
		/// for the list.
		/// </summary>
		/// <returns>The negotiated buffering rate.</returns>
		protected uint NegotiateBufferingRate(StandardMib mib, uint requestedBufferingRate)
		{
			uint negotiatedBufferingRate = 0;
			if ((mib.FeaturesSupported & (uint)XiFeatures.BufferingRate_Feature) > 0)
			{
				OnNegotiateBufferingRate(requestedBufferingRate);
			}
			return negotiatedBufferingRate;
		}

		/// <summary>
		/// This abstract method is overridden by implementations to set the buffering rate
		/// for the list
		/// </summary>
		/// <returns>The negotiated buffering rate.</returns>
		protected abstract uint OnNegotiateBufferingRate(uint requestedBufferingRate);

		/// <summary>
		/// Normally this method will be overridden in the implementation 
		/// subclass to perform any actions in changed the state of this 
		/// list and the specified data values to the requested updating state.
		/// </summary>
		/// <param name="enableUpdating"></param>
		public virtual ListAttributes OnEnableListUpdating(bool enableUpdating)
		{
			lock (_ListTransactionLock)
			{
				Enabled = enableUpdating;
			}
			return ListAttributes;
		}

		/// <summary>
		/// Normally this method will be overridden in the implementation 
		/// subclass to perform any actions in changed the state of this 
		/// list and the specified data values to the requested updating state.
		/// </summary>
		/// <param name="enableUpdating">
		/// Indicates, when TRUE, that updating of the list is to be enabled,
		/// and when FALSE, that updating of the list is to be disabled.
		/// </param>
		/// <param name="serverAliases">
		/// The list of aliases for data objects of a list for 
		/// which updating is to be enabled or disabled.
		/// When this value is null updating all elements of the list are to be 
		/// enabled/disabled. In this case, however, the enable/disable state 
		/// of the list itself is not changed.
		/// </param>
		/// <returns>
		/// <para>If the serverAliases parameter was null, returns 
		/// null if the server was able to successfully enable/disable 
		/// the list and all its elements.  If not, throws an exception 
		/// for event lists and for data lists, returns the client and server 
		/// aliases and result codes for the data objects that could not be 
		/// enabled/disabled.  </para> 
		/// <para>If the serverAliases parameter was not null, returns null 
		/// if the server was able to successfully enable/disable the data 
		/// objects identified by the serverAliases.  If not, returns the 
		/// client and server aliases and result codes for the data objects 
		/// that could not be enabled/disabled.</para> 
		/// </returns>
		public virtual List<AliasResult> OnEnableListElementUpdating(bool enableUpdating, List<uint> serverAliases)
		{
			throw FaultHelpers.Create(XiFaultCodes.E_NOTIMPL, "IResourceManagement.EnableListElementUpdating");
		}

		/// <summary>
		/// Gets the server aliases for all data objects in a Data List
		/// </summary>
		/// <returns>The list of server aliases</returns>
		public List<uint> GetServerAliases()
		{
			List<uint> serverAliases = new List<uint>();
			lock (_DictionaryIntegrityLock)
			{
				foreach (var item in this)
				{
					serverAliases.Add(item.Value.ServerAlias);
				}
			}
			return serverAliases;
		}

		public virtual List<TypeIdResult> OnAddEventMessageFields(uint categoryId, List<TypeId> fieldObjectTypeIds)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		public virtual uint OnTouchList()
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		public virtual List<AliasResult> OnTouchDataObjects(List<uint> serverAliases)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		#endregion


		// ********************************************************************************
		#region IPoll

		public virtual DataValueArraysWithAlias OnPollDataChanges()
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		public virtual EventMessage[] OnPollEventChanges(FilterSet filterSet)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		#endregion

		// ********************************************************************************
		#region IRead and IRestRead

		/// <summary>
		/// 
		/// </summary>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		public virtual DataValueArraysWithAlias OnReadData(List<uint> serverAliases)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="numValuesPerAlias"></param>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		public virtual JournalDataValues[] OnReadJournalDataForTimeInterval(
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numValuesPerAlias, List<uint> serverAliases)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="numEventMessages"></param>
		/// <returns></returns>
		public virtual JournalDataValues[] OnReadJournalDataNext(uint numEventMessages)
		{
			throw FaultHelpers.Create(XiFaultCodes.E_NOTIMPL, "IRead.ReadJournalDataNext");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="timestamps"></param>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		public virtual JournalDataValues[] OnReadJournalDataAtSpecificTimes(
			List<DateTime> timestamps, List<uint> serverAliases)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="numValuesPerAlias"></param>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		public virtual JournalDataChangedValues[] OnReadJournalDataChanges(
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numValuesPerAlias, List<uint> serverAliases)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="numEventMessages"></param>
		/// <returns></returns>
		public virtual JournalDataChangedValues[] OnReadJournalDataChangesNext(uint numEventMessages)
		{
			throw FaultHelpers.Create(XiFaultCodes.E_NOTIMPL, "IRead.ReadJournalDataChangesNext");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="calculationPeriod"></param>
		/// <param name="serverAliasesAndCalculations"></param>
		/// <returns></returns>
		public virtual JournalDataValues[] OnReadCalculatedJournalData(
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, TimeSpan calculationPeriod,
			List<AliasAndCalculation> serverAliasesAndCalculations)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="serverAlias"></param>
		/// <param name="propertiesToRead"></param>
		/// <returns></returns>
		public virtual JournalDataPropertyValue[] OnReadJournalDataProperties(
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint serverAlias, List<TypeId> propertiesToRead)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filterSet"></param>
		/// <returns></returns>
		public virtual EventMessage[] OnReadEvents(FilterSet filterSet)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="numEventMessages"></param>
		/// <param name="filterSet"></param>
		/// <returns></returns>
		public virtual EventMessage[] OnReadJournalEvents(
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numEventMessages, FilterSet filterSet)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="numEventMessages"></param>
		/// <returns></returns>
		public virtual EventMessage[] OnReadJournalEventsNext(uint numEventMessages)
		{
			throw FaultHelpers.Create(XiFaultCodes.E_NOTIMPL, "IRead.ReadJournalEventsNext");
		}

		#endregion

		// ********************************************************************************
		#region IWrite

		public virtual List<AliasResult> OnWriteValues(WriteValueArrays writeValueArrays)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		public virtual List<AliasResult> OnWriteVST(DataValueArraysWithAlias readValueArrays)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		public virtual List<DataJournalWriteResult> OnWriteJournalData(
			ModificationType modificationType, WriteJournalValues[] valuesToWrite)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		public virtual List<EventIdResult> OnWriteJournalEvents(
			ModificationType modificationType, EventMessage[] eventsToWrite)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		public virtual List<EventIdResult> OnAcknowledgeAlarms(
			string operatorName, string comment, List<EventId> alarmsToAck)
		{
			throw FaultHelpers.Create("Invalid List Type for this Request");
		}

		#endregion


		// ********************************************************************************
		#region ICallback

		/// <summary>
		/// This method is invoked to issue an Information Report 
		/// back to the Xi client for data changes.
		/// </summary>
		/// <param name="updatedValues"></param>
		public virtual void OnInformationReport(DataValueArraysWithAlias readValueList)
		{
			OwnerContext.OnInformationReport(ClientId, readValueList);
		}

		/// <summary>
		/// This method invokes an Event Notification back to the Xi
		/// client when an event needs to be reported.
		/// </summary>
		/// <param name="eventList"></param>
		public virtual void OnEventNotification(EventMessage[] eventsArray)
		{
			OwnerContext.OnEventNotification(ClientId, eventsArray);
		}

		#endregion

	}


}
