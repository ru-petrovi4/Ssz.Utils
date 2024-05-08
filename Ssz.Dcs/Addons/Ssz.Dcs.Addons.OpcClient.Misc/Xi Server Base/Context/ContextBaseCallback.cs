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
using System.Linq;
using System.Collections.Generic;
using System.ServiceModel;
using Ssz.Utils;
using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Microsoft.Extensions.Logging;


namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class defines the methods that support the methods 
	/// of the ICallback interface.
	/// </summary>
	public abstract partial class ContextBase<TList>
		where TList : ListRoot
	{
		/// <summary>
		/// Indicates, when TRUE, that the Callback endpoint is open
		/// </summary>
		public bool CallbackEndpointOpen { get { return (null != _iCallback); } }
		private ICallback _iCallback = null;

		/// <summary>
		/// The time of the completion of the last callback sent on this context. 
		/// The value is not set until the callback call returns.
		/// </summary>
		public DateTime LastCallbackTime { get { return _lastCallbackTime; } }
		protected DateTime _lastCallbackTime;

		/// <summary>
		/// The time interval for keep-alive callbacks
		/// </summary>
		public TimeSpan CallbackRate { get { return _callbackRate; } }
		protected TimeSpan _callbackRate;

		/// <summary>
		/// This method is used to indicate to the client that the Xi Server is shutting down. 
		/// </summary>
		/// <param name="reason"></param>
		public void OnAbort(ServerStatus serverStatus, string reason)
		{
			ICallback iCallback = null;
			lock (ContextLock)
			{
				if (null == _iCallback)
					return;
				iCallback = _iCallback;
			}

			try
			{
				if (null != iCallback)
				{
					iCallback.Abort(Id, serverStatus, reason);
				}
			}
			catch(Exception e)
			{
			    StaticLogger.Logger.LogDebug(e, @"Exception");
			}
		}

		/// <summary>
		/// This method invokes an Information Report back to the Xi client for data changes.
		/// </summary>
		/// <param name="listId"></param>
		/// <param name="updatedValues"></param>
		public void OnInformationReport(uint listId, DataValueArraysWithAlias readValueList)
		{
			ICallback iCallback = null;
			lock (ContextLock)
			{
				if (null == _iCallback)
					return;
				iCallback = _iCallback;
			}

		    try
		    {
		        if (null != iCallback)
		        {
		            iCallback.InformationReport(Id, listId, readValueList);
		            _lastCallbackTime = DateTime.UtcNow;
		        }
		    }		    
		    catch (Exception ex)
		    {
		        StaticLogger.Logger.LogDebug(ex, @"Exception");
		        exMsg1 = ex.Message;
		    }
		}
		private static string exMsg1;

		/// <summary>
		/// This method invokes an Event Notification back to the Xi client when an event needs to be reported.
		/// </summary>
		/// <param name="listId"></param>
		/// <param name="eventList"></param>
		public void OnEventNotification(uint listId, EventMessage[] eventsArray)
		{
			ICallback iCallback = null;
			lock (ContextLock)
			{
				if (null == _iCallback)
					return;
				iCallback = _iCallback;
			}

			try
			{
				if (null != iCallback)
				{
					iCallback.EventNotification(Id, listId, eventsArray);
					_lastCallbackTime = DateTime.UtcNow;
				}
			}            
            catch (Exception ex)
			{
                StaticLogger.Logger.LogDebug(ex, @"Exception");
                exMsg2 = ex.Message;

			    _iCallback = null;
			}
		}
		private static string exMsg2;

		/// <summary>
		/// This method is invoked by a Xi client to establish the clients ICallback interface.
		/// </summary>
		/// <param name="iCallBack">
		/// The reference to the callback to set.
		/// </param>		
		/// <param name="callbackRate">
		/// <para>Optional rate that specifies how often callbacks are to be sent to the client. </para> 
		/// </para>TimeSpan.Zero if not used. When not used, the UpdateRate of the lists assigned to this 
		/// callback dictates when callbacks are sent.  </para>
		/// <para>When present, the server buffers list outputs when the callback rate is longer 
		/// than list UpdateRates.  </para>
		/// </param>
		/// <returns>
		/// The results of the operation, including the negotiated keep-alive skip count and callback rate.
		/// </returns>
		public SetCallbackResult OnSetCallback(ICallback iCallBack,
			TimeSpan callbackRate)
		{
			lock (ContextLock)
			{
				_iCallback = iCallBack;
			}
			// do not lock the context for this call. Let the called method lock it instead
			// to allow for overrides that may have the potential for deadlocks
			return OnNegotiateCallbackParams(callbackRate);
		}

		/// <summary>
		/// This method can be overriddent by the implementation class to negotitate the 
		/// keep-alive skip count and the callback rate. 
		/// </summary>
		/// <param name="keepAliveSkipCount">
		/// The number of consecutive UpdateRate cycles that occur with nothing to send before 
		/// an empty callback is sent to indicate a keep-alive message. For example, if the value 
		/// of this parameter is 1, then a keep-alive callback will be sent each UpdateRate cycle 
		/// for which there is nothing to send. A value of 0 indicates that keep-alives are not 
		/// to be sent.
		/// </param>
		/// <returns>
		/// The results of the operation, including the negotiated keep-alive skip count and callback rate.
		/// </returns>
		public virtual SetCallbackResult OnNegotiateCallbackParams(TimeSpan callbackRate)
		{
			lock (ContextLock)
			{
				// Set the callback rate (the keep-alive rate) to between 5 seconds and one minute
				if (callbackRate.TotalMilliseconds < 5000)
					_callbackRate = new TimeSpan(0, 0, 0, 0, 5000);
				else if (callbackRate.TotalMilliseconds > 60000)
					_callbackRate = new TimeSpan(0, 0, 0, 0, 60000);
				else
					_callbackRate = callbackRate;
				return new SetCallbackResult(XiFaultCodes.S_OK,
					_callbackRate);
			}
		}

		/// <summary>
		/// Invoke this method to stop callbacks by letting the callback interface go.
		/// </summary>
		/// <returns></returns>
		public virtual uint OnClearCallback()
		{
			lock (ContextLock)
			{
				_iCallback = null;
				return XiFaultCodes.S_OK;
			}
		}
	}
}
