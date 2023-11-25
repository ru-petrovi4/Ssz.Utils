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
using System.Collections.Generic;
using System.Security.Principal;
using System.ServiceModel;

using Xi.Contracts.Data;
using Xi.Common.Support.Extensions;

namespace Xi.Server.Base
{
	/// <summary>
	/// This class is intended to be used as the base class for the server-side context of a client
	/// connection.  An instance of this class is instantiated for each client context established 
	/// by IResourceManagement.Initiate(...). <see cref="ResourceManagement.Initiate"/>
	/// </summary>
	/// <typeparam name="TList">
	/// The concrete type used for the Xi Lists managed by this Context.  
	/// This is commonly specified as "ListRoot" as a context will generally 
	/// manage lists of multiple types.
	/// </typeparam>
	public abstract partial class ContextBase<TList>
		: IDisposable
		where TList : ListRoot
	{
		/// <summary>
		/// This object provides the general lock used to control access to an instance of a context.
		/// Each method that may change the state of this context instance should obtain this lock on entry.
		/// It is initialized to a value to provide a unique object that can be locked.
		/// </summary>
		protected object ContextLock = new Nullable<long>(System.Environment.TickCount);

		/// <summary>
		/// This object provides the lock used to single thread Browse requests (FindObjects(), FindRootPaths(), 
		/// FindTypes()) within a context.
		/// It is initialized to a value to provide a unique object that can be locked.
		/// </summary>
		protected object ContextBrowseLock = new Nullable<long>(System.Environment.TickCount);

		/// <summary>
		/// This object provides the lock used to single thread access to server info (Status(), 
		/// LookupResultCodes()) within a context.
		/// It is initialized to a value to provide a unique object that can be locked.
		/// </summary>
		protected object ContextServerInfoLock = new Nullable<long>(System.Environment.TickCount);

		/// <summary>
		/// Constructor
		/// </summary>
		protected ContextBase()
		{
			LastAccess = DateTime.UtcNow;
		}

		/// <summary>
		/// "finalizer" This method is invoked by the garbage collector when all 
		/// reference to this instance have been removed.  This method then takes 
		/// care of any cleanup needed by this instance.
		/// </summary>
		~ContextBase()
		{
			Dispose(false);
		}

		/// <summary>
		/// Invoke the dispose method to clean up this context instance.
		/// </summary>
		public void Dispose()
		{
			if (Dispose(true))
				GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Subclasses are allowed to overload this method to take care of any cleanup 
		/// needed by the subclass.  The subclass should also invoke this method to 
		/// take care of cleaning up this base class.
		/// </summary>
		/// <param name="isDisposing"></param>
		/// <returns></returns>
		protected virtual bool Dispose(bool isDisposing)
		{
			if (_hasBeenDisposed)
				return false;

			if (isDisposing)
			{
				if (!Concluded)
					OnConclude();

				CloseEndpointConnections();
			}

			_hasBeenDisposed = true;
			return true;
		}

		/// <summary>
		/// This flag may (should) be checked to insure that this 
		/// context instance is still valid.
		/// </summary>
		public bool HasBeenDisposed { get { return _hasBeenDisposed; } }
		protected bool _hasBeenDisposed = false;

		private IIdentity _identity;
		private DateTime _lastAccess;

		/// <summary>
		/// The collection of lists for this context.
		/// </summary>
		protected readonly Dictionary<uint, TList> _XiLists = new Dictionary<uint, TList>();		

		/// <summary>
		/// The collection of Endpoints for this context.
		/// The key for this dictionary is the Endpoint LocalId a GUID created by the Xi Server.
		/// </summary>
		protected readonly Dictionary<string, EndpointEntry<TList>> _XiEndpoints
			= new Dictionary<string, EndpointEntry<TList>>();

		private Random rand = new Random(unchecked((int)(DateTime.UtcNow.Ticks & 0x000000007FFFFFFFFL)));
		/// <summary>
		/// This method is used to obtain a unique list identification (server alias) for a Xi List instance.
		/// Note: "ContextLock" should be locked prior to invoking this method and remain locked until the 
		/// Xi List has been added to this Xi Context.
		/// </summary>
		/// <returns></returns>
		protected uint NewUniqueListId()
		{
			uint key = 0;
			lock (ContextLock)
			{
				do
				{
					key = (uint)rand.Next(1, 0x3FFFFFFF);
				} while (_XiLists.ContainsKey(key));
			}
			return key;
		}

		/// <summary>
		/// Context identifier (must be unique).
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The key to be used when re-initiating the context.
		/// </summary>
		public string ReInitiateKey { get; set; }

		/// <summary>
		/// Transport session identifier (may be null or not present).
		/// </summary>
		public string TransportSessionId { get; set; }

		/// <summary>
		/// Application name handed to server when context was created.
		/// </summary>
		public string ApplicationName { get; set; }

		/// <summary>
		/// Workstation name handed to server when context was created.
		/// </summary>
		public string WorkstationName { get; set; }

		/// <summary>
		/// User identity (may be null).
		/// </summary>
		public IIdentity Identity
		{
			get { return _identity; }
			set { _identity = value; }
		}

		/// <summary>
		/// User's locale, negotiated when context was created, zero for default.
		/// </summary>
		public uint LocaleId
		{
			get { return _LocaleId; }
			set { _LocaleId = ValidateLocaleId(value); }
		}
		private uint _LocaleId = 0x0409u;

		/// <summary>
		/// This method is used to validate the selected LocalId.  
		/// It will default to 0x409 (US English) if not in the 
		/// supported list.  This method may be overridden if 
		/// an alternative validation is desired.
		/// </summary>
		/// <param name="localId"></param>
		/// <returns></returns>
		protected virtual uint ValidateLocaleId(uint localeId)
		{
			lock (ContextLock)
			{
				if (_LocaleIds.Contains(localeId))
					return localeId;
			}
			return 0x0409u;
		}

		/// <summary>
		/// This method is used to set the list of valid or supported 
		/// LocalId’s for the server.  This list is then used in the 
		/// validation of the LocalId.
		/// </summary>
		/// <param name="localIds"></param>
		public void SetSupportedLocaleIds(List<uint> localeIds)
		{
			// Do not allow an empty list.
			if (   (localeIds != null)
				&& (0 < localeIds.Count))
			{
				lock (ContextLock)
				{
					_LocaleIds = localeIds;
				}
			}
		}
		private List<uint> _LocaleIds = new List<uint>() { 0x0409u };

		/// <summary>
		/// The context options set for the context by the Initiate() method.
		/// </summary>
		public uint NegotiatedContextOptions
		{
			get { return _NegotiatedContextOptions; }
		}
		protected uint _NegotiatedContextOptions = 0;

		/// <summary>
		/// The number of accessible wrapped servers as defined by the context options .
		/// </summary>
		public uint NumberOfWrappedServersForThisContext
		{
			get { return _NumberOfWrappedServersForThisContext; }
			set { _NumberOfWrappedServersForThisContext = value; }
		}
		private uint _NumberOfWrappedServersForThisContext = 0;

		/// <summary>
		/// Indicates when TRUE that the Data Access capabilities of the server are enabled
		/// </summary>
		abstract public bool IsAccessibleDataAccess { get; }

		/// <summary>
		/// Indicates when TRUE that the Alarms and Events capabilities of the server are enabled
		/// </summary>
		abstract public bool IsAccessibleAlarmsAndEvents { get; }

		/// <summary>
		/// Indicates when TRUE that the Journal Data Access capabilities of the server are enabled
		/// </summary>
		abstract public bool IsAccessibleJournalDataAccess { get; }

		/// <summary>
		/// Indicates when TRUE that the Journal Alarms and Events capabilities of the server are enabled
		/// </summary>
		abstract public bool IsAccessibleJournalAlarmsAndEvents { get; }

		/// <summary>
		/// The negotiated timeout in milliseconds from the Resource Discover Initiate
		/// </summary>
		public TimeSpan ContextTimeout
		{
			get { return _ContextTimeout; }
			set { _ContextTimeout = ValidateContextTimeout(value); }
		}
		private TimeSpan _ContextTimeout = new TimeSpan(0, 10, 0);

		/// <summary>
		/// The implementation class may override this method to 
		/// validate an acceptable timeout for the server instance.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		protected virtual TimeSpan ValidateContextTimeout(TimeSpan value)
		{
			if (TimeSpan.Zero == value) return new TimeSpan(0, 10, 0);			// Default is 10 Minutes (a long time)
			if (new TimeSpan(0, 0, 9) > value) return new TimeSpan(0, 0, 9);	// The minimum timeout is nine seconds.
			if (new TimeSpan(0, 30, 0) < value) return new TimeSpan(0, 30, 0);	// The maximum timeout is 30 minutes.
			return value;
		}

		/// <summary>
		/// The last time the context was accessed.
		/// </summary>
		internal DateTime LastAccess
		{
			get { return _lastAccess; }
			set { _lastAccess = value; }
		}

		/// <summary>
		/// This method is invoked to determine if this context 
		/// instance has timed out.  And thus should be disposed.
		/// </summary>
		/// <param name="timeNow"></param>
		/// <returns></returns>
		internal bool CheckTimeout(DateTime timeNow)
		{
			return (timeNow - LastAccess) > ContextTimeout;
		}

		/// <summary>
		/// Invoke this method to set the valid endpoint for this context.
		/// </summary>
		/// <param name="listEndpointDefinitions"></param>
		public virtual void AddEndpointsToContext(List<EndpointDefinition> listEndpointDefinitions)
		{
			lock (ContextLock)
			{
				foreach (var ed in listEndpointDefinitions)
				{
					_XiEndpoints.Add(ed.EndpointId, new EndpointEntry<TList>(ed));
				}
			}
		}

		/// <summary>
		/// Indicates, when TRUE, that OnConclude has been completed on this context
		/// </summary>
		public bool Concluded
		{
			get { return _concluded; }
			set { _concluded = value; }
		}
		protected bool _concluded = false;

		/// <summary>
		/// Indicates, when TRUE, that OnConclude has been called on this context
		/// </summary>
		public bool Concluding
		{
			get { return _concluding; }
			set { _concluding = value; }
		}
		protected bool _concluding = false;

		/// <summary>
		/// This method is used to close a context.  It deletes all lists and closes 
		/// supporting resources held by the context implementation.
		/// </summary>
		public void OnConclude()
		{
			lock (ContextLock)
			{
				// remove all lists from the read, write, and subscribe endpoints
				foreach (var ep in _XiEndpoints)
					if (ep.Value.IsOpen)
						OnCloseEndpoint(ep.Value.EndpointDefinition.EndpointId);
			}

			List<TList> removedLists = new List<TList>();
			List<AliasResult> listAliasResult = RemoveListsFromContext(null, out removedLists);
			DisposeLists(removedLists);

			OnReleaseResources();
			_concluded = true; // this should already be set to true by the server's LookupContext() method.
		}

		/// <summary>
		/// Override this method in an implementation subclass to release resources 
		/// held by the server, such as connections to wrapped servers.
		/// </summary>
		public abstract void OnReleaseResources();

		/// <summary>
		/// This validates the security credentials of the user each time the
		/// context is retrieved.  It should ensure the Paged credentials match
		/// the current transport security credentials.
		/// </summary>
		/// <param name="ctx">WCF operation context currently active</param>
		/// <returns>true/false</returns>
		public bool ValidateSecurity(OperationContext ctx)
		{
			return true;
			//// Validate context ID
			//if (ctx == null)
			//	return false;

			//// Not the same transport connection?
			//if (ctx.SessionId != TransportSessionId)
			//	return false;

			//// No security?
			//if (ctx.ServiceSecurityContext == null)
			//	return false;

			//// Different user name?
			//if (ctx.ServiceSecurityContext.PrimaryIdentity.Name != Identity.Name)
			//	return false;

			//return OnValidateSecurity(ctx);
		}

		/// <summary>
		/// This abstract method is to be overridden to provide additional context
		/// security validation.
		/// </summary>
		/// <param name="ctx">WCF operation context currently active</param>
		/// <returns>true/false</returns>
		public virtual bool OnValidateSecurity(OperationContext ctx)
		{
			return true;
		}
	}
}
