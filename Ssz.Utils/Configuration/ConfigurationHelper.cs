using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public static class ConfigurationHelper
    {
        #region public functions

        /// <summary>
        ///     Returns defaultValue if key value is null or empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetValue<T>(IConfiguration? configuration, string key, T defaultValue)
            where T : notnull
        {
            if (configuration is null)
                return defaultValue;
            // null if no key in startup args string
            // null if --Rewiew or -r
            // "" if --Rewiew=
            var valueString = configuration[key];
            if (String.IsNullOrEmpty(valueString))
                return defaultValue;
            var result = new Any(valueString!).ValueAs<T>(false);
            if (result is null)
                return defaultValue;
            return result;
        }

        public static T GetValue_Base64<T>(IConfiguration? configuration, string key, T defaultValue)
            where T : notnull
        {
            if (configuration is null)
                return defaultValue;
            // null if no key in startup args string
            // null if --Rewiew or -r
            // "" if --Rewiew=
            var valueString = configuration[key];
            if (String.IsNullOrEmpty(valueString))
                return defaultValue;
            var result = new Any(Encoding.UTF8.GetString(Convert.FromBase64String(valueString))).ValueAs<T>(false);
            if (result is null)
                return defaultValue;
            return result;
        }

        public static T GetValue_Enum<T>(IConfiguration? configuration, string key, T defaultValue)
            where T : struct, System.Enum
        {
            if (configuration is null)
                return defaultValue;
            // null if no key in startup args string
            // null if --Rewiew or -r
            // "" if --Rewiew=
            var valueString = configuration[key];
            if (String.IsNullOrEmpty(valueString))
                return defaultValue;            
            return GetValue_Enum<T>(valueString!, defaultValue);
        }

        public static T GetValue_Enum<T>(string value, T defaultValue)
            where T : struct, System.Enum
        {
            if (!Enum.TryParse<T>(value, out var result))
                return defaultValue;
            else
                return result;
        }

        /// <summary>
        ///     Returns readable and/or writable file name or String.Empty.
        ///     Logs with Error log level.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="key"></param>
        /// <param name="defaultFileFullName"></param>
        /// <param name="canRead"></param>
        /// <param name="canWrite"></param>
        /// <param name="loggersSet"></param>
        /// <returns></returns>
        public static string GetValue_ValidatedFileName(IConfiguration configuration, string key, string defaultFileFullName, bool canRead, bool canWrite, ILoggersSet loggersSet)            
        {
            string fileName = GetValue<string>(configuration, key, defaultFileFullName);
            if (fileName == @"")
                return @"";

            if (canRead)
            {
                bool fileExists = File.Exists(fileName);
                if (!fileExists)
                {
                    using var fileNameScope = loggersSet.LoggerAndUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                    loggersSet.LoggerAndUserFriendlyLogger.LogWarning(Properties.Resources.FileDoesNotExist);
                    return @"";
                }

                try
                {
                    using (var fs = File.OpenRead(fileName))
                    {
                        if (!fs.CanRead)
                        {
                            using var fileNameScope = loggersSet.LoggerAndUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                            loggersSet.LoggerAndUserFriendlyLogger.LogWarning(Properties.Resources.FileIsNotReadable);
                            return @"";
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    using var fileNameScope = loggersSet.LoggerAndUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                    loggersSet.Logger.LogWarning(ex, @"GetValue_FileName(...) Exception.");
                    loggersSet.LoggerAndUserFriendlyLogger.LogWarning(Properties.Resources.FileIsNotReadable);
                    return @"";
                }
            }

            if (canWrite)
            {
                try
                {
                    using (var fs = File.OpenWrite(fileName))
                    {
                        if (!fs.CanWrite)
                        {
                            using var fileNameScope = loggersSet.LoggerAndUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                            loggersSet.LoggerAndUserFriendlyLogger.LogWarning(Properties.Resources.FileIsNotWritable);
                            return @"";
                        }
                    }
                }
                catch (Exception ex)
                {
                    using var fileNameScope = loggersSet.LoggerAndUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                    loggersSet.Logger.LogWarning(ex, @"GetValue_FileName(...) Exception.");
                    loggersSet.LoggerAndUserFriendlyLogger.LogWarning(Properties.Resources.FileIsNotWritable);
                    return @"";
                }
            }

            return fileName;
        }

        /// <summary>
        ///     Uses IsMainProcess configuration key.
        ///     Returns true if main process, false if additional.
        ///     Default is true.
        /// </summary>
        /// <remarks>
        ///     Additional processes are used in load balancing scenarios.
        /// </remarks>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static bool IsMainProcess(IConfiguration? configuration)
        {
            return GetValue(configuration, ConfigurationConstants.ConfigurationKey_IsMainProcess, true);
        }
        
        /// <summary>
        ///     Default is 'Production' environment.
        /// </summary>
        /// <param name="hostEnvironment"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetYamlConfigurationFilePaths(IHostEnvironment? hostEnvironment)
        {
            string? environmentName = hostEnvironment?.EnvironmentName;
            if (String.IsNullOrEmpty(environmentName))
                environmentName = @"Production";

            yield return $"appsettings.yml";
            yield return $"appsettings.{environmentName}.yml";            
        }

        public static void SetCurrentDirectory(string[] args)
        {
            int i = Array.FindIndex(args, a => String.Equals(a, "--" + ConfigurationConstants.ConfigurationKey_CurrentDirectory, StringComparison.InvariantCultureIgnoreCase) ||
                a == ConfigurationConstants.ConfigurationKeyMapping_CurrentDirectory);
            if (i == -1 || i + 1 >= args.Length)
                return;

            string currentDirectory = args[i + 1];
            if (!String.IsNullOrEmpty(currentDirectory))
            {
                // Creates all directories and subdirectories in the specified path unless they already exist.
                Directory.CreateDirectory(currentDirectory);
                Directory.SetCurrentDirectory(currentDirectory);
            }
        }

        #endregion
    }

    /// <summary>
    ///     Interface for configuration values processing.
    /// </summary>    
    public interface IConfigurationProcessor
    {
        [return: NotNullIfNotNull("value")]
        string? ProcessValue(string? value);
    }
}
