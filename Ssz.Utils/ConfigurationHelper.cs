using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
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
        public static T GetValue<T>(IConfiguration configuration, string key, T defaultValue)
            where T : notnull
        {
            // null if no key in startup args string
            // null if --Rewiew or -r
            // "" if --Rewiew=
            var valueString = configuration[key];
            if (String.IsNullOrEmpty(valueString))
                return defaultValue;
            var result = new Any(valueString).ValueAs<T>(false);
            if (result is null)
                return defaultValue;
            return result;
        }

        public static T GetValue_Base64<T>(IConfiguration configuration, string key, T defaultValue)
            where T : notnull
        {
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

        public static T GetValue_Enum<T>(IConfiguration configuration, string key, T defaultValue)
            where T : struct, System.Enum
        {
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
        /// <param name="defaultFileName"></param>
        /// <param name="canRead"></param>
        /// <param name="canWrite"></param>
        /// <param name="loggersSet"></param>
        /// <returns></returns>
        public static string GetValue_FileName(IConfiguration configuration, string key, string defaultFileName, bool canRead, bool canWrite, ILoggersSet loggersSet)            
        {
            string fileName = GetValue<string>(configuration, key, defaultFileName);            

            if (canRead)
            {
                bool fileExists = File.Exists(fileName);
                if (!fileExists)
                {
                    using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                    loggersSet.WrapperUserFriendlyLogger.LogError(Properties.Resources.FileDoesNotExist);
                    return @"";
                }

                try
                {
                    using (var fs = File.OpenRead(fileName))
                    {
                        if (!fs.CanRead)
                        {
                            using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                            loggersSet.WrapperUserFriendlyLogger.LogError(Properties.Resources.FileIsNotReadable);
                            return @"";
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                    loggersSet.Logger.LogError(ex, @"GetValue_FileName(...) Exception.");
                    loggersSet.WrapperUserFriendlyLogger.LogError(Properties.Resources.FileIsNotReadable);
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
                            using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                            loggersSet.WrapperUserFriendlyLogger.LogError(Properties.Resources.FileIsNotWritable);
                            return @"";
                        }
                    }
                }
                catch (Exception ex)
                {
                    using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                    loggersSet.Logger.LogError(ex, @"GetValue_FileName(...) Exception.");
                    loggersSet.WrapperUserFriendlyLogger.LogError(Properties.Resources.FileIsNotWritable);
                    return @"";
                }
            }

            return fileName;
        }

        /// <summary>
        ///     Uses IsSecondaryProcess configuration key.
        ///     Returns false if primary process, true if secondary.
        /// </summary>
        /// <remarks>
        ///     Secondary process is used in load balancing scenarios.
        /// </remarks>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static bool IsSecondaryProcess(IConfiguration configuration)
        {
            return GetValue(configuration, @"IsSecondaryProcess", false);
        }

        /// <summary>
        ///     Uses ProgramDataDirectory configuration key.
        ///     Returns ProgramDataDirectory full path.
        ///     Expands environmental variables, if any. 
        ///     if ProgramDataDirectory is relative path, AppContext.BaseDirectory is used for relation.
        ///     if ProgramDataDirectory is not configured, returns app current directory.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetProgramDataDirectoryFullName(IConfiguration configuration)
        {
            string programDataDirectoryFullName = GetValue<string>(configuration, @"ProgramDataDirectory", @"");
            if (programDataDirectoryFullName != @"")
            {
                programDataDirectoryFullName = Environment.ExpandEnvironmentVariables(programDataDirectoryFullName);

                if (!Path.IsPathRooted(programDataDirectoryFullName))
                    programDataDirectoryFullName = Path.Combine(AppContext.BaseDirectory, programDataDirectoryFullName);

                // Creates all directories and subdirectories in the specified path unless they already exist.
                Directory.CreateDirectory(programDataDirectoryFullName);
            }
            else
            {
                programDataDirectoryFullName = Directory.GetCurrentDirectory();
            }   

            return programDataDirectoryFullName;
        }

        #endregion
    }

    /// <summary>
    ///     Interface for configuration values processing.
    /// </summary>
    public interface IConfigurationProcessor
    {
        string ProcessValue(string value);
    }
}
