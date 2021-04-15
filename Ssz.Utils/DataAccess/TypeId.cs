using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public class TypeId
    {
        #region construction and destruction

        public TypeId()
        {            
        }

        public TypeId(string schemaType, string nameSpace, string localId)
        {
            SchemaType = schemaType;
            Namespace = nameSpace;
            LocalId = localId;
        }

        /// <summary>
        ///     Construct a Type LocalId given a .NET / CLI Type.
        /// </summary>
        /// <param name="type">
        ///     The .NET / CLI Type for which the TypeId is being constructed.
        /// </param>
        public TypeId(Type type)
        {
            LocalId = type.ToString();
        }

        #endregion        

        #region public functions

        public string SchemaType { get; set; } = @"";

        public string Namespace { get; set; } = @"";

        public string LocalId { get; set; } = @"";

        public bool Compare(TypeId that)
        {            
            return SchemaType == that.SchemaType && Namespace == that.Namespace && LocalId == that.LocalId;
        }

        #endregion        
    }
}
