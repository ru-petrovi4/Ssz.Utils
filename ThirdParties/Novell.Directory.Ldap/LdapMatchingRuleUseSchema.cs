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

using Novell.Directory.Ldap.Utilclass;
using System.IO;
using System.Text;

namespace Novell.Directory.Ldap
{
    /// <summary>
    ///     Represents the definition of a specific matching rule use in the
    ///     directory schema.
    ///     The LdapMatchingRuleUseSchema class represents the definition of a
    ///     matching rule use.  It is used to discover or modify which attributes are
    ///     suitable for use with an extensible matching rule. It contains the name and
    ///     identifier of a matching rule, and a list of attributes which
    ///     it applies to.
    /// </summary>
    /// <seealso cref="LdapAttributeSchema">
    /// </seealso>
    /// <seealso cref="LdapSchemaElement">
    /// </seealso>
    /// <seealso cref="LdapSchema">
    /// </seealso>
    public class LdapMatchingRuleUseSchema : LdapSchemaElement
    {
        /// <summary>
        ///     Constructs a matching rule use definition for adding to or deleting
        ///     from the schema.
        /// </summary>
        /// <param name="names">
        ///     Name(s) of the matching rule.
        /// </param>
        /// <param name="oid">
        ///     Object Identifier of the the matching rule
        ///     in dotted-decimal format.
        /// </param>
        /// <param name="description">
        ///     Optional description of the matching rule use.
        /// </param>
        /// <param name="obsolete">
        ///     True if the matching rule use is obsolete.
        /// </param>
        /// <param name="attributes">
        ///     List of attributes that this matching rule
        ///     applies to. These values may be either the
        ///     names or numeric oids of the attributes.
        /// </param>
        public LdapMatchingRuleUseSchema(string[] names, string oid, string description, bool obsolete,
            string[] attributes)
            : base(LdapSchema.SchemaTypeNames[LdapSchema.MatchingUse])
        {
            Names = new string[names.Length];
            names.CopyTo(Names, 0);
            Id = oid;
            Description = description;
            Obsolete = obsolete;
            Attributes = new string[attributes.Length];
            attributes.CopyTo(Attributes, 0);
            Value = FormatString();
        }

        /// <summary>
        ///     Constructs a matching rule use definition from the raw string value
        ///     returned on a schema query for matchingRuleUse.
        /// </summary>
        /// <param name="raw">
        ///     The raw string value returned on a schema
        ///     query for matchingRuleUse.
        /// </param>
        public LdapMatchingRuleUseSchema(string raw)
            : base(LdapSchema.SchemaTypeNames[LdapSchema.MatchingUse])
        {
            try
            {
                var matchParser = new SchemaParser(raw);
                Names = new string[matchParser.Names.Length];
                matchParser.Names.CopyTo(Names, 0);
                Id = matchParser.Id;
                Description = matchParser.Description;
                Obsolete = matchParser.Obsolete;
                Attributes = matchParser.Applies;
                Value = FormatString();
            }
            catch (IOException)
            {
                //Logger.Log.LogWarning("Exception swallowed", ex);
            }
        }

        /// <summary>
        ///     Returns an array of all the attributes which this matching rule
        ///     applies to.
        /// </summary>
        /// <returns>
        ///     An array of all the attributes which this matching rule applies to.
        /// </returns>
        public string[] Attributes { get; }

        /// <summary>
        ///     Returns a string in a format suitable for directly adding to a
        ///     directory, as a value of the particular schema element attribute.
        /// </summary>
        /// <returns>
        ///     A string representation of the attribute's definition.
        /// </returns>
        protected override string FormatString()
        {
            var valueBuffer = new StringBuilder("( ");
            string token;

            if ((token = Id) != null)
            {
                valueBuffer.Append(token);
            }

            var strArray = GetNames();
            if (strArray != null)
            {
                valueBuffer.Append(" NAME ");
                if (strArray.Length == 1)
                {
                    valueBuffer.Append("'" + strArray[0] + "'");
                }
                else
                {
                    valueBuffer.Append("( ");

                    for (var i = 0; i < strArray.Length; i++)
                    {
                        valueBuffer.Append(" '" + strArray[i] + "'");
                    }

                    valueBuffer.Append(" )");
                }
            }

            if ((token = Description) != null)
            {
                valueBuffer.Append(" DESC ");
                valueBuffer.Append("'" + token + "'");
            }

            if (Obsolete)
            {
                valueBuffer.Append(" OBSOLETE");
            }

            if ((strArray = Attributes) != null)
            {
                valueBuffer.Append(" APPLIES ");
                if (strArray.Length > 1)
                {
                    valueBuffer.Append("( ");
                }

                for (var i = 0; i < strArray.Length; i++)
                {
                    if (i > 0)
                    {
                        valueBuffer.Append(" $ ");
                    }

                    valueBuffer.Append(strArray[i]);
                }

                if (strArray.Length > 1)
                {
                    valueBuffer.Append(" )");
                }
            }

            valueBuffer.Append(" )");
            return valueBuffer.ToString();
        }
    }
}
