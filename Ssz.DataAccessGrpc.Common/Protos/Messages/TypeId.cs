using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Common
{
    public sealed partial class TypeId
    {
        #region construction and destruction        

        public TypeId(Ssz.Utils.DataAccess.TypeId? typeId)
        {
            if (typeId is not null)
            {
                SchemaType = typeId.SchemaType;
                Namespace = typeId.Namespace;
                LocalId = typeId.LocalId;
            }            
        }

        #endregion

        #region public functions

        public Ssz.Utils.DataAccess.TypeId ToTypeId()
        {
            var typeId = new Ssz.Utils.DataAccess.TypeId();
            typeId.SchemaType = SchemaType;
            typeId.Namespace = Namespace;
            typeId.LocalId = LocalId;
            return typeId;
        }

        #endregion
    }
}
