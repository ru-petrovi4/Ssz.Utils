﻿//
// IMailService.cs
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
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using SslProtocols = System.Security.Authentication.SslProtocols;

using MailKit.Net.Proxy;

using MailKit.Security;

namespace MailKit {
	/// <summary>
	/// An interface for message services such as SMTP, POP3, or IMAP.
	/// </summary>
	/// <remarks>
	/// Implemented by <see cref="MailKit.Net.Imap.ImapClient"/>,
	/// <see cref="MailKit.Net.Pop3.Pop3Client"/> and
	/// <see cref="MailKit.Net.Smtp.SmtpClient"/>.
	/// </remarks>
	public interface IMailService : IDisposable
	{
		/// <summary>
		/// Get an object that can be used to synchronize access to the folder.
		/// </summary>
		/// <remarks>
		/// Gets an object that can be used to synchronize access to the folder.
		/// </remarks>
		/// <value>The sync root.</value>
		object SyncRoot { get; }

		/// <summary>
		/// Get or set the set of enabled SSL and/or TLS protocol versions that the client is allowed to use.
		/// </summary>
		/// <remarks>
		/// <para>By default, MailKit initializes this value to enable only TLS v1.2 and greater.
		/// TLS v1.1, TLS v1.0 and all versions of SSL are not enabled by default due to them all being
		/// susceptible to security vulnerabilities such as POODLE.</para>
		/// <para>This property should be set before calling any of the
		/// <a href="Overload_MailKit_IMailService_Connect.htm">Connect</a> methods.</para>
		/// </remarks>
		/// <value>The SSL and TLS protocol versions that are supported.</value>
		SslProtocols SslProtocols { get; set; }

#if NET5_0_OR_GREATER
		/// <summary>
		/// Get or set the cipher suites allowed to be used when negotiating an SSL or TLS connection.
		/// </summary>
		/// <remarks>
		/// Specifies the cipher suites allowed to be used when negotiating an SSL or TLS connection.
		/// When set to <c>null</c>, the operating system default is used. Use extreme caution when
		/// changing this setting.
		/// </remarks>
		/// <value>The cipher algorithms allowed for use when negotiating SSL or TLS encryption.</value>
		CipherSuitesPolicy SslCipherSuitesPolicy { get; set; }

		/// <summary>
		/// Get the negotiated SSL or TLS cipher suite.
		/// </summary>
		/// <remarks>
		/// Gets the negotiated SSL or TLS cipher suite once an SSL or TLS connection has been made.
		/// </remarks>
		/// <value>The negotiated SSL or TLS cipher suite.</value>
		TlsCipherSuite? SslCipherSuite { get; }
#endif

		/// <summary>
		/// Get or set the client SSL certificates.
		/// </summary>
		/// <remarks>
		/// <para>Some servers may require the client SSL certificates in order
		/// to allow the user to connect.</para>
		/// <para>This property should be set before calling any of the
		/// <a href="Overload_MailKit_IMailService_Connect.htm">Connect</a> methods.</para>
		/// </remarks>
		/// <value>The client SSL certificates.</value>
		X509CertificateCollection ClientCertificates { get; set; }

		/// <summary>
		/// Get or set whether connecting via SSL/TLS should check certificate revocation.
		/// </summary>
		/// <remarks>
		/// <para>Gets or sets whether connecting via SSL/TLS should check certificate revocation.</para>
		/// <para>Normally, the value of this property should be set to <c>true</c> (the default) for security
		/// reasons, but there are times when it may be necessary to set it to <c>false</c>.</para>
		/// <para>For example, most Certificate Authorities are probably pretty good at keeping their CRL and/or
		/// OCSP servers up 24/7, but occasionally they do go down or are otherwise unreachable due to other
		/// network problems between the client and the Certificate Authority. When this happens, it becomes
		/// impossible to check the revocation status of one or more of the certificates in the chain
		/// resulting in an <see cref="SslHandshakeException"/> being thrown in the
		/// <a href="Overload_MailKit_IMailService_Connect.htm">Connect</a> method. If this becomes a problem,
		/// it may become desirable to set <see cref="CheckCertificateRevocation"/> to <c>false</c>.</para>
		/// </remarks>
		/// <value><c>true</c> if certificate revocation should be checked; otherwise, <c>false</c>.</value>
		bool CheckCertificateRevocation { get; set; }

		/// <summary>
		/// Get or set a callback function to validate the server certificate.
		/// </summary>
		/// <remarks>
		/// <para>Gets or sets a callback function to validate the server certificate.</para>
		/// <para>This property should be set before calling any of the
		/// <a href="Overload_MailKit_IMailService_Connect.htm">Connect</a> methods.</para>
		/// </remarks>
		/// <example>
		/// <code language="c#" source="Examples\SslCertificateValidation.cs"/>
		/// </example>
		/// <value>The server certificate validation callback function.</value>
		RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

		/// <summary>
		/// Get or set the local IP end point to use when connecting to a remote host.
		/// </summary>
		/// <remarks>
		/// Gets or sets the local IP end point to use when connecting to a remote host.
		/// </remarks>
		/// <value>The local IP end point or <c>null</c> to use the default end point.</value>
		IPEndPoint LocalEndPoint { get; set; }

		/// <summary>
		/// Get or set the proxy client to use when connecting to a remote host.
		/// </summary>
		/// <remarks>
		/// Gets or sets the proxy client to use when connecting to a remote host via any of the
		/// <a href="Overload_MailKit_IMailService_Connect.htm">Connect</a> methods.
		/// </remarks>
		/// <value>The proxy client.</value>
		IProxyClient ProxyClient { get; set; }

		/// <summary>
		/// Get the authentication mechanisms supported by the message service.
		/// </summary>
		/// <remarks>
		/// The authentication mechanisms are queried durring the
		/// <a href="Overload_MailKit_IMailService_Connect.htm">Connect</a> method.
		/// </remarks>
		/// <value>The supported authentication mechanisms.</value>
		HashSet<string> AuthenticationMechanisms { get; }

		/// <summary>
		/// Get whether or not the client is currently authenticated with the mail server.
		/// </summary>
		/// <remarks>
		/// <para>Gets whether or not the client is currently authenticated with the mail server.</para>
		/// <para>To authenticate with the mail server, use one of the
		/// <a href="Overload_MailKit_MailService_Authenticate.htm">Authenticate</a> methods
		/// or any of the Async alternatives.</para>
		/// </remarks>
		/// <value><c>true</c> if the client is authenticated; otherwise, <c>false</c>.</value>
		bool IsAuthenticated { get; }

		/// <summary>
		/// Get whether or not the service is currently connected.
		/// </summary>
		/// <remarks>
		/// <para>The <see cref="IsConnected"/> state is set to <c>true</c> immediately after
		/// one of the <a href="Overload_MailKit_IMailService_Connect.htm">Connect</a>
		/// methods succeeds and is not set back to <c>false</c> until either the client
		/// is disconnected via <see cref="Disconnect(bool,CancellationToken)"/> or until a
		/// <see cref="ProtocolException"/> is thrown while attempting to read or write to
		/// the underlying network socket.</para>
		/// <para>When an <see cref="ProtocolException"/> is caught, the connection state of the
		/// <see cref="IMailService"/> should be checked before continuing.</para>
		/// </remarks>
		/// <value><c>true</c> if the service connected; otherwise, <c>false</c>.</value>
		bool IsConnected { get; }

		/// <summary>
		/// Get whether or not the connection is secure (typically via SSL or TLS).
		/// </summary>
		/// <remarks>
		/// Gets whether or not the connection is secure (typically via SSL or TLS).
		/// </remarks>
		/// <value><c>true</c> if the connection is secure; otherwise, <c>false</c>.</value>
		bool IsSecure { get; }

		/// <summary>
		/// Get whether or not the connection is encrypted (typically via SSL or TLS).
		/// </summary>
		/// <remarks>
		/// Gets whether or not the connection is encrypted (typically via SSL or TLS).
		/// </remarks>
		/// <value><c>true</c> if the connection is encrypted; otherwise, <c>false</c>.</value>
		bool IsEncrypted { get; }

		/// <summary>
		/// Get whether or not the connection is signed (typically via SSL or TLS).
		/// </summary>
		/// <remarks>
		/// Gets whether or not the connection is signed (typically via SSL or TLS).
		/// </remarks>
		/// <value><c>true</c> if the connection is signed; otherwise, <c>false</c>.</value>
		bool IsSigned { get; }

		/// <summary>
		/// Get the negotiated SSL or TLS protocol version.
		/// </summary>
		/// <remarks>
		/// <para>Gets the negotiated SSL or TLS protocol version once an SSL or TLS connection has been made.</para>
		/// </remarks>
		/// <value>The negotiated SSL or TLS protocol version.</value>
		SslProtocols SslProtocol { get; }

		/// <summary>
		/// Get the negotiated SSL or TLS cipher algorithm.
		/// </summary>
		/// <remarks>
		/// Gets the negotiated SSL or TLS cipher algorithm once an SSL or TLS connection has been made.
		/// </remarks>
		/// <value>The negotiated SSL or TLS cipher algorithm.</value>
		CipherAlgorithmType? SslCipherAlgorithm { get; }

		/// <summary>
		/// Get the negotiated SSL or TLS cipher algorithm strength.
		/// </summary>
		/// <remarks>
		/// Gets the negotiated SSL or TLS cipher algorithm strength once an SSL or TLS connection has been made.
		/// </remarks>
		/// <value>The negotiated SSL or TLS cipher algorithm strength.</value>
		int? SslCipherStrength { get; }

		/// <summary>
		/// Get the negotiated SSL or TLS hash algorithm.
		/// </summary>
		/// <remarks>
		/// Gets the negotiated SSL or TLS hash algorithm once an SSL or TLS connection has been made.
		/// </remarks>
		/// <value>The negotiated SSL or TLS hash algorithm.</value>
		HashAlgorithmType? SslHashAlgorithm { get; }

		/// <summary>
		/// Get the negotiated SSL or TLS hash algorithm strength.
		/// </summary>
		/// <remarks>
		/// Gets the negotiated SSL or TLS hash algorithm strength once an SSL or TLS connection has been made.
		/// </remarks>
		/// <value>The negotiated SSL or TLS hash algorithm strength.</value>
		int? SslHashStrength { get; }

		/// <summary>
		/// Get the negotiated SSL or TLS key exchange algorithm.
		/// </summary>
		/// <remarks>
		/// Gets the negotiated SSL or TLS key exchange algorithm once an SSL or TLS connection has been made.
		/// </remarks>
		/// <value>The negotiated SSL or TLS key exchange algorithm.</value>
		ExchangeAlgorithmType? SslKeyExchangeAlgorithm { get; }

		/// <summary>
		/// Get the negotiated SSL or TLS key exchange algorithm strength.
		/// </summary>
		/// <remarks>
		/// Gets the negotiated SSL or TLS key exchange algorithm strength once an SSL or TLS connection has been made.
		/// </remarks>
		/// <value>The negotiated SSL or TLS key exchange algorithm strength.</value>
		int? SslKeyExchangeStrength { get; }

		/// <summary>
		/// Get or set the timeout for network streaming operations, in milliseconds.
		/// </summary>
		/// <remarks>
		/// Gets or sets the underlying socket stream's <see cref="System.IO.Stream.ReadTimeout"/>
		/// and <see cref="System.IO.Stream.WriteTimeout"/> values.
		/// </remarks>
		/// <value>The timeout in milliseconds.</value>
		int Timeout { get; set; }

		/// <summary>
		/// Establish a connection to the specified mail server.
		/// </summary>
		/// <remarks>
		/// <para>Establish a connection to the specified mail server.</para>
		/// <para>If a successful connection is made, the <see cref="AuthenticationMechanisms"/>
		/// property will be populated.</para>
		/// </remarks>
		/// <param name="host">The host name to connect to.</param>
		/// <param name="port">The port to connect to. If the specified port is <c>0</c>, then the default port will be used.</param>
		/// <param name="useSsl"><value>true</value> if the client should make an SSL-wrapped connection to the server; otherwise, <value>false</value>.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="host"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="port"/> is not between <c>0</c> and <c>65535</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// The <paramref name="host"/> is a zero-length string.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="CommandException">
		/// The command was rejected by the mail server.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		void Connect (string host, int port, bool useSsl, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously establish a connection to the specified mail server.
		/// </summary>
		/// <remarks>
		/// <para>Asynchronously establishes a connection to the specified mail server.</para>
		/// <para>If a successful connection is made, the <see cref="AuthenticationMechanisms"/>
		/// property will be populated.</para>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="host">The host name to connect to.</param>
		/// <param name="port">The port to connect to. If the specified port is <c>0</c>, then the default port will be used.</param>
		/// <param name="useSsl"><value>true</value> if the client should make an SSL-wrapped connection to the server; otherwise, <value>false</value>.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// The <paramref name="host"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="port"/> is not between <c>0</c> and <c>65535</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// The <paramref name="host"/> is a zero-length string.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="IMailService"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		Task ConnectAsync (string host, int port, bool useSsl, CancellationToken cancellationToken = default);

		/// <summary>
		/// Establish a connection to the specified mail server.
		/// </summary>
		/// <remarks>
		/// <para>Establish a connection to the specified mail server.</para>
		/// <para>If a successful connection is made, the <see cref="AuthenticationMechanisms"/>
		/// property will be populated.</para>
		/// </remarks>
		/// <param name="host">The host name to connect to.</param>
		/// <param name="port">The port to connect to. If the specified port is <c>0</c>, then the default port will be used.</param>
		/// <param name="options">The secure socket options to when connecting.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="host"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="port"/> is not between <c>0</c> and <c>65535</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// The <paramref name="host"/> is a zero-length string.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="CommandException">
		/// The command was rejected by the mail server.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		void Connect (string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously establish a connection to the specified mail server.
		/// </summary>
		/// <remarks>
		/// <para>Asynchronously establishes a connection to the specified mail server.</para>
		/// <para>If a successful connection is made, the <see cref="AuthenticationMechanisms"/>
		/// property will be populated.</para>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="host">The host name to connect to.</param>
		/// <param name="port">The port to connect to. If the specified port is <c>0</c>, then the default port will be used.</param>
		/// <param name="options">The secure socket options to when connecting.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// The <paramref name="host"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="port"/> is not between <c>0</c> and <c>65535</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// The <paramref name="host"/> is a zero-length string.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="IMailService"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		Task ConnectAsync (string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default);

		/// <summary>
		/// Establish a connection to the specified mail server using the provided socket.
		/// </summary>
		/// <remarks>
		/// <para>Establish a connection to the specified mail server using the provided socket.</para>
		/// <para>If a successful connection is made, the <see cref="AuthenticationMechanisms"/>
		/// property will be populated.</para>
		/// </remarks>
		/// <param name="socket">The socket to use for the connection.</param>
		/// <param name="host">The host name to connect to.</param>
		/// <param name="port">The port to connect to. If the specified port is <c>0</c>, then the default port will be used.</param>
		/// <param name="options">The secure socket options to when connecting.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="socket"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="host"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="port"/> is not between <c>0</c> and <c>65535</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para><paramref name="socket"/> is not connected.</para>
		/// <para>-or-</para>
		/// <para>The <paramref name="host"/> is a zero-length string.</para>
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="CommandException">
		/// The command was rejected by the mail server.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		void Connect (Socket socket, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously establish a connection to the specified mail server using the provided socket.
		/// </summary>
		/// <remarks>
		/// <para>Asynchronously establishes a connection to the specified mail server using the provided socket.</para>
		/// <para>If a successful connection is made, the <see cref="AuthenticationMechanisms"/>
		/// property will be populated.</para>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="socket">The socket to use for the connection.</param>
		/// <param name="host">The host name to connect to.</param>
		/// <param name="port">The port to connect to. If the specified port is <c>0</c>, then the default port will be used.</param>
		/// <param name="options">The secure socket options to when connecting.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="socket"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="host"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="port"/> is not between <c>0</c> and <c>65535</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para><paramref name="socket"/> is not connected.</para>
		/// <para>-or-</para>
		/// <para>The <paramref name="host"/> is a zero-length string.</para>
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="CommandException">
		/// The command was rejected by the mail server.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		Task ConnectAsync (Socket socket, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default);

		/// <summary>
		/// Establish a connection to the specified mail server using the provided stream.
		/// </summary>
		/// <remarks>
		/// <para>Establish a connection to the specified mail server using the provided stream.</para>
		/// <para>If a successful connection is made, the <see cref="AuthenticationMechanisms"/>
		/// property will be populated.</para>
		/// </remarks>
		/// <param name="stream">The stream to use for the connection.</param>
		/// <param name="host">The host name to connect to.</param>
		/// <param name="port">The port to connect to. If the specified port is <c>0</c>, then the default port will be used.</param>
		/// <param name="options">The secure socket options to when connecting.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="stream"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="host"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="port"/> is not between <c>0</c> and <c>65535</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// The <paramref name="host"/> is a zero-length string.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="CommandException">
		/// The command was rejected by the mail server.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		void Connect (Stream stream, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously establish a connection to the specified mail server using the provided stream.
		/// </summary>
		/// <remarks>
		/// <para>Asynchronously establishes a connection to the specified mail server using the provided stream.</para>
		/// <para>If a successful connection is made, the <see cref="AuthenticationMechanisms"/>
		/// property will be populated.</para>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="stream">The stream to use for the connection.</param>
		/// <param name="host">The host name to connect to.</param>
		/// <param name="port">The port to connect to. If the specified port is <c>0</c>, then the default port will be used.</param>
		/// <param name="options">The secure socket options to when connecting.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="stream"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="host"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="port"/> is not between <c>0</c> and <c>65535</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// The <paramref name="host"/> is a zero-length string.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="CommandException">
		/// The command was rejected by the mail server.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		Task ConnectAsync (Stream stream, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default);

		/// <summary>
		/// Authenticate using the supplied credentials.
		/// </summary>
		/// <remarks>
		/// <para>Authenticates using the supplied credentials.</para>
		/// <para>If the server supports one or more SASL authentication mechanisms, then
		/// the SASL mechanisms that both the client and server support (not including any
		/// OAUTH mechanisms) are tried in order of greatest security to weakest security.
		/// Once a SASL authentication mechanism is found that both client and server support,
		/// the credentials are used to authenticate.</para>
		/// <para>If the server does not support SASL or if no common SASL mechanisms
		/// can be found, then the default login command is used as a fallback.</para>
		/// </remarks>
		/// <param name="credentials">The user's credentials.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="credentials"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="IMailService"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="IMailService"/> is not connected.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		void Authenticate (ICredentials credentials, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously authenticate using the supplied credentials.
		/// </summary>
		/// <remarks>
		/// <para>Asynchronously authenticates using the supplied credentials.</para>
		/// <para>If the server supports one or more SASL authentication mechanisms, then
		/// the SASL mechanisms that both the client and server support (not including any
		/// OAUTH mechanisms) are tried in order of greatest security to weakest security.
		/// Once a SASL authentication mechanism is found that both client and server support,
		/// the credentials are used to authenticate.</para>
		/// <para>If the server does not support SASL or if no common SASL mechanisms
		/// can be found, then the default login command is used as a fallback.</para>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="credentials">The user's credentials.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="credentials"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="IMailService"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="IMailService"/> is not connected.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		Task AuthenticateAsync (ICredentials credentials, CancellationToken cancellationToken = default);

		/// <summary>
		/// Authenticate using the supplied credentials.
		/// </summary>
		/// <remarks>
		/// <para>Authenticates using the supplied credentials.</para>
		/// <para>If the server supports one or more SASL authentication mechanisms, then
		/// the SASL mechanisms that both the client and server support (not including any
		/// OAUTH mechanisms) are tried in order of greatest security to weakest security.
		/// Once a SASL authentication mechanism is found that both client and server support,
		/// the credentials are used to authenticate.</para>
		/// <para>If the server does not support SASL or if no common SASL mechanisms
		/// can be found, then the default login command is used as a fallback.</para>
		/// </remarks>
		/// <param name="encoding">The encoding to use for the user's credentials.</param>
		/// <param name="credentials">The user's credentials.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="encoding"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="credentials"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="IMailService"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="IMailService"/> is not connected.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		void Authenticate (Encoding encoding, ICredentials credentials, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously authenticate using the supplied credentials.
		/// </summary>
		/// <remarks>
		/// <para>Asynchronously authenticates using the supplied credentials.</para>
		/// <para>If the server supports one or more SASL authentication mechanisms, then
		/// the SASL mechanisms that both the client and server support (not including any
		/// OAUTH mechanisms) are tried in order of greatest security to weakest security.
		/// Once a SASL authentication mechanism is found that both client and server support,
		/// the credentials are used to authenticate.</para>
		/// <para>If the server does not support SASL or if no common SASL mechanisms
		/// can be found, then the default login command is used as a fallback.</para>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="encoding">The encoding to use for the user's credentials.</param>
		/// <param name="credentials">The user's credentials.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="encoding"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="credentials"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="IMailService"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="IMailService"/> is not connected.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="IMailService"/> is already authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		Task AuthenticateAsync (Encoding encoding, ICredentials credentials, CancellationToken cancellationToken = default);

		/// <summary>
		/// Authenticate using the specified user name and password.
		/// </summary>
		/// <remarks>
		/// <para>Authenticates using the supplied credentials.</para>
		/// <para>If the server supports one or more SASL authentication mechanisms, then
		/// the SASL mechanisms that both the client and server support (not including any
		/// OAUTH mechanisms) are tried in order of greatest security to weakest security.
		/// Once a SASL authentication mechanism is found that both client and server support,
		/// the credentials are used to authenticate.</para>
		/// <para>If the server does not support SASL or if no common SASL mechanisms
		/// can be found, then the default login command is used as a fallback.</para>
		/// <note type="tip">To prevent the usage of certain authentication mechanisms,
		/// simply remove them from the <see cref="AuthenticationMechanisms"/> hash set
		/// before calling this method.</note>
		/// </remarks>
		/// <param name="encoding">The encoding to use for the user's credentials.</param>
		/// <param name="userName">The user name.</param>
		/// <param name="password">The password.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="encoding"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="userName"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="password"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailService"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailService"/> is not connected or is already authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		void Authenticate (Encoding encoding, string userName, string password, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously authenticate using the specified user name and password.
		/// </summary>
		/// <remarks>
		/// <para>Asynchronously authenticates using the supplied credentials.</para>
		/// <para>If the server supports one or more SASL authentication mechanisms, then
		/// the SASL mechanisms that both the client and server support (not including any
		/// OAUTH mechanisms) are tried in order of greatest security to weakest security.
		/// Once a SASL authentication mechanism is found that both client and server support,
		/// the credentials are used to authenticate.</para>
		/// <para>If the server does not support SASL or if no common SASL mechanisms
		/// can be found, then the default login command is used as a fallback.</para>
		/// <note type="tip">To prevent the usage of certain authentication mechanisms,
		/// simply remove them from the <see cref="AuthenticationMechanisms"/> hash set
		/// before calling this method.</note>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="encoding">The encoding to use for the user's credentials.</param>
		/// <param name="userName">The user name.</param>
		/// <param name="password">The password.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="encoding"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="userName"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="password"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailService"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailService"/> is not connected or is already authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		Task AuthenticateAsync (Encoding encoding, string userName, string password, CancellationToken cancellationToken = default);

		/// <summary>
		/// Authenticate using the specified user name and password.
		/// </summary>
		/// <remarks>
		/// <para>Authenticates using the supplied credentials.</para>
		/// <para>If the server supports one or more SASL authentication mechanisms, then
		/// the SASL mechanisms that both the client and server support (not including any
		/// OAUTH mechanisms) are tried in order of greatest security to weakest security.
		/// Once a SASL authentication mechanism is found that both client and server support,
		/// the credentials are used to authenticate.</para>
		/// <para>If the server does not support SASL or if no common SASL mechanisms
		/// can be found, then the default login command is used as a fallback.</para>
		/// <note type="tip">To prevent the usage of certain authentication mechanisms,
		/// simply remove them from the <see cref="AuthenticationMechanisms"/> hash set
		/// before calling this method.</note>
		/// </remarks>
		/// <example>
		/// <code language="c#" source="Examples\SmtpExamples.cs" region="SendMessage"/>
		/// </example>
		/// <param name="userName">The user name.</param>
		/// <param name="password">The password.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="userName"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="password"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailService"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailService"/> is not connected or is already authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		void Authenticate (string userName, string password, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously authenticate using the specified user name and password.
		/// </summary>
		/// <remarks>
		/// <para>Asynchronously authenticates using the supplied credentials.</para>
		/// <para>If the server supports one or more SASL authentication mechanisms, then
		/// the SASL mechanisms that both the client and server support (not including any
		/// OAUTH mechanisms) are tried in order of greatest security to weakest security.
		/// Once a SASL authentication mechanism is found that both client and server support,
		/// the credentials are used to authenticate.</para>
		/// <para>If the server does not support SASL or if no common SASL mechanisms
		/// can be found, then the default login command is used as a fallback.</para>
		/// <note type="tip">To prevent the usage of certain authentication mechanisms,
		/// simply remove them from the <see cref="AuthenticationMechanisms"/> hash set
		/// before calling this method.</note>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="userName">The user name.</param>
		/// <param name="password">The password.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="userName"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="password"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailService"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailService"/> is not connected or is already authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		Task AuthenticateAsync (string userName, string password, CancellationToken cancellationToken = default);

		/// <summary>
		/// Authenticate using the specified SASL mechanism.
		/// </summary>
		/// <remarks>
		/// <para>Authenticates using the specified SASL mechanism.</para>
		/// <para>For a list of available SASL authentication mechanisms supported by the server,
		/// check the <see cref="AuthenticationMechanisms"/> property after the service has been
		/// connected.</para>
		/// </remarks>
		/// <param name="mechanism">The SASL mechanism.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="mechanism"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailService"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailService"/> is not connected or is already authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		void Authenticate (SaslMechanism mechanism, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously authenticate using the specified SASL mechanism.
		/// </summary>
		/// <remarks>
		/// <para>Authenticates using the specified SASL mechanism.</para>
		/// <para>For a list of available SASL authentication mechanisms supported by the server,
		/// check the <see cref="AuthenticationMechanisms"/> property after the service has been
		/// connected.</para>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="mechanism">The SASL mechanism.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="mechanism"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailService"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailService"/> is not connected or is already authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		Task AuthenticateAsync (SaslMechanism mechanism, CancellationToken cancellationToken = default);

		/// <summary>
		/// Disconnect the service.
		/// </summary>
		/// <remarks>
		/// <para>Disconnects from the service.</para>
		/// <para>If <paramref name="quit"/> is <c>true</c>, a "QUIT" command will be issued in order to disconnect cleanly.</para>
		/// </remarks>
		/// <param name="quit">If set to <c>true</c>, a "QUIT" command will be issued in order to disconnect cleanly.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="IMailService"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="IMailService"/> is not connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="CommandException">
		/// The command was rejected by the mail server.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		void Disconnect (bool quit, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously disconnect the service.
		/// </summary>
		/// <remarks>
		/// <para>Asynchronously disconnects from the service.</para>
		/// <para>If <paramref name="quit"/> is <c>true</c>, a "QUIT" command will be issued in order to disconnect cleanly.</para>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="quit">If set to <c>true</c>, a logout/quit command will be issued in order to disconnect cleanly.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="IMailService"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="IMailService"/> is not connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="CommandException">
		/// The command was rejected by the mail server.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		Task DisconnectAsync (bool quit, CancellationToken cancellationToken = default);

		/// <summary>
		/// Ping the message service to keep the connection alive.
		/// </summary>
		/// <remarks>
		/// Mail servers, if left idle for too long, will automatically drop the connection.
		/// </remarks>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="IMailService"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="IMailService"/> is not connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="CommandException">
		/// The command was rejected by the mail server.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		void NoOp (CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously ping the mail server to keep the connection alive.
		/// </summary>
		/// <remarks>
		/// Mail servers, if left idle for too long, will automatically drop the connection.
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="IMailService"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="IMailService"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="IMailService"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="CommandException">
		/// The command was rejected by the mail server.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		Task NoOpAsync (CancellationToken cancellationToken = default);

		/// <summary>
		/// Occurs when the client has been successfully connected.
		/// </summary>
		/// <remarks>
		/// The <see cref="Connected"/> event is raised when the client
		/// successfully connects to the mail server.
		/// </remarks>
		event EventHandler<ConnectedEventArgs> Connected;

		/// <summary>
		/// Occurs when the client has been disconnected.
		/// </summary>
		/// <remarks>
		/// The <see cref="Disconnected"/> event is raised whenever the client
		/// has been disconnected.
		/// </remarks>
		event EventHandler<DisconnectedEventArgs> Disconnected;

		/// <summary>
		/// Occurs when the client has been successfully authenticated.
		/// </summary>
		/// <remarks>
		/// The <see cref="Authenticated"/> event is raised whenever the client
		/// has been authenticated.
		/// </remarks>
		event EventHandler<AuthenticatedEventArgs> Authenticated;
	}
}
