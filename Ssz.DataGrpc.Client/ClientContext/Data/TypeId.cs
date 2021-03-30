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

        public TypeId(Utils.DataSource.TypeId typeId)
        {
            SchemaType = typeId.SchemaType;
            Namespace = typeId.Namespace;
            LocalId = typeId.LocalId;
        }

        #endregion

        #region public functions

        public Utils.DataSource.TypeId ToTypeId()
        {
            var typeId = new Utils.DataSource.TypeId();
            typeId.SchemaType = SchemaType;
            typeId.Namespace = Namespace;
            typeId.LocalId = LocalId;
            return typeId;
        }

        #endregion
    }
}
