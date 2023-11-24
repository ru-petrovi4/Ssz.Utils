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

using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Xi.OPCWrapper.Service
{
	[RunInstaller(true)]
	public partial class XiOPCwInstaller
		: Installer
	{
		private ServiceProcessInstaller processInstaller;
		private ServiceInstaller serviceInstaller;

		public XiOPCwInstaller()
		{
			InitializeComponent();
			processInstaller = new ServiceProcessInstaller();
			serviceInstaller = new ServiceInstaller();

			processInstaller.Account = ServiceAccount.LocalSystem;
			processInstaller.Username = null;
			processInstaller.Password = null;

			serviceInstaller.ServiceName = XiOPCWrapperService.WindowsServiceName;
			serviceInstaller.DisplayName = XiOPCWrapperService.WindowsServiceDisplayName;
			serviceInstaller.Description = XiOPCWrapperService.WindowsServiceDescription;
			serviceInstaller.ServicesDependedOn = XiOPCWrapperService.WindowsServiceDependedOn;
			serviceInstaller.StartType = ServiceStartMode.Automatic;

			this.Installers.Add(processInstaller);
			this.Installers.Add(serviceInstaller);
		}
	}
}
