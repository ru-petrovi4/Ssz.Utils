using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
        ///     By default contains CultureInfo.InvariantCulture.
        ///     If CultureHelper.InitializeCulture() is called, contains CultureInfo that corresponds operating system culture.
        ///     SystemCultureInfo field is used in Utils.Any class when func param stringIsLocalized = True.
        /// </summary>
        public static CultureInfo SystemCultureInfo { get; set; } = CultureInfo.InvariantCulture;

        /// <summary>
        ///     If configuration is unknown, use new ConfigurationBuilder().AddJsonFile(@"appSettings.json", optional: true, reloadOnChange: true).Build() 
        ///     Initializes SystemCultureInfo field to operating system culture.        
        ///     Sets CurrentUICulture from configuration -> UICulture for all threads, if setting exists.
        ///     Otherwise, CurrentUICulture remains unchanged.
        /// </summary>
        /// <param name="configuration"></param>
        public static void Initialize(IConfiguration configuration)
        {            
            SystemCultureInfo = CultureInfo.CurrentCulture;
            
            string uiCultureName = configuration["UICulture"];
            if (!String.IsNullOrWhiteSpace(uiCultureName))
            {
                try
                {
                    var uiCultureInfo = new CultureInfo(uiCultureName);
                    CultureInfo.DefaultThreadCurrentUICulture = uiCultureInfo;
                    CultureInfo.CurrentUICulture = uiCultureInfo;
                }
                catch
                {
                    //Logger.Error(ex, "App settings file error. UI Culture not found: " + uiCultureName);
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


 //= new ConfigurationBuilder()
 //           .AddJsonFile(@"appSettings.json", optional: true, reloadOnChange: true)
 //           .Build()
 //           .GetSection(@"AppSettings");