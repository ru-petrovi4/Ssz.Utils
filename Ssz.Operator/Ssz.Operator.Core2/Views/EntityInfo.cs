using System;

namespace Ssz.Operator.Core
{
    public class EntityInfo
    {
        #region construction and destruction

        public EntityInfo(string name, Guid guid, string desc, string group, byte[]? previewImageBytes = null)
        {
            Name = name;
            Guid = guid;
            Desc = desc;
            Group = group;
            PreviewImageBytes = previewImageBytes;
        }

        public EntityInfo(string name)
            : this(name, Guid.Empty, "", "")
        {
        }

        #endregion

        #region public functions

        public string Name { get; set; }

        public Guid Guid { get; set; }

        public string Desc { get; set; }

        public string Group { get; set; }

        public byte[]? PreviewImageBytes { get; set; }

        #endregion
    }
}