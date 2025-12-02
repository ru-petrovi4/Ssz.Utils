using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Configuration;
using System.Globalization;
using System.Threading;

namespace Ssz.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class CultureHelper
    {
        #region public functions

        /// <summary>             
        ///     Sets CurrentUICulture from configuration["UICulture"] for all threads, if setting exists.
        ///     Otherwise, CurrentUICulture remains unchanged.        
        /// </summary>
        /// <param name="configuration"></param>
        public static void InitializeUICulture(IConfiguration configuration, ILogger logger)
        {            
            string? uiCultureName = ConfigurationHelper.GetValue<string>(configuration, ConfigurationHelper.ConfigurationKey_UICulture, @"");
            if (!String.IsNullOrWhiteSpace(uiCultureName))
            {
                try
                {
                    var uiCultureInfo = new CultureInfo(uiCultureName, true);
                    CultureInfo.CurrentUICulture = uiCultureInfo;
                    CultureInfo.DefaultThreadCurrentUICulture = uiCultureInfo;
                    logger.LogInformation("Current UI culture: " + uiCultureInfo.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "App settings file error. UI Culture not found: " + uiCultureName);                    
                }
            }
        }

        /// <summary>
        ///     Returns invariant culture for null or empty  or invalid cultureName.
        ///     Uses Windows-user overrides, if any.
        ///     No throws.
        /// </summary>
        /// <param name="cultureName"></param>
        /// <returns></returns>
        public static CultureInfo GetCultureInfo(string? cultureName)
        {
            if (String.IsNullOrEmpty(cultureName)) return CultureInfo.InvariantCulture;
            try
            {
                return new CultureInfo(cultureName, true);
            }
            catch
            {
                //Logger.Error(ex, "Error: " + cultureName);
                return CultureInfo.InvariantCulture;
            }
        }

        #endregion
    }
}


//     If configuration is unknown, use new ConfigurationBuilder().AddJsonFile(@"appSettings.json", optional: true, reloadOnChange: true).Build()    
//= new ConfigurationBuilder()
//           .AddJsonFile(@"appSettings.json", optional: true, reloadOnChange: true)
//           .Build()
//           .GetSection(@"AppSettings");