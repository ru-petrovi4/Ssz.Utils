﻿//
// ImapFolder.cs
//
// Author: Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2013-2023 .NET Foundation and Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

using MimeKit;

using MailKit.Search;

#if NET5_0_OR_GREATER
using IReadOnlySetOfStrings = System.Collections.Generic.IReadOnlySet<string>;
#else
using IReadOnlySetOfStrings = System.Collections.Generic.ISet<string>;
#endif

namespace MailKit.Net.Imap {
	/// <summary>
	/// An IMAP folder.
	/// </summary>
	/// <remarks>
	/// An IMAP folder.
	/// </remarks>
	/// <example>
	/// <code language="c#" source="Examples\ImapExamples.cs" region="DownloadMessages"/>
	/// </example>
	/// <example>
	/// <code language="c#" source="Examples\ImapExamples.cs" region="DownloadBodyParts"/>
	/// </example>
	public partial class ImapFolder : MailFolder, IImapFolder
	{
		bool supportsModSeq;
		bool countChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Net.Imap.ImapFolder"/> class.
		/// </summary>
		/// <remarks>
		/// <para>Creates a new <see cref="ImapFolder"/>.</para>
		/// <para>If you subclass <see cref="ImapFolder"/>, you will also need to subclass
		/// <see cref="ImapClient"/> and override the
		/// <see cref="ImapClient.CreateImapFolder(ImapFolderConstructorArgs)"/>
		/// method in order to return a new instance of your ImapFolder subclass.</para>
		/// </remarks>
		/// <param name="args">The constructor arguments.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="args"/> is <c>null</c>.
		/// </exception>
		public ImapFolder (ImapFolderConstructorArgs args)
		{
			if (args == null)
				throw new ArgumentNullException (nameof (args));

			PermanentKeywords = new HashSet<string> (StringComparer.Ordinal);
			AcceptedKeywords = new HashSet<string> (StringComparer.Ordinal);

			InitializeProperties (args);
		}

		void InitializeProperties (ImapFolderConstructorArgs args)
		{
			DirectorySeparator = args.DirectorySeparator;
			EncodedName = args.EncodedName;
			Attributes = args.Attributes;
			FullName = args.FullName;
			Engine = args.Engine;
			Name = args.Name;
		}

		/// <summary>
		/// Get the IMAP command engine.
		/// </summary>
		/// <remarks>
		/// Gets the IMAP command engine.
		/// </remarks>
		/// <value>The engine.</value>
		internal ImapEngine Engine {
			get; private set;
		}

		/// <summary>
		/// Get the encoded name of the folder.
		/// </summary>
		/// <remarks>
		/// Gets the encoded name of the folder.
		/// </remarks>
		/// <value>The encoded name.</value>
		internal string EncodedName {
			get; set;
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to the IMAP server.
		/// </summary>
		/// <remarks>
		/// <para>Gets an object that can be used to synchronize access to the IMAP server.</para>
		/// <para>When using the non-Async methods from multiple threads, it is important to lock the
		/// <see cref="SyncRoot"/> object for thread safety when using the synchronous methods.</para>
		/// </remarks>
		/// <value>The lock object.</value>
		public override object SyncRoot {
			get { return Engine; }
		}

		/// <summary>
		/// Get the threading algorithms supported by the folder.
		/// </summary>
		/// <remarks>
		/// Get the threading algorithms supported by the folder.
		/// </remarks>
		/// <value>The supported threading algorithms.</value>
		public override HashSet<ThreadingAlgorithm> ThreadingAlgorithms {
			get { return Engine.ThreadingAlgorithms; }
		}

		/// <summary>
		/// Determine whether or not an <see cref="ImapFolder"/> supports a feature.
		/// </summary>
		/// <remarks>
		/// Determines whether or not an <see cref="ImapFolder"/> supports a feature.
		/// </remarks>
		/// <param name="feature">The desired feature.</param>
		/// <returns><c>true</c> if the feature is supported; otherwise, <c>false</c>.</returns>
		public override bool Supports (FolderFeature feature)
		{
			switch (feature) {
			case FolderFeature.AccessRights: return (Engine.Capabilities & ImapCapabilities.Acl) != 0;
			case FolderFeature.Annotations: return AnnotationAccess != AnnotationAccess.None;
			case FolderFeature.Metadata: return (Engine.Capabilities & ImapCapabilities.Metadata) != 0;
			case FolderFeature.ModSequences: return supportsModSeq;
			case FolderFeature.QuickResync: return Engine.QResyncEnabled;
			case FolderFeature.Quotas: return (Engine.Capabilities & ImapCapabilities.Quota) != 0;
			case FolderFeature.Sorting: return (Engine.Capabilities & ImapCapabilities.Sort) != 0;
			case FolderFeature.Threading: return (Engine.Capabilities & ImapCapabilities.Thread) != 0;
			case FolderFeature.UTF8: return Engine.UTF8Enabled;
			default: return false;
			}
		}

		void CheckState (bool open, bool rw)
		{
			if (Engine.IsDisposed)
				throw new ObjectDisposedException (nameof (ImapClient));

			if (!Engine.IsConnected)
				throw new ServiceNotConnectedException ("The ImapClient is not connected.");

			if (Engine.State < ImapEngineState.Authenticated)
				throw new ServiceNotAuthenticatedException ("The ImapClient is not authenticated.");

			if (open) {
				var access = rw ? FolderAccess.ReadWrite : FolderAccess.ReadOnly;

				if (!IsOpen || Access < access)
					throw new FolderNotOpenException (FullName, access);
			}
		}

		void CheckAllowIndexes ()
		{
			// Indexes ("Message Sequence Numbers" or MSNs in the RFCs) and * are not stable while MessageNew/MessageExpunge is registered for SELECTED and therefore should not be used
			// https://tools.ietf.org/html/rfc5465#section-5.2
			if (Engine.NotifySelectedNewExpunge)
				throw new InvalidOperationException ("Indexes and '*' cannot be used while MessageNew/MessageExpunge is registered with NOTIFY for SELECTED.");
		}

		internal void Reset ()
		{
			// basic state
			((HashSet<string>) PermanentKeywords).Clear ();
			((HashSet<string>) AcceptedKeywords).Clear ();
			PermanentFlags = MessageFlags.None;
			AcceptedFlags = MessageFlags.None;
			Access = FolderAccess.None;

			// annotate state
			AnnotationAccess = AnnotationAccess.None;
			AnnotationScopes = AnnotationScope.None;
			MaxAnnotationSize = 0;

			// condstore state
			supportsModSeq = false;
			HighestModSeq = 0;
		}

		/// <summary>
		/// Notifies the folder that a parent folder has been renamed.
		/// </summary>
		/// <remarks>
		/// Updates the <see cref="MailFolder.FullName"/> property.
		/// </remarks>
		protected override void OnParentFolderRenamed ()
		{
			var oldEncodedName = EncodedName;

			FullName = ParentFolder.FullName + DirectorySeparator + Name;
			EncodedName = Engine.EncodeMailboxName (FullName);
			Engine.FolderCache.Remove (oldEncodedName);
			Engine.FolderCache[EncodedName] = this;
			Reset ();

			if (Engine.Selected == this) {
				Engine.State = ImapEngineState.Authenticated;
				Engine.Selected = null;
				OnClosed ();
			}
		}

		void ProcessResponseCodes (ImapCommand ic, IMailFolder folder, bool throwNotFound = true)
		{
			bool tryCreate = false;

			foreach (var code in ic.RespCodes) {
				switch (code.Type) {
				case ImapResponseCodeType.PermanentFlags:
					var permanent = (PermanentFlagsResponseCode) code;
					PermanentKeywords = permanent.Keywords;
					PermanentFlags = permanent.Flags;
					break;
				case ImapResponseCodeType.ReadOnly:
					if (code.IsTagged)
						Access = FolderAccess.ReadOnly;
					break;
				case ImapResponseCodeType.ReadWrite:
					if (code.IsTagged)
						Access = FolderAccess.ReadWrite;
					break;
				case ImapResponseCodeType.TryCreate:
					tryCreate = true;
					break;
				case ImapResponseCodeType.UidNext:
					UidNext = ((UidNextResponseCode) code).Uid;
					break;
				case ImapResponseCodeType.UidValidity:
					var uidValidity = ((UidValidityResponseCode) code).UidValidity;
					if (IsOpen)
						UpdateUidValidity (uidValidity);
					else
						UidValidity = uidValidity;
					break;
				case ImapResponseCodeType.Unseen:
					FirstUnread = ((UnseenResponseCode) code).Index;
					break;
				case ImapResponseCodeType.HighestModSeq:
					var highestModSeq = ((HighestModSeqResponseCode) code).HighestModSeq;
					supportsModSeq = true;
					if (IsOpen)
						UpdateHighestModSeq (highestModSeq);
					else
						HighestModSeq = highestModSeq;
					break;
				case ImapResponseCodeType.NoModSeq:
					supportsModSeq = false;
					HighestModSeq = 0;
					break;
				case ImapResponseCodeType.MailboxId:
					// Note: an untagged MAILBOX resp-code is returned on SELECT/EXAMINE while
					// a *tagged* MAILBOXID resp-code is returned on CREATE.
					if (!code.IsTagged)
						Id = ((MailboxIdResponseCode) code).MailboxId;
					break;
				case ImapResponseCodeType.Annotations:
					var annotations = (AnnotationsResponseCode) code;
					AnnotationAccess = annotations.Access;
					AnnotationScopes = annotations.Scopes;
					MaxAnnotationSize = annotations.MaxSize;
					break;
				}
			}

			if (tryCreate && throwNotFound && folder != null)
				throw new FolderNotFoundException (folder.FullName);
		}

		#region IMailFolder implementation

		/// <summary>
		/// Gets a value indicating whether the folder is currently open.
		/// </summary>
		/// <remarks>
		/// Gets a value indicating whether the folder is currently open.
		/// </remarks>
		/// <value><c>true</c> if the folder is currently open; otherwise, <c>false</c>.</value>
		public override bool IsOpen {
			get { return Engine.Selected == this; }
		}

		static string SelectOrExamine (FolderAccess access)
		{
			return access == FolderAccess.ReadOnly ? "EXAMINE" : "SELECT";
		}

		static Task QResyncFetchAsync (ImapEngine engine, ImapCommand ic, int index, bool doAsync)
		{
			return ic.Folder.OnFetchAsync (engine, index, doAsync, ic.CancellationToken);
		}

		async Task<FolderAccess> OpenAsync (ImapCommand ic, FolderAccess access, bool doAsync, CancellationToken cancellationToken)
		{
			Reset ();

			if (access == FolderAccess.ReadWrite) {
				// Note: if the server does not respond with a PERMANENTFLAGS response,
				// then we need to assume all flags are permanent.
				PermanentFlags = SettableFlags | MessageFlags.UserDefined;
			} else {
				PermanentFlags = MessageFlags.None;
			}

			try {
				Engine.QueueCommand (ic);

				await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

				ProcessResponseCodes (ic, this);

				if (ic.Response != ImapCommandResponse.Ok)
					throw ImapCommandException.Create (access == FolderAccess.ReadOnly ? "EXAMINE" : "SELECT", ic);
			} catch {
				PermanentFlags = MessageFlags.None;
				throw;
			}

			if (Engine.Selected != null && Engine.Selected != this) {
				var folder = Engine.Selected;

				folder.Reset ();

				folder.OnClosed ();
			}

			Engine.State = ImapEngineState.Selected;
			Engine.Selected = this;

			OnOpened ();

			return Access;
		}

		Task<FolderAccess> OpenAsync (FolderAccess access, uint uidValidity, ulong highestModSeq, IList<UniqueId> uids, bool doAsync, CancellationToken cancellationToken)
		{
			if (access != FolderAccess.ReadOnly && access != FolderAccess.ReadWrite)
				throw new ArgumentOutOfRangeException (nameof (access));

			if (uids == null)
				throw new ArgumentNullException (nameof (uids));

			CheckState (false, false);

			if ((Engine.Capabilities & ImapCapabilities.QuickResync) == 0)
				throw new NotSupportedException ("The IMAP server does not support the QRESYNC extension.");

			if (!Supports (FolderFeature.QuickResync))
				throw new InvalidOperationException ("The QRESYNC extension has not been enabled.");

			string qresync;

			if ((Engine.Capabilities & ImapCapabilities.Annotate) != 0 && Engine.QuirksMode != ImapQuirksMode.SunMicrosystems)
				qresync = string.Format (CultureInfo.InvariantCulture, "(ANNOTATE QRESYNC ({0} {1}", uidValidity, highestModSeq);
			else
				qresync = string.Format (CultureInfo.InvariantCulture, "(QRESYNC ({0} {1}", uidValidity, highestModSeq);

			if (uids.Count > 0) {
				var set = UniqueIdSet.ToString (uids);
				qresync += " " + set;
			}

			qresync += "))";

			var command = string.Format ("{0} %F {1}\r\n", SelectOrExamine (access), qresync);
			var ic = new ImapCommand (Engine, cancellationToken, this, command, this);
			ic.RegisterUntaggedHandler ("FETCH", QResyncFetchAsync);

			return OpenAsync (ic, access, doAsync, cancellationToken);
		}

		/// <summary>
		/// Open the folder using the requested folder access.
		/// </summary>
		/// <remarks>
		/// <para>This variant of the <see cref="Open(FolderAccess,System.Threading.CancellationToken)"/>
		/// method is meant for quick resynchronization of the folder. Before calling this method,
		/// the <see cref="ImapClient.EnableQuickResync(CancellationToken)"/> method MUST be called.</para>
		/// <para>You should also make sure to add listeners to the <see cref="MailFolder.MessagesVanished"/> and
		/// <see cref="MailFolder.MessageFlagsChanged"/> events to get notifications of changes since
		/// the last time the folder was opened.</para>
		/// </remarks>
		/// <returns>The <see cref="FolderAccess"/> state of the folder.</returns>
		/// <param name="access">The requested folder access.</param>
		/// <param name="uidValidity">The last known <see cref="MailFolder.UidValidity"/> value.</param>
		/// <param name="highestModSeq">The last known <see cref="MailFolder.HighestModSeq"/> value.</param>
		/// <param name="uids">The last known list of unique message identifiers.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="access"/> is not a valid value.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The QRESYNC feature has not been enabled.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the QRESYNC extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override FolderAccess Open (FolderAccess access, uint uidValidity, ulong highestModSeq, IList<UniqueId> uids, CancellationToken cancellationToken = default)
		{
			return OpenAsync (access, uidValidity, highestModSeq, uids, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously open the folder using the requested folder access.
		/// </summary>
		/// <remarks>
		/// <para>This variant of the <see cref="Open(FolderAccess,System.Threading.CancellationToken)"/>
		/// method is meant for quick resynchronization of the folder. Before calling this method,
		/// the <see cref="ImapClient.EnableQuickResync(CancellationToken)"/> method MUST be called.</para>
		/// <para>You should also make sure to add listeners to the <see cref="MailFolder.MessagesVanished"/> and
		/// <see cref="MailFolder.MessageFlagsChanged"/> events to get notifications of changes since
		/// the last time the folder was opened.</para>
		/// </remarks>
		/// <returns>The <see cref="FolderAccess"/> state of the folder.</returns>
		/// <param name="access">The requested folder access.</param>
		/// <param name="uidValidity">The last known <see cref="MailFolder.UidValidity"/> value.</param>
		/// <param name="highestModSeq">The last known <see cref="MailFolder.HighestModSeq"/> value.</param>
		/// <param name="uids">The last known list of unique message identifiers.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="access"/> is not a valid value.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The QRESYNC feature has not been enabled.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the QRESYNC extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<FolderAccess> OpenAsync (FolderAccess access, uint uidValidity, ulong highestModSeq, IList<UniqueId> uids, CancellationToken cancellationToken = default)
		{
			return OpenAsync (access, uidValidity, highestModSeq, uids, true, cancellationToken);
		}

		Task<FolderAccess> OpenAsync (FolderAccess access, bool doAsync, CancellationToken cancellationToken)
		{
			if (access != FolderAccess.ReadOnly && access != FolderAccess.ReadWrite)
				throw new ArgumentOutOfRangeException (nameof (access));

			CheckState (false, false);

			var @params = string.Empty;

			if ((Engine.Capabilities & ImapCapabilities.CondStore) != 0)
				@params += "CONDSTORE";
			if ((Engine.Capabilities & ImapCapabilities.Annotate) != 0 && Engine.QuirksMode != ImapQuirksMode.SunMicrosystems)
				@params += " ANNOTATE";

			if (@params.Length > 0)
				@params = " (" + @params.TrimStart () + ")";

			var command = string.Format ("{0} %F{1}\r\n", SelectOrExamine (access), @params);
			var ic = new ImapCommand (Engine, cancellationToken, this, command, this);

			return OpenAsync (ic, access, doAsync, cancellationToken);
		}

		/// <summary>
		/// Open the folder using the requested folder access.
		/// </summary>
		/// <remarks>
		/// Opens the folder using the requested folder access.
		/// </remarks>
		/// <returns>The <see cref="FolderAccess"/> state of the folder.</returns>
		/// <param name="access">The requested folder access.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="access"/> is not a valid value.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override FolderAccess Open (FolderAccess access, CancellationToken cancellationToken = default)
		{
			return OpenAsync (access, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously open the folder using the requested folder access.
		/// </summary>
		/// <remarks>
		/// Opens the folder using the requested folder access.
		/// </remarks>
		/// <returns>The <see cref="FolderAccess"/> state of the folder.</returns>
		/// <param name="access">The requested folder access.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="access"/> is not a valid value.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<FolderAccess> OpenAsync (FolderAccess access, CancellationToken cancellationToken = default)
		{
			return OpenAsync (access, true, cancellationToken);
		}

		async Task CloseAsync (bool expunge, bool doAsync, CancellationToken cancellationToken)
		{
			CheckState (true, expunge);

			ImapCommand ic;

			if (expunge) {
				ic = Engine.QueueCommand (cancellationToken, this, "CLOSE\r\n");
			} else if ((Engine.Capabilities & ImapCapabilities.Unselect) != 0) {
				ic = Engine.QueueCommand (cancellationToken, this, "UNSELECT\r\n");
			} else {
				ic = null;
			}

			if (ic != null) {
				await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

				ProcessResponseCodes (ic, null);

				if (ic.Response != ImapCommandResponse.Ok)
					throw ImapCommandException.Create (expunge ? "CLOSE" : "UNSELECT", ic);
			}

			Reset ();

			if (Engine.Selected == this) {
				Engine.State = ImapEngineState.Authenticated;
				Engine.Selected = null;
				OnClosed ();
			}
		}

		/// <summary>
		/// Close the folder, optionally expunging the messages marked for deletion.
		/// </summary>
		/// <remarks>
		/// Closes the folder, optionally expunging the messages marked for deletion.
		/// </remarks>
		/// <param name="expunge">If set to <c>true</c>, expunge.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void Close (bool expunge = false, CancellationToken cancellationToken = default)
		{
			CloseAsync (expunge, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously close the folder, optionally expunging the messages marked for deletion.
		/// </summary>
		/// <remarks>
		/// Closes the folder, optionally expunging the messages marked for deletion.
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="expunge">If set to <c>true</c>, expunge.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task CloseAsync (bool expunge = false, CancellationToken cancellationToken = default)
		{
			return CloseAsync (expunge, true, cancellationToken);
		}

		async Task<IMailFolder> GetCreatedFolderAsync (string encodedName, string id, bool specialUse, bool doAsync, CancellationToken cancellationToken)
		{
			var ic = new ImapCommand (Engine, cancellationToken, null, "LIST \"\" %S\r\n", encodedName);
			var list = new List<ImapFolder> ();
			ImapFolder folder;

			ic.RegisterUntaggedHandler ("LIST", ImapUtils.ParseFolderListAsync);
			ic.UserData = list;

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("LIST", ic);

			if ((folder = ImapEngine.GetFolder (list, encodedName)) != null) {
				folder.ParentFolder = this;
				folder.Id = id;

				if (specialUse)
					Engine.AssignSpecialFolder (folder);
			}

			return folder;
		}

		async Task<IMailFolder> CreateAsync (string name, bool isMessageFolder, bool doAsync, CancellationToken cancellationToken)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (!ImapEngine.IsValidMailboxName (name, DirectorySeparator))
				throw new ArgumentException ("The name is not a legal folder name.", nameof (name));

			CheckState (false, false);

			if (!string.IsNullOrEmpty (FullName) && DirectorySeparator == '\0')
				throw new InvalidOperationException ("Cannot create child folders.");

			var fullName = !string.IsNullOrEmpty (FullName) ? FullName + DirectorySeparator + name : name;
			var encodedName = Engine.EncodeMailboxName (fullName);
			var createName = encodedName;

			if (!isMessageFolder && Engine.QuirksMode != ImapQuirksMode.GMail)
				createName += DirectorySeparator;

			var ic = Engine.QueueCommand (cancellationToken, null, "CREATE %S\r\n", createName);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok && ic.GetResponseCode (ImapResponseCodeType.AlreadyExists) == null)
				throw ImapCommandException.Create ("CREATE", ic);

			var code = (MailboxIdResponseCode) ic.GetResponseCode (ImapResponseCodeType.MailboxId);
			var id = code?.MailboxId;

			var created = await GetCreatedFolderAsync (encodedName, id, false, doAsync, cancellationToken).ConfigureAwait (false);

			Engine.OnFolderCreated (created);

			return created;
		}

		/// <summary>
		/// Create a new subfolder with the given name.
		/// </summary>
		/// <remarks>
		/// Creates a new subfolder with the given name.
		/// </remarks>
		/// <returns>The created folder.</returns>
		/// <param name="name">The name of the folder to create.</param>
		/// <param name="isMessageFolder"><c>true</c> if the folder will be used to contain messages; otherwise, <c>false</c>.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <paramref name="name"/> is empty.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailFolder.DirectorySeparator"/> is nil, and thus child folders cannot be created.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override IMailFolder Create (string name, bool isMessageFolder, CancellationToken cancellationToken = default)
		{
			return CreateAsync (name, isMessageFolder, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously create a new subfolder with the given name.
		/// </summary>
		/// <remarks>
		/// Creates a new subfolder with the given name.
		/// </remarks>
		/// <returns>The created folder.</returns>
		/// <param name="name">The name of the folder to create.</param>
		/// <param name="isMessageFolder"><c>true</c> if the folder will be used to contain messages; otherwise, <c>false</c>.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <paramref name="name"/> is empty.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailFolder.DirectorySeparator"/> is nil, and thus child folders cannot be created.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<IMailFolder> CreateAsync (string name, bool isMessageFolder, CancellationToken cancellationToken = default)
		{
			return CreateAsync (name, isMessageFolder, true, cancellationToken);
		}

		async Task<IMailFolder> CreateAsync (string name, IEnumerable<SpecialFolder> specialUses, bool doAsync, CancellationToken cancellationToken)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (!ImapEngine.IsValidMailboxName (name, DirectorySeparator))
				throw new ArgumentException ("The name is not a legal folder name.", nameof (name));

			if (specialUses == null)
				throw new ArgumentNullException (nameof (specialUses));

			CheckState (false, false);

			if (!string.IsNullOrEmpty (FullName) && DirectorySeparator == '\0')
				throw new InvalidOperationException ("Cannot create child folders.");

			if ((Engine.Capabilities & ImapCapabilities.CreateSpecialUse) == 0)
				throw new NotSupportedException ("The IMAP server does not support the CREATE-SPECIAL-USE extension.");

			var uses = new StringBuilder ();
			uint used = 0;

			foreach (var use in specialUses) {
				var bit = (uint) (1 << ((int) use));

				if ((used & bit) != 0)
					continue;

				used |= bit;

				if (uses.Length > 0)
					uses.Append (' ');

				switch (use) {
				case SpecialFolder.All:       uses.Append ("\\All"); break;
				case SpecialFolder.Archive:   uses.Append ("\\Archive"); break;
				case SpecialFolder.Drafts:    uses.Append ("\\Drafts"); break;
				case SpecialFolder.Flagged:   uses.Append ("\\Flagged"); break;
				case SpecialFolder.Important: uses.Append ("\\Important"); break;
				case SpecialFolder.Junk:      uses.Append ("\\Junk"); break;
				case SpecialFolder.Sent:      uses.Append ("\\Sent"); break;
				case SpecialFolder.Trash:     uses.Append ("\\Trash"); break;
				default: if (uses.Length > 0) uses.Length--; break;
				}
			}

			var fullName = !string.IsNullOrEmpty (FullName) ? FullName + DirectorySeparator + name : name;
			var encodedName = Engine.EncodeMailboxName (fullName);
			string command;

			if (uses.Length > 0)
				command = string.Format ("CREATE %S (USE ({0}))\r\n", uses);
			else
				command = "CREATE %S\r\n";

			var ic = Engine.QueueCommand (cancellationToken, null, command, encodedName);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("CREATE", ic);

			var code = (MailboxIdResponseCode) ic.GetResponseCode (ImapResponseCodeType.MailboxId);
			var id = code?.MailboxId;

			var created = await GetCreatedFolderAsync (encodedName, id, true, doAsync, cancellationToken).ConfigureAwait (false);

			Engine.OnFolderCreated (created);

			return created;
		}

		/// <summary>
		/// Create a new subfolder with the given name.
		/// </summary>
		/// <remarks>
		/// Creates a new subfolder with the given name.
		/// </remarks>
		/// <returns>The created folder.</returns>
		/// <param name="name">The name of the folder to create.</param>
		/// <param name="specialUses">A list of special uses for the folder being created.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="specialUses"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <paramref name="name"/> is empty.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailFolder.DirectorySeparator"/> is nil, and thus child folders cannot be created.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the CREATE-SPECIAL-USE extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override IMailFolder Create (string name, IEnumerable<SpecialFolder> specialUses, CancellationToken cancellationToken = default)
		{
			return CreateAsync (name, specialUses, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously create a new subfolder with the given name.
		/// </summary>
		/// <remarks>
		/// Creates a new subfolder with the given name.
		/// </remarks>
		/// <returns>The created folder.</returns>
		/// <param name="name">The name of the folder to create.</param>
		/// <param name="specialUses">A list of special uses for the folder being created.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="specialUses"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <paramref name="name"/> is empty.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailFolder.DirectorySeparator"/> is nil, and thus child folders cannot be created.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the CREATE-SPECIAL-USE extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<IMailFolder> CreateAsync (string name, IEnumerable<SpecialFolder> specialUses, CancellationToken cancellationToken = default)
		{
			return CreateAsync (name, specialUses, true, cancellationToken);
		}

		async Task RenameAsync (IMailFolder parent, string name, bool doAsync, CancellationToken cancellationToken)
		{
			if (parent == null)
				throw new ArgumentNullException (nameof (parent));

			if (parent == this)
				throw new ArgumentException ("Cannot rename a folder using itself as the new parent folder.", nameof (parent));

			if (parent is not ImapFolder || ((ImapFolder) parent).Engine != Engine)
				throw new ArgumentException ("The parent folder does not belong to this ImapClient.", nameof (parent));

			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (!ImapEngine.IsValidMailboxName (name, DirectorySeparator))
				throw new ArgumentException ("The name is not a legal folder name.", nameof (name));

			if (IsNamespace || (Attributes & FolderAttributes.Inbox) != 0)
				throw new InvalidOperationException ("Cannot rename this folder.");

			CheckState (false, false);

			string newFullName;

			if (!string.IsNullOrEmpty (parent.FullName))
				newFullName = parent.FullName + parent.DirectorySeparator + name;
			else
				newFullName = name;

			var encodedName = Engine.EncodeMailboxName (newFullName);
			var ic = Engine.QueueCommand (cancellationToken, null, "RENAME %F %S\r\n", this, encodedName);
			var oldFullName = FullName;

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, this);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("RENAME", ic);

			Engine.FolderCache.Remove (EncodedName);
			Engine.FolderCache[encodedName] = this;

			ParentFolder = parent;

			FullName = Engine.DecodeMailboxName (encodedName);
			EncodedName = encodedName;
			Name = name;

			Reset ();

			if (Engine.Selected == this) {
				Engine.State = ImapEngineState.Authenticated;
				Engine.Selected = null;
				OnClosed ();
			}

			OnRenamed (oldFullName, FullName);
		}

		/// <summary>
		/// Rename the folder to exist with a new name under a new parent folder.
		/// </summary>
		/// <remarks>
		/// Renames the folder to exist with a new name under a new parent folder.
		/// </remarks>
		/// <param name="parent">The new parent folder.</param>
		/// <param name="name">The new name of the folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="parent"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para><paramref name="parent"/> does not belong to the <see cref="ImapClient"/>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="name"/> is not a legal folder name.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The folder cannot be renamed (it is either a namespace or the Inbox).
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void Rename (IMailFolder parent, string name, CancellationToken cancellationToken = default)
		{
			RenameAsync (parent, name, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously rename the folder to exist with a new name under a new parent folder.
		/// </summary>
		/// <remarks>
		/// Renames the folder to exist with a new name under a new parent folder.
		/// </remarks>
		/// <returns>An awaitable task.</returns>
		/// <param name="parent">The new parent folder.</param>
		/// <param name="name">The new name of the folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="parent"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para><paramref name="parent"/> does not belong to the <see cref="ImapClient"/>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="name"/> is not a legal folder name.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The folder cannot be renamed (it is either a namespace or the Inbox).
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task RenameAsync (IMailFolder parent, string name, CancellationToken cancellationToken = default)
		{
			return RenameAsync (parent, name, true, cancellationToken);
		}

		async Task DeleteAsync (bool doAsync, CancellationToken cancellationToken)
		{
			if (IsNamespace || (Attributes & FolderAttributes.Inbox) != 0)
				throw new InvalidOperationException ("Cannot delete this folder.");

			CheckState (false, false);

			var ic = Engine.QueueCommand (cancellationToken, null, "DELETE %F\r\n", this);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, this);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("DELETE", ic);

			Reset ();

			if (Engine.Selected == this) {
				Engine.State = ImapEngineState.Authenticated;
				Engine.Selected = null;
				OnClosed ();
			}

			Attributes |= FolderAttributes.NonExistent;
			OnDeleted ();
		}

		/// <summary>
		/// Delete the folder on the IMAP server.
		/// </summary>
		/// <remarks>
		/// <para>Deletes the folder on the IMAP server.</para>
		/// <note type="note">This method will not delete any child folders.</note>
		/// </remarks>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The folder cannot be deleted (it is either a namespace or the Inbox).
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void Delete (CancellationToken cancellationToken = default)
		{
			DeleteAsync (false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously delete the folder on the IMAP server.
		/// </summary>
		/// <remarks>
		/// <para>Deletes the folder on the IMAP server.</para>
		/// <note type="note">This method will not delete any child folders.</note>
		/// </remarks>
		/// <returns>An awaitable task.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The folder cannot be deleted (it is either a namespace or the Inbox).
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task DeleteAsync (CancellationToken cancellationToken = default)
		{
			return DeleteAsync (true, cancellationToken);
		}

		async Task SubscribeAsync (bool doAsync, CancellationToken cancellationToken)
		{
			CheckState (false, false);

			var ic = Engine.QueueCommand (cancellationToken, null, "SUBSCRIBE %F\r\n", this);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("SUBSCRIBE", ic);

			if ((Attributes & FolderAttributes.Subscribed) == 0) {
				Attributes |= FolderAttributes.Subscribed;

				OnSubscribed ();
			}
		}

		/// <summary>
		/// Subscribe the folder.
		/// </summary>
		/// <remarks>
		/// Subscribes the folder.
		/// </remarks>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void Subscribe (CancellationToken cancellationToken = default)
		{
			SubscribeAsync (false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously subscribe the folder.
		/// </summary>
		/// <remarks>
		/// Subscribes the folder.
		/// </remarks>
		/// <returns>An awaitable task.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task SubscribeAsync (CancellationToken cancellationToken = default)
		{
			return SubscribeAsync (true, cancellationToken);
		}

		async Task UnsubscribeAsync (bool doAsync, CancellationToken cancellationToken)
		{
			CheckState (false, false);

			var ic = Engine.QueueCommand (cancellationToken, null, "UNSUBSCRIBE %F\r\n", this);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("UNSUBSCRIBE", ic);

			if ((Attributes & FolderAttributes.Subscribed) != 0) {
				Attributes &= ~FolderAttributes.Subscribed;

				OnUnsubscribed ();
			}
		}

		/// <summary>
		/// Unsubscribe the folder.
		/// </summary>
		/// <remarks>
		/// Unsubscribes the folder.
		/// </remarks>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void Unsubscribe (CancellationToken cancellationToken = default)
		{
			UnsubscribeAsync (false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously unsubscribe the folder.
		/// </summary>
		/// <remarks>
		/// Unsubscribes the folder.
		/// </remarks>
		/// <returns>An awaitable task.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task UnsubscribeAsync (CancellationToken cancellationToken = default)
		{
			return UnsubscribeAsync (true, cancellationToken);
		}

		async Task<IList<IMailFolder>> GetSubfoldersAsync (StatusItems items, bool subscribedOnly, bool doAsync, CancellationToken cancellationToken)
		{
			CheckState (false, false);

			// Note: folder names can contain wildcards (including '*' and '%'), so replace '*' with '%'
			// in order to reduce the list of folders returned by our LIST command.
			var pattern = new StringBuilder (EncodedName.Length + 2);
			pattern.Append (EncodedName);
			for (int i = 0; i < EncodedName.Length; i++) {
				if (pattern[i] == '*')
					pattern[i] = '%';
			}
			if (pattern.Length > 0)
				pattern.Append (DirectorySeparator);
			pattern.Append ('%');

			var children = new List<IMailFolder> ();
			var status = items != StatusItems.None;
			var list = new List<ImapFolder> ();
			var command = new StringBuilder ();
			var returnsSubscribed = false;
			var lsub = subscribedOnly;

			if (subscribedOnly) {
				if ((Engine.Capabilities & ImapCapabilities.ListExtended) != 0) {
					command.Append ("LIST (SUBSCRIBED)");
					returnsSubscribed = true;
					lsub = false;
				} else {
					command.Append ("LSUB");
				}
			} else {
				command.Append ("LIST");
			}

			command.Append (" \"\" %S");

			if (!lsub) {
				if (items != StatusItems.None && (Engine.Capabilities & ImapCapabilities.ListStatus) != 0) {
					command.Append (" RETURN (");

					if ((Engine.Capabilities & ImapCapabilities.ListExtended) != 0) {
						if (!subscribedOnly) {
							command.Append ("SUBSCRIBED ");
							returnsSubscribed = true;
						}
						command.Append ("CHILDREN ");
					}

					command.Append ("STATUS (");
					command.Append (Engine.GetStatusQuery (items));
					command.Append ("))");
					status = false;
				} else if ((Engine.Capabilities & ImapCapabilities.ListExtended) != 0) {
					command.Append (" RETURN (");
					if (!subscribedOnly) {
						command.Append ("SUBSCRIBED ");
						returnsSubscribed = true;
					}
					command.Append ("CHILDREN");
					command.Append (')');
				}
			}

			command.Append ("\r\n");

			var ic = new ImapCommand (Engine, cancellationToken, null, command.ToString (), pattern.ToString ());
			ic.RegisterUntaggedHandler (lsub ? "LSUB" : "LIST", ImapUtils.ParseFolderListAsync);
			ic.ListReturnsSubscribed = returnsSubscribed;
			ic.UserData = list;
			ic.Lsub = lsub;

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			// Note: Due to the fact that folders can contain wildcards in them, we'll need to
			// filter out any folders that are not children of this folder.
			var prefix = FullName.Length > 0 ? FullName + DirectorySeparator : string.Empty;
			prefix = ImapUtils.CanonicalizeMailboxName (prefix, DirectorySeparator);
			var unparented = false;

			foreach (var folder in list) {
				var canonicalFullName = ImapUtils.CanonicalizeMailboxName (folder.FullName, folder.DirectorySeparator);
				var canonicalName = ImapUtils.IsInbox (folder.FullName) ? "INBOX" : folder.Name;

				if (!canonicalFullName.StartsWith (prefix, StringComparison.Ordinal)) {
					unparented |= folder.ParentFolder == null;
					continue;
				}

				if (string.Compare (canonicalFullName, prefix.Length, canonicalName, 0, canonicalName.Length, StringComparison.Ordinal) != 0) {
					unparented |= folder.ParentFolder == null;
					continue;
				}

				folder.ParentFolder = this;
				children.Add (folder);
			}

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create (lsub ? "LSUB" : "LIST", ic);

			// Note: if any folders returned in the LIST command are unparented, have the ImapEngine look up their
			// parent folders now so that they are not left in an inconsistent state.
			if (unparented)
				await Engine.LookupParentFoldersAsync (list, doAsync, cancellationToken).ConfigureAwait (false);

			if (status) {
				for (int i = 0; i < children.Count; i++) {
					if (children[i].Exists)
						await ((ImapFolder) children[i]).StatusAsync (items, doAsync, false, cancellationToken).ConfigureAwait (false);
				}
			}

			return children;
		}

		/// <summary>
		/// Get the subfolders.
		/// </summary>
		/// <remarks>
		/// Gets the subfolders.
		/// </remarks>
		/// <returns>The subfolders.</returns>
		/// <param name="items">The status items to pre-populate.</param>
		/// <param name="subscribedOnly">If set to <c>true</c>, only subscribed folders will be listed.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override IList<IMailFolder> GetSubfolders (StatusItems items, bool subscribedOnly = false, CancellationToken cancellationToken = default)
		{
			return GetSubfoldersAsync (items, subscribedOnly, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously get the subfolders.
		/// </summary>
		/// <remarks>
		/// Gets the subfolders.
		/// </remarks>
		/// <returns>The subfolders.</returns>
		/// <param name="items">The status items to pre-populate.</param>
		/// <param name="subscribedOnly">If set to <c>true</c>, only subscribed folders will be listed.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<IList<IMailFolder>> GetSubfoldersAsync (StatusItems items, bool subscribedOnly = false, CancellationToken cancellationToken = default)
		{
			return GetSubfoldersAsync (items, subscribedOnly, true, cancellationToken);
		}

		async Task<IMailFolder> GetSubfolderAsync (string name, bool doAsync, CancellationToken cancellationToken)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (!ImapEngine.IsValidMailboxName (name, DirectorySeparator))
				throw new ArgumentException ("The name of the subfolder is invalid.", nameof (name));

			CheckState (false, false);

			var fullName = FullName.Length > 0 ? FullName + DirectorySeparator + name : name;
			var encodedName = Engine.EncodeMailboxName (fullName);
			List<ImapFolder> list;

			if (Engine.GetCachedFolder (encodedName, out var folder))
				return folder;

			// Note: folder names can contain wildcards (including '*' and '%'), so replace '*' with '%'
			// in order to reduce the list of folders returned by our LIST command.
			var pattern = encodedName.Replace ('*', '%');

			var ic = new ImapCommand (Engine, cancellationToken, null, "LIST \"\" %S\r\n", pattern);
			ic.RegisterUntaggedHandler ("LIST", ImapUtils.ParseFolderListAsync);
			ic.UserData = list = new List<ImapFolder> ();

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("LIST", ic);

			if ((folder = ImapEngine.GetFolder (list, encodedName)) != null)
				folder.ParentFolder = this;

			if (list.Count > 1 || folder == null) {
				// Note: if any folders returned in the LIST command are unparented, have the ImapEngine look up their
				// parent folders now so that they are not left in an inconsistent state.
				await Engine.LookupParentFoldersAsync (list, doAsync, cancellationToken).ConfigureAwait (false);
			}

			if (folder == null)
				throw new FolderNotFoundException (fullName);

			return folder;
		}

		/// <summary>
		/// Get the specified subfolder.
		/// </summary>
		/// <remarks>
		/// Gets the specified subfolder.
		/// </remarks>
		/// <returns>The subfolder.</returns>
		/// <param name="name">The name of the subfolder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <paramref name="name"/> is either an empty string or contains the <see cref="MailFolder.DirectorySeparator"/>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The requested folder could not be found.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override IMailFolder GetSubfolder (string name, CancellationToken cancellationToken = default)
		{
			return GetSubfolderAsync (name, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously get the specified subfolder.
		/// </summary>
		/// <remarks>
		/// Gets the specified subfolder.
		/// </remarks>
		/// <returns>The subfolder.</returns>
		/// <param name="name">The name of the subfolder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <paramref name="name"/> is either an empty string or contains the <see cref="MailFolder.DirectorySeparator"/>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The requested folder could not be found.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<IMailFolder> GetSubfolderAsync (string name, CancellationToken cancellationToken = default)
		{
			return GetSubfolderAsync (name, true, cancellationToken);
		}

		async Task CheckAsync (bool doAsync, CancellationToken cancellationToken)
		{
			CheckState (true, false);

			var ic = Engine.QueueCommand (cancellationToken, this, "CHECK\r\n");

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("CHECK", ic);
		}

		/// <summary>
		/// Force the server to sync its in-memory state with its disk state.
		/// </summary>
		/// <remarks>
		/// <para>The <c>CHECK</c> command forces the IMAP server to sync its
		/// in-memory state with its disk state.</para>
		/// <para>For more information about the <c>CHECK</c> command, see
		/// <a href="https://tools.ietf.org/html/rfc3501#section-6.4.1">rfc350101</a>.</para>
		/// </remarks>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void Check (CancellationToken cancellationToken = default)
		{
			CheckAsync (false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously force the server to sync its in-memory state with its disk state.
		/// </summary>
		/// <remarks>
		/// <para>The <c>CHECK</c> command forces the IMAP server to sync its
		/// in-memory state with its disk state.</para>
		/// <para>For more information about the <c>CHECK</c> command, see
		/// <a href="https://tools.ietf.org/html/rfc3501#section-6.4.1">rfc350101</a>.</para>
		/// </remarks>
		/// <returns>An awaitable task.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task CheckAsync (CancellationToken cancellationToken = default)
		{
			return CheckAsync (true, cancellationToken);
		}

		internal async Task StatusAsync (StatusItems items, bool doAsync, bool throwNotFound, CancellationToken cancellationToken)
		{
			if ((Engine.Capabilities & ImapCapabilities.Status) == 0)
				throw new NotSupportedException ("The IMAP server does not support the STATUS command.");

			CheckState (false, false);

			if (items == StatusItems.None)
				return;

			var command = string.Format ("STATUS %F ({0})\r\n", Engine.GetStatusQuery (items));
			var ic = Engine.QueueCommand (cancellationToken, null, command, this);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, this, throwNotFound);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("STATUS", ic);
		}

		/// <summary>
		/// Update the values of the specified items.
		/// </summary>
		/// <remarks>
		/// <para>Updates the values of the specified items.</para>
		/// <para>The <see cref="Status(StatusItems, System.Threading.CancellationToken)"/> method
		/// MUST NOT be used on a folder that is already in the opened state. Instead, other ways
		/// of getting the desired information should be used.</para>
		/// <para>For example, a common use for the <see cref="Status(StatusItems,System.Threading.CancellationToken)"/>
		/// method is to get the number of unread messages in the folder. When the folder is open, however, it is
		/// possible to use the <see cref="MailFolder.Search(MailKit.Search.SearchQuery, System.Threading.CancellationToken)"/>
		/// method to query for the list of unread messages.</para>
		/// <para>For more information about the <c>STATUS</c> command, see
		/// <a href="https://tools.ietf.org/html/rfc3501#section-6.3.10">rfc3501</a>.</para>
		/// </remarks>
		/// <param name="items">The items to update.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the STATUS command.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void Status (StatusItems items, CancellationToken cancellationToken = default)
		{
			StatusAsync (items, false, true, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously update the values of the specified items.
		/// </summary>
		/// <remarks>
		/// <para>Updates the values of the specified items.</para>
		/// <para>The <see cref="Status(StatusItems, System.Threading.CancellationToken)"/> method
		/// MUST NOT be used on a folder that is already in the opened state. Instead, other ways
		/// of getting the desired information should be used.</para>
		/// <para>For example, a common use for the <see cref="Status(StatusItems,System.Threading.CancellationToken)"/>
		/// method is to get the number of unread messages in the folder. When the folder is open, however, it is
		/// possible to use the <see cref="MailFolder.Search(MailKit.Search.SearchQuery, System.Threading.CancellationToken)"/>
		/// method to query for the list of unread messages.</para>
		/// <para>For more information about the <c>STATUS</c> command, see
		/// <a href="https://tools.ietf.org/html/rfc3501#section-6.3.10">rfc3501</a>.</para>
		/// </remarks>
		/// <returns>An awaitable task.</returns>
		/// <param name="items">The items to update.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the STATUS command.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task StatusAsync (StatusItems items, CancellationToken cancellationToken = default)
		{
			return StatusAsync (items, true, true, cancellationToken);
		}

		static async Task<string> ReadStringTokenAsync (ImapEngine engine, string format, bool doAsync, CancellationToken cancellationToken)
		{
			var token = await engine.ReadTokenAsync (ImapStream.AtomSpecials, doAsync, cancellationToken).ConfigureAwait (false);

			switch (token.Type) {
			case ImapTokenType.Literal: return await engine.ReadLiteralAsync (doAsync, cancellationToken).ConfigureAwait (false);
			case ImapTokenType.QString: return (string) token.Value;
			case ImapTokenType.Atom:    return (string) token.Value;
			default:
				throw ImapEngine.UnexpectedToken (format, token);
			}
		}

		static async Task UntaggedAclAsync (ImapEngine engine, ImapCommand ic, int index, bool doAsync)
		{
			string format = string.Format (ImapEngine.GenericUntaggedResponseSyntaxErrorFormat, "ACL", "{0}");
			var acl = (AccessControlList) ic.UserData;
			string name, rights;
			ImapToken token;

			// read the mailbox name
			await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false);

			do {
				name = await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false);
				rights = await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false);

				acl.Add (new AccessControl (name, rights));

				token = await engine.PeekTokenAsync (doAsync, ic.CancellationToken).ConfigureAwait (false);
			} while (token.Type != ImapTokenType.Eoln);
		}

		async Task<AccessControlList> GetAccessControlListAsync (bool doAsync, CancellationToken cancellationToken)
		{
			if ((Engine.Capabilities & ImapCapabilities.Acl) == 0)
				throw new NotSupportedException ("The IMAP server does not support the ACL extension.");

			CheckState (false, false);

			var ic = new ImapCommand (Engine, cancellationToken, null, "GETACL %F\r\n", this);
			ic.RegisterUntaggedHandler ("ACL", UntaggedAclAsync);
			ic.UserData = new AccessControlList ();

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("GETACL", ic);

			return (AccessControlList) ic.UserData;
		}

		/// <summary>
		/// Get the complete access control list for the folder.
		/// </summary>
		/// <remarks>
		/// Gets the complete access control list for the folder.
		/// </remarks>
		/// <returns>The access control list.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override AccessControlList GetAccessControlList (CancellationToken cancellationToken = default)
		{
			return GetAccessControlListAsync (false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously get the complete access control list for the folder.
		/// </summary>
		/// <remarks>
		/// Gets the complete access control list for the folder.
		/// </remarks>
		/// <returns>The access control list.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override Task<AccessControlList> GetAccessControlListAsync (CancellationToken cancellationToken = default)
		{
			return GetAccessControlListAsync (true, cancellationToken);
		}

		static async Task UntaggedListRightsAsync (ImapEngine engine, ImapCommand ic, int index, bool doAsync)
		{
			string format = string.Format (ImapEngine.GenericUntaggedResponseSyntaxErrorFormat, "LISTRIGHTS", "{0}");
			var access = (AccessRights) ic.UserData;
			ImapToken token;

			// read the mailbox name
			await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false);

			// read the identity name
			await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false);

			do {
				var rights = await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false);

				access.AddRange (rights);

				token = await engine.PeekTokenAsync (doAsync, ic.CancellationToken).ConfigureAwait (false);
			} while (token.Type != ImapTokenType.Eoln);
		}

		async Task<AccessRights> GetAccessRightsAsync (string name, bool doAsync, CancellationToken cancellationToken)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if ((Engine.Capabilities & ImapCapabilities.Acl) == 0)
				throw new NotSupportedException ("The IMAP server does not support the ACL extension.");

			CheckState (false, false);

			var ic = new ImapCommand (Engine, cancellationToken, null, "LISTRIGHTS %F %S\r\n", this, name);
			ic.RegisterUntaggedHandler ("LISTRIGHTS", UntaggedListRightsAsync);
			ic.UserData = new AccessRights ();

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("LISTRIGHTS", ic);

			return (AccessRights) ic.UserData;
		}

		/// <summary>
		/// Get the access rights for a particular identifier.
		/// </summary>
		/// <remarks>
		/// Gets the access rights for a particular identifier.
		/// </remarks>
		/// <returns>The access rights.</returns>
		/// <param name="name">The identifier name.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override AccessRights GetAccessRights (string name, CancellationToken cancellationToken = default)
		{
			return GetAccessRightsAsync (name, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously get the access rights for a particular identifier.
		/// </summary>
		/// <remarks>
		/// Gets the access rights for a particular identifier.
		/// </remarks>
		/// <returns>The access rights.</returns>
		/// <param name="name">The identifier name.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override Task<AccessRights> GetAccessRightsAsync (string name, CancellationToken cancellationToken = default)
		{
			return GetAccessRightsAsync (name, true, cancellationToken);
		}

		static async Task UntaggedMyRightsAsync (ImapEngine engine, ImapCommand ic, int index, bool doAsync)
		{
			string format = string.Format (ImapEngine.GenericUntaggedResponseSyntaxErrorFormat, "MYRIGHTS", "{0}");
			var access = (AccessRights) ic.UserData;

			// read the mailbox name
			await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false);

			// read the access rights
			access.AddRange (await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false));
		}

		async Task<AccessRights> GetMyAccessRightsAsync (bool doAsync, CancellationToken cancellationToken)
		{
			if ((Engine.Capabilities & ImapCapabilities.Acl) == 0)
				throw new NotSupportedException ("The IMAP server does not support the ACL extension.");

			CheckState (false, false);

			var ic = new ImapCommand (Engine, cancellationToken, null, "MYRIGHTS %F\r\n", this);
			ic.RegisterUntaggedHandler ("MYRIGHTS", UntaggedMyRightsAsync);
			ic.UserData = new AccessRights ();

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("MYRIGHTS", ic);

			return (AccessRights) ic.UserData;
		}

		/// <summary>
		/// Get the access rights for the current authenticated user.
		/// </summary>
		/// <remarks>
		/// Gets the access rights for the current authenticated user.
		/// </remarks>
		/// <returns>The access rights.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override AccessRights GetMyAccessRights (CancellationToken cancellationToken = default)
		{
			return GetMyAccessRightsAsync (false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously get the access rights for the current authenticated user.
		/// </summary>
		/// <remarks>
		/// Gets the access rights for the current authenticated user.
		/// </remarks>
		/// <returns>The access rights.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override Task<AccessRights> GetMyAccessRightsAsync (CancellationToken cancellationToken = default)
		{
			return GetMyAccessRightsAsync (true, cancellationToken);
		}

		async Task ModifyAccessRightsAsync (string name, AccessRights rights, string action, bool doAsync, CancellationToken cancellationToken)
		{
			if ((Engine.Capabilities & ImapCapabilities.Acl) == 0)
				throw new NotSupportedException ("The IMAP server does not support the ACL extension.");

			CheckState (false, false);

			var ic = Engine.QueueCommand (cancellationToken, null, "SETACL %F %S %S\r\n", this, name, action + rights);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("SETACL", ic);
		}

		/// <summary>
		/// Add access rights for the specified identity.
		/// </summary>
		/// <remarks>
		/// Adds the given access rights for the specified identity.
		/// </remarks>
		/// <param name="name">The identity name.</param>
		/// <param name="rights">The access rights.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="rights"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// No rights were specified.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override void AddAccessRights (string name, AccessRights rights, CancellationToken cancellationToken = default)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (rights == null)
				throw new ArgumentNullException (nameof (rights));

			if (rights.Count == 0)
				throw new ArgumentException ("No rights were specified.", nameof (rights));

			ModifyAccessRightsAsync (name, rights, "+", false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously add access rights for the specified identity.
		/// </summary>
		/// <remarks>
		/// Adds the given access rights for the specified identity.
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="name">The identity name.</param>
		/// <param name="rights">The access rights.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="rights"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// No rights were specified.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override Task AddAccessRightsAsync (string name, AccessRights rights, CancellationToken cancellationToken = default)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (rights == null)
				throw new ArgumentNullException (nameof (rights));

			if (rights.Count == 0)
				throw new ArgumentException ("No rights were specified.", nameof (rights));

			return ModifyAccessRightsAsync (name, rights, "+", true, cancellationToken);
		}

		/// <summary>
		/// Remove access rights for the specified identity.
		/// </summary>
		/// <remarks>
		/// Removes the given access rights for the specified identity.
		/// </remarks>
		/// <param name="name">The identity name.</param>
		/// <param name="rights">The access rights.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="rights"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// No rights were specified.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override void RemoveAccessRights (string name, AccessRights rights, CancellationToken cancellationToken = default)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (rights == null)
				throw new ArgumentNullException (nameof (rights));

			if (rights.Count == 0)
				throw new ArgumentException ("No rights were specified.", nameof (rights));

			ModifyAccessRightsAsync (name, rights, "-", false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously remove access rights for the specified identity.
		/// </summary>
		/// <remarks>
		/// Removes the given access rights for the specified identity.
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="name">The identity name.</param>
		/// <param name="rights">The access rights.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="rights"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// No rights were specified.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override Task RemoveAccessRightsAsync (string name, AccessRights rights, CancellationToken cancellationToken = default)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (rights == null)
				throw new ArgumentNullException (nameof (rights));

			if (rights.Count == 0)
				throw new ArgumentException ("No rights were specified.", nameof (rights));

			return ModifyAccessRightsAsync (name, rights, "-", true, cancellationToken);
		}

		/// <summary>
		/// Set the access rights for the specified identity.
		/// </summary>
		/// <remarks>
		/// Sets the access rights for the specified identity.
		/// </remarks>
		/// <param name="name">The identity name.</param>
		/// <param name="rights">The access rights.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="rights"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override void SetAccessRights (string name, AccessRights rights, CancellationToken cancellationToken = default)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (rights == null)
				throw new ArgumentNullException (nameof (rights));

			ModifyAccessRightsAsync (name, rights, string.Empty, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously get the access rights for the specified identity.
		/// </summary>
		/// <remarks>
		/// Sets the access rights for the specified identity.
		/// </remarks>
		/// <returns>An awaitable task.</returns>
		/// <param name="name">The identity name.</param>
		/// <param name="rights">The access rights.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="rights"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override Task SetAccessRightsAsync (string name, AccessRights rights, CancellationToken cancellationToken = default)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (rights == null)
				throw new ArgumentNullException (nameof (rights));

			return ModifyAccessRightsAsync (name, rights, string.Empty, true, cancellationToken);
		}

		async Task RemoveAccessAsync (string name, bool doAsync, CancellationToken cancellationToken)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if ((Engine.Capabilities & ImapCapabilities.Acl) == 0)
				throw new NotSupportedException ("The IMAP server does not support the ACL extension.");

			CheckState (false, false);

			var ic = Engine.QueueCommand (cancellationToken, null, "DELETEACL %F %S\r\n", this, name);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("DELETEACL", ic);
		}

		/// <summary>
		/// Remove all access rights for the given identity.
		/// </summary>
		/// <remarks>
		/// Removes all access rights for the given identity.
		/// </remarks>
		/// <param name="name">The identity name.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override void RemoveAccess (string name, CancellationToken cancellationToken = default)
		{
			RemoveAccessAsync (name, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously remove all access rights for the given identity.
		/// </summary>
		/// <remarks>
		/// Removes all access rights for the given identity.
		/// </remarks>
		/// <returns>An awaitable task.</returns>
		/// <param name="name">The identity name.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the ACL extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The command failed.
		/// </exception>
		public override Task RemoveAccessAsync (string name, CancellationToken cancellationToken = default)
		{
			return RemoveAccessAsync (name, true, cancellationToken);
		}

		async Task<string> GetMetadataAsync (MetadataTag tag, bool doAsync, CancellationToken cancellationToken)
		{
			CheckState (false, false);

			if ((Engine.Capabilities & ImapCapabilities.Metadata) == 0)
				throw new NotSupportedException ("The IMAP server does not support the METADATA extension.");

			var ic = new ImapCommand (Engine, cancellationToken, null, "GETMETADATA %F %S\r\n", this, tag.Id);
			ic.RegisterUntaggedHandler ("METADATA", ImapUtils.ParseMetadataAsync);
			var metadata = new MetadataCollection ();
			ic.UserData = metadata;

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("GETMETADATA", ic);

			string value = null;

			for (int i = 0; i < metadata.Count; i++) {
				if (metadata[i].EncodedName == EncodedName && metadata[i].Tag.Id == tag.Id) {
					value = metadata[i].Value;
					metadata.RemoveAt (i);
					break;
				}
			}

			Engine.ProcessMetadataChanges (metadata);

			return value;
		}

		/// <summary>
		/// Get the specified metadata.
		/// </summary>
		/// <remarks>
		/// Gets the specified metadata.
		/// </remarks>
		/// <returns>The requested metadata value.</returns>
		/// <param name="tag">The metadata tag.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the METADATA extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override string GetMetadata (MetadataTag tag, CancellationToken cancellationToken = default)
		{
			return GetMetadataAsync (tag, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously get the specified metadata.
		/// </summary>
		/// <remarks>
		/// Gets the specified metadata.
		/// </remarks>
		/// <returns>The requested metadata value.</returns>
		/// <param name="tag">The metadata tag.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the METADATA extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<string> GetMetadataAsync (MetadataTag tag, CancellationToken cancellationToken = default)
		{
			return GetMetadataAsync (tag, true, cancellationToken);
		}

		async Task<MetadataCollection> GetMetadataAsync (MetadataOptions options, IEnumerable<MetadataTag> tags, bool doAsync, CancellationToken cancellationToken)
		{
			if (options == null)
				throw new ArgumentNullException (nameof (options));

			if (tags == null)
				throw new ArgumentNullException (nameof (tags));

			CheckState (false, false);

			if ((Engine.Capabilities & ImapCapabilities.Metadata) == 0)
				throw new NotSupportedException ("The IMAP server does not support the METADATA extension.");

			var command = new StringBuilder ("GETMETADATA %F");
			var args = new List<object> ();
			bool hasOptions = false;

			if (options.MaxSize.HasValue || options.Depth != 0) {
				command.Append (" (");
				if (options.MaxSize.HasValue) {
					command.Append ("MAXSIZE ");
					command.Append (options.MaxSize.Value.ToString (CultureInfo.InvariantCulture));
					command.Append (' ');
				}
				if (options.Depth > 0) {
					command.Append ("DEPTH ");
					command.Append (options.Depth == int.MaxValue ? "infinity" : "1");
					command.Append (' ');
				}
				command[command.Length - 1] = ')';
				command.Append (' ');
				hasOptions = true;
			}

			args.Add (this);

			int startIndex = command.Length;
			foreach (var tag in tags) {
				command.Append (" %S");
				args.Add (tag.Id);
			}

			if (hasOptions) {
				command[startIndex] = '(';
				command.Append (')');
			}

			command.Append ("\r\n");

			if (args.Count == 1)
				return new MetadataCollection ();

			var ic = new ImapCommand (Engine, cancellationToken, null, command.ToString (), args.ToArray ());
			ic.RegisterUntaggedHandler ("METADATA", ImapUtils.ParseMetadataAsync);
			ic.UserData = new MetadataCollection ();
			options.LongEntries = 0;

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("GETMETADATA", ic);

			var metadata = (MetadataResponseCode) ic.GetResponseCode (ImapResponseCodeType.Metadata);
			if (metadata != null && metadata.SubType == MetadataResponseCodeSubType.LongEntries)
				options.LongEntries = metadata.Value;

			return Engine.FilterMetadata ((MetadataCollection) ic.UserData, EncodedName);
		}

		/// <summary>
		/// Get the specified metadata.
		/// </summary>
		/// <remarks>
		/// Gets the specified metadata.
		/// </remarks>
		/// <returns>The requested metadata.</returns>
		/// <param name="options">The metadata options.</param>
		/// <param name="tags">The metadata tags.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="options"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="tags"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the METADATA extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override MetadataCollection GetMetadata (MetadataOptions options, IEnumerable<MetadataTag> tags, CancellationToken cancellationToken = default)
		{
			return GetMetadataAsync (options, tags, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously get the specified metadata.
		/// </summary>
		/// <remarks>
		/// Gets the specified metadata.
		/// </remarks>
		/// <returns>The requested metadata.</returns>
		/// <param name="options">The metadata options.</param>
		/// <param name="tags">The metadata tags.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="options"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="tags"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the METADATA extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<MetadataCollection> GetMetadataAsync (MetadataOptions options, IEnumerable<MetadataTag> tags, CancellationToken cancellationToken = default)
		{
			return GetMetadataAsync (options, tags, true, cancellationToken);
		}

		async Task SetMetadataAsync (MetadataCollection metadata, bool doAsync, CancellationToken cancellationToken)
		{
			if (metadata == null)
				throw new ArgumentNullException (nameof (metadata));

			CheckState (false, false);

			if ((Engine.Capabilities & ImapCapabilities.Metadata) == 0)
				throw new NotSupportedException ("The IMAP server does not support the METADATA extension.");

			if (metadata.Count == 0)
				return;

			var command = new StringBuilder ("SETMETADATA %F (");
			var args = new List<object> {
				this
			};

			for (int i = 0; i < metadata.Count; i++) {
				if (i > 0)
					command.Append (' ');

				if (metadata[i].Value != null) {
					command.Append ("%S %S");
					args.Add (metadata[i].Tag.Id);
					args.Add (metadata[i].Value);
				} else {
					command.Append ("%S NIL");
					args.Add (metadata[i].Tag.Id);
				}
			}
			command.Append (")\r\n");

			var ic = new ImapCommand (Engine, cancellationToken, null, command.ToString (), args.ToArray ());

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("SETMETADATA", ic);
		}

		/// <summary>
		/// Set the specified metadata.
		/// </summary>
		/// <remarks>
		/// Sets the specified metadata.
		/// </remarks>
		/// <param name="metadata">The metadata.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="metadata"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the METADATA extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void SetMetadata (MetadataCollection metadata, CancellationToken cancellationToken = default)
		{
			SetMetadataAsync (metadata, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously set the specified metadata.
		/// </summary>
		/// <remarks>
		/// Sets the specified metadata.
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="metadata">The metadata.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="metadata"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the METADATA extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task SetMetadataAsync (MetadataCollection metadata, CancellationToken cancellationToken = default)
		{
			return SetMetadataAsync (metadata, true, cancellationToken);
		}

		class Quota
		{
			public uint? MessageLimit;
			public uint? StorageLimit;
			public uint? CurrentMessageCount;
			public uint? CurrentStorageSize;
		}

		class QuotaContext
		{
			public QuotaContext ()
			{
				Quotas = new Dictionary<string, Quota> ();
				QuotaRoots = new List<string> ();
			}

			public IList<string> QuotaRoots {
				get; private set;
			}

			public IDictionary<string, Quota> Quotas {
				get; private set;
			}
		}

		static async Task UntaggedQuotaRootAsync (ImapEngine engine, ImapCommand ic, int index, bool doAsync)
		{
			var format = string.Format (ImapEngine.GenericUntaggedResponseSyntaxErrorFormat, "QUOTAROOT", "{0}");
			var ctx = (QuotaContext) ic.UserData;

			// The first token should be the mailbox name
			await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false);

			// ...followed by 0 or more quota roots
			var token = await engine.PeekTokenAsync (doAsync, ic.CancellationToken).ConfigureAwait (false);

			while (token.Type != ImapTokenType.Eoln) {
				var root = await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false);
				ctx.QuotaRoots.Add (root);

				token = await engine.PeekTokenAsync (doAsync, ic.CancellationToken).ConfigureAwait (false);
			}
		}

		static async Task UntaggedQuotaAsync (ImapEngine engine, ImapCommand ic, int index, bool doAsync)
		{
			var format = string.Format (ImapEngine.GenericUntaggedResponseSyntaxErrorFormat, "QUOTA", "{0}");
			var quotaRoot = await ReadStringTokenAsync (engine, format, doAsync, ic.CancellationToken).ConfigureAwait (false);
			var ctx = (QuotaContext) ic.UserData;
			var quota = new Quota ();

			var token = await engine.ReadTokenAsync (doAsync, ic.CancellationToken).ConfigureAwait (false);

			ImapEngine.AssertToken (token, ImapTokenType.OpenParen, format, token);

			while (token.Type != ImapTokenType.CloseParen) {
				ulong used, limit;
				string resource;

				token = await engine.ReadTokenAsync (doAsync, ic.CancellationToken).ConfigureAwait (false);

				ImapEngine.AssertToken (token, ImapTokenType.Atom, format, token);

				resource = (string) token.Value;

				token = await engine.ReadTokenAsync (doAsync, ic.CancellationToken).ConfigureAwait (false);

				// Note: We parse these quota values as UInt64 because GMail uses 64bit integer values.
				// See https://github.com/jstedfast/MailKit/issues/1602 for details.
				used = ImapEngine.ParseNumber64 (token, false, format, token);

				token = await engine.ReadTokenAsync (doAsync, ic.CancellationToken).ConfigureAwait (false);

				// Note: We parse these quota values as UInt64 because GMail uses 64bit integer values.
				// See https://github.com/jstedfast/MailKit/issues/1602 for details.
				limit = ImapEngine.ParseNumber64 (token, false, format, token);

				if (resource.Equals ("MESSAGE", StringComparison.OrdinalIgnoreCase)) {
					quota.CurrentMessageCount = (uint) (used & 0xffffffff);
					quota.MessageLimit = (uint) (limit & 0xffffffff);
				} else if (resource.Equals ("STORAGE", StringComparison.OrdinalIgnoreCase)) {
					quota.CurrentStorageSize = (uint) (used & 0xffffffff);
					quota.StorageLimit = (uint) (limit & 0xffffffff);
				}

				token = await engine.PeekTokenAsync (doAsync, ic.CancellationToken).ConfigureAwait (false);
			}

			// read the closing paren
			await engine.ReadTokenAsync (doAsync, ic.CancellationToken).ConfigureAwait (false);

			ctx.Quotas[quotaRoot] = quota;
		}

		async Task<FolderQuota> GetQuotaAsync (bool doAsync, CancellationToken cancellationToken)
		{
			CheckState (false, false);

			if ((Engine.Capabilities & ImapCapabilities.Quota) == 0)
				throw new NotSupportedException ("The IMAP server does not support the QUOTA extension.");

			var ic = new ImapCommand (Engine, cancellationToken, null, "GETQUOTAROOT %F\r\n", this);
			var ctx = new QuotaContext ();

			ic.RegisterUntaggedHandler ("QUOTAROOT", UntaggedQuotaRootAsync);
			ic.RegisterUntaggedHandler ("QUOTA", UntaggedQuotaAsync);
			ic.UserData = ctx;

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("GETQUOTAROOT", ic);

			for (int i = 0; i < ctx.QuotaRoots.Count; i++) {
				var encodedName = ctx.QuotaRoots[i];
				ImapFolder quotaRoot;

				if (!ctx.Quotas.TryGetValue (encodedName, out var quota))
					continue;

				quotaRoot = await Engine.GetQuotaRootFolderAsync (encodedName, doAsync, cancellationToken).ConfigureAwait (false);

				return new FolderQuota (quotaRoot) {
					CurrentMessageCount = quota.CurrentMessageCount,
					CurrentStorageSize = quota.CurrentStorageSize,
					MessageLimit = quota.MessageLimit,
					StorageLimit = quota.StorageLimit
				};
			}

			return new FolderQuota (null);
		}

		/// <summary>
		/// Get the quota information for the folder.
		/// </summary>
		/// <remarks>
		/// <para>Gets the quota information for the folder.</para>
		/// <para>To determine if a quotas are supported, check the 
		/// <see cref="ImapClient.SupportsQuotas"/> property.</para>
		/// </remarks>
		/// <returns>The folder quota.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the QUOTA extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override FolderQuota GetQuota (CancellationToken cancellationToken = default)
		{
			return GetQuotaAsync (false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously get the quota information for the folder.
		/// </summary>
		/// <remarks>
		/// <para>Gets the quota information for the folder.</para>
		/// <para>To determine if a quotas are supported, check the 
		/// <see cref="ImapClient.SupportsQuotas"/> property.</para>
		/// </remarks>
		/// <returns>The folder quota.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the QUOTA extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<FolderQuota> GetQuotaAsync (CancellationToken cancellationToken = default)
		{
			return GetQuotaAsync (true, cancellationToken);
		}

		async Task<FolderQuota> SetQuotaAsync (uint? messageLimit, uint? storageLimit, bool doAsync, CancellationToken cancellationToken)
		{
			CheckState (false, false);

			if ((Engine.Capabilities & ImapCapabilities.Quota) == 0)
				throw new NotSupportedException ("The IMAP server does not support the QUOTA extension.");

			var command = new StringBuilder ("SETQUOTA %F (");
			if (messageLimit.HasValue) {
				command.Append ("MESSAGE ");
				command.Append (messageLimit.Value.ToString (CultureInfo.InvariantCulture));
				command.Append (' ');
			}
			if (storageLimit.HasValue) {
				command.Append ("STORAGE ");
				command.Append (storageLimit.Value.ToString (CultureInfo.InvariantCulture));
				command.Append (' ');
			}
			command[command.Length - 1] = ')';
			command.Append ("\r\n");

			var ic = new ImapCommand (Engine, cancellationToken, null, command.ToString (), this);
			var ctx = new QuotaContext ();

			ic.RegisterUntaggedHandler ("QUOTA", UntaggedQuotaAsync);
			ic.UserData = ctx;

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("SETQUOTA", ic);

			if (ctx.Quotas.TryGetValue (EncodedName, out var quota)) {
				return new FolderQuota (this) {
					CurrentMessageCount = quota.CurrentMessageCount,
					CurrentStorageSize = quota.CurrentStorageSize,
					MessageLimit = quota.MessageLimit,
					StorageLimit = quota.StorageLimit
				};
			}

			return new FolderQuota (null);
		}

		/// <summary>
		/// Set the quota limits for the folder.
		/// </summary>
		/// <remarks>
		/// <para>Sets the quota limits for the folder.</para>
		/// <para>To determine if a quotas are supported, check the 
		/// <see cref="ImapClient.SupportsQuotas"/> property.</para>
		/// </remarks>
		/// <returns>The folder quota.</returns>
		/// <param name="messageLimit">If not <c>null</c>, sets the maximum number of messages to allow.</param>
		/// <param name="storageLimit">If not <c>null</c>, sets the maximum storage size (in kilobytes).</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the QUOTA extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override FolderQuota SetQuota (uint? messageLimit, uint? storageLimit, CancellationToken cancellationToken = default)
		{
			return SetQuotaAsync (messageLimit, storageLimit, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously set the quota limits for the folder.
		/// </summary>
		/// <remarks>
		/// <para>Sets the quota limits for the folder.</para>
		/// <para>To determine if a quotas are supported, check the 
		/// <see cref="ImapClient.SupportsQuotas"/> property.</para>
		/// </remarks>
		/// <returns>The folder quota.</returns>
		/// <param name="messageLimit">If not <c>null</c>, sets the maximum number of messages to allow.</param>
		/// <param name="storageLimit">If not <c>null</c>, sets the maximum storage size (in kilobytes).</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the QUOTA extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<FolderQuota> SetQuotaAsync (uint? messageLimit, uint? storageLimit, CancellationToken cancellationToken = default)
		{
			return SetQuotaAsync (messageLimit, storageLimit, true, cancellationToken);
		}

		async Task ExpungeAsync (bool doAsync, CancellationToken cancellationToken)
		{
			CheckState (true, true);

			var ic = Engine.QueueCommand (cancellationToken, this, "EXPUNGE\r\n");

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("EXPUNGE", ic);
		}

		/// <summary>
		/// Expunge the folder, permanently removing all messages marked for deletion.
		/// </summary>
		/// <remarks>
		/// <para>The <c>EXPUNGE</c> command permanently removes all messages in the folder
		/// that have the <see cref="MessageFlags.Deleted"/> flag set.</para>
		/// <para>For more information about the <c>EXPUNGE</c> command, see
		/// <a href="https://tools.ietf.org/html/rfc3501#section-6.4.3">rfc3501</a>.</para>
		/// <note type="note">Normally, a <see cref="MailFolder.MessageExpunged"/> event will be emitted
		/// for each message that is expunged. However, if the IMAP server supports the QRESYNC extension
		/// and it has been enabled via the <see cref="ImapClient.EnableQuickResync(CancellationToken)"/>
		/// method, then the <see cref="MailFolder.MessagesVanished"/> event will be emitted rather than
		/// the <see cref="MailFolder.MessageExpunged"/> event.</note>
		/// </remarks>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void Expunge (CancellationToken cancellationToken = default)
		{
			ExpungeAsync (false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously expunge the folder, permanently removing all messages marked for deletion.
		/// </summary>
		/// <remarks>
		/// <para>The <c>EXPUNGE</c> command permanently removes all messages in the folder
		/// that have the <see cref="MessageFlags.Deleted"/> flag set.</para>
		/// <para>For more information about the <c>EXPUNGE</c> command, see
		/// <a href="https://tools.ietf.org/html/rfc3501#section-6.4.3">rfc3501</a>.</para>
		/// <note type="note">Normally, a <see cref="MailFolder.MessageExpunged"/> event will be emitted
		/// for each message that is expunged. However, if the IMAP server supports the QRESYNC extension
		/// and it has been enabled via the <see cref="ImapClient.EnableQuickResync(CancellationToken)"/>
		/// method, then the <see cref="MailFolder.MessagesVanished"/> event will be emitted rather than
		/// the <see cref="MailFolder.MessageExpunged"/> event.</note>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task ExpungeAsync (CancellationToken cancellationToken = default)
		{
			return ExpungeAsync (true, cancellationToken);
		}

		async Task ExpungeAsync (IList<UniqueId> uids, bool doAsync, CancellationToken cancellationToken)
		{
			if (uids == null)
				throw new ArgumentNullException (nameof (uids));

			CheckState (true, true);

			if (uids.Count == 0)
				return;

			if ((Engine.Capabilities & ImapCapabilities.UidPlus) == 0) {
				// get the list of messages marked for deletion that should not be expunged
				var query = SearchQuery.Deleted.And (SearchQuery.Not (SearchQuery.Uids (uids)));
				var unmark = await SearchAsync (SearchOptions.None, query, doAsync, false, cancellationToken).ConfigureAwait (false);

				if (unmark.Count > 0) {
					// clear the \Deleted flag on all messages except the ones that are to be expunged
					await StoreAsync (unmark.UniqueIds, RemoveDeletedFlag, doAsync, cancellationToken).ConfigureAwait (false);
				}

				// expunge the folder
				await ExpungeAsync (doAsync, cancellationToken).ConfigureAwait (false);

				if (unmark.Count > 0) {
					// restore the \Deleted flags
					await StoreAsync (unmark.UniqueIds, AddDeletedFlag, doAsync, cancellationToken).ConfigureAwait (false);
				}

				return;
			}

			foreach (var ic in Engine.QueueCommands (cancellationToken, this, "UID EXPUNGE %s\r\n", uids)) {
				await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

				ProcessResponseCodes (ic, null);

				if (ic.Response != ImapCommandResponse.Ok)
					throw ImapCommandException.Create ("EXPUNGE", ic);
			}
		}

		/// <summary>
		/// Expunge the specified uids, permanently removing them from the folder.
		/// </summary>
		/// <remarks>
		/// <para>Expunges the specified uids, permanently removing them from the folder.</para>
		/// <para>If the IMAP server supports the UIDPLUS extension (check the
		/// <see cref="ImapClient.Capabilities"/> for the <see cref="ImapCapabilities.UidPlus"/>
		/// flag), then this operation is atomic. Otherwise, MailKit implements this operation
		/// by first searching for the full list of message uids in the folder that are marked for
		/// deletion, unmarking the set of message uids that are not within the specified list of
		/// uids to be be expunged, expunging the folder (thus expunging the requested uids), and
		/// finally restoring the deleted flag on the collection of message uids that were originally
		/// marked for deletion that were not included in the list of uids provided. For this reason,
		/// it is advisable for clients that wish to maintain state to implement this themselves when
		/// the IMAP server does not support the UIDPLUS extension.</para>
		/// <para>For more information about the <c>UID EXPUNGE</c> command, see
		/// <a href="https://tools.ietf.org/html/rfc4315#section-2.1">rfc4315</a>.</para>
		/// <note type="note">Normally, a <see cref="MailFolder.MessageExpunged"/> event will be emitted
		/// for each message that is expunged. However, if the IMAP server supports the QRESYNC extension
		/// and it has been enabled via the <see cref="ImapClient.EnableQuickResync(CancellationToken)"/>
		/// method, then the <see cref="MailFolder.MessagesVanished"/> event will be emitted rather than
		/// the <see cref="MailFolder.MessageExpunged"/> event.</note>
		/// </remarks>
		/// <param name="uids">The message uids.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="uids"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// One or more of the <paramref name="uids"/> is invalid.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void Expunge (IList<UniqueId> uids, CancellationToken cancellationToken = default)
		{
			ExpungeAsync (uids, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously expunge the specified uids, permanently removing them from the folder.
		/// </summary>
		/// <remarks>
		/// <para>Expunges the specified uids, permanently removing them from the folder.</para>
		/// <para>If the IMAP server supports the UIDPLUS extension (check the
		/// <see cref="ImapClient.Capabilities"/> for the <see cref="ImapCapabilities.UidPlus"/>
		/// flag), then this operation is atomic. Otherwise, MailKit implements this operation
		/// by first searching for the full list of message uids in the folder that are marked for
		/// deletion, unmarking the set of message uids that are not within the specified list of
		/// uids to be be expunged, expunging the folder (thus expunging the requested uids), and
		/// finally restoring the deleted flag on the collection of message uids that were originally
		/// marked for deletion that were not included in the list of uids provided. For this reason,
		/// it is advisable for clients that wish to maintain state to implement this themselves when
		/// the IMAP server does not support the UIDPLUS extension.</para>
		/// <para>For more information about the <c>UID EXPUNGE</c> command, see
		/// <a href="https://tools.ietf.org/html/rfc4315#section-2.1">rfc4315</a>.</para>
		/// <note type="note">Normally, a <see cref="MailFolder.MessageExpunged"/> event will be emitted
		/// for each message that is expunged. However, if the IMAP server supports the QRESYNC extension
		/// and it has been enabled via the <see cref="ImapClient.EnableQuickResync(CancellationToken)"/>
		/// method, then the <see cref="MailFolder.MessagesVanished"/> event will be emitted rather than
		/// the <see cref="MailFolder.MessageExpunged"/> event.</note>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="uids">The message uids.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="uids"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// One or more of the <paramref name="uids"/> is invalid.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task ExpungeAsync (IList<UniqueId> uids, CancellationToken cancellationToken = default)
		{
			return ExpungeAsync (uids, true, cancellationToken);
		}

		ImapCommand QueueAppend (FormatOptions options, IAppendRequest request, CancellationToken cancellationToken)
		{
			int numKeywords = request.Keywords != null ? request.Keywords.Count : 0;
			var builder = new StringBuilder ("APPEND %F ");
			var list = new List<object> {
				this
			};

			if ((request.Flags & SettableFlags) != 0 || numKeywords > 0) {
				ImapUtils.FormatFlagsList (builder, request.Flags, numKeywords);
				builder.Append (' ');
			}

			if (request.Keywords != null) {
				foreach (var keyword in request.Keywords)
					list.Add (keyword);
			}

			if (request.InternalDate.HasValue) {
				builder.Append ('"');
				builder.Append (ImapUtils.FormatInternalDate (request.InternalDate.Value));
				builder.Append ("\" ");
			}

			if (request.Annotations != null && request.Annotations.Count > 0) {
				ImapUtils.FormatAnnotations (builder, request.Annotations, list, false);

				if (builder[builder.Length - 1] != ' ')
					builder.Append (' ');
			}

			builder.Append ("%L\r\n");
			list.Add (request.Message);

			var command = builder.ToString ();
			var args = list.ToArray ();

			var ic = new ImapCommand (Engine, cancellationToken, null, options, command, args) {
				Progress = request.TransferProgress
			};

			Engine.QueueCommand (ic);

			return ic;
		}

		async Task<UniqueId?> AppendAsync (FormatOptions options, IAppendRequest request, bool doAsync, CancellationToken cancellationToken)
		{
			if (options == null)
				throw new ArgumentNullException (nameof (options));

			if (request == null)
				throw new ArgumentNullException (nameof (request));

			CheckState (false, false);

			if (options.International && (Engine.Capabilities & ImapCapabilities.UTF8Accept) == 0)
				throw new NotSupportedException ("The IMAP server does not support the UTF8 extension.");

			if (request.Annotations != null && request.Annotations.Count > 0 && (Engine.Capabilities & ImapCapabilities.Annotate) == 0)
				throw new NotSupportedException ("The IMAP server does not support annotations.");

			var format = options.Clone ();
			format.NewLineFormat = NewLineFormat.Dos;
			format.EnsureNewLine = true;

			if ((Engine.Capabilities & ImapCapabilities.UTF8Only) == ImapCapabilities.UTF8Only)
				format.International = true;

			if (format.International && !Engine.UTF8Enabled)
				throw new InvalidOperationException ("The UTF8 extension has not been enabled.");

			var ic = QueueAppend (format, request, cancellationToken);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, this);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("APPEND", ic);

			var append = (AppendUidResponseCode) ic.GetResponseCode (ImapResponseCodeType.AppendUid);

			if (append != null)
				return append.UidSet[0];

			return null;
		}

		/// <summary>
		/// Append a message to the folder.
		/// </summary>
		/// <remarks>
		/// Appends a message to the folder and returns the UniqueId assigned to the message.
		/// </remarks>
		/// <returns>The UID of the appended message, if available; otherwise, <c>null</c>.</returns>
		/// <param name="options">The formatting options.</param>
		/// <param name="request">The append request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="options"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="request"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Internationalized formatting was requested but has not been enabled.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// <para>Internationalized formatting was requested but is not supported by the server.</para>
		/// <para>-or-</para>
		/// <para>The request included annotations but the folder does not support annotations.</para>
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override UniqueId? Append (FormatOptions options, IAppendRequest request, CancellationToken cancellationToken = default)
		{
			return AppendAsync (options, request, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously append a message to the folder.
		/// </summary>
		/// <remarks>
		/// Asynchronously appends a message to the folder and returns the UniqueId assigned to the message.
		/// </remarks>
		/// <returns>The UID of the appended message, if available; otherwise, <c>null</c>.</returns>
		/// <param name="options">The formatting options.</param>
		/// <param name="request">The append request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="options"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="request"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Internationalized formatting was requested but has not been enabled.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// <para>Internationalized formatting was requested but is not supported by the server.</para>
		/// <para>-or-</para>
		/// <para>The request included annotations but the folder does not support annotations.</para>
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<UniqueId?> AppendAsync (FormatOptions options, IAppendRequest request, CancellationToken cancellationToken = default)
		{
			return AppendAsync (options, request, true, cancellationToken);
		}

		ImapCommand QueueMultiAppend (FormatOptions options, IList<IAppendRequest> requests, CancellationToken cancellationToken)
		{
			var builder = new StringBuilder ("APPEND %F");
			var list = new List<object> {
				this
			};

			for (int i = 0; i < requests.Count; i++) {
				int numKeywords = requests[i].Keywords != null ? requests[i].Keywords.Count : 0;

				builder.Append (' ');

				if ((requests[i].Flags & SettableFlags) != 0 || numKeywords > 0) {
					ImapUtils.FormatFlagsList (builder, requests[i].Flags, numKeywords);
					builder.Append (' ');
				}

				if (requests[i].Keywords != null) {
					foreach (var keyword in requests[i].Keywords)
						list.Add (keyword);
				}

				if (requests[i].InternalDate.HasValue) {
					builder.Append ('"');
					builder.Append (ImapUtils.FormatInternalDate (requests[i].InternalDate.Value));
					builder.Append ("\" ");
				}

				if (requests[i].Annotations != null && requests[i].Annotations.Count > 0) {
					ImapUtils.FormatAnnotations (builder, requests[i].Annotations, list, false);

					if (builder[builder.Length - 1] != ' ')
						builder.Append (' ');
				}

				builder.Append ("%L");
				list.Add (requests[i].Message);
			}

			builder.Append ("\r\n");

			var command = builder.ToString ();
			var args = list.ToArray ();

			var ic = new ImapCommand (Engine, cancellationToken, null, options, command, args) {
				Progress = requests[0].TransferProgress
			};

			Engine.QueueCommand (ic);

			return ic;
		}

		async Task<IList<UniqueId>> AppendAsync (FormatOptions options, IList<IAppendRequest> requests, bool doAsync, CancellationToken cancellationToken)
		{
			if (options == null)
				throw new ArgumentNullException (nameof (options));

			if (requests == null)
				throw new ArgumentNullException (nameof (requests));

			for (int i = 0; i < requests.Count; i++) {
				if (requests[i] == null)
					throw new ArgumentException ("One or more of the requests is null.");

				if (requests[i].Annotations != null && requests[i].Annotations.Count > 0 && (Engine.Capabilities & ImapCapabilities.Annotate) == 0)
					throw new NotSupportedException ("One ore more requests included annotations but the IMAP server does not support annotations.");
			}

			CheckState (false, false);

			if (options.International && (Engine.Capabilities & ImapCapabilities.UTF8Accept) == 0)
				throw new NotSupportedException ("The IMAP server does not support the UTF8 extension.");

			var format = options.Clone ();
			format.NewLineFormat = NewLineFormat.Dos;
			format.EnsureNewLine = true;

			if ((Engine.Capabilities & ImapCapabilities.UTF8Only) == ImapCapabilities.UTF8Only)
				format.International = true;

			if (format.International && !Engine.UTF8Enabled)
				throw new InvalidOperationException ("The UTF8 extension has not been enabled.");

			if (requests.Count == 0)
				return Array.Empty<UniqueId> ();

			if ((Engine.Capabilities & ImapCapabilities.MultiAppend) != 0) {
				var ic = QueueMultiAppend (format, requests, cancellationToken);

				await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

				ProcessResponseCodes (ic, this);

				if (ic.Response != ImapCommandResponse.Ok)
					throw ImapCommandException.Create ("APPEND", ic);

				var append = (AppendUidResponseCode) ic.GetResponseCode (ImapResponseCodeType.AppendUid);

				if (append != null)
					return append.UidSet;

				return Array.Empty<UniqueId> ();
			}

			// FIXME: use an aggregate progress reporter
			var uids = new List<UniqueId> ();

			for (int i = 0; i < requests.Count; i++) {
				var uid = await AppendAsync (format, requests[i], doAsync, cancellationToken).ConfigureAwait (false);
				if (uids != null && uid.HasValue)
					uids.Add (uid.Value);
				else
					uids = null;
			}

			if (uids == null)
				return Array.Empty<UniqueId> ();

			return uids;
		}

		/// <summary>
		/// Append multiple messages to the folder.
		/// </summary>
		/// <remarks>
		/// Appends multiple messages to the folder and returns the UniqueIds assigned to the messages.
		/// </remarks>
		/// <returns>The UIDs of the appended messages, if available; otherwise an empty array.</returns>
		/// <param name="options">The formatting options.</param>
		/// <param name="requests">The append requests.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="options"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="requests"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="ArgumentException">
		/// One or more of the <paramref name="requests"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Internationalized formatting was requested but has not been enabled.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// <para>Internationalized formatting was requested but is not supported by the server.</para>
		/// <para>-or-</para>
		/// <para>One ore more requests included annotations but the folder does not support annotations.</para>
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override IList<UniqueId> Append (FormatOptions options, IList<IAppendRequest> requests, CancellationToken cancellationToken = default)
		{
			return AppendAsync (options, requests, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously append multiple messages to the folder.
		/// </summary>
		/// <remarks>
		/// Asynchronously appends multiple messages to the folder and returns the UniqueIds assigned to the messages.
		/// </remarks>
		/// <returns>The UIDs of the appended messages, if available; otherwise an empty array.</returns>
		/// <param name="options">The formatting options.</param>
		/// <param name="requests">The append requests.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="options"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="requests"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="ArgumentException">
		/// One or more of the <paramref name="requests"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Internationalized formatting was requested but has not been enabled.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// <para>Internationalized formatting was requested but is not supported by the server.</para>
		/// <para>-or-</para>
		/// <para>One ore more requests included annotations but the folder does not support annotations.</para>
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<IList<UniqueId>> AppendAsync (FormatOptions options, IList<IAppendRequest> requests, CancellationToken cancellationToken = default)
		{
			return AppendAsync (options, requests, true, cancellationToken);
		}

		ImapCommand QueueReplace (FormatOptions options, UniqueId uid, IReplaceRequest request, CancellationToken cancellationToken)
		{
			int numKeywords = request.Keywords != null ? request.Keywords.Count : 0;
			var builder = new StringBuilder ($"UID REPLACE {uid} %F ");
			var list = new List<object> {
				request.Destination ?? this
			};

			if ((request.Flags & SettableFlags) != 0 || numKeywords > 0) {
				ImapUtils.FormatFlagsList (builder, request.Flags, numKeywords);
				builder.Append (' ');
			}

			if (request.Keywords != null) {
				foreach (var keyword in request.Keywords)
					list.Add (keyword);
			}

			if (request.InternalDate.HasValue) {
				builder.Append ('"');
				builder.Append (ImapUtils.FormatInternalDate (request.InternalDate.Value));
				builder.Append ("\" ");
			}

			if (request.Annotations != null && request.Annotations.Count > 0) {
				ImapUtils.FormatAnnotations (builder, request.Annotations, list, false);

				if (builder[builder.Length - 1] != ' ')
					builder.Append (' ');
			}

			builder.Append ("%L\r\n");
			list.Add (request.Message);

			var command = builder.ToString ();
			var args = list.ToArray ();

			var ic = new ImapCommand (Engine, cancellationToken, null, options, command, args) {
				Progress = request.TransferProgress
			};

			Engine.QueueCommand (ic);

			return ic;
		}

		async Task<UniqueId?> ReplaceAsync (FormatOptions options, UniqueId uid, IReplaceRequest request, bool doAsync, CancellationToken cancellationToken)
		{
			if (options == null)
				throw new ArgumentNullException (nameof (options));

			if (!uid.IsValid)
				throw new ArgumentException ("The uid is invalid.", nameof (uid));

			if (request == null)
				throw new ArgumentNullException (nameof (request));

			if (request.Destination != null && !(request.Destination is ImapFolder target && target.Engine == Engine))
				throw new ArgumentException ("The destination folder does not belong to this ImapClient.", nameof (request));

			if (request.Annotations != null && request.Annotations.Count > 0 && (Engine.Capabilities & ImapCapabilities.Annotate) == 0)
				throw new NotSupportedException ("The IMAP server does not support annotations.");

			CheckState (true, true);

			if ((Engine.Capabilities & ImapCapabilities.Replace) == 0) {
				var destination = request.Destination as ImapFolder ?? this;
				var appended = await destination.AppendAsync (options, request, doAsync, cancellationToken).ConfigureAwait (false);
				await StoreAsync (new[] { uid }, AddDeletedFlag, doAsync, cancellationToken).ConfigureAwait (false);
				if ((Engine.Capabilities & ImapCapabilities.UidPlus) != 0)
					await ExpungeAsync (new[] { uid }, doAsync, cancellationToken).ConfigureAwait (false);
				return appended;
			}

			if (options.International && (Engine.Capabilities & ImapCapabilities.UTF8Accept) == 0)
				throw new NotSupportedException ("The IMAP server does not support the UTF8 extension.");

			var format = options.Clone ();
			format.NewLineFormat = NewLineFormat.Dos;
			format.EnsureNewLine = true;

			if ((Engine.Capabilities & ImapCapabilities.UTF8Only) == ImapCapabilities.UTF8Only)
				format.International = true;

			if (format.International && !Engine.UTF8Enabled)
				throw new InvalidOperationException ("The UTF8 extension has not been enabled.");

			var ic = QueueReplace (format, uid, request, cancellationToken);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, this);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("REPLACE", ic);

			var append = (AppendUidResponseCode) ic.GetResponseCode (ImapResponseCodeType.AppendUid);

			if (append != null)
				return append.UidSet[0];

			return null;
		}

		/// <summary>
		/// Replace a message in the folder.
		/// </summary>
		/// <remarks>
		/// Replaces a message in the folder and returns the UniqueId assigned to the new message.
		/// </remarks>
		/// <returns>The UID of the new message, if available; otherwise, <c>null</c>.</returns>
		/// <param name="options">The formatting options.</param>
		/// <param name="uid">The UID of the message to be replaced.</param>
		/// <param name="request">The replace request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="options"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="request"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para><paramref name="uid"/> is invalid.</para>
		/// <para>-or-</para>
		/// <para>The destination folder does not belong to this <see cref="ImapClient"/>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Internationalized formatting was requested but has not been enabled.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// <para>Internationalized formatting was requested but is not supported by the server.</para>
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override UniqueId? Replace (FormatOptions options, UniqueId uid, IReplaceRequest request, CancellationToken cancellationToken = default)
		{
			return ReplaceAsync (options, uid, request, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously replace a message in the folder.
		/// </summary>
		/// <remarks>
		/// Asynchronously replaces a message in the folder and returns the UniqueId assigned to the new message.
		/// </remarks>
		/// <returns>The UID of the new message, if available; otherwise, <c>null</c>.</returns>
		/// <param name="options">The formatting options.</param>
		/// <param name="uid">The UID of the message to be replaced.</param>
		/// <param name="request">The replace request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="options"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="request"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para><paramref name="uid"/> is invalid.</para>
		/// <para>-or-</para>
		/// <para>The destination folder does not belong to this <see cref="ImapClient"/>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Internationalized formatting was requested but has not been enabled.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// <para>Internationalized formatting was requested but is not supported by the server.</para>
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<UniqueId?> ReplaceAsync (FormatOptions options, UniqueId uid, IReplaceRequest request, CancellationToken cancellationToken = default)
		{
			return ReplaceAsync (options, uid, request, true, cancellationToken);
		}

		ImapCommand QueueReplace (FormatOptions options, int index, IReplaceRequest request, CancellationToken cancellationToken)
		{
			int numKeywords = request.Keywords != null ? request.Keywords.Count : 0;
			var builder = new StringBuilder ($"REPLACE %d %F ");
			var list = new List<object> {
				index + 1,
				request.Destination ?? this
			};

			if ((request.Flags & SettableFlags) != 0) {
				ImapUtils.FormatFlagsList (builder, request.Flags, numKeywords);
				builder.Append (' ');
			}

			if (request.Keywords != null) {
				foreach (var keyword in request.Keywords)
					list.Add (keyword);
			}

			if (request.InternalDate.HasValue) {
				builder.Append ('"');
				builder.Append (ImapUtils.FormatInternalDate (request.InternalDate.Value));
				builder.Append ("\" ");
			}

			if (request.Annotations != null && request.Annotations.Count > 0) {
				ImapUtils.FormatAnnotations (builder, request.Annotations, list, false);

				if (builder[builder.Length - 1] != ' ')
					builder.Append (' ');
			}

			builder.Append ("%L\r\n");
			list.Add (request.Message);

			var command = builder.ToString ();
			var args = list.ToArray ();

			var ic = new ImapCommand (Engine, cancellationToken, null, options, command, args) {
				Progress = request.TransferProgress
			};

			Engine.QueueCommand (ic);

			return ic;
		}

		async Task<UniqueId?> ReplaceAsync (FormatOptions options, int index, IReplaceRequest request, bool doAsync, CancellationToken cancellationToken)
		{
			if (options == null)
				throw new ArgumentNullException (nameof (options));

			if (index < 0 || index >= Count)
				throw new ArgumentOutOfRangeException (nameof (index));

			if (request == null)
				throw new ArgumentNullException (nameof (request));

			if (request.Destination != null && !(request.Destination is ImapFolder target && target.Engine == Engine))
				throw new ArgumentException ("The destination folder does not belong to this ImapClient.", nameof (request));

			if (request.Annotations != null && request.Annotations.Count > 0 && (Engine.Capabilities & ImapCapabilities.Annotate) == 0)
				throw new NotSupportedException ("The IMAP server does not support annotations.");

			CheckState (true, true);

			if ((Engine.Capabilities & ImapCapabilities.Replace) == 0) {
				var destination = request.Destination as ImapFolder ?? this;
				var uid = await destination.AppendAsync (options, request, doAsync, cancellationToken).ConfigureAwait (false);
				await StoreAsync (new[] { index }, AddDeletedFlag, doAsync, cancellationToken).ConfigureAwait (false);
				return uid;
			}

			if (options.International && (Engine.Capabilities & ImapCapabilities.UTF8Accept) == 0)
				throw new NotSupportedException ("The IMAP server does not support the UTF8 extension.");

			var format = options.Clone ();
			format.NewLineFormat = NewLineFormat.Dos;
			format.EnsureNewLine = true;

			if ((Engine.Capabilities & ImapCapabilities.UTF8Only) == ImapCapabilities.UTF8Only)
				format.International = true;

			if (format.International && !Engine.UTF8Enabled)
				throw new InvalidOperationException ("The UTF8 extension has not been enabled.");

			var ic = QueueReplace (format, index, request, cancellationToken);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, this);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("REPLACE", ic);

			var append = (AppendUidResponseCode) ic.GetResponseCode (ImapResponseCodeType.AppendUid);

			if (append != null)
				return append.UidSet[0];

			return null;
		}

		/// <summary>
		/// Replace a message in the folder.
		/// </summary>
		/// <remarks>
		/// Replaces a message in the folder and returns the UniqueId assigned to the new message.
		/// </remarks>
		/// <returns>The UID of the new message, if available; otherwise, <c>null</c>.</returns>
		/// <param name="options">The formatting options.</param>
		/// <param name="index">The index of the message to be replaced.</param>
		/// <param name="request">The replace request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="options"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="request"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="index"/> is out of range.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// The destination folder does not belong to this <see cref="ImapClient"/>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Internationalized formatting was requested but has not been enabled.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// <para>Internationalized formatting was requested but is not supported by the server.</para>
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override UniqueId? Replace (FormatOptions options, int index, IReplaceRequest request, CancellationToken cancellationToken = default)
		{
			return ReplaceAsync (options, index, request, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously replace a message in the folder.
		/// </summary>
		/// <remarks>
		/// Asynchronously replaces a message in the folder and returns the UniqueId assigned to the new message.
		/// </remarks>
		/// <returns>The UID of the new message, if available; otherwise, <c>null</c>.</returns>
		/// <param name="options">The formatting options.</param>
		/// <param name="index">The index of the message to be replaced.</param>
		/// <param name="request">The replace request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="options"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="request"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="index"/> is out of range.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// The destination folder does not belong to this <see cref="ImapClient"/>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Internationalized formatting was requested but has not been enabled.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The <see cref="ImapFolder"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// <para>Internationalized formatting was requested but is not supported by the server.</para>
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<UniqueId?> ReplaceAsync (FormatOptions options, int index, IReplaceRequest request, CancellationToken cancellationToken = default)
		{
			return ReplaceAsync (options, index, request, true, cancellationToken);
		}

		async Task<IList<int>> GetIndexesAsync (IList<UniqueId> uids, bool doAsync, CancellationToken cancellationToken)
		{
			var command = string.Format ("SEARCH UID {0}\r\n", UniqueIdSet.ToString (uids));
			var ic = new ImapCommand (Engine, cancellationToken, this, command);
			var results = new SearchResults (SortOrder.Ascending);

			if ((Engine.Capabilities & ImapCapabilities.ESearch) != 0)
				ic.RegisterUntaggedHandler ("ESEARCH", ESearchMatchesAsync);

			ic.RegisterUntaggedHandler ("SEARCH", SearchMatchesAsync);
			ic.UserData = results;

			Engine.QueueCommand (ic);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, null);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("SEARCH", ic);

			var indexes = new int[results.UniqueIds.Count];
			for (int i = 0; i < indexes.Length; i++)
				indexes[i] = (int) results.UniqueIds[i].Id - 1;

			return indexes;
		}

		async Task<UniqueIdMap> CopyToAsync (IList<UniqueId> uids, IMailFolder destination, bool doAsync, CancellationToken cancellationToken)
		{
			if (uids == null)
				throw new ArgumentNullException (nameof (uids));

			if (destination == null)
				throw new ArgumentNullException (nameof (destination));

			if (destination is not ImapFolder target || (target.Engine != Engine))
				throw new ArgumentException ("The destination folder does not belong to this ImapClient.", nameof (destination));

			CheckState (true, false);

			if (uids.Count == 0)
				return UniqueIdMap.Empty;

			if ((Engine.Capabilities & ImapCapabilities.UidPlus) == 0) {
				var indexes = await GetIndexesAsync (uids, doAsync, cancellationToken).ConfigureAwait (false);
				await CopyToAsync (indexes, destination, doAsync, cancellationToken).ConfigureAwait (false);
				return UniqueIdMap.Empty;
			}

			UniqueIdSet dest = null;
			UniqueIdSet src = null;

			foreach (var ic in Engine.QueueCommands (cancellationToken, this, "UID COPY %s %F\r\n", uids, destination)) {
				await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

				ProcessResponseCodes (ic, destination);

				if (ic.Response != ImapCommandResponse.Ok)
					throw ImapCommandException.Create ("COPY", ic);

				var copy = (CopyUidResponseCode) ic.GetResponseCode (ImapResponseCodeType.CopyUid);

				if (copy != null) {
					if (dest == null) {
						dest = copy.DestUidSet;
						src = copy.SrcUidSet;
					} else {
						dest.AddRange (copy.DestUidSet);
						src.AddRange (copy.SrcUidSet);
					}
				}
			}

			if (dest == null)
				return UniqueIdMap.Empty;

			return new UniqueIdMap (src, dest);
		}

		/// <summary>
		/// Copy the specified messages to the destination folder.
		/// </summary>
		/// <remarks>
		/// Copies the specified messages to the destination folder.
		/// </remarks>
		/// <returns>The UID mapping of the messages in the destination folder, if available; otherwise an empty mapping.</returns>
		/// <param name="uids">The UIDs of the messages to copy.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="uids"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="destination"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para>One or more of the <paramref name="uids"/> is invalid.</para>
		/// <para>-or-</para>
		/// <para>The destination folder does not belong to the <see cref="ImapClient"/>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// <paramref name="destination"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the UIDPLUS extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override UniqueIdMap CopyTo (IList<UniqueId> uids, IMailFolder destination, CancellationToken cancellationToken = default)
		{
			return CopyToAsync (uids, destination, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously copy the specified messages to the destination folder.
		/// </summary>
		/// <remarks>
		/// Copies the specified messages to the destination folder.
		/// </remarks>
		/// <returns>The UID mapping of the messages in the destination folder, if available; otherwise an empty mapping.</returns>
		/// <param name="uids">The UIDs of the messages to copy.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="uids"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="destination"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para>One or more of the <paramref name="uids"/> is invalid.</para>
		/// <para>-or-</para>
		/// <para>The destination folder does not belong to the <see cref="ImapClient"/>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// <paramref name="destination"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The IMAP server does not support the UIDPLUS extension.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<UniqueIdMap> CopyToAsync (IList<UniqueId> uids, IMailFolder destination, CancellationToken cancellationToken = default)
		{
			return CopyToAsync (uids, destination, true, cancellationToken);
		}

		async Task<UniqueIdMap> MoveToAsync (IList<UniqueId> uids, IMailFolder destination, bool doAsync, CancellationToken cancellationToken)
		{
			if ((Engine.Capabilities & ImapCapabilities.Move) == 0) {
				var copied = await CopyToAsync (uids, destination, doAsync, cancellationToken).ConfigureAwait (false);
				await StoreAsync (uids, AddDeletedFlag, doAsync, cancellationToken).ConfigureAwait (false);
				await ExpungeAsync (uids, doAsync, cancellationToken).ConfigureAwait (false);
				return copied;
			}

			if ((Engine.Capabilities & ImapCapabilities.UidPlus) == 0) {
				var indexes = await GetIndexesAsync (uids, doAsync, cancellationToken).ConfigureAwait (false);
				await MoveToAsync (indexes, destination, doAsync, cancellationToken).ConfigureAwait (false);
				return UniqueIdMap.Empty;
			}

			if (uids == null)
				throw new ArgumentNullException (nameof (uids));

			if (destination == null)
				throw new ArgumentNullException (nameof (destination));

			if (destination is not ImapFolder || ((ImapFolder) destination).Engine != Engine)
				throw new ArgumentException ("The destination folder does not belong to this ImapClient.", nameof (destination));

			CheckState (true, true);

			if (uids.Count == 0)
				return UniqueIdMap.Empty;

			UniqueIdSet dest = null;
			UniqueIdSet src = null;

			foreach (var ic in Engine.QueueCommands (cancellationToken, this, "UID MOVE %s %F\r\n", uids, destination)) {
				await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

				ProcessResponseCodes (ic, destination);

				if (ic.Response != ImapCommandResponse.Ok)
					throw ImapCommandException.Create ("MOVE", ic);

				var copy = (CopyUidResponseCode) ic.GetResponseCode (ImapResponseCodeType.CopyUid);

				if (copy != null) {
					if (dest == null) {
						dest = copy.DestUidSet;
						src = copy.SrcUidSet;
					} else {
						dest.AddRange (copy.DestUidSet);
						src.AddRange (copy.SrcUidSet);
					}
				}
			}

			if (dest == null)
				return UniqueIdMap.Empty;

			return new UniqueIdMap (src, dest);
		}

		/// <summary>
		/// Move the specified messages to the destination folder.
		/// </summary>
		/// <remarks>
		/// <para>Moves the specified messages to the destination folder.</para>
		/// <para>If the IMAP server supports the MOVE extension (check the <see cref="ImapClient.Capabilities"/>
		/// property for the <see cref="ImapCapabilities.Move"/> flag), then this operation will be atomic.
		/// Otherwise, MailKit implements this by first copying the messages to the destination folder, then
		/// marking them for deletion in the originating folder, and finally expunging them (see
		/// <see cref="Expunge(IList&lt;UniqueId&gt;,CancellationToken)"/> for more information about how a
		/// subset of messages are expunged). Since the server could disconnect at any point between those 3
		/// (or more) commands, it is advisable for clients to implement their own logic for moving messages when
		/// the IMAP server does not support the MOVE command in order to better handle spontanious server
		/// disconnects and other error conditions.</para>
		/// </remarks>
		/// <returns>The UID mapping of the messages in the destination folder, if available; otherwise an empty mapping.</returns>
		/// <param name="uids">The UIDs of the messages to move.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="uids"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="destination"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para><paramref name="uids"/> is empty.</para>
		/// <para>-or-</para>
		/// <para>One or more of the <paramref name="uids"/> is invalid.</para>
		/// <para>-or-</para>
		/// <para>The destination folder does not belong to the <see cref="ImapClient"/>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// <paramref name="destination"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override UniqueIdMap MoveTo (IList<UniqueId> uids, IMailFolder destination, CancellationToken cancellationToken = default)
		{
			return MoveToAsync (uids, destination, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously move the specified messages to the destination folder.
		/// </summary>
		/// <remarks>
		/// <para>Moves the specified messages to the destination folder.</para>
		/// <para>If the IMAP server supports the MOVE extension (check the <see cref="ImapClient.Capabilities"/>
		/// property for the <see cref="ImapCapabilities.Move"/> flag), then this operation will be atomic.
		/// Otherwise, MailKit implements this by first copying the messages to the destination folder, then
		/// marking them for deletion in the originating folder, and finally expunging them (see
		/// <see cref="Expunge(IList&lt;UniqueId&gt;,CancellationToken)"/> for more information about how a
		/// subset of messages are expunged). Since the server could disconnect at any point between those 3
		/// (or more) commands, it is advisable for clients to implement their own logic for moving messages when
		/// the IMAP server does not support the MOVE command in order to better handle spontanious server
		/// disconnects and other error conditions.</para>
		/// </remarks>
		/// <returns>The UID mapping of the messages in the destination folder, if available; otherwise an empty mapping.</returns>
		/// <param name="uids">The UIDs of the messages to move.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="uids"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="destination"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para><paramref name="uids"/> is empty.</para>
		/// <para>-or-</para>
		/// <para>One or more of the <paramref name="uids"/> is invalid.</para>
		/// <para>-or-</para>
		/// <para>The destination folder does not belong to the <see cref="ImapClient"/>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// <paramref name="destination"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task<UniqueIdMap> MoveToAsync (IList<UniqueId> uids, IMailFolder destination, CancellationToken cancellationToken = default)
		{
			return MoveToAsync (uids, destination, true, cancellationToken);
		}

		async Task CopyToAsync (IList<int> indexes, IMailFolder destination, bool doAsync, CancellationToken cancellationToken)
		{
			if (indexes == null)
				throw new ArgumentNullException (nameof (indexes));

			if (destination == null)
				throw new ArgumentNullException (nameof (destination));

			if (destination is not ImapFolder || ((ImapFolder) destination).Engine != Engine)
				throw new ArgumentException ("The destination folder does not belong to this ImapClient.", nameof (destination));

			CheckState (true, false);
			CheckAllowIndexes ();

			if (indexes.Count == 0)
				return;

			var command = new StringBuilder ("COPY ");
			ImapUtils.FormatIndexSet (Engine, command, indexes);
			command.Append (" %F\r\n");

			var ic = Engine.QueueCommand (cancellationToken, this, command.ToString (), destination);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, destination);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("COPY", ic);
		}

		/// <summary>
		/// Copy the specified messages to the destination folder.
		/// </summary>
		/// <remarks>
		/// Copies the specified messages to the destination folder.
		/// </remarks>
		/// <param name="indexes">The indexes of the messages to copy.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="indexes"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="destination"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para>One or more of the <paramref name="indexes"/> is invalid.</para>
		/// <para>-or-</para>
		/// <para>The destination folder does not belong to the <see cref="ImapClient"/>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// <paramref name="destination"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void CopyTo (IList<int> indexes, IMailFolder destination, CancellationToken cancellationToken = default)
		{
			CopyToAsync (indexes, destination, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously copy the specified messages to the destination folder.
		/// </summary>
		/// <remarks>
		/// Copies the specified messages to the destination folder.
		/// </remarks>
		/// <returns>An awaitable task.</returns>
		/// <param name="indexes">The indexes of the messages to copy.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="indexes"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="destination"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para>One or more of the <paramref name="indexes"/> is invalid.</para>
		/// <para>-or-</para>
		/// <para>The destination folder does not belong to the <see cref="ImapClient"/>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// <paramref name="destination"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task CopyToAsync (IList<int> indexes, IMailFolder destination, CancellationToken cancellationToken = default)
		{
			return CopyToAsync (indexes, destination, true, cancellationToken);
		}

		async Task MoveToAsync (IList<int> indexes, IMailFolder destination, bool doAsync, CancellationToken cancellationToken)
		{
			if ((Engine.Capabilities & ImapCapabilities.Move) == 0) {
				await CopyToAsync (indexes, destination, doAsync, cancellationToken).ConfigureAwait (false);
				await StoreAsync (indexes, AddDeletedFlag, doAsync, cancellationToken).ConfigureAwait (false);
				return;
			}

			if (indexes == null)
				throw new ArgumentNullException (nameof (indexes));

			if (destination == null)
				throw new ArgumentNullException (nameof (destination));

			if (destination is not ImapFolder || ((ImapFolder) destination).Engine != Engine)
				throw new ArgumentException ("The destination folder does not belong to this ImapClient.", nameof (destination));

			CheckState (true, true);
			CheckAllowIndexes ();

			if (indexes.Count == 0)
				return;

			var command = new StringBuilder ("MOVE ");
			ImapUtils.FormatIndexSet (Engine, command, indexes);
			command.Append (" %F\r\n");

			var ic = Engine.QueueCommand (cancellationToken, this, command.ToString (), destination);

			await Engine.RunAsync (ic, doAsync).ConfigureAwait (false);

			ProcessResponseCodes (ic, destination);

			if (ic.Response != ImapCommandResponse.Ok)
				throw ImapCommandException.Create ("MOVE", ic);
		}

		/// <summary>
		/// Move the specified messages to the destination folder.
		/// </summary>
		/// <remarks>
		/// <para>If the IMAP server supports the MOVE command, then the MOVE command will be used. Otherwise,
		/// the messages will first be copied to the destination folder and then marked as \Deleted in the
		/// originating folder. Since the server could disconnect at any point between those 2 operations, it
		/// may be advisable to implement your own logic for moving messages in this case in order to better
		/// handle spontanious server disconnects and other error conditions.</para>
		/// </remarks>
		/// <param name="indexes">The indexes of the messages to move.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="indexes"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="destination"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para>One or more of the <paramref name="indexes"/> is invalid.</para>
		/// <para>-or-</para>
		/// <para>The destination folder does not belong to the <see cref="ImapClient"/>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// <paramref name="destination"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override void MoveTo (IList<int> indexes, IMailFolder destination, CancellationToken cancellationToken = default)
		{
			MoveToAsync (indexes, destination, false, cancellationToken).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Asynchronously move the specified messages to the destination folder.
		/// </summary>
		/// <remarks>
		/// <para>If the IMAP server supports the MOVE command, then the MOVE command will be used. Otherwise,
		/// the messages will first be copied to the destination folder and then marked as \Deleted in the
		/// originating folder. Since the server could disconnect at any point between those 2 operations, it
		/// may be advisable to implement your own logic for moving messages in this case in order to better
		/// handle spontanious server disconnects and other error conditions.</para>
		/// </remarks>
		/// <returns>An awaitable task.</returns>
		/// <param name="indexes">The indexes of the messages to move.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="indexes"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="destination"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para>One or more of the <paramref name="indexes"/> is invalid.</para>
		/// <para>-or-</para>
		/// <para>The destination folder does not belong to the <see cref="ImapClient"/>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// <paramref name="destination"/> does not exist.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open in read-write mode.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server's response contained unexpected tokens.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied with a NO or BAD response.
		/// </exception>
		public override Task MoveToAsync (IList<int> indexes, IMailFolder destination, CancellationToken cancellationToken = default)
		{
			return MoveToAsync (indexes, destination, true, cancellationToken);
		}

		#region IEnumerable<MimeMessage> implementation

		/// <summary>
		/// Get an enumerator for the messages in the folder.
		/// </summary>
		/// <remarks>
		/// Gets an enumerator for the messages in the folder.
		/// </remarks>
		/// <returns>The enumerator.</returns>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="FolderNotOpenException">
		/// The <see cref="ImapFolder"/> is not currently open.
		/// </exception>
		public override IEnumerator<MimeMessage> GetEnumerator ()
		{
			CheckState (true, false);

			for (int i = 0; i < Count; i++)
				yield return GetMessage (i, CancellationToken.None);

			yield break;
		}

		#endregion

		#region Untagged response handlers called by ImapEngine

		internal void OnExists (int count)
		{
			countChanged = false;
			Count = count;

			OnCountChanged ();
		}

		internal void OnExpunge (int index)
		{
			// Note: It is not required for the IMAP server to send an explicit untagged `* # EXISTS` response if it sends
			// untagged `* # EXPUNGE` responses, so we queue a CountChanged event (that is only emitted if the server does
			// NOT send the `* # EXISTS` response).
			countChanged = true;
			Count--;

			OnMessageExpunged (new MessageEventArgs (index));
		}

		internal void FlushQueuedEvents ()
		{
			if (countChanged) {
				countChanged = false;
				OnCountChanged ();
			}
		}

		void OnFetchAsyncCompleted (MessageSummary message)
		{
			int index = message.Index;
			UniqueId? uid = null;

			if ((message.Fields & MessageSummaryItems.UniqueId) != 0)
				uid = message.UniqueId;

			if ((message.Fields & MessageSummaryItems.Flags) != 0) {
				var args = new MessageFlagsChangedEventArgs (index, message.Flags.Value, (HashSet<string>) message.Keywords) {
					ModSeq = message.ModSeq,
					UniqueId = uid
				};

				OnMessageFlagsChanged (args);
			}

			if ((message.Fields & MessageSummaryItems.GMailLabels) != 0) {
				var args = new MessageLabelsChangedEventArgs (index, message.GMailLabels) {
					ModSeq = message.ModSeq,
					UniqueId = uid
				};

				OnMessageLabelsChanged (args);
			}

			if ((message.Fields & MessageSummaryItems.Annotations) != 0) {
				var args = new AnnotationsChangedEventArgs (index, message.Annotations) {
					ModSeq = message.ModSeq,
					UniqueId = uid
				};

				OnAnnotationsChanged (args);
			}

			if ((message.Fields & MessageSummaryItems.ModSeq) != 0) {
				var args = new ModSeqChangedEventArgs (index, message.ModSeq.Value) {
					UniqueId = uid
				};

				OnModSeqChanged (args);
			}

			if (message.Fields != MessageSummaryItems.None)
				OnMessageSummaryFetched (message);
		}

		internal Task OnFetchAsync (ImapEngine engine, int index, bool doAsync, CancellationToken cancellationToken)
		{
			var message = new MessageSummary (this, index);

			if (doAsync)
				return FetchSummaryItemsAsync (engine, message, OnFetchAsyncCompleted, cancellationToken);

			FetchSummaryItems (engine, message, OnFetchAsyncCompleted, cancellationToken);

			return Task.CompletedTask;
		}

		internal void OnRecent (int count)
		{
			if (Recent == count)
				return;

			Recent = count;

			OnRecentChanged ();
		}

		internal async Task OnVanishedAsync (ImapEngine engine, bool doAsync, CancellationToken cancellationToken)
		{
			var token = await engine.ReadTokenAsync (doAsync, cancellationToken).ConfigureAwait (false);
			UniqueIdSet vanished;
			bool earlier = false;

			if (token.Type == ImapTokenType.OpenParen) {
				do {
					token = await engine.ReadTokenAsync (doAsync, cancellationToken).ConfigureAwait (false);

					if (token.Type == ImapTokenType.CloseParen)
						break;

					ImapEngine.AssertToken (token, ImapTokenType.Atom, ImapEngine.GenericUntaggedResponseSyntaxErrorFormat, "VANISHED", token);

					var atom = (string) token.Value;

					if (atom.Equals ("EARLIER", StringComparison.OrdinalIgnoreCase))
						earlier = true;
				} while (true);

				token = await engine.ReadTokenAsync (doAsync, cancellationToken).ConfigureAwait (false);
			}

			vanished = ImapEngine.ParseUidSet (token, UidValidity, out _, out _, ImapEngine.GenericUntaggedResponseSyntaxErrorFormat, "VANISHED", token);

			OnMessagesVanished (new MessagesVanishedEventArgs (vanished, earlier));

			if (!earlier) {
				Count -= vanished.Count;

				OnCountChanged ();
			}
		}

		internal void UpdateAttributes (FolderAttributes attrs)
		{
			var unsubscribed = false;
			var subscribed = false;

			if ((attrs & FolderAttributes.Subscribed) == 0)
				unsubscribed = (Attributes & FolderAttributes.Subscribed) != 0;
			else
				subscribed = (Attributes & FolderAttributes.Subscribed) == 0;

			var deleted = ((attrs & FolderAttributes.NonExistent) != 0) &&
				(Attributes & FolderAttributes.NonExistent) == 0;

			Attributes = attrs;

			if (unsubscribed)
				OnUnsubscribed ();

			if (subscribed)
				OnSubscribed ();

			if (deleted)
				OnDeleted ();
		}

		internal void UpdateAcceptedFlags (MessageFlags flags, IReadOnlySetOfStrings keywords)
		{
			AcceptedKeywords = keywords;
			AcceptedFlags = flags;
		}

		internal void UnsetAcceptedFlags ()
		{
			((HashSet<string>) AcceptedKeywords).Clear ();
			AcceptedFlags = MessageFlags.None;
		}

		internal void UnsetPermanentFlags ()
		{
			((HashSet<string>) PermanentKeywords).Clear ();
			PermanentFlags = MessageFlags.None;
		}

		internal void UpdateIsNamespace (bool value)
		{
			IsNamespace = value;
		}

		internal void UpdateUnread (int count)
		{
			if (Unread == count)
				return;

			Unread = count;

			OnUnreadChanged ();
		}

		internal void UpdateUidNext (UniqueId uid)
		{
			if (UidNext.HasValue && UidNext.Value == uid)
				return;

			UidNext = uid;

			OnUidNextChanged ();
		}

		internal void UpdateAppendLimit (uint? limit)
		{
			AppendLimit = limit;
		}

		internal void UpdateSize (ulong? size)
		{
			if (Size == size)
				return;

			Size = size;

			OnSizeChanged ();
		}

		internal void UpdateId (string id)
		{
			if (Id == id)
				return;

			Id = id;

			OnIdChanged ();
		}

		internal void UpdateHighestModSeq (ulong modseq)
		{
			if (HighestModSeq == modseq)
				return;

			HighestModSeq = modseq;

			OnHighestModSeqChanged ();
		}

		internal void UpdateUidValidity (uint validity)
		{
			if (UidValidity == validity)
				return;

			UidValidity = validity;

			OnUidValidityChanged ();
		}

		internal void OnRenamed (ImapFolderConstructorArgs args)
		{
			var oldFullName = FullName;

			InitializeProperties (args);

			OnRenamed (oldFullName, FullName);
		}

		#endregion

		#endregion
	}
}
