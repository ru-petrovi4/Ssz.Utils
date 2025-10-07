using Microsoft.Extensions.Logging;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UtfUnknown;

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
                var results = CharsetDetector.DetectFromBytes(bytes);
                // Get the best Detection
                DetectionDetail resultDetected = results.Detected;                

                var encoding2 = resultDetected.Encoding;
                if (encoding2 is not null && resultDetected.Confidence > 0.9)
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
            Encoding? encoding = null;
            var results = CharsetDetector.DetectFromStream(textStream);
            // Get the best Detection
            DetectionDetail resultDetected = results.Detected;

            var encoding2 = resultDetected?.Encoding;
            if (encoding2 is not null && resultDetected!.Confidence > 0.9)
                encoding = encoding2;

            if (encoding is null)
                loggersSet?.LoggerAndUserFriendlyLogger.LogError(Properties.Resources.Error_CannotDetermineFileEncoding);
            else
                loggersSet?.Logger.LogDebug(@"Detected Encoding: " + encoding.EncodingName);
            textStream.Position = 0;
            return new StreamReader(textStream, encoding ?? defaultEncoding, false);
        }
    }
}
