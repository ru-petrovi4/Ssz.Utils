using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UtfUnknown;

namespace Ssz.Utils
{
    public static class ZipArchiveHelper
    {
        /// <summary>
        ///     Gets ZipArchive for reading with correct file names encoding.
        ///     Leave stream open upon disposing.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static ZipArchive GetZipArchiveForRead(Stream stream)
        {            
            Dictionary<Encoding, int> map = new();
#if NETCOREAPP            
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var transportEncoding = Encoding.GetEncoding(866);
#else
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var transportEncoding = Encoding.UTF8; // TODO, not working
#endif            
            ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, true, transportEncoding);
            string s = @"";
            foreach (var entry in zipArchive.Entries)
            {
                s += entry.FullName + " ";                
            }
            var bytes = transportEncoding.GetBytes(s);
            // Detect from bytes
            var results = CharsetDetector.DetectFromBytes(bytes);
            // Get the best Detection
            DetectionDetail resultDetected = results.Detected;                       
            if (resultDetected is null || resultDetected.Confidence < 0.9f || resultDetected.Encoding == transportEncoding)
            {
                return zipArchive;
            }
            else
            {
                zipArchive.Dispose();
                stream.Position = 0;
                return new ZipArchive(stream, ZipArchiveMode.Read, true, resultDetected.Encoding ?? Encoding.UTF8);
            }            
        }
    }
}
