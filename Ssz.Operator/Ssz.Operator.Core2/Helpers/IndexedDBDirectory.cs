using Microsoft.Extensions.FileProviders;
using Ssz.Utils.Serialization;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public class IndexedDBDirectory : IDirectoryContents, IFileInfo
    {
        #region public functions

        /// <summary>
        ///     String.Empty for the Files Store root directory.
        /// </summary>
        public string Name => PhysicalPath!.Substring(PhysicalPath!.LastIndexOf(Path.DirectorySeparatorChar) + 1);

        /// <summary>        
        ///     Path relative to the root of the Files Store.
        ///     No '/' at the begin, no '/' at the end.
        ///     String.Empty for the Files Store root directory.
        /// </summary>
        public string? PhysicalPath { get; set; }

        public FrozenDictionary<string, IndexedDBDirectory> ChildIndexedDBDirectoriesDictionary { get; set; } = null!;

        public FrozenDictionary<string, IndexedDBFile> IndexedDBFilesDictionary { get; set; } = null!;

        public bool Exists { get; set; } = true;

        public bool IsDirectory => true;

        /// <summary>
        /// </summary>
        public DateTimeOffset LastModified { get; set; }

        public long Length { get; set; }

        public Stream CreateReadStream()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return ChildIndexedDBDirectoriesDictionary.Values
                .OfType<IFileInfo>()
                .Concat(IndexedDBFilesDictionary.Values
                    .OfType<IFileInfo>())
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
