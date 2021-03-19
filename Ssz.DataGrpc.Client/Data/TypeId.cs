using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class TypeId
    {
        #region construction and destruction

        /// <summary>
        ///     Construct a Type LocalId given a .NET / CLI Type.
        /// </summary>
        /// <param name="id">
        ///     The .NET / CLI Type for which the TypeId is being constructed.
        /// </param>
        public TypeId(Type id)
        {
            SchemaType = @"";
            Namespace = @"";
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

        #endregion
    }
}
