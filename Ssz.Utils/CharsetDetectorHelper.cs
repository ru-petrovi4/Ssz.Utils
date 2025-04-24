using Microsoft.Extensions.Logging;
using Ssz.Utils.Logging;
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
        public static StreamReader GetStreamReader(string textFileFullName, Encoding defaultEncoding, ILoggersSet? loggersSet = null)
        {            
            byte[] bytes;
            using (FileStream fileStream = File.Open(textFileFullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (MemoryStream memoryStream = new())
            {
                fileStream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }
            Encoding? encoding = null;
            if (bytes.Length > 0)
            {
                CharsetDetector charsetDetector = new();
                charsetDetector.Feed(bytes, 0, bytes.Length);
                charsetDetector.DataEnd();

                var encoding2 = charsetDetector.Encoding;
                if (encoding2 is not null && charsetDetector.Confidence > 0.9)
                    encoding = encoding2;
                
                if (encoding is null)
                    loggersSet?.LoggerAndUserFriendlyLogger.LogError(Properties.Resources.Error_CannotDetermineFileEncoding);
                else
                    loggersSet?.Logger.LogDebug(@"Detected Encoding: " + encoding.EncodingName);
            }

            return new StreamReader(new MemoryStream(bytes), encoding ?? defaultEncoding, false);
        }

        /// <summary>
        ///     Returns StreamReader with correct encoding.
        /// </summary>
        /// <param name="csvFileFullName"></param>
        /// <returns></returns>
        public static StreamReader GetStreamReader(Stream textStream, Encoding defaultEncoding, ILoggersSet? loggersSet = null)
        {
            MemoryStream memoryStream = new();
            textStream.CopyTo(memoryStream);
            byte[] bytes = memoryStream.ToArray();
            Encoding? encoding = null;
            if (bytes.Length > 0)
            {
                CharsetDetector charsetDetector = new();
                charsetDetector.Feed(bytes, 0, bytes.Length);
                charsetDetector.DataEnd();

                var encoding2 = charsetDetector.Encoding;
                if (encoding2 is not null && charsetDetector.Confidence > 0.9)
                    encoding = encoding2;

                if (encoding is null)
                    loggersSet?.LoggerAndUserFriendlyLogger.LogError(Properties.Resources.Error_CannotDetermineFileEncoding);
                else
                    loggersSet?.Logger.LogDebug(@"Detected Encoding: " + encoding.EncodingName);
            }
            memoryStream.Position = 0;
            return new StreamReader(memoryStream, encoding ?? defaultEncoding, false);
        }
    }
}
