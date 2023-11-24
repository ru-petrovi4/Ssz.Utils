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
using System.Reflection;
using System.Configuration.Install;
using System.Diagnostics;
using Ssz.Utils;
using Ssz.Utils.Net4;
using Xi.OPC.Wrapper.Impl;
using System;

namespace Xi.OPCWrapper.Service
{
	static class XiOPCwWinSrvProgram
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
            Logger.Initialize();
			Logger.Verbose("Service starting...");

            if (null == args || 0 == args.Length || 0 == args[0].Length)
			{
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new XiOPCWrapperService(),
                };
                ServiceBase.Run(ServicesToRun);                			
            }
			else
			{
				// Set up event logging
				System.Collections.Specialized.NameValueCollection appSettings
					= System.Configuration.ConfigurationManager.AppSettings;
				string source = XiOPCWrapperService.WindowsServiceDisplayName;
				try
				{
					source = appSettings["Server"];
				}
				catch
				{
					source = XiOPCWrapperService.WindowsServiceDisplayName;
				}

				string exeFullName = Assembly.GetExecutingAssembly().Location;
				string option = args[0].ToUpper();
				switch (option)
				{
					case "-I":
					case "-INSTALL":
					case "/I":
					case "/INSTALL":
						ManagedInstallerClass.InstallHelper(new string[] { exeFullName });
						Logger.Info("Installed" + source);
						break;

					case "-U":
					case "-UNINSTALL":
					case "/U":
					case "/UNINSTALL":
						ManagedInstallerClass.InstallHelper(new string[] { "/u", exeFullName });
						Logger.Info("Uninstalled" + source);
						break;

					default:
						break;
				}
			}
		}
	}
}

//var serviceMain = new XiServiceMain(XiServiceMain.MainProgramType.ServiceModeDataServer);
//if (!serviceMain.OnStartDataServer(30000, 5))
//    Console.WriteLine("Failed to initialize server.");
