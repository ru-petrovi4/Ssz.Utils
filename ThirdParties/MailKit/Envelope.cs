﻿//
// Envelope.cs
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
using System.Text;
using System.Linq;
using System.Collections.Generic;

using MimeKit;
using MimeKit.Utils;

namespace MailKit {
	/// <summary>
	/// A message envelope containing a brief summary of the message.
	/// </summary>
	/// <remarks>
	/// The envelope of a message contains information such as the
	/// date the message was sent, the subject of the message,
	/// the sender of the message, who the message was sent to,
	/// which message(s) the message may be in reply to,
	/// and the message id.
	/// </remarks>
	public class Envelope
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Envelope"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="Envelope"/>.
		/// </remarks>
		public Envelope ()
		{
			From = new InternetAddressList ();
			Sender = new InternetAddressList ();
			ReplyTo = new InternetAddressList ();
			To = new InternetAddressList ();
			Cc = new InternetAddressList ();
			Bcc = new InternetAddressList ();
		}

		/// <summary>
		/// Gets the address(es) that the message is from.
		/// </summary>
		/// <remarks>
		/// Gets the address(es) that the message is from.
		/// </remarks>
		/// <value>The address(es) that the message is from.</value>
		public InternetAddressList From {
			get; private set;
		}

		/// <summary>
		/// Gets the actual sender(s) of the message.
		/// </summary>
		/// <remarks>
		/// The senders may differ from the addresses in <see cref="From"/> if
		/// the message was sent by someone on behalf of someone else.
		/// </remarks>
		/// <value>The actual sender(s) of the message.</value>
		public InternetAddressList Sender {
			get; private set;
		}

		/// <summary>
		/// Gets the address(es) that replies should be sent to.
		/// </summary>
		/// <remarks>
		/// The senders of the message may prefer that replies are sent
		/// somewhere other than the address they used to send the message.
		/// </remarks>
		/// <value>The address(es) that replies should be sent to.</value>
		public InternetAddressList ReplyTo {
			get; private set;
		}

		/// <summary>
		/// Gets the list of addresses that the message was sent to.
		/// </summary>
		/// <remarks>
		/// Gets the list of addresses that the message was sent to.
		/// </remarks>
		/// <value>The address(es) that the message was sent to.</value>
		public InternetAddressList To {
			get; private set;
		}

		/// <summary>
		/// Gets the list of addresses that the message was carbon-copied to.
		/// </summary>
		/// <remarks>
		/// Gets the list of addresses that the message was carbon-copied to.
		/// </remarks>
		/// <value>The address(es) that the message was carbon-copied to.</value>
		public InternetAddressList Cc {
			get; private set;
		}

		/// <summary>
		/// Gets the list of addresses that the message was blind-carbon-copied to.
		/// </summary>
		/// <remarks>
		/// Gets the list of addresses that the message was blind-carbon-copied to.
		/// </remarks>
		/// <value>The address(es) that the message was carbon-copied to.</value>
		public InternetAddressList Bcc {
			get; private set;
		}

		/// <summary>
		/// The Message-Id that the message is replying to.
		/// </summary>
		/// <remarks>
		/// The Message-Id that the message is replying to.
		/// </remarks>
		/// <value>The Message-Id that the message is replying to.</value>
		public string InReplyTo {
			get; set;
		}

		/// <summary>
		/// Gets the date that the message was sent on, if available.
		/// </summary>
		/// <remarks>
		/// Gets the date that the message was sent on, if available.
		/// </remarks>
		/// <value>The date the message was sent.</value>
		public DateTimeOffset? Date {
			get; set;
		}

		/// <summary>
		/// Gets the ID of the message, if available.
		/// </summary>
		/// <remarks>
		/// Gets the ID of the message, if available.
		/// </remarks>
		/// <value>The message identifier.</value>
		public string MessageId {
			get; set;
		}

		/// <summary>
		/// Gets the subject of the message.
		/// </summary>
		/// <remarks>
		/// Gets the subject of the message.
		/// </remarks>
		/// <value>The subject.</value>
		public string Subject {
			get; set;
		}

		static void EncodeMailbox (StringBuilder builder, MailboxAddress mailbox)
		{
			builder.Append ('(');

			if (mailbox.Name != null) {
				MimeUtils.AppendQuoted (builder, mailbox.Name);
				builder.Append (' ');
			} else {
				builder.Append ("NIL ");
			}

			if (mailbox.Route.Count != 0) {
				MimeUtils.AppendQuoted (builder, mailbox.Route.ToString ());
				builder.Append (' ');
			} else {
				builder.Append ("NIL ");
			}

			int at = mailbox.Address.LastIndexOf ('@');

			if (at >= 0) {
				var domain = mailbox.Address.Substring (at + 1);
				var user = mailbox.Address.Substring (0, at);

				MimeUtils.AppendQuoted (builder, user);
				builder.Append (' ');
				MimeUtils.AppendQuoted (builder, domain);
			} else {
				MimeUtils.AppendQuoted (builder, mailbox.Address);
				builder.Append (" \"localhost\"");
			}

			builder.Append (')');
		}

		static void EncodeInternetAddressListAddresses (StringBuilder builder, InternetAddressList addresses)
		{
			foreach (var addr in addresses) {
				if (addr is MailboxAddress mailbox)
					EncodeMailbox (builder, mailbox);
				else if (addr is GroupAddress group)
					EncodeGroup (builder, group);
			}
		}

		static void EncodeGroup (StringBuilder builder, GroupAddress group)
		{
			builder.Append ("(NIL NIL ");
			MimeUtils.AppendQuoted (builder, group.Name);
			builder.Append (" NIL)");
			EncodeInternetAddressListAddresses (builder, group.Members);
			builder.Append ("(NIL NIL NIL NIL)");
		}

		static void EncodeAddressList (StringBuilder builder, InternetAddressList list)
		{
			builder.Append ('(');
			EncodeInternetAddressListAddresses (builder, list);
			builder.Append (')');
		}

		internal void Encode (StringBuilder builder)
		{
			builder.Append ('(');

			if (Date.HasValue) {
				builder.Append ('"');
				builder.Append (DateUtils.FormatDate (Date.Value));
				builder.Append ("\" ");
			} else {
				builder.Append ("NIL ");
			}

			if (Subject != null) {
				MimeUtils.AppendQuoted (builder, Subject);
				builder.Append (' ');
			} else {
				builder.Append ("NIL ");
			}

			if (From.Count > 0) {
				EncodeAddressList (builder, From);
				builder.Append (' ');
			} else {
				builder.Append ("NIL ");
			}

			if (Sender.Count > 0) {
				EncodeAddressList (builder, Sender);
				builder.Append (' ');
			} else {
				builder.Append ("NIL ");
			}

			if (ReplyTo.Count > 0) {
				EncodeAddressList (builder, ReplyTo);
				builder.Append (' ');
			} else {
				builder.Append ("NIL ");
			}

			if (To.Count > 0) {
				EncodeAddressList (builder, To);
				builder.Append (' ');
			} else {
				builder.Append ("NIL ");
			}

			if (Cc.Count > 0) {
				EncodeAddressList (builder, Cc);
				builder.Append (' ');
			} else {
				builder.Append ("NIL ");
			}

			if (Bcc.Count > 0) {
				EncodeAddressList (builder, Bcc);
				builder.Append (' ');
			} else {
				builder.Append ("NIL ");
			}

			if (InReplyTo != null) {
				string inReplyTo;

				if (InReplyTo.Length > 1 && InReplyTo[0] != '<' && InReplyTo[InReplyTo.Length - 1] != '>')
					inReplyTo = '<' + InReplyTo + '>';
				else
					inReplyTo = InReplyTo;

				MimeUtils.AppendQuoted (builder, inReplyTo);
				builder.Append (' ');
			} else {
				builder.Append ("NIL ");
			}

			if (MessageId != null) {
				string messageId;

				if (MessageId.Length > 1 && MessageId[0] != '<' && MessageId[MessageId.Length - 1] != '>')
					messageId = '<' + MessageId + '>';
				else
					messageId = MessageId;

				MimeUtils.AppendQuoted (builder, messageId);
			} else {
				builder.Append ("NIL");
			}

			builder.Append (')');
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="MailKit.Envelope"/>.
		/// </summary>
		/// <remarks>
		/// <para>The returned string can be parsed by <see cref="TryParse(string,out Envelope)"/>.</para>
		/// <note type="warning">The syntax of the string returned, while similar to IMAP's ENVELOPE syntax,
		/// is not completely compatible.</note>
		/// </remarks>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="MailKit.Envelope"/>.</returns>
		public override string ToString ()
		{
			var builder = new StringBuilder ();

			Encode (builder);

			return builder.ToString ();
		}

		static bool IsNIL (string text, int index)
		{
			return string.Compare (text, index, "NIL", 0, 3, StringComparison.Ordinal) == 0;
		}

		static bool TryParse (string text, ref int index, out string nstring)
		{
			nstring = null;

			while (index < text.Length && text[index] == ' ')
				index++;

			if (index >= text.Length)
				return false;

			if (text[index] != '"') {
				if (index + 3 <= text.Length && IsNIL (text, index)) {
					index += 3;
					return true;
				}

				return false;
			}

			var token = new StringBuilder ();
			bool escaped = false;

			index++;

			while (index < text.Length) {
				if (text[index] == '"' && !escaped)
					break;

				if (escaped || text[index] != '\\') {
					token.Append (text[index]);
					escaped = false;
				} else {
					escaped = true;
				}

				index++;
			}

			if (index >= text.Length)
				return false;

			nstring = token.ToString ();

			index++;

			return true;
		}

		static bool TryParse (string text, ref int index, out InternetAddress addr)
		{
			addr = null;

			if (text[index] != '(')
				return false;

			index++;

			if (!TryParse (text, ref index, out string name))
				return false;

			if (!TryParse (text, ref index, out string route))
				return false;

			if (!TryParse (text, ref index, out string user))
				return false;

			if (!TryParse (text, ref index, out string domain))
				return false;

			while (index < text.Length && text[index] == ' ')
				index++;

			if (index >= text.Length || text[index] != ')')
				return false;

			index++;

			if (domain != null) {
				// Note: The serializer injects "localhost" as the domain when provided a UNIX mailbox or the special <> mailbox.
				var address = domain == "localhost" ? user : user + "@" + domain;

				if (route != null && DomainList.TryParse (route, out var domains))
					addr = new MailboxAddress (name, domains, address);
				else
					addr = new MailboxAddress (name, address);
			} else if (user != null) {
				addr = new GroupAddress (user);
			}

			return true;
		}

		static bool TryParse (string text, ref int index, out InternetAddressList list)
		{
			list = null;

			while (index < text.Length && text[index] == ' ')
				index++;

			if (index >= text.Length)
				return false;

			if (text[index] != '(') {
				if (index + 3 <= text.Length && IsNIL (text, index)) {
					list = new InternetAddressList ();
					index += 3;
					return true;
				}

				return false;
			}

			index++;

			if (index >= text.Length)
				return false;

			list = new InternetAddressList ();
			var stack = new List<InternetAddressList> ();
			int sp = 0;

			stack.Add (list);

			do {
				if (text[index] == ')')
					break;

				if (!TryParse (text, ref index, out InternetAddress addr))
					return false;

				if (addr != null) {
					stack[sp].Add (addr);

					if (addr is GroupAddress group) {
						stack.Add (group.Members);
						sp++;
					}
				} else if (sp > 0) {
					stack.RemoveAt (sp);
					sp--;
				}

				while (index < text.Length && text[index] == ' ')
					index++;
			} while (index < text.Length);

			// Note: technically, we should check that sp == 0 as well, since all groups should
			// be popped off the stack, but in the interest of being liberal in what we accept,
			// we'll ignore that.
			if (index >= text.Length)
				return false;

			index++;

			return true;
		}

		internal static bool TryParse (string text, ref int index, out Envelope envelope)
		{
			DateTimeOffset? date = null;

			envelope = null;

			while (index < text.Length && text[index] == ' ')
				index++;

			if (index >= text.Length || text[index] != '(') {
				if (index + 3 <= text.Length && IsNIL (text, index)) {
					index += 3;
					return true;
				}

				return false;
			}

			index++;

			if (!TryParse (text, ref index, out string nstring))
				return false;

			if (nstring != null) {
				if (!DateUtils.TryParse (nstring, out DateTimeOffset value))
					return false;

				date = value;
			}

			if (!TryParse (text, ref index, out string subject))
				return false;

			if (!TryParse (text, ref index, out InternetAddressList from))
				return false;

			if (!TryParse (text, ref index, out InternetAddressList sender))
				return false;

			if (!TryParse (text, ref index, out InternetAddressList replyto))
				return false;

			if (!TryParse (text, ref index, out InternetAddressList to))
				return false;

			if (!TryParse (text, ref index, out InternetAddressList cc))
				return false;

			if (!TryParse (text, ref index, out InternetAddressList bcc))
				return false;

			if (!TryParse (text, ref index, out string inreplyto))
				return false;

			if (!TryParse (text, ref index, out string messageid))
				return false;

			if (index >= text.Length || text[index] != ')')
				return false;

			index++;

			envelope = new Envelope {
				Date = date,
				Subject = subject,
				From = from,
				Sender = sender,
				ReplyTo = replyto,
				To = to,
				Cc = cc,
				Bcc = bcc,
				InReplyTo = inreplyto != null ? MimeUtils.EnumerateReferences (inreplyto).FirstOrDefault () ?? inreplyto : null,
				MessageId = messageid != null ? MimeUtils.EnumerateReferences (messageid).FirstOrDefault () ?? messageid : null
			};

			return true;
		}

		/// <summary>
		/// Tries to parse the given text into a new <see cref="MailKit.Envelope"/> instance.
		/// </summary>
		/// <remarks>
		/// <para>Parses an Envelope value from the specified text.</para>
		/// <note type="warning">This syntax, while similar to IMAP's ENVELOPE syntax, is not
		/// completely compatible.</note>
		/// </remarks>
		/// <returns><c>true</c>, if the envelope was successfully parsed, <c>false</c> otherwise.</returns>
		/// <param name="text">The text to parse.</param>
		/// <param name="envelope">The parsed envelope.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="text"/> is <c>null</c>.
		/// </exception>
		public static bool TryParse (string text, out Envelope envelope)
		{
			if (text == null)
				throw new ArgumentNullException (nameof (text));

			int index = 0;

			return TryParse (text, ref index, out envelope) && index == text.Length;
		}
	}
}
