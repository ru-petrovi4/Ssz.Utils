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
using Ssz.Utils.Net4;

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

		/// <summary>
		/// This method is called by the server when it is being shutdown.
		/// </summary>
		/// <param name="reason">The reason for shutting down.</param>
		public static void OnShutdown(ServerStatus serverStatus, string reason)
		{
			_pauseMonitor = true;
			IsServerShutdown = true;
			ShutdownReason = reason;
			bool waitForContextsToClose = _activeContexts.Count > 0;
			lock (_activeContexts)
			{
				foreach (var ctx in _activeContexts)
				{
					if (ctx.Value.CallbackEndpointOpen)
					{
						ctx.Value.OnAbort(serverStatus, reason);
					}
					if (ctx.Value.Concluded == false)
					{
						ctx.Value.OnConclude();
					}
				}
			}
			if (waitForContextsToClose)
				Thread.Sleep(30000); //wait for the contexts to close
			_pauseMonitor = false;
		}

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
			OperationContext ctx = OperationContext.Current;
			if (null != ctx)
			{
				if (!(validate && (ctx.ServiceSecurityContext == null
								   || ctx.ServiceSecurityContext.PrimaryIdentity == null)
					 )
				   )
				{
					lock (_activeContexts)
					{
						if (_activeContexts.TryGetValue(contextId, out context))
						{
							if (validate == false || context.ValidateSecurity(ctx))
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
						}
					}
				}
				if (null == context)
				{
					ctx.Channel.Close();
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

		private static volatile bool _stopMonitor;
		private static volatile bool _pauseMonitor;
		private static Thread _contextMonitor;

		/// <summary>
		/// 
		/// </summary>
		public static void StartContextMonitor()
		{
			if (_contextMonitor == null)
			{
				_stopMonitor = false;
				_pauseMonitor = false;
				_contextMonitor = new Thread(CheckContextTimeout)
				{
					Name = "Context Timeout Thread",
					IsBackground = true,
					Priority = ThreadPriority.BelowNormal
				};
				_contextMonitor.Start();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public static void StopContextMonitor()
		{
			if (_contextMonitor != null)
			{
				_stopMonitor = true;
				// The join time needs to be longer than the thread sleep time.
				if (!_contextMonitor.Join(3000))
					_contextMonitor.Interrupt();
				_contextMonitor = null;
			}
		}

		private static void CheckContextTimeout()
		{
			DateTime previousTimeoutCheckTime = DateTime.UtcNow;
			DateTime currentTimeoutCheckTime = DateTime.UtcNow;
			while (!_stopMonitor)
			{
				try
				{
					Thread.Sleep(2500); // set to 2500 so that the minimum callback rate of 5000 is divisible by this sleep time
					if (!_pauseMonitor)
					{
						currentTimeoutCheckTime = DateTime.UtcNow;

						// This is here to support debugging.  
						// Don’t check for timeouts if this thread has been suspended while stepping through code during debugging.
						if (5000 > (currentTimeoutCheckTime - previousTimeoutCheckTime).TotalMilliseconds)
						{
							// contexts that have been closed by the client
							List<TContext> closedContexts = null;

							// Contexts that have timed-out
							List<TContext> timedOutContexts = null;

							// Contexts that need to have a keep-alive callback sent
							IEnumerable<TContext> callbackContexts = null;

							if (_activeContexts.Count > 0)
							{
								lock (_activeContexts)
								{
									IEnumerable<TContext> enumClosedContexts = _activeContexts.Values.Where(
											ctx => (ctx.Concluded == true));
									if (enumClosedContexts != null)
										closedContexts = enumClosedContexts.ToList();

									if ((closedContexts != null) && (closedContexts.Count > 0))
										closedContexts.ForEach(sess => _activeContexts.Remove(sess.Id));

									IEnumerable<TContext> enumTimedOutContexts = _activeContexts.Values.Where(
											ctx => (ctx.CheckTimeout(currentTimeoutCheckTime)));
									if (enumTimedOutContexts != null)
										timedOutContexts = enumTimedOutContexts.ToList();

									if ((timedOutContexts != null) && (timedOutContexts.Count > 0))
										timedOutContexts.ForEach(sess => _activeContexts.Remove(sess.Id));

									callbackContexts = _activeContexts.Values.Where
										(
											ctx => (   (ctx.CallbackEndpointOpen)
													&& (ctx.CallbackRate < (currentTimeoutCheckTime - ctx.LastCallbackTime))
												   )
										);
								}
							}

							if ((closedContexts != null) && (closedContexts.Count > 0))
							{
								foreach (TContext context in closedContexts)
								{
									RaiseContextChanged(new ContextCollectionChangedEventArgs<TContext>(null, context));
									if (context.HasBeenDisposed == false)
										context.Dispose();
								}
							}

							if ((timedOutContexts != null) && (timedOutContexts.Count > 0))
							{
								timedOutContexts.ForEach(ctx =>
                                                     Logger.Info("Timeout out Context {0}", ctx.Id));
								foreach (TContext context in timedOutContexts)
								{
									RaiseContextChanged(new ContextCollectionChangedEventArgs<TContext>(null, context));
									if (context.HasBeenDisposed == false)
										context.Dispose();
								}
							}

							if ((callbackContexts != null) && (callbackContexts.Count<TContext>() > 0))
							{
								foreach (TContext context in callbackContexts)
								{
									context.OnInformationReport(0, null); // the keep-alive callback
								}
							}
						}
						previousTimeoutCheckTime = currentTimeoutCheckTime;
					}
				}
				catch (Exception ex)
				{
					string msg = "Context Manager CheckContextTimeout loop, StackTrace=" + ex.StackTrace + ", Exception=" + ex.Message;
					if (ex.InnerException != null)
						msg += ", InnerException=" + ex.InnerException.Message;
					Logger.Info(msg);
				}
			}
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
