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
using System.ServiceModel;
using Ssz.Utils;
using Xi.Contracts.Constants;
using Xi.Common.Support;
using Xi.Common.Support.Extensions;
using Ssz.Utils.Net4;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class defines the methods to be overridden by the server implementation 
	/// to support the Context Management methods of the IResourceManagement interface.
	/// </summary>
	public abstract partial class ContextBase<TList>
		where TList : ListRoot
	{
		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="negotiatedContextOptions"></param>
		/// <param name="reInitiateKey"></param>
		public void OnReInitiate(bool allowDifferentClientIpAddress, uint negotiatedContextOptions, ref string reInitiateKey)
		{
			lock (ContextLock)
			{
				if (ReInitiateKey == null) // Not having an existing ReInitiateKey is an error
				{
					LastAccess = DateTime.Now - ContextTimeout; // set last access back so it will timeout
					throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);
				}
				// otherwise if the reInitiateKey parameter doesn't match the ReIniitateKey of the context
				else if (   (reInitiateKey == null)
						 || (string.Compare(reInitiateKey, ReInitiateKey, false) != 0))
				{
					throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT);
				}

				// Validate OperationContext 
				if (OperationContext.Current == null)
					throw FaultHelpers.Create(XiFaultCodes.E_INVALIDREQUEST, "No OperationsContext");

				// No security?
				if (OperationContext.Current.ServiceSecurityContext == null)
					throw FaultHelpers.Create(XiFaultCodes.E_INVALIDREQUEST, "No ServiceSecurityContext");

				// Different user name?
				if (OperationContext.Current.ServiceSecurityContext.PrimaryIdentity.Name != Identity.Name)
					throw FaultHelpers.Create(XiFaultCodes.E_INVALIDREQUEST, "Different User");

				_NegotiatedContextOptions = negotiatedContextOptions;

				OperationContext ctx = OperationContext.Current;
				TransportSessionId = ctx.SessionId;
			}

			// call implementation-specific processing
			// do not lock the context for this call. Let the called method lock it instead
			// to allow for overrides that may have the potential for deadlocks
			OnReInitiate(OperationContext.Current);
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		public virtual void OnReInitiate(OperationContext ctx)
		{
			// TODO - Implement an override of the OnReInitiate method for any additional processing associated with OnReInitiate();
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project. This method is invoked when the context times-out.
		/// </summary>
		public abstract void OnClientKeepAlive();

		public void CloseEndpointConnections()
		{
		    if ((_iRegisterForCallbackEndpointEntry != null) && (_iRegisterForCallbackEndpointEntry.WcfChannel != null))
			{
				try { ChannelCloser.Close(_iRegisterForCallbackEndpointEntry.WcfChannel); }
                catch (Exception e)
                {
                    Logger.Verbose(e);
                }
                _iRegisterForCallbackEndpointEntry.WcfChannel = null;
				_iRegisterForCallbackEndpointEntry.Dispose();
				_iRegisterForCallbackEndpointEntry = null;
			}

			foreach (var ep in this._XiEndpoints)
			{
				if (ep.Value.WcfChannel != null)
				{
					try { ChannelCloser.Close(ep.Value.WcfChannel); }
                    catch (Exception e)
                    {
                        Logger.Verbose(e);
                    }
                    ep.Value.WcfChannel = null;
					ep.Value.Dispose();
				}
			}
			_XiEndpoints.Clear();
		}

		/// <summary>
		/// This method validates that a Read, Write, Poll, or RegisterForCallback endpoint can be used.
		/// </summary>
		/// <param name="endpointEntry">The endpoint entry for the Read, Write, Poll, or RegisterForCallback endpoint.</param>
		public void AuthorizeEndpointUse(EndpointEntry<TList> endpointEntry)
		{
            endpointEntry.IsOpen = true;
        }
	}
}
