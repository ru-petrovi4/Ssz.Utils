﻿//
// StoreLabelsRequest.cs
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
using System.Collections.Generic;

namespace MailKit {
	/// <summary>
	/// A request for storing GMail-style labels.
	/// </summary>
	/// <remarks>
	/// <para>A request suitable for storing GMail-style labels.</para>
	/// <para>This request is designed to be used with the <a href="Overload_MailKit_IMailFolder_Store.htm">Store</a> and
	/// <a href="Overload_MailKit_IMailFolder_StoreAsync.htm">StoreAsync</a> methods.</para>
	/// </remarks>
	public class StoreLabelsRequest : IStoreLabelsRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StoreLabelsRequest"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="StoreLabelsRequest"/>.
		/// </remarks>
		/// <param name="action">The store action to perform.</param>
		public StoreLabelsRequest (StoreAction action)
		{
			Labels = new HashSet<string> ();
			Action = action;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StoreLabelsRequest"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="StoreLabelsRequest"/>.
		/// </remarks>
		/// <param name="action">The store action to perform.</param>
		/// <param name="labels">The custom keywords to add, remove or set on the message.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="labels"/> is <c>null</c>.
		/// </exception>
		public StoreLabelsRequest (StoreAction action, IEnumerable<string> labels)
		{
			if (labels == null)
				throw new ArgumentNullException (nameof (labels));

			Labels = labels as ISet<string> ?? new HashSet<string> (labels);
			Action = action;
		}

		/// <summary>
		/// Get the store action to perform.
		/// </summary>
		/// <remarks>
		/// Gets the store action to perform.
		/// </remarks>
		/// <value>The store action.</value>
		public StoreAction Action {
			get; private set;
		}

		/// <summary>
		/// Get the GMail-style labels to add, remove or set on the message.
		/// </summary>
		/// <remarks>
		/// Gets the GMail-style labels to add, remove or set on the message.
		/// </remarks>
		/// <value>The GMail-style labels.</value>
		public ISet<string> Labels {
			get;
		}

		/// <summary>
		/// Get or set whether the store operation should run silently.
		/// </summary>
		/// <remarks>
		/// <para>Gets or sets whether the store operation should run silently.</para>
		/// <para>Normally, when flags or keywords are changed on a message, a <see cref="IMailFolder.MessageLabelsChanged"/> event is emitted.
		/// By setting <see cref="Silent"/> to <c>true</c>, this event will not be emitted as a result of this store operation.</para>
		/// </remarks>
		/// <value><c>true</c> if the store operation should run silently (not emitting events for label changes); otherwise, <c>false</c>.</value>
		public bool Silent {
			get; set;
		}

		/// <summary>
		/// Get or set a mod-sequence number that the store operation should use to decide if the labels of a message should be updated or not.
		/// </summary>
		/// <remarks>
		/// <para>Gets or sets a mod-sequence number that the store operation should use to decide if the labels of a message should be updated or not.</para>
		/// <para>For each message specified in the message set, the server performs the following. If the mod-sequence of every metadata item of the
		/// message affected by the store operation is equal to or less than the specified <see cref="UnchangedSince"/> value, then the requested operation
		/// is performed.</para>
		/// <para>However, if the mod-sequence of any metadata item of the message is greater than the specified <see cref="UnchangedSince"/> value, then the
		/// requested operation WILL NOT be performed. In this case, the mod-sequence attribute of the message is not updated, and the message index
		/// (or unique identifier in cases where <see cref="IMailFolder.Store(IList{UniqueId}, IStoreLabelsRequest, System.Threading.CancellationToken)"/> or
		/// <see cref="IMailFolder.StoreAsync(IList{UniqueId}, IStoreLabelsRequest, System.Threading.CancellationToken)"/> is used) is added to the list of
		/// messages that failed the UNCHANGEDSINCE test.</para>
		/// <note type="note">The <see cref="UnchangedSince"/> mod-sequence number can only be used if the server supports the <see cref="FolderFeature.ModSequences"/>
		/// feature.</note>
		/// </remarks>
		/// <value>The mod-sequence number.</value>
		public ulong? UnchangedSince {
			get; set;
		}
	}
}
