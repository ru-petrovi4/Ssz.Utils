﻿//
// SaslMechanismPlain.cs
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
using System.Net;
using System.Text;
using System.Threading;

namespace MailKit.Security {
	/// <summary>
	/// The PLAIN SASL mechanism.
	/// </summary>
	/// <remarks>
	/// The PLAIN SASL mechanism provides little protection over the use
	/// of plain-text passwords by combining the user name and password and
	/// obscuring them within a base64-encoded blob and should be avoided
	/// unless used in combination with an SSL or TLS connection.
	/// </remarks>
	public class SaslMechanismPlain : SaslMechanism
	{
		readonly Encoding encoding;

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Security.SaslMechanismPlain"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new PLAIN SASL context.
		/// </remarks>
		/// <param name="encoding">The encoding to use for the user's credentials.</param>
		/// <param name="credentials">The user's credentials.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="encoding"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="credentials"/> is <c>null</c>.</para>
		/// </exception>
		public SaslMechanismPlain (Encoding encoding, NetworkCredential credentials) : base (credentials)
		{
			if (encoding == null)
				throw new ArgumentNullException (nameof (encoding));

			this.encoding = encoding;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Security.SaslMechanismPlain"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new PLAIN SASL context.
		/// </remarks>
		/// <param name="encoding">The encoding to use for the user's credentials.</param>
		/// <param name="userName">The user name.</param>
		/// <param name="password">The password.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="encoding"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="userName"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="password"/> is <c>null</c>.</para>
		/// </exception>
		public SaslMechanismPlain (Encoding encoding, string userName, string password) : base (userName, password)
		{
			if (encoding == null)
				throw new ArgumentNullException (nameof (encoding));

			this.encoding = encoding;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Security.SaslMechanismPlain"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new PLAIN SASL context.
		/// </remarks>
		/// <param name="credentials">The user's credentials.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="credentials"/> is <c>null</c>.
		/// </exception>
		public SaslMechanismPlain (NetworkCredential credentials) : this (Encoding.UTF8, credentials)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Security.SaslMechanismPlain"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new PLAIN SASL context.
		/// </remarks>
		/// <param name="userName">The user name.</param>
		/// <param name="password">The password.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="userName"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="password"/> is <c>null</c>.</para>
		/// </exception>
		public SaslMechanismPlain (string userName, string password) : this (Encoding.UTF8, userName, password)
		{
		}

		/// <summary>
		/// Get or set the authorization identifier.
		/// </summary>
		/// <remarks>
		/// The authorization identifier is the desired user account that the server should use
		/// for all accesses. This is separate from the user name used for authentication.
		/// </remarks>
		/// <value>The authorization identifier.</value>
		public string AuthorizationId {
			get; set;
		}

		/// <summary>
		/// Get the name of the SASL mechanism.
		/// </summary>
		/// <remarks>
		/// Gets the name of the SASL mechanism.
		/// </remarks>
		/// <value>The name of the SASL mechanism.</value>
		public override string MechanismName {
			get { return "PLAIN"; }
		}

		/// <summary>
		/// Get whether or not the mechanism supports an initial response (SASL-IR).
		/// </summary>
		/// <remarks>
		/// <para>Gets whether or not the mechanism supports an initial response (SASL-IR).</para>
		/// <para>SASL mechanisms that support sending an initial client response to the server
		/// should return <value>true</value>.</para>
		/// </remarks>
		/// <value><c>true</c> if the mechanism supports an initial response; otherwise, <c>false</c>.</value>
		public override bool SupportsInitialResponse {
			get { return true; }
		}

		/// <summary>
		/// Parse the server's challenge token and return the next challenge response.
		/// </summary>
		/// <remarks>
		/// Parses the server's challenge token and returns the next challenge response.
		/// </remarks>
		/// <returns>The next challenge response.</returns>
		/// <param name="token">The server's challenge token.</param>
		/// <param name="startIndex">The index into the token specifying where the server's challenge begins.</param>
		/// <param name="length">The length of the server's challenge.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.NotSupportedException">
		/// The SASL mechanism does not support SASL-IR.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="SaslException">
		/// An error has occurred while parsing the server's challenge token.
		/// </exception>
		protected override byte[] Challenge (byte[] token, int startIndex, int length, CancellationToken cancellationToken)
		{
			if (IsAuthenticated)
				return null;

			var authzid = encoding.GetBytes (AuthorizationId ?? string.Empty);
			var authcid = encoding.GetBytes (Credentials.UserName);
			var passwd = encoding.GetBytes (Credentials.Password);
			var buffer = new byte[authzid.Length + authcid.Length + passwd.Length + 2];
			int offset = 0;

			for (int i = 0; i < authzid.Length; i++)
				buffer[offset++] = authzid[i];

			buffer[offset++] = 0;
			for (int i = 0; i < authcid.Length; i++)
				buffer[offset++] = authcid[i];

			buffer[offset++] = 0;
			for (int i = 0; i < passwd.Length; i++)
				buffer[offset++] = passwd[i];

			Array.Clear (passwd, 0, passwd.Length);

			IsAuthenticated = true;

			return buffer;
		}
	}
}
