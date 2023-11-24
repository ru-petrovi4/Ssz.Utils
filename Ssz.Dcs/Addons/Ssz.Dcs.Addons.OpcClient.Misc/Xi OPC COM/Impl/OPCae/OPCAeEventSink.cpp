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

#include "..\StdAfx.h"
#include "OPCAeEventSink.h"
#include "OPCAeServer.h"
#include "..\Helper.h"

using namespace System::Collections::Generic;

using namespace Xi::Contracts::Data;
using namespace Xi::Contracts::Constants;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	COPCAeEventSink::COPCAeEventSink(void)
	{
	}

	COPCAeEventSink::~COPCAeEventSink(void)
	{
	}

	STDMETHODIMP COPCAeEventSink::OnEvent (
		/*[in]*/ unsigned long hClientSubscription,
		/*[in]*/ long bRefresh,
		/*[in]*/ long bLastRefresh,
		/*[in]*/ unsigned long dwCount,
		/*[in]*/ ONEVENTSTRUCT * pEvents )
	{
		HRESULT hr = S_OK;
		COPCAeSubscription^ opcAeSubscription = AeServer->FindUsingKey(hClientSubscription);
		if (nullptr != opcAeSubscription)
		{
			bool Refresh = (0 != bRefresh);
			bool LastRefresh = (0 != bLastRefresh);
			array<EventMessage^>^ evtMsgArray = gcnew array<EventMessage^>(dwCount);
			for (unsigned long idx = 0; idx < dwCount; idx++)
			{
				EventMessage^ evtMsg = gcnew EventMessage();
				// Each element of the Event Message is converted from the On Event Struct
				// Occurrence Time
				evtMsg->OccurrenceTime = DateTime::FromFileTimeUtc(*((__int64*)(&pEvents[idx].ftTime)));
				// Event Id - Parts may be updated
				evtMsg->EventId = gcnew EventId();
				evtMsg->EventId->SourceId = gcnew InstanceId(
					InstanceIds::ResourceType_AE, nullptr, 
					gcnew String(pEvents[idx].szSource));
				evtMsg->EventId->MultiplexedAlarmContainer = nullptr;
				evtMsg->EventId->Condition = nullptr;
				evtMsg->EventId->OccurrenceId = nullptr;
				// Text Message
				evtMsg->TextMessage = gcnew String(pEvents[idx].szMessage);
				// Category Id
				evtMsg->CategoryId = pEvents[idx].dwEventCategory;
				// Priority
				evtMsg->Priority = pEvents[idx].dwSeverity;
				// Operator Name - May be updated
				evtMsg->OperatorName = nullptr;
				// Alarm Data - May be updated
				evtMsg->AlarmData = nullptr;
				// Event Type
				switch (pEvents[idx].dwEventType)
				{
					case OPCAEEVENTTYPE::OPC_SIMPLE_EVENT:
						evtMsg->EventType = EventType::SystemEvent;
						break;

					case OPCAEEVENTTYPE::OPC_TRACKING_EVENT:
						evtMsg->EventType = EventType::OperatorActionEvent;
						evtMsg->OperatorName = gcnew String(pEvents[idx].szActorID);
						break;

					case OPCAEEVENTTYPE::OPC_CONDITION_EVENT:
					{
						evtMsg->EventType = EventType::SimpleAlarm;
						int action = 0;
						if (nullptr == pEvents[idx].szConditionName || 0 == wcslen(pEvents[idx].szConditionName)) action += 2;			//determine if the condition is present
						if (nullptr == pEvents[idx].szSubconditionName || 0 == wcslen(pEvents[idx].szSubconditionName)) action += 1;	//determine if the subcondition is present
						if (0 == action)
						{
							if (0 != _wcsicmp(pEvents[idx].szConditionName, pEvents[idx].szSubconditionName)) action = 4;				//determine if condition and subcondition are the same value
						}
						switch (action)
						{
							case 0:		// Both present and same
							case 1:		// Only Condition is present
								evtMsg->EventId->Condition = gcnew List<TypeId^>();
								evtMsg->EventId->Condition->Add(gcnew TypeId(
									XiSchemaType::OPC,
									opcAeSubscription->OPCAeServer->ServerDescription->VendorNamespace,
									gcnew String(pEvents[idx].szConditionName)));
								break;

							case 2:		// Only Subcondition is present
								evtMsg->EventId->Condition = gcnew List<TypeId^>();
								evtMsg->EventId->Condition->Add(gcnew TypeId(
									XiSchemaType::OPC,
									opcAeSubscription->OPCAeServer->ServerDescription->VendorNamespace,
									gcnew String(pEvents[idx].szSubconditionName)));
								break;

							case 3:		// Neither condition or subconditon is present
								break;

							case 4:		// Both present and different
								evtMsg->EventId->MultiplexedAlarmContainer = gcnew TypeId(
									XiSchemaType::OPC,
									opcAeSubscription->OPCAeServer->ServerDescription->VendorNamespace,
									gcnew String(pEvents[idx].szConditionName));
								evtMsg->EventId->Condition = gcnew List<TypeId^>();
								evtMsg->EventId->Condition->Add(gcnew TypeId(
									XiSchemaType::OPC,
									opcAeSubscription->OPCAeServer->ServerDescription->VendorNamespace,
									gcnew String(pEvents[idx].szSubconditionName)));
								break;

							default:
								break;
						}
						int alarmState = 0;
						if (0 == ((short)OPCAECONDITIONSTATE::OPC_CONDITION_ENABLED & pEvents[idx].wNewState))
							alarmState |= (int)AlarmState::Disabled;
						if (0 != ((short)OPCAECONDITIONSTATE::OPC_CONDITION_ACTIVE & pEvents[idx].wNewState))
							alarmState |= (int)AlarmState::Active;
						if (0 == ((short)OPCAECONDITIONSTATE::OPC_CONDITION_ACKED & pEvents[idx].wNewState))
							alarmState |= (int)AlarmState::Unacked;
						evtMsg->AlarmData = gcnew AlarmMessageData();
						evtMsg->AlarmData->AlarmState = (AlarmState)alarmState;
						evtMsg->AlarmData->AlarmStateChange = pEvents[idx].wChangeMask;
						evtMsg->AlarmData->TimeLastActive = DateTime::FromFileTimeUtc(*((__int64*)(&pEvents[idx].ftActiveTime)));
						evtMsg->EventId->OccurrenceId = pEvents[idx].dwCookie.ToString();
						evtMsg->OperatorName = gcnew String(pEvents[idx].szActorID);
						break;
					}
					default:
						evtMsg->EventType = (EventType)0;
						_ASSERTE(1 == pEvents[idx].dwEventType || 2 ==  pEvents[idx].dwEventType || 4 ==  pEvents[idx].dwEventType);
						break;
				}
				// Client Requested Fields
				evtMsg->ClientRequestedFields = nullptr;
				if (0 < pEvents[idx].dwNumEventAttrs)
				{
					evtMsg->ClientRequestedFields = gcnew List<Object^>(pEvents[idx].dwNumEventAttrs);
					for (unsigned int idx1 = 0; idx1 < pEvents[idx].dwNumEventAttrs; idx1++)
					{
						evtMsg->ClientRequestedFields->Add(
							CHelper::ConvertFromVARIANT(&pEvents[idx].pEventAttributes[idx1]).DataValue);
					}
				}
				// = pEvents[idx].wQuality;
				// = pEvents[idx].bAckRequired;
				evtMsgArray[idx] = evtMsg;
			}

			// Make the callback (invoke delegate) to the C# code to complete the processing
			if (nullptr != opcAeSubscription->m_onEvent)
			{
				opcAeSubscription->m_onEvent(hClientSubscription, Refresh, LastRefresh, evtMsgArray);
			}
		}
		return hr;
	}

}}}}
