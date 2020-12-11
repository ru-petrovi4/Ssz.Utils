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
        public static CultureInfo SystemCultureInfo = CultureInfo.InvariantCulture;

        /// <summary>
        ///     Initializes SystemCultureInfo field to operating system culture.        
        ///     Sets CurrentUICulture from AppSettings["UICulture"] for all threads, if setting exists in app.config.
        ///     Otherwise, CurrentUICulture remains unchanged.
        /// </summary>
        public static void InitializeCulture()
        {
            SystemCultureInfo = Thread.CurrentThread.CurrentCulture;

            // TODO:
            string uiCultureName = ""; //ConfigurationManager.AppSettings["UICulture"];
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