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
using System;

namespace Novell.Directory.Ldap.Controls
{
    /// <summary>
    ///     LdapSortControl is a Server Control to specify how search results are
    ///     to be sorted by the server. If a server does not support
    ///     sorting in general or for a particular query, the results will be
    ///     returned unsorted, along with a control indicating why they were not
    ///     sorted (or that sort controls are not supported). If the control was
    ///     marked "critical", the whole search operation will fail if the sort
    ///     control is not supported.
    /// </summary>
    public class LdapSortControl : LdapControl
    {
        private const int OrderingRule = 0;
        private const int ReverseOrder = 1;

        /// <summary> The requestOID of the sort control.</summary>
        private const string RequestOid = "1.2.840.113556.1.4.473";

        /// <summary> The responseOID of the sort control.</summary>
        private const string ResponseOid = "1.2.840.113556.1.4.474";

        static LdapSortControl()
        {
            /*
            * This is where we register the control responses
            */
            {
                /*
                * Register the Server Sort Control class which is returned by the
                * server in response to a Sort Request
                */
                try
                {
                    Register(ResponseOid, Type.GetType("Novell.Directory.Ldap.Controls.LdapSortResponse"));
                }
                catch (Exception)
                {
                    //Logger.Log.LogWarning("Exception swallowed", e);
                }
            }
        }

        /// <summary>
        ///     Constructs a sort control with a single key.
        /// </summary>
        /// <param name="key">
        ///     A sort key object, which specifies attribute,
        ///     order, and optional matching rule.
        /// </param>
        /// <param name="critical   True">
        ///     if the search operation is to fail if the
        ///     server does not support this control.
        /// </param>
        public LdapSortControl(LdapSortKey key, bool critical)
            : this(new[] { key }, critical)
        {
        }

        /// <summary>
        ///     Constructs a sort control with multiple sort keys.
        /// </summary>
        /// <param name="keys       An">
        ///     array of sort key objects, to be processed in
        ///     order.
        /// </param>
        /// <param name="critical   True">
        ///     if the search operation is to fail if the
        ///     server does not support this control.
        /// </param>
        public LdapSortControl(LdapSortKey[] keys, bool critical)
            : base(RequestOid, critical, null)
        {
            var sortKeyList = new Asn1SequenceOf();

            for (var i = 0; i < keys.Length; i++)
            {
                var key = new Asn1Sequence();

                key.Add(new Asn1OctetString(keys[i].Key));

                if (keys[i].MatchRule != null)
                {
                    key.Add(new Asn1Tagged(
                        new Asn1Identifier(Asn1Identifier.Context, false, OrderingRule),
                        new Asn1OctetString(keys[i].MatchRule), false));
                }

                if (keys[i].Reverse)
                {
                    // only add if true
                    key.Add(new Asn1Tagged(
                        new Asn1Identifier(Asn1Identifier.Context, false, ReverseOrder),
                        new Asn1Boolean(true), false));
                }

                sortKeyList.Add(key);
            }

            SetValue(sortKeyList.GetEncoding(new LberEncoder()));
        }
    }
}
