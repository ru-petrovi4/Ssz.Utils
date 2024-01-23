using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ude;

namespace Ssz.Utils
{
    public static class CharsetDetectorHelper
    {
        /// <summary>
        ///     Returns StreamReader with correct encoding.
        /// </summary>
        /// <param name="csvFileFullName"></param>
        /// <returns></returns>
        public static StreamReader GetStreamReader(string csvFileFullName, Encoding defaultEncoding, ILogger? logger = null)
        {
            int bytesCount = (int)(new FileInfo(csvFileFullName).Length);
            byte[] bytes = new byte[bytesCount];
            using (FileStream fileStream = File.Open(csvFileFullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Read(bytes, 0, bytesCount);
            }
            CharsetDetector charsetDetector = new();
            charsetDetector.Feed(bytes, 0, bytes.Length);
            charsetDetector.DataEnd();

            logger?.LogDebug(@"charsetDetector.Encoding: " + charsetDetector.Encoding?.EncodingName);

            return new StreamReader(new MemoryStream(bytes), charsetDetector.Encoding ?? defaultEncoding, false);
        }

        /// <summary>
        ///     Returns StreamReader with correct encoding.
        /// </summary>
        /// <param name="csvFileFullName"></param>
        /// <returns></returns>
        public static StreamReader GetStreamReader(Stream stream, Encoding defaultEncoding, ILogger? logger = null)
        {
            MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
            byte[] bytes = memoryStream.ToArray();
            var encoding = defaultEncoding;
            if (bytes.Length > 0)
            {
                CharsetDetector charsetDetector = new();
                charsetDetector.Feed(bytes, 0, bytes.Length);
                charsetDetector.DataEnd();
                var encoding2 = charsetDetector.Encoding;
                if (encoding2 is not null && charsetDetector.Confidence > 0.9)
                    encoding = encoding2;
                logger?.LogDebug(@"Detected Encoding: " + encoding.EncodingName);
            }
            memoryStream.Position = 0;
            return new StreamReader(memoryStream, encoding, false);
        }
    }
}
