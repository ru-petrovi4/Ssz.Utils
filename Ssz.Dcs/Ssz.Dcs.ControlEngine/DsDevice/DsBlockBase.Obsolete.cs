using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public abstract partial class DsBlockBase
    {
        #region private functions

        private void DeserializeOwnedDataObsolete(SerializationReader reader, object? context, UInt16 blockType, UInt16 paramInfosVersion)
        {

        }        

        #endregion
    }
}
