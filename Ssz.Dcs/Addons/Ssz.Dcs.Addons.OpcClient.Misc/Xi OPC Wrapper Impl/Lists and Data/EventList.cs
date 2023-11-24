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
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.OPC.COM.API;
using Xi.Server.Base;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// 
	/// </summary>
	public class EventsList
		: EventsListBase
	{
        #region Constants
        ///<summary>Level category that the USO Alarm BL requests from the AE Server</summary>
        public const int LevelCategoryID = 0x100;
        ///<summary>Deviation category that the USO Alarm BL requests from the AE Server</summary>
        public const int DeviationCategoryID = 0x101;
        ///<summary>Digital category that the USO Alarm BL requests from the AE Server</summary>
        public const int DigitalCategoryID = 0x102;
        ///<summary>ROC category that the USO Alarm BL requests from the AE Server</summary>
        public const int RateOfChangeCategoryID = 0x103;

        ///<summary>Current Value attribute specific for the USO DSS.</summary>
        public const int AttributeCV = 0x104;
        ///<summary>Trip Value attribute specific for the USO DSS.</summary>
        public const int AttributeTripValue = 0x105;
        ///<summary>GCB Register containing the Current Value attribute specific for the USO DSS.</summary>
        public const int AttributeCVRegister = 0x106;
        ///<summary>Area attribute specific for the USO DSS.</summary>
        public const int AttributeArea = 0x107;
        ///<summary>PTDESC attribute specific for the USO DSS.</summary>
        public const int AttributeTagDescription = 0x108;
        ///<summary>EUDESC attribute specific for the USO DSS.</summary>
        public const int AttributeEu = 0x109;
        ///<summary>Current Value as text (ON/OFF) attribute specific for the USO DSS.</summary>
        public const int AttributeCVText = 0x10A;
        ///<summary>Trip Value as text (ON/OFF) attribute specific for the USO DSS.</summary>
        public const int AttributeTripText = 0x10B;
        #endregion

        /// <summary>
        /// A list of the attributes that are provided by the USO OPC AE server
        /// </summary>
        static public uint[] UsoAeEventAttributes
        {
            get
            {
                List<uint> retVal = new List<uint>();

                //There is some Black Magic here - the order in which these attibutes are ordered within the array somehow
                //influence the pEvent VARIANT array order - but not in the way you might think. Organising the attributes 
                //as below  results in area[3], tagDesc[4] and eu[5], which is how they are added in opcaeserver... and
                //what we are expecting.  If you shuffle the array to present the EVENT_ATTERIBUTES in consecutively (i.e. how they
                //are added to the list by OPcAEServer and how we unravel them here the order gets mixed up.
                retVal.Add(AttributeCV);
                retVal.Add(AttributeTripValue);
                retVal.Add(AttributeCVRegister);
                retVal.Add(AttributeEu);
                retVal.Add(AttributeArea);
                retVal.Add(AttributeTagDescription);
                retVal.Add(AttributeCVText);
                retVal.Add(AttributeTripText);
                return retVal.ToArray();
            }
        }

		internal EventsList(ContextImpl context, uint clientId, uint updateRate, uint bufferingRate,
							uint listType, uint listKey, FilterSet filterSet, StandardMib mib)
			: base(context, clientId, updateRate, bufferingRate, listType, listKey, mib)
		{
			IOPCEventSubscriptionMgtCli iOpcEventSubsMgt = null;
			uint uiRevisedBuffer = 0; // OPC A&E name for update rate 
			uint uiRevisedMaxSize = 0;
			cliHRESULT hr = 0;

			uint eventType = 0;
			List<uint> categories = new List<uint>();
			Nullable<uint> lowSeverity = null;
			Nullable<uint> highSeverity = null;
			List<string> areas = new List<string>();
			List<string> eventSources = new List<string>();

			bool bOpcFilters = false;
			if (filterSet != null && filterSet.Filters != null && filterSet.Filters.Count > 0)
			{
				bOpcFilters = ParseEventFilters(filterSet, out eventType, out categories, lowSeverity,
					highSeverity, out areas, out eventSources);
			}

			if (context.IsAccessibleAlarmsAndEvents == false)
				context.ThrowDisconnectedServerException(context.IOPCEventServer_ProgId);

			// TODO: Change the default dwMaxSize of 1000 to a value compatible with your A&E server
			// Note that the OPC A&E specification allows the server to ignore this input value
			hr = context.IOPCEventServer.CreateEventSubscription(false, UpdateRate,
					1000, ClientId, out iOpcEventSubsMgt, out uiRevisedBuffer, out uiRevisedMaxSize);
			if (hr == XiFaultCodes.S_OK)
			{
				IOPCEventSubscriptionMgt = iOpcEventSubsMgt;
                List<uint> usoEventAttributes = UsoAeEventAttributes.ToList();
                iOpcEventSubsMgt.SelectReturnedAttributes(LevelCategoryID, usoEventAttributes);
                iOpcEventSubsMgt.SelectReturnedAttributes(DeviationCategoryID, usoEventAttributes);
                iOpcEventSubsMgt.SelectReturnedAttributes(DigitalCategoryID, usoEventAttributes);
                iOpcEventSubsMgt.SelectReturnedAttributes(RateOfChangeCategoryID, usoEventAttributes);
                
                IAdviseOPCEventSink = iOpcEventSubsMgt as IAdviseOPCEventSink;
				Debug.Assert(null != iOpcEventSubsMgt);
				if (bOpcFilters)
					filterSet = SetOpcEventFilters(eventType, categories, lowSeverity, highSeverity, areas, eventSources);

				_filterSet = filterSet;

				/*_revisedBuffer = */
				UpdateRate = uiRevisedBuffer;

				_revisedMaxSize = uiRevisedMaxSize;
			}
			else
			{
				context.ThrowOnDisconnectedServer(hr.hResult, context.IOPCEventServer_ProgId);
				// The next line will not be executed if the call above throws
				throw FaultHelpers.Create((uint)hr.hResult, "OPC AE CreateEventSubscription() failed.");
			}
		}

		// member variable
		private IOPCEventSubscriptionMgtCli _iOpcEventSubsMgt = null;
		private IAdviseOPCEventSink _iAdviseOPCEventSink = null;
		private uint _revisedMaxSize = 0;
		private List<ORedFilters> AdditionalEventTypeFilters;
		private List<ORedFilters> AdditionalNonAEFilters; // Non OPC A&E Filters supported by this server

		// accessors
		protected IOPCEventSubscriptionMgtCli IOPCEventSubscriptionMgt
		{
			get { return _iOpcEventSubsMgt; }
			private set { _iOpcEventSubsMgt = value; }
		}
		protected IAdviseOPCEventSink IAdviseOPCEventSink
		{
			get { return _iAdviseOPCEventSink; }
			private set { _iAdviseOPCEventSink = value; }
		}
		public uint RevisedMaxSize { get { return _revisedMaxSize; } protected set { _revisedMaxSize = value; } }

		// methods
		protected override bool Dispose(bool isDisposing)
		{
			if (_hasBeenDisposed)
				return false;

			if (isDisposing)
			{
				lock (_QueueOfChangedValuesLock)
				{
					_queueOfChangedValues.Clear();
				}
				if (0 < this.Count)
				{
					lock (_DictionaryIntegrityLock)
					{
						this.Clear();
					}
				}
			}

			lock (_ListTransactionLock)
			{
				if (isDisposing)
				{
					IAdviseOPCEventSink.UnadviseOnEvent(OnEvent);
					if (null != IOPCEventSubscriptionMgt && null != (IOPCEventSubscriptionMgt as IDisposable))
					{
						(IOPCEventSubscriptionMgt as IDisposable).Dispose();
					}
					_hasBeenDisposed = true;
				}
				_queueOfChangedValues = null;
				IOPCEventSubscriptionMgt = null;
				IAdviseOPCEventSink = null;
				_hasBeenDisposed = true;
			}
			return true;
		}

		protected override uint OnNegotiateBufferingRate(uint requestedBufferingRate)
		{
			uint negotiatedBufferingRate = 0;
			// TODO:  Negotiate Buffering Rate if Buffering Rate is supported.
			//        Also add code to implement buffering rate.
			return negotiatedBufferingRate;
		}

		public override ModifyListAttrsResult OnModifyListAttributes(
			Nullable<uint> updateRate, Nullable<uint> bufferingRate, FilterSet filterSet)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleAlarmsAndEvents == false)
				context.ThrowDisconnectedServerException(context.IOPCEventServer_ProgId);

			lock (_ListTransactionLock)
			{
				ModifyListAttrsResult results = new ModifyListAttrsResult();

				// Update the updateRate
				uint dwRevisedUpdateRate = 0;
				uint dwRevisedMaxSize = 0;

				cliHRESULT hr = IOPCEventSubscriptionMgt.SetState(
									null, //                    /*[in]*/ Nullable<bool> bActive,
									updateRate, //              /*[in]*/ Nullable<uint> dwBufferTime,
									null, //                    /*[in]*/ Nullable<uint> dwMaxSize,
									this.ClientId, //           /*[in]*/ uint hClientSubscription,
									out dwRevisedUpdateRate, // /*[out]*/ out uint dwRevisedBufferTime,
									out dwRevisedMaxSize); //   /*[out]*/ out uint dwRevisedMaxSize);
				if (hr.Succeeded)
				{
					results.RevisedUpdateRate = dwRevisedUpdateRate;
					_missedPollIntervalMsecs = UpdateRate * _numberOfUpdateCyclesToQueue;

					uint eventType = 0;
					List<uint> categories = new List<uint>();
					Nullable<uint> lowSeverity = 0;
					Nullable<uint> highSeverity = 0;
					List<string> areas = new List<string>();
					List<string> eventSources = new List<string>();

					if (filterSet != null && filterSet.Filters != null && filterSet.Filters.Count > 0)
					{
						ParseEventFilters(filterSet, out eventType, out categories, lowSeverity,
							highSeverity, out areas, out eventSources);

						SetOpcEventFilters(eventType, categories, lowSeverity, highSeverity, areas, eventSources);
						_filterSet = filterSet;
					}
				}
				else
				{
					throw FaultHelpers.Create((uint)hr.hResult, "The call to SetFilters failed.");
				}
				return results;
			}
		}

		/// <summary>
		/// This method is used to request that category-specific fields be 
		/// included in event messages generated for alarms and events of 
		/// the category for this Event/Alarm List.
		/// </summary>
		/// <param name="categoryId">
		/// The category for which event message fields are being added.
		/// </param>
		/// <param name="fieldObjectTypeIds">
		/// The list of category-specific fields to be included in the event 
		/// messages generated for alarms and events of the category.  Each field 
		/// is identified by its ObjectType LocalId obtained from the EventMessageFields 
		/// contained in the EventCategoryConfigurations Standard MIB element.
		/// </param>
		/// <returns>
		/// The client alias and result codes for the fields that could not be  
		/// added to the event message. Returns null if all succeeded.  
		/// </returns>
		public override List<TypeIdResult> OnAddEventMessageFields(
			uint categoryId, List<TypeId> fieldObjectTypeIds)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleAlarmsAndEvents == false)
				context.ThrowDisconnectedServerException(context.IOPCEventServer_ProgId);

			lock (_ListTransactionLock)
			{
				List<TypeIdResult> results = null;
				List<uint> attrIds = new List<uint>();
				foreach (var fieldTypeId in fieldObjectTypeIds)
				{
					attrIds.Add(Convert.ToUInt32(fieldTypeId.LocalId));
				}
				cliHRESULT HR = IOPCEventSubscriptionMgt.SelectReturnedAttributes(categoryId, attrIds);
				if (HR.Failed)
				{
					// force all result codes to the HR result code since OPC only returns one result
					// code for all message fields
					results = new List<TypeIdResult>();
					foreach (var fieldObjectTypeId in fieldObjectTypeIds)
					{
						TypeIdResult result = new TypeIdResult((uint)HR.hResult, fieldObjectTypeId);
						results.Add(result);
					}
				}
				return results;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="enableUpdating"></param>
		public override ListAttributes OnEnableListUpdating(bool enableUpdating)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleAlarmsAndEvents == false)
				context.ThrowDisconnectedServerException(context.IOPCEventServer_ProgId);

			lock (_ListTransactionLock)
			{
				if (null != IOPCEventSubscriptionMgt)
				{
					bool bActive = !enableUpdating;
					uint dwBufferTime = 0x7FFFFFFFu;
					uint dwMaxSize = 0x7FFFFFFu;
					uint hClientSubscription = 0;
					IOPCEventSubscriptionMgt.GetState(out bActive,
						out dwBufferTime, out dwMaxSize, out hClientSubscription);
					Debug.Assert(hClientSubscription == ClientId);
					if (bActive != enableUpdating)
					{
						if (enableUpdating)
						{
							IAdviseOPCEventSink.AdviseOnEvent(OnEvent);
						}
						uint dwRevisedBufferTime = 0;
						uint dwRevisedMaxSize = 0;
						IOPCEventSubscriptionMgt.SetState(enableUpdating, dwBufferTime, dwMaxSize,
							hClientSubscription, out dwRevisedBufferTime, out dwRevisedMaxSize);
						if (!enableUpdating)
						{
							IAdviseOPCEventSink.UnadviseOnEvent(OnEvent);
						}
					}
					Enabled = enableUpdating;
				}
				return ListAttributes;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override uint OnTouchList()
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleAlarmsAndEvents == false)
				context.ThrowDisconnectedServerException(context.IOPCEventServer_ProgId);

			lock (_ListTransactionLock)
			{
				uint dwConnection = 0;
				if (null != IOPCEventSubscriptionMgt)
				{
					cliHRESULT HR = IOPCEventSubscriptionMgt.Refresh(dwConnection);
					return (uint)HR.hResult;
				}
				return XiFaultCodes.E_NOTFOUND;
			}
		}

		/// <summary>
		/// This method is invoked from the OPC Wrapper code to notify of event messages.
		/// </summary>
		/// <param name="hClientSubscription"></param>
		/// <param name="bRefresh"></param>
		/// <param name="bLastRefresh"></param>
		/// <param name="Events"></param>
		/// <returns></returns>
		public void OnEvent(
			uint hClientSubscription,
			bool bRefresh,
			bool bLastRefresh,
			EventMessage[] events)
		{
			if ((Enabled) && (BeingDeleted == false))
			{
				if ((0 < events.Length) && (AdditionalEventTypeFilters != null) && (AdditionalEventTypeFilters.Count > 0))
					events = ApplyAdditionalEventTypeFilters(events);

                events = events.Concat(ContextImpl.OperatorActions).ToArray();

				if (CallbackActivated)
				{
					// TODO: If BufferingRate is supported, add code to buffer the events and return them at the proper time
					if (0 < events.Length)
						OnEventNotification(events);
				}
				else if (PollingActivated)
				{
					List<ValueRoot> elvList = new List<ValueRoot>();
					foreach (var evtMsg in events)
					{
						EventListValue elv = new EventListValue(0, 0, evtMsg);
						elv.EntryQueued = true;
						elvList.Add(elv);
					}

					QueueChangedValues(elvList);
				}
			}
			return;
		}

		protected EventMessage[] ApplyAdditionalEventTypeFilters(EventMessage[] events)
		{
			EventMessage[] eventArray = events;
			if ((0 < events.Length) && (AdditionalEventTypeFilters != null) && (AdditionalEventTypeFilters.Count > 0))
			{
				List<EventMessage> eventList = new List<EventMessage>();
				// TODO:  If AdditionalFilters is not null, then apply the additional filters here
				eventArray = eventList.ToArray();
			}
			return eventArray;
		}

		protected bool ParseEventFilters(FilterSet filterSet, out uint eventType, out List<uint> categories,
						Nullable<uint> lowSeverity, Nullable<uint> highSeverity, out List<string> areas, out List<string> eventSources)
		{
			bool bOpcFilters = false;
			eventType    = (uint)OPCAEEVENTTYPE.OPC_SIMPLE_EVENT | (uint)OPCAEEVENTTYPE.OPC_CONDITION_EVENT | (uint)OPCAEEVENTTYPE.OPC_TRACKING_EVENT;
			categories   = new List<uint>();
			areas        = new List<string>();
			eventSources = new List<string>();

			if (   (filterSet == null)
				|| (filterSet.Filters == null)
				|| (filterSet.Filters.Count == 0))
			{
				return false;
			}

			int eventTypeIdx    = -1;  // identifies the OredFilter in the filterSet that contains the EventType filter
			int categoriesIdx   = -1;  // identifies the OredFilter in the filterSet that contains the Categories filter
			int lowSeverityIdx  = -1;  // identifies the OredFilter in the filterSet that contains the lowSeverity filter
			int highSeverityIdx = -1;  // identifies the OredFilter in the filterSet that contains the highSeverity filter
			int areasIdx        = -1;  // identifies the OredFilter in the filterSet that contains the areas filter
			int eventSourcesIdx = -1;  // identifies the OredFilter in the filterSet that contains the eventSources filter

			AdditionalEventTypeFilters = new List<ORedFilters>();
			AdditionalNonAEFilters = new List<ORedFilters>();

			for (int idx = 0; idx < filterSet.Filters.Count; idx++)
			{
				ORedFilters oredFilter = filterSet.Filters[idx];
				if (   (oredFilter.FilterCriteria != null)
					&& (oredFilter.FilterCriteria.Count > 0))
				{
					switch (oredFilter.FilterCriteria[0].OperandName)
					{
						case FilterOperandNames.EventType:
							if (eventTypeIdx != -1)
								throw FaultHelpers.Create("FilterSet contains ANDed event types");
							eventTypeIdx = idx;
							ParseEventTypeFilterCriteria(oredFilter.FilterCriteria, ref eventType);
							if (eventType > 0)
								bOpcFilters = true;
							break;

						case FilterOperandNames.EventCategory:
							if (categoriesIdx != -1)
								throw FaultHelpers.Create("FilterSet contains ANDed event categories");
							categoriesIdx = idx;
							ParseCategoryFilterCriteria(oredFilter.FilterCriteria, categories);
							if ((categories != null) && (categories.Count > 0))
								bOpcFilters = true;
							break;
						case FilterOperandNames.EventPriority:
							if (oredFilter.FilterCriteria[0].Operator == FilterOperator.GreaterThanOrEqual)
							{
								if (lowSeverityIdx != -1)
									throw FaultHelpers.Create("FilterSet contains ANDed low event priorities");
								lowSeverityIdx = idx;
								ParseEventPriorityFilterCriteria(oredFilter.FilterCriteria, ref lowSeverity);
								if (lowSeverity != null)
								{
									bOpcFilters = true;
								}
							}
							else if (oredFilter.FilterCriteria[0].Operator == FilterOperator.LessThanOrEqual)
							{
								if (highSeverityIdx != -1)
									throw FaultHelpers.Create("FilterSet contains ANDed high event priorities");
								highSeverityIdx = idx;
								ParseEventPriorityFilterCriteria(oredFilter.FilterCriteria, ref highSeverity);
								if (highSeverity > 0)
									bOpcFilters = true;
							}
							else if (oredFilter.FilterCriteria[0].Operator == FilterOperator.Equal)
							{
								if (lowSeverityIdx != -1)
									throw FaultHelpers.Create("FilterSet contains ANDed event priorities");
								lowSeverityIdx = highSeverityIdx = idx;
								ParseEventPriorityFilterCriteria(oredFilter.FilterCriteria, ref lowSeverity);
								lowSeverity = highSeverity;
								if ((lowSeverity > 0) || (highSeverity < 0xffffffff))
									bOpcFilters = true;
							}
							break;
						case FilterOperandNames.Area:
							if (areasIdx != -1)
								throw FaultHelpers.Create("FilterSet contains ANDed areas");
							areasIdx = idx;
							ParseAreaFilterCriteria(oredFilter.FilterCriteria, areas);
							if ((areas != null) && (areas.Count > 0))
								bOpcFilters = true;
							break;
						case FilterOperandNames.EventSourceId:
							if (eventSourcesIdx != -1)
								throw FaultHelpers.Create("FilterSet contains ANDed event sources");
							eventSourcesIdx = idx;
							ParseEventSourceFilterCriteria(oredFilter.FilterCriteria, eventSources);
							if ((eventSources != null) && (eventSources.Count > 0))
								bOpcFilters = true;
							break;
						default:
							AdditionalNonAEFilters.Add(oredFilter);
							break;
					}
				}
			}
			return bOpcFilters;
		}

		protected FilterSet SetOpcEventFilters(uint eventType, List<uint> categories, Nullable<uint> lowSeverity, Nullable<uint> highSeverity,
											List<string> areas, List<string> eventSources)
		{
			// the current values in the OPC A&E server
			uint curEventType;
			List<uint> curCategories;
			uint curLowSeverity;
			uint curHighSeverity;
			List<string> curAreas;
			List<string> curEventSources;

			cliHRESULT hr = IOPCEventSubscriptionMgt.GetFilter(out curEventType,
														out curCategories,
														out curLowSeverity,
														out curHighSeverity,
														out curAreas,
														out curEventSources);
			{
				if (hr.Failed)
					throw FaultHelpers.Create((uint)hr.hResult, "Failure when calling GetFilter() on the A&E server.");
			}

			if ((eventType == 0) || (eventType == curEventType))
				eventType = curEventType;
			uint newLowSeverity = (lowSeverity != null) ? (uint)lowSeverity : curLowSeverity;
			uint newHighSeverity = (highSeverity != null) ? (uint)highSeverity : curHighSeverity;
			hr = IOPCEventSubscriptionMgt.SetFilter(eventType,
													categories,
													newLowSeverity,
													newHighSeverity,
													areas,
													eventSources);

			if (hr.Succeeded)
			{
				lowSeverity = curLowSeverity;
				highSeverity = curHighSeverity;
			}
			else // sync with OPC server's values in case the server set some and not others
			{
				hr = IOPCEventSubscriptionMgt.GetFilter(out eventType,
														out categories,
														out curLowSeverity,
														out curHighSeverity,
														out areas,
														out eventSources);
				if (hr.Failed)
					throw FaultHelpers.Create((uint)hr.hResult, "Failure when calling GetFilter() on the A&E server.");
				lowSeverity = curLowSeverity;
				highSeverity = curHighSeverity;
			}

			FilterSet revisedFilterSet = new FilterSet();
			revisedFilterSet.Filters = new List<ORedFilters>();
			ORedFilters oredFilter;
			FilterCriterion fc;
			if (eventType > 0)
			{
				oredFilter = new ORedFilters();
				oredFilter.FilterCriteria = new List<FilterCriterion>();
				if ((eventType | (uint)OPCAEEVENTTYPE.OPC_SIMPLE_EVENT) > 0)
				{
					fc = new FilterCriterion();
					fc.OperandName = FilterOperandNames.EventType;
					fc.Operator = FilterOperator.Equal;
					fc.ComparisonValue = EventType.SystemEvent.ToString("G");
					oredFilter.FilterCriteria.Add(fc);
				}
				if ((eventType | (uint)OPCAEEVENTTYPE.OPC_TRACKING_EVENT) > 0)
				{
					fc = new FilterCriterion();
					fc.OperandName = FilterOperandNames.EventType;
					fc.Operator = FilterOperator.Equal;
					fc.ComparisonValue = EventType.OperatorActionEvent.ToString("G");
					oredFilter.FilterCriteria.Add(fc);
				}
				if ((eventType | (uint)OPCAEEVENTTYPE.OPC_CONDITION_EVENT) > 0)
				{
					fc = new FilterCriterion();
					fc.OperandName = FilterOperandNames.EventType;
					fc.Operator = FilterOperator.Equal;
					fc.ComparisonValue = EventType.SimpleAlarm.ToString("G");
					oredFilter.FilterCriteria.Add(fc);
				}
				revisedFilterSet.Filters.Add(oredFilter);
			}
			if ((categories != null) && (categories.Count > 0))
			{
				oredFilter = new ORedFilters();
				oredFilter.FilterCriteria = new List<FilterCriterion>();
				foreach (var categoryId in categories)
				{
					fc = new FilterCriterion();
					fc.OperandName = FilterOperandNames.EventCategory;
					fc.Operator = FilterOperator.Equal;
					fc.ComparisonValue = categoryId;
					oredFilter.FilterCriteria.Add(fc);
				}
				revisedFilterSet.Filters.Add(oredFilter);
			}
			if (lowSeverity > 0)
			{
				oredFilter = new ORedFilters();
				oredFilter.FilterCriteria = new List<FilterCriterion>();
				if (lowSeverity == highSeverity)
				{
					fc = new FilterCriterion();
					fc.OperandName = FilterOperandNames.EventPriority;
					fc.Operator = FilterOperator.Equal;
					fc.ComparisonValue = lowSeverity;
					oredFilter.FilterCriteria.Add(fc);
				}
				else
				{
					fc = new FilterCriterion();
					fc.OperandName = FilterOperandNames.EventPriority;
					fc.Operator = FilterOperator.GreaterThanOrEqual;
					fc.ComparisonValue = lowSeverity;
					oredFilter.FilterCriteria.Add(fc);
				}
				revisedFilterSet.Filters.Add(oredFilter);
			}
			if (highSeverity > 0)
			{
				oredFilter = new ORedFilters();
				oredFilter.FilterCriteria = new List<FilterCriterion>();
				if (lowSeverity != highSeverity)
				{
					fc = new FilterCriterion();
					fc.OperandName = FilterOperandNames.EventPriority;
					fc.Operator = FilterOperator.LessThanOrEqual;
					fc.ComparisonValue = highSeverity;
					oredFilter.FilterCriteria.Add(fc);
				}
				revisedFilterSet.Filters.Add(oredFilter);
			}
			if ((areas != null) && (areas.Count > 0))
			{
				oredFilter = new ORedFilters();
				oredFilter.FilterCriteria = new List<FilterCriterion>();
				foreach (var area in areas)
				{
					fc = new FilterCriterion();
					fc.OperandName = FilterOperandNames.Area;
					fc.Operator = FilterOperator.Equal;
					fc.ComparisonValue = area;
					oredFilter.FilterCriteria.Add(fc);
				}
				revisedFilterSet.Filters.Add(oredFilter);
			}
			if ((eventSources != null) && (eventSources.Count > 0))
			{
				oredFilter = new ORedFilters();
				oredFilter.FilterCriteria = new List<FilterCriterion>();
				foreach (var eventSource in eventSources)
				{
					fc = new FilterCriterion();
					fc.OperandName = FilterOperandNames.Area;
					fc.Operator = FilterOperator.Equal;
					InstanceId iid = new InstanceId(InstanceIds.ResourceType_AE, null, eventSource);
					fc.ComparisonValue = iid.ToString();
					oredFilter.FilterCriteria.Add(fc);
				}
				revisedFilterSet.Filters.Add(oredFilter);
			}
			return revisedFilterSet;
		}

		protected void ParseEventTypeFilterCriteria(List<FilterCriterion> filterCriteria, ref uint eventType)
		{
			uint extractedEventType = 0;
			ORedFilters oredFilters = new ORedFilters();
			oredFilters.FilterCriteria = new List<FilterCriterion>();
			// loop through all the filterCriterion in this FilterCriteria and make sure they are all EventTypes
			// and that they all use the Equal operator. Then make sure their comparison values are strings
			foreach (var filterCriterion in filterCriteria)
			{
				if (filterCriterion.OperandName != FilterOperandNames.EventType)
					throw FaultHelpers.Create("Invalid Operand in EventType Filter Criteria = "
						+ filterCriterion.OperandName);
				if (filterCriterion.Operator != FilterOperator.Equal)
					throw FaultHelpers.Create("Invalid EventType Filter Operator = "
						+ FilterOperator.ToString(filterCriterion.Operator));

				string comparisonValue = "";
				try
				{
					comparisonValue = (string)filterCriterion.ComparisonValue;
				}
				catch
				{
					throw FaultHelpers.Create("Invalid EventType Filter Comparison Value Type = "
						+ filterCriterion.ComparisonValue.GetType().ToString());
				}

				if (string.Compare(comparisonValue, EventType.SystemEvent.ToString("G")) == 0)
					extractedEventType |= (uint)OPCAEEVENTTYPE.OPC_SIMPLE_EVENT;
				else if (string.Compare(comparisonValue, EventType.OperatorActionEvent.ToString("G")) == 0)
					extractedEventType |= (uint)OPCAEEVENTTYPE.OPC_TRACKING_EVENT;
				else if (string.Compare(comparisonValue, EventType.SimpleAlarm.ToString("G")) == 0)
					extractedEventType |= (uint)OPCAEEVENTTYPE.OPC_CONDITION_EVENT;
				else if (   (string.Compare(comparisonValue, EventType.Alert.ToString("G")) == 0)
						 || (string.Compare(comparisonValue, EventType.EclipsedAlarm.ToString("G")) == 0)
						 || (string.Compare(comparisonValue, EventType.GroupedAlarm.ToString("G")) == 0)
						)
				{
					// add any other event type filters to the list that gets passed back
					oredFilters.FilterCriteria.Add(filterCriterion);
				}
				else
					throw FaultHelpers.Create("Invalid EventType Filter Comparison Value = " + comparisonValue);
			}
			if (oredFilters.FilterCriteria.Count > 0)
				AdditionalEventTypeFilters.Add(oredFilters);
			// If one or more event types were extracted, return them in eventType.
			// If not, then eventType should not be changed
			if (extractedEventType > 0)
				eventType = extractedEventType;
		}

		protected void ParseCategoryFilterCriteria(List<FilterCriterion> filterCriteria, List<uint> categories)
		{
			// loop through all the filterCriterion in this FilterCriteria and make sure they are all EventCategories
			// and that they all use the Equal operator. Then make sure their comparison values are uints.
			foreach (var filterCriterion in filterCriteria)
			{
				if (filterCriterion.OperandName != FilterOperandNames.EventCategory)
					throw FaultHelpers.Create("Invalid Operand in EventCategory Filter Criteria = "
						+ filterCriterion.OperandName);
				if (filterCriterion.Operator != FilterOperator.Equal)
					throw FaultHelpers.Create("Invalid EventCategory Filter Operator = "
						+ FilterOperator.ToString(filterCriterion.Operator));

				uint comparisonValue = 0;
				try
				{
					comparisonValue = (uint)filterCriterion.ComparisonValue;
				}
				catch
				{
					throw FaultHelpers.Create("Invalid EventCategory Filter Comparison Value Type = "
						+ filterCriterion.ComparisonValue.GetType().ToString());
				}
				categories.Add(comparisonValue);
			}
		}

		protected void ParseEventPriorityFilterCriteria(List<FilterCriterion> filterCriteria, ref Nullable<uint> severity)
		{
			if (filterCriteria.Count > 1)
				throw FaultHelpers.Create("Invalid ORed Event Priorities in Filter Criteria");
			try
			{
				severity = (uint)filterCriteria[0].ComparisonValue;
			}
			catch
			{
				throw FaultHelpers.Create("Invalid EventPriority Filter Comparison Value Type = "
					+ filterCriteria[0].ComparisonValue.GetType().ToString());
			}

		}

		protected void ParseAreaFilterCriteria(List<FilterCriterion> filterCriteria, List<string> areas)
		{
			// loop through all the filterCriterion in this FilterCriteria and make sure they are all Areas
			// and that they all use the Equal operator. Then make sure their comparison values are strings.
			foreach (var filterCriterion in filterCriteria)
			{
				if (filterCriterion.OperandName != FilterOperandNames.Area)
					throw FaultHelpers.Create("Invalid Operand in Area Filter Criteria = "
						+ filterCriterion.OperandName);
				if (filterCriterion.Operator != FilterOperator.Equal)
					throw FaultHelpers.Create("Invalid Area Filter Operator = "
						+ FilterOperator.ToString(filterCriterion.Operator));

				string comparisonValue = "";
				try
				{
					comparisonValue = (string)filterCriterion.ComparisonValue;
				}
				catch
				{
					throw FaultHelpers.Create("Invalid Area Filter Comparison Value Type = "
						+ filterCriterion.ComparisonValue.GetType().ToString());
				}
				if (comparisonValue.Length > 0)
					areas.Add(comparisonValue);
			}
		}

		protected void ParseEventSourceFilterCriteria(List<FilterCriterion> filterCriteria, List<string> eventSources)
		{
			// loop through all the filterCriterion in this FilterCriteria and make sure they are all EventSources
			// and that they all use the Equal operator. Then make sure their comparison values are strings.
			foreach (var filterCriterion in filterCriteria)
			{
				if (filterCriterion.OperandName != FilterOperandNames.EventSourceId)
					throw FaultHelpers.Create("Invalid Operand in EventSource Filter Criteria = "
						+ filterCriterion.OperandName);
				if (filterCriterion.Operator != FilterOperator.Equal)
					throw FaultHelpers.Create("Invalid Area Filter Operator = "
						+ FilterOperator.ToString(filterCriterion.Operator));

				string comparisonValue = "";
				try
				{
					comparisonValue = (string)filterCriterion.ComparisonValue;
				}
				catch
				{
					throw FaultHelpers.Create("Invalid Area Filter Comparison Value Type = "
						+ filterCriterion.ComparisonValue.GetType().ToString());
				}
				if (comparisonValue.Length > 0)
				{
					InstanceId eventSourceId = new InstanceId(InstanceIds.ResourceType_AE, null, comparisonValue);
					if (string.IsNullOrEmpty(eventSourceId.LocalId) == false)
					{
						if (   (string.IsNullOrEmpty(eventSourceId.ResourceType))
							|| (eventSourceId.ResourceType == InstanceIds.ResourceType_AE)
						   )
						{
							eventSources.Add(eventSourceId.LocalId);
						}
						else
							throw FaultHelpers.Create("Invalid EventSource Id = " + comparisonValue);
					}
				}
			}
		}

        public override List<EventIdResult> OnAcknowledgeAlarms(
            string operatorName, string comment, List<EventId> alarmsToAck)
        {
            operatorName = string.IsNullOrEmpty(operatorName) ? "TEST USER" : operatorName;
            comment = string.IsNullOrEmpty(comment) ? "" : comment;

            var context = (ContextImpl)OwnerContext;

            var conditions = alarmsToAck.Select((alarm, idx) =>
                new OPCEVENTACKCONDITION
                {
                    dtActiveTime = alarm.TimeLastActive.GetValueOrDefault(),
                    dwCookie = uint.Parse(alarm.OccurrenceId),
                    sConditionName = alarm.Condition[0].LocalId,
                    sSource = alarm.SourceId.LocalId
                }).ToList();

            List<HandleAndHRESULT> errors;
            var result = context.IOPCEventServer.AckCondition(operatorName, comment, conditions, out errors);

            return alarmsToAck
                .Select(alarm => new EventIdResult { EventId = alarm, ResultCode = 0 })
                .ToList();
        }

	}
}
