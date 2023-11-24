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
using System.ServiceModel;
using Xi.Server.Base;
#if USO
#if USO410_AND_LESS
#else
using Honeywell.UniSim.Operations;
#endif
#endif

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// Each client is represented by an instance of a this class.
	/// </summary>
	public partial class ContextImpl : ContextBase<ListRoot>
	{
		internal ContextImpl(XiOPCWrapper server)
		{
		}

		internal ContextImpl(XiOPCWrapper server, string transportSessionId, string applicationName,
            string encriptedWorkstationName, ref uint localeId, ref uint contextTimeout, uint contextOptions,
			System.Security.Principal.IIdentity userIdentity)
		{
            // We have two sources of information about UserId
            // 1. From encripted workstation name (it looks like: WorkstatinName?UserName=UserId&UserRole=SomeRole&SessionId=TrainingSample"
            // 2. From userIdentity.Name

            _userId = extractParameter(encriptedWorkstationName, "UserName");
            var userRole = extractParameter(encriptedWorkstationName, "UserRole");

            Id = Guid.NewGuid().ToString();
			ReInitiateKey = Guid.NewGuid().ToString();
			TransportSessionId = transportSessionId;
			ApplicationName = applicationName;
			WorkstationName = extractWorkstationName(encriptedWorkstationName);
			LocaleId = localeId;
			Identity = userIdentity;
			ContextTimeout = new TimeSpan(0, 0, 0, 0, (int)contextTimeout);
			_NegotiatedContextOptions = contextOptions;
            SessionId = extractParameter(encriptedWorkstationName, "SessionId");

#if USO
			_simExec = UsoClient.InitializeConnectionToUSO(SessionId);
#endif
        }

#if USO
	    public SimExec SimExec
	    {
	        get
	        {
	            if (_simExec != null)
	                return _simExec;

                return _simExec = UsoClient.InitializeConnectionToUSO(SessionId);
	        }
	    }

        private SimExec _simExec;
#endif

		public string UserId
        {
            get
            {
                return (string.IsNullOrEmpty(_userId) ? Identity.Name : _userId);
            }
        }
        public string ComputerAndUserId
        {
            get
            {
                return (string.IsNullOrEmpty(WorkstationName) ? "" : WorkstationName + "\\") + UserId;
            }
        }
	    
        private string  _userId;

        private static string extractWorkstationName(string encriptedWorkstationName)
	    {
            var strings = encriptedWorkstationName.Split(new[] { '?' });
            if (strings.Count() > 1)
                return strings[0];
            else
                return "";
	    }

        private static string extractParameter(string encriptedWorkstationName, string findParameter)
        {
            return (from pair in encriptedWorkstationName.Split(new[] { '?', '&' })
                    let kv = pair.Split('=')
                    let left = kv.ElementAtOrDefault(0)
                    let right = kv.ElementAtOrDefault(1)
                    where left == findParameter
                    select right).FirstOrDefault() ?? "";
        }

	    public override bool OnValidateSecurity(OperationContext ctx)
		{
			// TODO: Implement additional context security
			return true;
		}

		public override void OnReInitiate(OperationContext ctx)
		{
			// TODO: Implement additional context security
		}

		protected override bool Dispose(bool isDisposing)
		{
			bool success = base.Dispose(isDisposing);

			if (isDisposing)
			{
				OpcRelease();
				// Dispose SimExec

#if USO
                _simExec = null;
#endif
			}
			return success;
		}

	}
}
