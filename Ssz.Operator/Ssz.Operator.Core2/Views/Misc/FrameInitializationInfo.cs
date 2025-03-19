using Ssz.Operator.Core.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public class FrameInitializationInfo :
        Ssz.Operator.Core.Utils.OwnedDataSerializableAndCloneable
    {
        #region construction and destruction

        public FrameInitializationInfo()
        {
            FrameName = @"";
            StartDsPageFileRelativePath = @"";
        }

        #endregion

        #region public functions

        public string FrameName { get; set; }

        public string StartDsPageFileRelativePath { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(FrameName);
                writer.Write(StartDsPageFileRelativePath);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        FrameName = reader.ReadString();
                        StartDsPageFileRelativePath = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion

        //public class EqualityComparer : IEqualityComparer<FrameInitializationInfo>
        //{
        //    public static readonly EqualityComparer Instance = new EqualityComparer();

        //    public bool Equals(FrameInitializationInfo? x, FrameInitializationInfo? y)
        //    {
        //        return x?.FrameName == y?.FrameName;
        //    }

        //    public int GetHashCode(FrameInitializationInfo obj)
        //    {
        //        return 0;
        //    }
        //}
    }
}
