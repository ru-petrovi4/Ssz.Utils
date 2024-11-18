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
        /// <param name="textFileFullName"></param>
        /// <returns></returns>
        public static StreamReader GetStreamReader(string textFileFullName, Encoding defaultEncoding, ILogger? logger = null)
        {            
            byte[] bytes;
            using (FileStream fileStream = File.Open(textFileFullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (MemoryStream memoryStream = new())
            {
                fileStream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
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
        public static StreamReader GetStreamReader(Stream textStream, Encoding defaultEncoding, ILogger? logger = null)
        {
            MemoryStream memoryStream = new();
            textStream.CopyTo(memoryStream);
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
