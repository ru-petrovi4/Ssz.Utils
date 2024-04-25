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
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Diagnostics;
using Ssz.Utils;
using Xi.Contracts.Data;
using Xi.Contracts.Constants;
using Xi.Common.Support;


namespace Xi.Server.Base
{
	/// <summary>
	/// This is the Context Manager for the reference implementation of an Express Interface (Xi) Server.
	/// The reference implantation provides some base classes that allow for the implantation of 
	/// a Xi Server with some common or standardized behavior.
	/// This class manages the active contexts (sessions) and provides lookup, timeout and caching support.
	/// </summary>
	/// <typeparam name="TContext">Concrete server context type</typeparam>
	/// <typeparam name="TList">Concrete server List type base class</typeparam>
	public static class ContextManager<TContext, TList>
		where TContext : ContextBase<TList>
		where TList : ListRoot
	{
		public static bool IsServerShutdown = false;
		public static string ShutdownReason = null;

  //      public static void OnStart()
  //      {
  //          IsServerShutdown = false;            
  //      }

  //      /// <summary>
  //      /// This method is called by the server when it is being shutdown.
  //      /// </summary>
  //      /// <param name="reason">The reason for shutting down.</param>
  //      public static void OnShutdown(ServerStatus serverStatus, string reason)
		//{			
		//	IsServerShutdown = true;
		//	ShutdownReason = reason;
		//	TContext[] activeContexts;
  //          lock (_activeContexts)
		//	{
  //              activeContexts = _activeContexts.Values.ToArray();
  //          }
  //          foreach (var ctx in activeContexts)
  //          {
  //              if (ctx.CallbackEndpointOpen)
  //              {
  //                  ctx.OnAbort(serverStatus, reason);
  //              }
  //              if (ctx.Concluded == false)
  //              {
  //                  ctx.OnConclude();
  //              }
  //          }
  //      }

		private static readonly Dictionary<string, TContext> _activeContexts = new Dictionary<string, TContext>();

		/// <summary>
		/// This event is raised when the context collection managed by this class is altered.  It provides
		/// an opportunity for the server implementation to know about clients added/removed outside of the WCF
		/// connections (i.e. timeout conditions, etc.)
		/// </summary>
		public static event EventHandler<ContextCollectionChangedEventArgs<TContext>> ContextChanged;

		/// <summary>
		/// This method locates a context object using the context ID.
		/// Proper security checks are performed.
		/// </summary>
		/// <param name="contextId">ContextID</param>
		/// <returns>TContext object</returns>
		public static TContext LookupContext(string contextId)
		{
			return LookupContext(contextId, false, false);
		}

		/// <summary>
		/// This method locates a context object using the context ID and marks it as closed.
		/// Proper security checks are performed.
		/// </summary>
		/// <param name="contextId">ContextID</param>
		/// <returns>TContext object</returns>
		public static TContext CloseContext(string contextId)
		{
			return LookupContext(contextId, false, true);
		}

		/// <summary>
		/// This method locates a context object using the context ID.
		/// It allows security checks to be disabled
		/// </summary>
		/// <param name="contextId">Context ID</param>
		/// <param name="validate">Whether to validate the context credentials</param>
		/// <returns>TContext object</returns>
		public static TContext LookupContext(string contextId, bool validate)
		{
			return LookupContext(contextId, validate, false);
		}

		/// <summary>
		/// This method locates a context object using the context ID.
		/// It allows the context to be marked as closed.
		/// It allows security checks to be disabled
		/// </summary>
		/// <param name="contextId">Context ID</param>
		/// <param name="validate">Whether to validate the context credentials</param>
		/// <param name="conclude">Indicates that the context is being closed</param>
		/// <returns>TContext object</returns>
		public static TContext LookupContext(string contextId, bool validate, bool concluding)
		{
			if (IsServerShutdown)
			{
				throw FaultHelpers.Create(XiFaultCodes.E_SERVER_SHUTDOWN, ShutdownReason);
			}
			TContext context = null;
            lock (_activeContexts)
            {
				_activeContexts.TryGetValue(contextId, out context);
            }
            if (context is not null)
            {
                if ((context.Concluded) || (context.Concluding))
                    throw FaultHelpers.Create("Context is closed");
                else
                {
                    if (concluding)
                        context.Concluding = concluding;
                    context.LastAccess = DateTime.UtcNow;
                }
            }
            return context;
		}

		/// <summary>
		/// This method adds a new context to the manager's collection.  The assigned
		/// Context.LocalId is used to store the context object.
		/// </summary>
		/// <param name="context">Context to add</param>
		internal static void AddContext(TContext context)
		{
			lock (_activeContexts)
			{
				_activeContexts.Add(context.Id, context);
			}
			RaiseContextChanged(new ContextCollectionChangedEventArgs<TContext>(context, null));
		}

		/// <summary>
		/// This returns the current (active) list of contexts.
		/// </summary>
		public static List<TContext> Contexts
		{
			get
			{
				lock (_activeContexts)
				{
					return _activeContexts.Values.ToList();
				}
			}
		}

		private static void RaiseContextChanged(ContextCollectionChangedEventArgs<TContext> e)
		{
			EventHandler<ContextCollectionChangedEventArgs<TContext>> changed = ContextChanged;
			if (changed != null)
				changed(null, e);
		}
	}

	/// <summary>
	/// This event is raised when the context collection is changed.
	/// The removal of a context may be a due to closing the context or the context timed out.
	/// </summary>
	/// <typeparam name="TContext"></typeparam>
	public class ContextCollectionChangedEventArgs<TContext> : EventArgs
	{
		/// <summary>
		/// The Xi Context that was added or null.
		/// </summary>
		public TContext AddedContext { get; private set; }

		/// <summary>
		/// The Xi Context that was removed or null
		/// </summary>
		public TContext RemovedContext { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="added"></param>
		/// <param name="removed"></param>
		internal ContextCollectionChangedEventArgs(TContext added, TContext removed)
		{
			AddedContext = added;
			RemovedContext = removed;
		}
	}
}
