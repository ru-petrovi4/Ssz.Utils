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
using System.ServiceProcess;
using Ssz.Utils;
using Ssz.Utils.Net4;
using Xi.OPC.Wrapper.Impl;

namespace Xi.OPCWrapper.Service
{
	public partial class XiOPCWrapperService
		: ServiceBase
	{
		// TODO:  Update these entries with the Service Name, Display Name and Description
		//        Also setup the Server Account and Password this service is to run under
		public const string WindowsServiceName = "UsoXiService";
		public const string WindowsServiceDisplayName = "UsoXiService";
		public const string WindowsServiceDescription = "UniSim Operations OPC Xi Service for DA, HDA and A&E COM Servers";
		public static string[] WindowsServiceDependedOn = new string[] { @"" };

		private XiServiceMain _serviceMain = null;

		public XiOPCWrapperService()
		{
			InitializeComponent();
			_serviceMain = new XiServiceMain(XiServiceMain.MainProgramType.ServiceModeDataServer);
		}

		protected override void OnStart(string[] args)
		{
			// Wait additional time for the MEX to load and for the COM servers to startup
			this.RequestAdditionalTime(300000);
			// Wait 30 seconds before connecting to the COM servers to give them time to startup.
			// Retry connecting to the COM servers 5 times before giving up.
			try
			{				
                if (!_serviceMain.OnStartDataServer(30000, 5))
					Logger.Info("Failed to initialize server.");
            }
			catch (Exception ex)
			{
				string msg = ex.Message;
				if (ex.InnerException != null)
					msg += " InnerException = " + ex.InnerException.Message;
                Logger.Info("Failed to initialize server. Exception = " + msg);
			}
		}

		protected override void OnStop()
		{
			_serviceMain.OnStopDataServer();
		}
	}
}
