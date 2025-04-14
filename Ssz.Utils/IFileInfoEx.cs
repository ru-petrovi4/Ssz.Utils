using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public interface IFileInfoEx : IFileInfo
    {
        Task<Stream> CreateReadStreamAsync();
    }
}
