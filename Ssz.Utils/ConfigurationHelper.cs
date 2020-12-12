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
    public static class ConfigurationHelper
    {
        #region public functions

        /// <summary>
        ///     By default contains CultureInfo.InvariantCulture.
        ///     If CultureHelper.InitializeCulture() is called, contains CultureInfo that corresponds operating system culture.
        ///     SystemCultureInfo field is used in Utils.Any class when func param stringIsLocalized = True.
        /// </summary>
        public static CultureInfo SystemCultureInfo { get; private set; } = CultureInfo.InvariantCulture;

        /// <summary>
        ///     appSettings.json -> AppSettings section.
        /// </summary>
        public static IConfiguration AppSettings { get; private set; } = new ConfigurationBuilder()
            .AddJsonFile(@"appSettings.json", optional: true, reloadOnChange: true)
            .Build()
            .GetSection(@"AppSettings");

        /// <summary>
        ///     Initializes SystemCultureInfo field to operating system culture.        
        ///     Sets CurrentUICulture from appSettings.json -> AppSettings -> UICulture for all threads, if setting exists.
        ///     Otherwise, CurrentUICulture remains unchanged.
        /// </summary>
        public static void InitializeCulture()
        {            
            SystemCultureInfo = Thread.CurrentThread.CurrentCulture;
            
            string uiCultureName = AppSettings.GetSection("UICulture").Value;
            if (!String.IsNullOrWhiteSpace(uiCultureName))
            {
                try
                {
                    var uiCultureInfo = new CultureInfo(uiCultureName);
                    CultureInfo.DefaultThreadCurrentUICulture = uiCultureInfo;
                    Thread.CurrentThread.CurrentUICulture = uiCultureInfo;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "App settings file error. UI Culture not found: " + uiCultureName);
                }
            }
        }

        #endregion
    }
}