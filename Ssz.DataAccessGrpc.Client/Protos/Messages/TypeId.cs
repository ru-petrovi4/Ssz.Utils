using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    internal sealed partial class TypeId
    {
        #region construction and destruction        

        public TypeId(Utils.DataAccess.TypeId? typeId)
        {
            if (typeId is null)
                return;
            SchemaType = typeId.SchemaType;
            Namespace = typeId.Namespace;
            LocalId = typeId.LocalId;
        }

        #endregion

        #region public functions

        public Utils.DataAccess.TypeId ToTypeId()
        {
            var typeId = new Utils.DataAccess.TypeId();
            typeId.SchemaType = SchemaType;
            typeId.Namespace = Namespace;
            typeId.LocalId = LocalId;
            return typeId;
        }

        #endregion
    }
}
