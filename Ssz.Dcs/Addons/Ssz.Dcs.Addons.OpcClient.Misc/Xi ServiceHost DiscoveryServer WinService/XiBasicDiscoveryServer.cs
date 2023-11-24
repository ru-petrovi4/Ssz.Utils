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

using System.ServiceProcess;

using Xi.Server.Base;

namespace Xi.DiscoveryServer.Service
{
	public partial class XiBasicDiscoveryServer
		: ServiceBase
	{
		// TODO:  Update these entries with the Service Name, Display Name and Description
		//        Also setup the Server Account and Password this service is to run under
		public const string WindowsServiceName = "OPC.NETDiscoveryServer";
		public const string WindowsServiceDisplayName = "OPC .NET Discovery";
		public const string WindowsServiceDescription = "The OPC .NET Discovery Server discovers OPC .NET servers and makes the list of OPC .NET servers available to client applications.";
		public static string[] WindowsServiceDependedOn = new string[] { @"" };

		private XiDiscoveryMain _serviceMain = null;

		public XiBasicDiscoveryServer()
		{
			InitializeComponent();
			_serviceMain = new XiDiscoveryMain(XiDiscoveryMain.MainProgramType.ServiceModeDiscoveryServer);
		}

		protected override void OnStart(string[] args)
		{
			_serviceMain.OnStartDiscoveryServer();
		}

		protected override void OnStop()
		{
			_serviceMain.OnStopDiscoveryServer();
		}
	}
}
