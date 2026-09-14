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

        /// <summary>   
        ///     <para>!!! Warning: always '/' as path separator !!!</para>
        ///     <para>Path relative to the root of the Files Store.</para>
        ///     <para>No '/' at the begin, no '/' at the end.</para>
        ///     <para>String.Empty for the Files Store root directory.</para>
        /// </summary>
        public string ProjectDirectoryInvariantPathRelativeToRootDirectory { get; set; } = @"";

        public string Name => PhysicalPath!.Substring(PhysicalPath!.LastIndexOf(Path.DirectorySeparatorChar) + 1);

        /// <summary>
        ///     <para>For browser: fileId in Project</para>
        ///     <para>File relative path in Project directory. Path.DirectorySeparatorChar for current system is used.</para>
        /// </summary>
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
#if !TEST_BROWSER_IN_DESKTOP
            if (!OperatingSystem.IsBrowser())
                throw new InvalidOperationException();

            // The content is keyed by name plus modification time, not by path, so that a file
            // repeated in several directories is stored only once.
            var obj = await IndexedDBInterop.GetFileAsync(
                ProjectDirectoryInvariantPathRelativeToRootDirectory,
                IndexedDBHelper.GetContentId(Name, LastModified));
            System.Runtime.InteropServices.JavaScript.JSObject jSObject =
                (System.Runtime.InteropServices.JavaScript.JSObject)obj;
            byte[] fileData = jSObject.GetPropertyAsByteArray(@"file")!;
#else
            byte[] fileData = File.ReadAllBytes(Path.Combine(IndexedDBHelper.GetPathInTempDirectory(ProjectDirectoryInvariantPathRelativeToRootDirectory), PhysicalPath!));
#endif

            return new MemoryStream(fileData);
        }

        #endregion
    }
}
