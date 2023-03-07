using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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

        #endregion
    }
}
