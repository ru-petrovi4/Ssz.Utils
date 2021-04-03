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

        public TypeId(string schemaType, string nameSpace, string id)
        {
            SchemaType = schemaType;
            Namespace = nameSpace;
            LocalId = id;
        }

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

        #endregion        

        #region public functions

        public string SchemaType { get; set; } = @"";

        public string Namespace { get; set; } = @"";

        public string LocalId { get; set; } = @"";

        #endregion        
    }
}
