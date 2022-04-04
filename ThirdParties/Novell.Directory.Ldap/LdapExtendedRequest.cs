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

namespace Novell.Directory.Ldap
{
    /// <summary>
    ///     Represents an Ldap Extended Request.
    /// </summary>
    /// <seealso cref="LdapConnection.SendRequestAsync">
    /// </seealso>
    /*
     *       ExtendedRequest ::= [APPLICATION 23] SEQUENCE {
     *               requestName      [0] LdapOID,
     *               requestValue     [1] OCTET STRING OPTIONAL }
     */
    public class LdapExtendedRequest : LdapMessage
    {
        public override DebugId DebugId { get; } = DebugId.ForType<LdapExtendedRequest>();

        /// <summary>
        ///     Constructs an LdapExtendedRequest.
        /// </summary>
        /// <param name="op">
        ///     The object which contains (1) an identifier of an extended
        ///     operation which should be recognized by the particular Ldap
        ///     server this client is connected to, and (2) an operation-
        ///     specific sequence of octet strings or BER-encoded values.
        /// </param>
        /// <param name="cont">
        ///     Any controls that apply to the extended request
        ///     or null if none.
        /// </param>
        public LdapExtendedRequest(LdapExtendedOperation op, LdapControl[] cont)
            : base(
                ExtendedRequest,
                new RfcExtendedRequest(
                    new RfcLdapOid(op.GetId()),
                    op.GetValue() != null ? new Asn1OctetString(op.GetValue()) : null), cont)
        {
        }

        /// <summary> Retrieves an extended operation from this request.</summary>
        /// <returns>
        ///     extended operation represented in this request.
        /// </returns>
        public LdapExtendedOperation ExtendedOperation
        {
            get
            {
                var xreq = (RfcExtendedRequest)Asn1Object.get_Renamed(1);

                // Zeroth element is the OID, element one is the value
                var tag = (Asn1Tagged)xreq.get_Renamed(0);
                var oid = (RfcLdapOid)tag.TaggedValue;
                var requestId = oid.StringValue();

                byte[] requestValue = null;
                if (xreq.Size() >= 2)
                {
                    tag = (Asn1Tagged)xreq.get_Renamed(1);
                    var valueRenamed = (Asn1OctetString)tag.TaggedValue;
                    requestValue = valueRenamed.ByteValue();
                }

                return new LdapExtendedOperation(requestId, requestValue);
            }
        }
    }
}
