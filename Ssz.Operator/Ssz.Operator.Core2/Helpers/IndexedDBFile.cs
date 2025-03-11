using Microsoft.Extensions.FileProviders;
using Ssz.Utils;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public class IndexedDBFile : IFileInfoEx
    {
        #region public functions

        public string ProjectDirectoryInvariantPathRelativeToRootDirectory { get; set; } = @"";

        public string Name => PhysicalPath!.Substring(PhysicalPath!.LastIndexOf(Path.DirectorySeparatorChar) + 1);

        public string? PhysicalPath { get; set; }

        public bool Exists { get; set; } = true;

        public bool IsDirectory => false;

        /// <summary>
        ///     FileInfo.LastWriteTimeUtc
        /// </summary>
        public DateTimeOffset LastModified { get; set; }

        public long Length { get; set; }

        public Stream CreateReadStream()
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> CreateReadStreamAsync()
        {
            if (!OperatingSystem.IsBrowser())
                throw new InvalidOperationException();

            var obj = await IndexedDBInterop.GetFileAsync(ProjectDirectoryInvariantPathRelativeToRootDirectory, PhysicalPath!);            
            System.Runtime.InteropServices.JavaScript.JSObject jSObject =
                (System.Runtime.InteropServices.JavaScript.JSObject)obj;
            byte[] fileData = jSObject.GetPropertyAsByteArray(@"file")!;
            return new MemoryStream(fileData);
        }

        #endregion
    }
}
