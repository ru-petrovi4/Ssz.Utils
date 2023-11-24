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
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

using Xi.OPC.COM.API;

using Xi.Server.Base;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// 
	/// </summary>
	public partial class ContextImpl
		: ContextBase<ListRoot>
	{
		/// <summary>
		/// This method will be invoked as a result of the OPC DA COM
		/// server issuing a call back to update data.
		/// </summary>
		/// <param name="dwTransid"></param>
		/// <param name="hGroup"></param>
		/// <param name="hrMasterquality"></param>
		/// <param name="hrMastererror"></param>
		/// <param name="hClientItems"></param>
		/// <param name="vDataValues"></param>
		/// <param name="uStatusCode"></param>
		/// <param name="dtTimeStamps"></param>
		public void OnDataChange(
			/*[in]*/ uint dwTransid,
			/*[in]*/ uint hGroup,
			/*[in]*/ cliHRESULT hrMasterquality,
			/*[in]*/ cliHRESULT hrMastererror,
			/*[in]*/ DataValueArraysWithAlias valueArrays)
		{
			try
			{
				ListRoot listRoot = null;
				lock (ContextLock)
				{
					_XiLists.TryGetValue(hGroup, out listRoot);
				}

				if (null != listRoot)
				{
					DataList dataList = listRoot as DataList;
					if (null != dataList)
					{
						dataList.OnDataChange(hrMasterquality, hrMastererror, valueArrays);
					}
				}
			}
			catch //(Exception e)
			{
			    //Logger.Verbose(e);
			    // for debugging purposes
			}
		}

		public void OnDAShutdown(string sReason)
		{
			ServerBase<ContextImpl, ListRoot>.DaRolesMethodsAndFeaturesSet = false;
			ServerBase<ContextImpl, ListRoot>.DaLocaleIdSet = false;
			OpcReleaseDA();
			OnShutdown(Xi.Contracts.Constants.ServerType.OPC_DA205_Wrapper, IOPCServer_ProgId, sReason);
		}

		public void OnHDAShutdown(string sReason)
		{
			ServerBase<ContextImpl, ListRoot>.HdaRolesMethodsAndFeaturesSet = false;
			ServerBase<ContextImpl, ListRoot>.HdaLocaleIdSet = false;
			OpcReleaseHDA();
			OnShutdown(Xi.Contracts.Constants.ServerType.OPC_HDA12_Wrapper, IOPCHDAServer_ProgId, sReason);
		}

		public void OnAEShutdown(string sReason)
		{
			ServerBase<ContextImpl, ListRoot>.AeRolesMethodsAndFeaturesSet = false;
			ServerBase<ContextImpl, ListRoot>.AeLocaleIdSet = false;
			OpcReleaseAE();
			OnShutdown(Xi.Contracts.Constants.ServerType.OPC_AE11_Wrapper, IOPCEventServer_ProgId, sReason);
		}

		public void OnShutdown(uint serverType, string serverName, string sReason)
		{
			ServerStatus serverStatus = new ServerStatus();
			serverStatus.ServerType = serverType;
			serverStatus.ServerName = serverName;
			serverStatus.ServerState = ServerState.Aborting;
			serverStatus.CurrentTime = DateTime.UtcNow;
			serverStatus.Info = sReason;
			try
			{
				if (CallbackEndpointOpen)
				{
					this.OnAbort(serverStatus, sReason);
				}
			}
			catch //(Exception e)
			{
			    //Logger.Verbose(e);
			}
		}

	}
}
