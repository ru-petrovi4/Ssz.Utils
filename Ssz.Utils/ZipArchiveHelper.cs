using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Ude;

namespace Ssz.Utils
{
    public static class ZipArchiveHelper
    {
        /// <summary>
        ///     Gets ZipArchive for reading with correct file names encoding.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static ZipArchive GetZipArchiveForRead(Stream stream)
        {
            CharsetDetector charsetDetector = new();
            Dictionary<Encoding, int> map = new();
#if NETCOREAPP            
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var transportEncoding = Encoding.GetEncoding(866);
#else
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var transportEncoding = Encoding.UTF8; // TODO, not working
#endif            
            ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, true, transportEncoding);
            foreach (var entry in zipArchive.Entries)
            {
                var bytes = transportEncoding.GetBytes(entry.FullName);
                charsetDetector.Feed(bytes, 0, bytes.Length);
            }
            charsetDetector.DataEnd();
            if (!charsetDetector.IsDone() || charsetDetector.Encoding == transportEncoding)
            {
                return zipArchive;
            }
            else
            {
                zipArchive.Dispose();
                stream.Position = 0;
                return new ZipArchive(stream, ZipArchiveMode.Read, true, charsetDetector.Encoding ?? Encoding.UTF8);
            }            
        }
    }
}
