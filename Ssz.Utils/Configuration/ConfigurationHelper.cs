﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
                    loggersSet.WrapperUserFriendlyLogger.LogWarning(Properties.Resources.FileDoesNotExist);
                    return @"";
                }

                try
                {
                    using (var fs = File.OpenRead(fileName))
                    {
                        if (!fs.CanRead)
                        {
                            using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                            loggersSet.WrapperUserFriendlyLogger.LogWarning(Properties.Resources.FileIsNotReadable);
                            return @"";
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                    loggersSet.Logger.LogWarning(ex, @"GetValue_FileName(...) Exception.");
                    loggersSet.WrapperUserFriendlyLogger.LogWarning(Properties.Resources.FileIsNotReadable);
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
                            loggersSet.WrapperUserFriendlyLogger.LogWarning(Properties.Resources.FileIsNotWritable);
                            return @"";
                        }
                    }
                }
                catch (Exception ex)
                {
                    using var fileNameScope = loggersSet.WrapperUserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, fileName));
                    loggersSet.Logger.LogWarning(ex, @"GetValue_FileName(...) Exception.");
                    loggersSet.WrapperUserFriendlyLogger.LogWarning(Properties.Resources.FileIsNotWritable);
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
            return GetValue(configuration, @"IsMainProcess", true);
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
        public static string GetProgramDataDirectoryFullName(IConfiguration? configuration)
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
        [return: NotNullIfNotNull("value")]
        string? ProcessValue(string? value);
    }
}
