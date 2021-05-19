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
        public static CultureInfo SystemCultureInfo { get; set; } = CultureInfo.InvariantCulture;
        
        public static IConfiguration? Configuration { get; private set; }

        /// <summary>
        ///     appSettings.json -> AppSettings section.
        /// </summary>
        public static IConfiguration AppSettings
        {
            get
            {
                if (Configuration == null) throw new InvalidOperationException();
                return Configuration.GetSection(@"AppSettings");
            }
        }

        /// <summary>
        ///     If configuration is unknown, use new ConfigurationBuilder().AddJsonFile(@"appSettings.json", optional: true, reloadOnChange: true).Build() 
        ///     Initializes SystemCultureInfo field to operating system culture.        
        ///     Sets CurrentUICulture from appSettings.json -> AppSettings -> UICulture for all threads, if setting exists.
        ///     Otherwise, CurrentUICulture remains unchanged.
        /// </summary>
        /// <param name="configuration"></param>
        public static void Initialize(IConfiguration configuration)
        {
            Configuration = configuration;
            SystemCultureInfo = Thread.CurrentThread.CurrentCulture;
            
            string uiCultureName = AppSettings["UICulture"];
            if (!String.IsNullOrWhiteSpace(uiCultureName))
            {
                try
                {
                    var uiCultureInfo = new CultureInfo(uiCultureName);
                    CultureInfo.DefaultThreadCurrentUICulture = uiCultureInfo;
                    Thread.CurrentThread.CurrentUICulture = uiCultureInfo;
                }
                catch
                {
                    //Logger.Error(ex, "App settings file error. UI Culture not found: " + uiCultureName);
                }
            }
        }

        #endregion
    }
}


 //= new ConfigurationBuilder()
 //           .AddJsonFile(@"appSettings.json", optional: true, reloadOnChange: true)
 //           .Build()
 //           .GetSection(@"AppSettings");