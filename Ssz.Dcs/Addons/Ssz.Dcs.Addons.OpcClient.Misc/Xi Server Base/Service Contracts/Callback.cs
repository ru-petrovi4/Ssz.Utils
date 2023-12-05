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
using Ssz.Utils.Net4;
using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class implements the IRegisterForCallback interface
	/// </summary>
	public abstract partial class ServerBase<TContext, TList> : ServerRoot
									, IRegisterForCallback
									where TContext : ContextBase<TList>
									where TList : ListRoot
	{
		SetCallbackResult IRegisterForCallback.SetCallback(string contextId,
			uint keepAliveSkipCount, TimeSpan callbackRate, ICallback iCallBack)
		{
			using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);					

					return context.OnSetCallback(iCallBack, keepAliveSkipCount, callbackRate);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

	}
}
