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
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
    /// <summary>
    ///     This class defines the identifier for data types and object types.  Each
    ///     element of the TypeId is case-sensitive.
    /// </summary>
    [DataContract(Namespace = "urn:xi/data")]
    public class TypeId
    {
        #region construction and destruction

        /// <summary>
        ///     The default constructor.
        /// </summary>
        public TypeId()
        {
        }

        /// <summary>
        ///     Construct a Type LocalId given a .NET / CLI Type.
        ///     Preconditions: id != Null.
        /// </summary>
        /// <param name="id">
        ///     The .NET / CLI Type for which the TypeId is being constructed.
        /// </param>
        public TypeId(Type id)
        {
            if (id == null) throw new ArgumentNullException("id");

            SchemaType = null;
            Namespace = null;
            LocalId = id.ToString();
        }

        /// <summary>
        ///     This constructor initializes a new TypeId object.
        /// </summary>
        /// <param name="schemaType">
        ///     The SchemaType.
        /// </param>
        /// <param name="nameSpace">
        ///     The Namespace.
        /// </param>
        /// <param name="id">
        ///     The LocalId.
        /// </param>
        public TypeId(string schemaType, string nameSpace, string id)
        {
            SchemaType = schemaType;
            Namespace = nameSpace;
            LocalId = id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeId"></param>
        public TypeId(Ssz.Utils.DataAccess.TypeId typeId)
        {
            SchemaType = typeId.SchemaType;
            Namespace = typeId.Namespace;
            LocalId = typeId.LocalId;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     <para>
        ///         This string identifies the type of the type definition
        ///         (the type of its schema).  Standard values are defined by the XiSchemaType
        ///         enumeration.
        ///     </para>
        ///     <para>
        ///         For Data Types, the value XiSchemaType.Xi, whose value is null,
        ///         is used for the standard .NET data types and those defined by the Xi.
        ///     </para>
        ///     <para>
        ///         The forward slash '/'character and the dot character, '.',
        ///         cannot be used in the SchemaType string.
        ///     </para>
        /// </summary>
        [DataMember] public string SchemaType;

        /// <summary>
        ///     <para>
        ///         This member is used to identify the context for the
        ///         Identifier. The context defines which organization defined
        ///         the type and any additional qualifying information. For CLS
        ///         data types, the SchemaType is set to XiSchemaType.Xi and the
        ///         namespace is set to XiNamespace.Xi. Both the XiSchemaType.Xi
        ///         and XiNamespace.Xi strings are defined to have null values.
        ///     </para>
        ///     <para>
        ///         For example, data types for Fieldbus Foundation devices
        ///         are defined either by the Fieldbus Foundation or by device
        ///         manufacturers. When defined by the Fieldbus Foundation, the
        ///         Namespace would be composed of a single string
        ///         with a value of "FF", and if defined by a device manufacturer,
        ///         the path would be composed of the Manufacturer LocalId registered by
        ///         the Fieldbus Foundation, the device type, and the device revision.
        ///         If the type is an EDDL type, the EDD revision is also needed.
        ///     </para>
        ///     <para>Set to XiNamespace.Xi (null) for .NET defined data types.</para>
        ///     <para>
        ///         For types defined by the server vendor for use in multiple
        ///         Xi servers, the ServerDescription VendorName should be used as
        ///         the namespace.
        ///     </para>
        ///     <para>
        ///         The forward slash '/' character is not permitted to be used
        ///         within the namespace.  Instead, the dot '.' character should be used
        ///         to separate elements of the namespace.
        ///     </para>
        ///     <para>
        ///         Following the example above, if the vendor defines the type
        ///         specifically for a given server, then the ServerDescription ServerName,
        ///         separated by a '.' should be appended after the vendor name.
        ///         (e.g. "MyVendor.MyServer").
        ///     </para>
        /// </summary>
        [DataMember] public string Namespace;

        /// <summary>
        ///     The string representation of the identifier for the type.
        /// </summary>
        [DataMember] public string LocalId;

        /// <summary>
        ///     This method compares this TypeId with a TypeId passed-in as a parameter.
        /// </summary>
        /// <param name="typeId">
        ///     The TypeId to compare against this TypeId.
        /// </param>
        /// <returns>
        ///     True if the two TypeIds are the same, and false if not.
        /// </returns>
        public bool Compare(TypeId typeId)
        {
            bool equals = true;
            if (string.IsNullOrEmpty(SchemaType))
            {
                if (!string.IsNullOrEmpty(typeId.SchemaType))
                    equals = false;
            }
            else // this SchemaId has a value
            {
                if (string.IsNullOrEmpty(typeId.SchemaType))
                    equals = false;
                else if (string.Compare(SchemaType, typeId.SchemaType, false) != 0)
                    equals = false;
            }
            if (equals) // so far
            {
                if (string.IsNullOrEmpty(Namespace))
                {
                    if (string.IsNullOrEmpty(typeId.Namespace) == false)
                        equals = false;
                }
                else // this Namespace has a value
                {
                    if (string.IsNullOrEmpty(typeId.Namespace))
                        equals = false;
                    else if (Namespace != typeId.Namespace)
                        equals = false;
                }
            }
            if (equals) // so far
            {
                if (LocalId != typeId.LocalId)
                    equals = false;
            }
            return equals;
        }

        /// <summary>
        ///     <para>
        ///         This method converts a type id to a string. The string form of the TypeId
        ///         closely resembles a URL, containing a resource type prefix, a namespace qualifier,
        ///         and the identifier with the exception that the namespace qualifier and the local
        ///         identifier are separated by the dot '.' character.
        ///     </para>
        ///     <para>  SchemaType:Namespace.Identifier</para>
        ///     <para>
        ///         If the SchemaType is present, it is terminated with the colon ':' character,
        ///         and followed by the Namespace.
        ///     </para>
        ///     <para>
        ///         If the SchemaType is not present, the Namespace is the first element of the
        ///         string.
        ///     </para>
        ///     <para>
        ///         If the Namespace is present, it is terminated with the dot '.' character, and
        ///         followed by the LocalId.
        ///     </para>
        ///     <para>If the Namespace is not present, the LocalId follows immediately. </para>
        ///     <para>
        ///         For example, if the type is the CLS Int32 type, the string representation would be
        ///         "System.Int32".
        ///     </para>
        /// </summary>
        /// <returns>
        ///     The resulting string.
        /// </returns>
        public override string ToString()
        {
            string typeIdString = null;
            if (LocalId != null)
            {
                if (!String.IsNullOrEmpty(SchemaType))
                    typeIdString = SchemaType + ":";

                if (!String.IsNullOrEmpty(Namespace))
                    typeIdString += Namespace + ".";

                typeIdString += LocalId;
            }
            return typeIdString ?? "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Ssz.Utils.DataAccess.TypeId ToTypeId()
        {
            var typeId = new Ssz.Utils.DataAccess.TypeId();
            typeId.SchemaType = SchemaType ?? @"";
            typeId.Namespace = Namespace ?? @"";
            typeId.LocalId = LocalId ?? @"";
            return typeId;
        }

        #endregion
    }
}