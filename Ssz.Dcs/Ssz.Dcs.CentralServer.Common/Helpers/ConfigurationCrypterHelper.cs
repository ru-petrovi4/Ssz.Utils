using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ssz.Dcs.CentralServer.Common.Helpers
{
    public static class ConfigurationCrypterHelper
    {
        /// <summary>
        ///     Returns valid file name, or writes to log with Error level and returns empty string.
        /// </summary>
        /// <param name="loggersSet"></param>
        /// <returns></returns>
        public static string GetConfigurationCrypterCertificateFileName(ILoggersSet loggersSet)
        {
            return @"appsettings.pfx";
            //string? readableFileName = null;

            //string appsettingsJsonContent = File.ReadAllText(@"appsettings.json");
            //try
            //{
            //    JObject jObject = JObject.Parse(appsettingsJsonContent);
            //    JToken? cryptoConfigurationChildJToken = jObject.SelectToken(@"ConfigurationCrypterCertificateFileName");
            //    if (cryptoConfigurationChildJToken is not null)
            //        readableFileName = cryptoConfigurationChildJToken.Value<string>();                
            //}
            //catch
            //{
            //}   

            //if (String.IsNullOrEmpty(readableFileName))
            //    readableFileName = @"appsettings.pfx";

            //bool fileExists = File.Exists(readableFileName);
            //if (!fileExists)
            //{
            //    using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Ssz.Utils.Properties.Resources.FileNameScopeName, readableFileName));
            //    loggersSet.WrapperUserFriendlyLogger.LogError(Ssz.Utils.Properties.Resources.FileDoesNotExist);
            //    return @"";
            //}
            //try
            //{
            //    using (var fs = File.OpenRead(readableFileName))
            //    {
            //        var canRead = fs.CanRead;
            //        if (canRead)
            //            return readableFileName;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    loggersSet.Logger.LogError(ex, @"GetCryptoCertificateValidFileName Exception.");
            //}
            //using var fileNameScope2 = loggersSet.WrapperUserFriendlyLogger.BeginScope((Ssz.Utils.Properties.Resources.FileNameScopeName, readableFileName));
            //loggersSet.WrapperUserFriendlyLogger.LogError(Ssz.Utils.Properties.Resources.FileIsNotReadable);
            //return @"";
        }
    }
}
