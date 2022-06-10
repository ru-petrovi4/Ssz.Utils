﻿/******************************************************************************
* The MIT License
* Copyright (c) 2003 Novell Inc.  www.novell.com
*
* Permission is hereby granted, free of charge, to any person obtaining  a copy
* of this software and associated documentation files (the Software), to deal
* in the Software without restriction, including  without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to  permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*******************************************************************************/

using Novell.Directory.Ldap.Asn1;
using Novell.Directory.Ldap.Rfc2251;
using Novell.Directory.Ldap.Utilclass;
using System;

namespace Novell.Directory.Ldap
{
    /// <summary>
    ///     A message received from an LdapServer
    ///     in response to an asynchronous request.
    /// </summary>
    /// <seealso cref="LdapConnection.SearchAsync">
    /// </seealso>
    /*
        * Note: Exceptions generated by the reader thread are returned
        * to the application as an exception in an LdapResponse.  Thus
        * if <code>exception</code> has a value, it is not a server response,
        * but instad an exception returned to the application from the API.
        */
    public class LdapResponse : LdapMessage
    {
        public override DebugId DebugId { get; } = DebugId.ForType<LdapResponse>();
        private readonly InterThreadException _exception;

        /// <summary>
        ///     Creates an LdapResponse using an LdapException.
        ///     Used to wake up the user following an abandon.
        ///     Note: The abandon doesn't have to be user initiated
        ///     but may be the result of error conditions.
        ///     Referral information is available if this connection created solely
        ///     to follow a referral.
        /// </summary>
        /// <param name="ex">
        ///     The exception.
        /// </param>
        /// <param name="activeReferral">
        ///     The referral actually used to create the
        ///     connection.
        /// </param>
        public LdapResponse(InterThreadException ex, ReferralInfo activeReferral)
        {
            _exception = ex;
            ActiveReferral = activeReferral;
        }

        /// <summary>
        ///     Creates a response LdapMessage when receiving an asynchronous
        ///     response from a server.
        /// </summary>
        /// <param name="message">
        ///     The RfcLdapMessage from a server.
        /// </param>
        /*package*/
        internal LdapResponse(RfcLdapMessage message)
            : base(message)
        {
        }

        /// <summary>
        ///     Creates a SUCCESS response LdapMessage. Typically the response
        ///     comes from a source other than a BER encoded Ldap message,
        ///     such as from DSML.  Other values which are allowed in a response
        ///     are set to their empty values.
        /// </summary>
        /// <param name="type">
        ///     The message type as defined in LdapMessage.
        /// </param>
        /// <seealso cref="LdapMessage">
        /// </seealso>
        public LdapResponse(int type)
            : this(type, LdapException.Success, null, null)
        {
        }

        /// <summary>
        ///     Creates a response LdapMessage from parameters. Typically the data
        ///     comes from a source other than a BER encoded Ldap message,
        ///     such as from DSML.
        /// </summary>
        /// <param name="type">
        ///     The message type as defined in LdapMessage.
        /// </param>
        /// <param name="resultCode">
        ///     The result code as defined in LdapException.
        /// </param>
        /// <param name="matchedDn">
        ///     The name of the lowest entry that was matched
        ///     for some error result codes, an empty string
        ///     or. <code>null</code> if none.
        /// </param>
        /// <param name="serverMessage">
        ///     A diagnostic message returned by the server,
        ///     an empty string or. <code>null</code> if none.
        /// </param>
        /// <seealso cref="LdapMessage">
        /// </seealso>
        /// <seealso cref="LdapException">
        /// </seealso>
        public LdapResponse(int type, int resultCode, string matchedDn, string serverMessage)
            : base(new RfcLdapMessage(RfcResultFactory(type, resultCode, matchedDn, serverMessage)))
        {
        }

        /// <summary>
        ///     Returns any error message in the response.
        /// </summary>
        /// <returns>
        ///     Any error message in the response.
        /// </returns>
        public string ErrorMessage
        {
            get
            {
                if (_exception != null)
                {
                    return _exception.LdapErrorMessage;
                }

                /*              RfcResponse resp=(RfcResponse)( message.Response);
                                if(resp == null)
                                    Console.WriteLine(" Response is null");
                                else
                                    Console.WriteLine(" Response is non null");
                                string str=resp.getErrorMessage().stringValue();
                                if( str==null)
                                     Console.WriteLine("str is null..");
                                Console.WriteLine(" Response is non null" + str);
                                return str;
                */
                return ((IRfcResponse)Message.Response).GetErrorMessage().StringValue();
            }
        }

        /// <summary>
        ///     Returns the partially matched DN field from the server response,
        ///     if the response contains one.
        /// </summary>
        /// <returns>
        ///     The partially matched DN field, if the response contains one.
        /// </returns>
        public string MatchedDn
        {
            get
            {
                if (_exception != null)
                {
                    return _exception.MatchedDn;
                }

                return ((IRfcResponse)Message.Response).GetMatchedDn().StringValue();
            }
        }

        /// <summary>
        ///     Returns all referrals in a server response, if the response contains any.
        /// </summary>
        /// <returns>
        ///     All the referrals in the server response.
        /// </returns>
        public string[] Referrals
        {
            get
            {
                string[] referrals;
                var refRenamed = ((IRfcResponse)Message.Response).GetReferral();

                if (refRenamed == null)
                {
                    referrals = new string[0];
                }
                else
                {
                    // convert RFC 2251 Referral to String[]
                    var size = refRenamed.Size();
                    referrals = new string[size];
                    for (var i = 0; i < size; i++)
                    {
                        var aRef = ((Asn1OctetString)refRenamed.get_Renamed(i)).StringValue();
                        try
                        {
                            // get the referral URL
                            var urlRef = new LdapUrl(aRef);
                            if (urlRef.GetDn() == null)
                            {
                                var origMsg = Asn1Object.RequestingMessage.Asn1Object;
                                string dn;
                                if ((dn = origMsg.RequestDn) != null)
                                {
                                    urlRef.SetDn(dn);
                                    aRef = urlRef.ToString();
                                }
                            }
                        }
                        catch (UriFormatException)
                        {
                            //Logger.Log.LogWarning("Exception swallowed", mex);
                        }
                        finally
                        {
                            referrals[i] = aRef;
                        }
                    }
                }

                return referrals;
            }
        }

        /// <summary>
        ///     Returns the result code in a server response.
        ///     For a list of result codes, see the LdapException class.
        /// </summary>
        /// <returns>
        ///     The result code.
        /// </returns>
        public int ResultCode
        {
            get
            {
                if (_exception != null)
                {
                    return _exception.ResultCode;
                }

                if ((IRfcResponse)Message.Response is RfcIntermediateResponse)
                {
                    return 0;
                }

                return ((IRfcResponse)Message.Response).GetResultCode().IntValue();
            }
        }

        /// <summary>
        ///     Checks the resultCode and generates the appropriate exception or
        ///     null if success.
        /// </summary>
        private LdapException ResultException
        {
            get
            {
                LdapException ex = null;
                switch (ResultCode)
                {
                    case LdapException.Success:
                    case LdapException.CompareTrue:
                    case LdapException.CompareFalse:
                        break;

                    case LdapException.Referral:
                        var refs = Referrals;
                        ex = new LdapReferralException(
                            "Automatic referral following not enabled",
                            LdapException.Referral, ErrorMessage);
                        ((LdapReferralException)ex).SetReferrals(refs);
                        break;

                    default:
                        ex = new LdapException(LdapException.ResultCodeToString(ResultCode), ResultCode, ErrorMessage,
                            MatchedDn);
                        break;
                }

                return ex;
            }
        }

        /// <summary>
        ///     Returns any controls in the message.
        /// </summary>
        /// <seealso cref="Novell.Directory.Ldap.LdapMessage.Controls">
        /// </seealso>
        public override LdapControl[] Controls
        {
            get
            {
                if (_exception != null)
                {
                    return null;
                }

                return base.Controls;
            }
        }

        /// <summary>
        ///     Returns the message ID.
        /// </summary>
        /// <seealso cref="LdapMessage.MessageId">
        /// </seealso>
        public override int MessageId
        {
            get
            {
                if (_exception != null)
                {
                    return _exception.MessageId;
                }

                return base.MessageId;
            }
        }

        /// <summary>
        ///     Returns the Ldap operation type of the message.
        /// </summary>
        /// <returns>
        ///     The operation type of the message.
        /// </returns>
        /// <seealso cref="Novell.Directory.Ldap.LdapMessage.Type">
        /// </seealso>
        public override int Type
        {
            get
            {
                if (_exception != null)
                {
                    return _exception.ReplyType;
                }

                return base.Type;
            }
        }

        /// <summary>
        ///     Returns an embedded exception response.
        /// </summary>
        /// <returns>
        ///     an embedded exception if any.
        /// </returns>
        internal LdapException Exception => _exception;

        /// <summary>
        ///     Indicates the referral instance being followed if the
        ///     connection created to follow referrals.
        /// </summary>
        /// <returns>
        ///     the referral being followed.
        /// </returns>
        internal ReferralInfo ActiveReferral
        {
            /*package*/
            get;
        }

        private static Asn1Sequence RfcResultFactory(int type, int resultCode, string matchedDn, string serverMessage)
        {
            Asn1Sequence ret;

            if (matchedDn == null)
            {
                matchedDn = string.Empty;
            }

            if (serverMessage == null)
            {
                serverMessage = string.Empty;
            }

            switch (type)
            {
                case SearchResult:
                    ret = new RfcSearchResultDone(new Asn1Enumerated(resultCode), new RfcLdapDn(matchedDn),
                        new RfcLdapString(serverMessage), null);
                    break;

                case BindResponse:
                    ret = null; // Not yet implemented
                    break;

                case SearchResponse:
                    ret = null; // Not yet implemented
                    break;

                case ModifyResponse:
                    ret = new RfcModifyResponse(new Asn1Enumerated(resultCode), new RfcLdapDn(matchedDn),
                        new RfcLdapString(serverMessage), null);
                    break;

                case AddResponse:
                    ret = new RfcAddResponse(new Asn1Enumerated(resultCode), new RfcLdapDn(matchedDn),
                        new RfcLdapString(serverMessage), null);
                    break;

                case DelResponse:
                    ret = new RfcDelResponse(new Asn1Enumerated(resultCode), new RfcLdapDn(matchedDn),
                        new RfcLdapString(serverMessage), null);
                    break;

                case ModifyRdnResponse:
                    ret = new RfcModifyDnResponse(new Asn1Enumerated(resultCode), new RfcLdapDn(matchedDn),
                        new RfcLdapString(serverMessage), null);
                    break;

                case CompareResponse:
                    ret = new RfcCompareResponse(new Asn1Enumerated(resultCode), new RfcLdapDn(matchedDn),
                        new RfcLdapString(serverMessage), null);
                    break;

                case SearchResultReference:
                    ret = null; // Not yet implemented
                    break;

                case ExtendedResponse:
                    ret = null; // Not yet implemented
                    break;

                default:
                    throw new Exception("Type " + type + " Not Supported");
            }

            return ret;
        }

        /// <summary>
        ///     Checks the resultCode and throws the appropriate exception.
        /// </summary>
        /// <exception>
        ///     LdapException A general exception which includes an error
        ///     message and an Ldap error code.
        /// </exception>
        internal void ChkResultCode()
        {
            if (_exception != null)
            {
                throw _exception;
            }

            var ex = ResultException;
            if (ex != null)
            {
                throw ex;
            }
        }

        /* Methods from LdapMessage */

        /// <summary>
        ///     Indicates if this response is an embedded exception response.
        /// </summary>
        /// <returns>
        ///     true if contains an embedded Ldapexception.
        /// </returns>
        /*package*/
        internal bool HasException()
        {
            return _exception != null;
        }
    }
}