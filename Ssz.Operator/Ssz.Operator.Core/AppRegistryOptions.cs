using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core
{
    public static class AppRegistryOptions
    {
        #region public functions

        public const string SszOperatorSubKeyString = @"SOFTWARE\Ssz\Ssz.Operator";
        public const string TouchScreenRectValueName = @"TouchScreenRect";
        public const string TouchScreenModeValueName = @"TouchScreenMode";
        public const string PlayWindowsInfoValueName = @"PlayWindowsInfo";
        public const string VirtualKeyboardTypeValueName = @"VirtualKeyboardType";
        
        public const string DsPagesAndDsShapesLibrariesSubKeyString = @"DsPagesAndDsShapesLibraries";

        public static void GetScreensInfo(out Rect? touchScreen, out TouchScreenMode touchScreenMode,
            out List<WindowProps> rootWindowPropsCollection, out string virtualKeyboardType)
        {
            touchScreen = null;
            touchScreenMode = TouchScreenMode.MouseClick;
            rootWindowPropsCollection = new List<WindowProps>();
            virtualKeyboardType = "";

            var skSimcodeSszOperator = Registry.CurrentUser.OpenSubKey(SszOperatorSubKeyString);
            string sTouchScreenRect = skSimcodeSszOperator is not null
                ? (string) (skSimcodeSszOperator.GetValue(TouchScreenRectValueName, "") ?? "")
                : "";
            if (!string.IsNullOrWhiteSpace(sTouchScreenRect))
            {
                var touchScreenRect = ObsoleteAnyHelper.ConvertTo<Rect>(sTouchScreenRect, false);
                if (!touchScreenRect.IsEmpty && touchScreenRect != new Rect()) touchScreen = touchScreenRect;
            }

            string sTouchScreenMode = skSimcodeSszOperator is not null
                ? (string) (skSimcodeSszOperator.GetValue(TouchScreenModeValueName, "") ?? "")
                : "";
            if (!string.IsNullOrWhiteSpace(sTouchScreenMode))
                touchScreenMode = ObsoleteAnyHelper.ConvertTo<TouchScreenMode>(sTouchScreenMode, false);

            var skSimcodeSszOperatorDsProject =
                Registry.CurrentUser.OpenSubKey(SszOperatorSubKeyString + @"\" +
                                                GetDsProjectRegistryKeyString());
            string sPlayWindowsInfo = skSimcodeSszOperatorDsProject is not null
                ? (string) (skSimcodeSszOperatorDsProject.GetValue(PlayWindowsInfoValueName, "") ?? "")
                : "";
            if (!string.IsNullOrWhiteSpace(sPlayWindowsInfo))
            {
                foreach (string sPlayWindowInfo in sPlayWindowsInfo.Split('|'))
                {
                    if (string.IsNullOrWhiteSpace(sPlayWindowInfo)) continue;

                    var playWindowProps =
                        NameValueCollectionValueSerializer<WindowProps>.Instance.ConvertFromString(
                            sPlayWindowInfo, null) as WindowProps;
                    if (playWindowProps is null) continue;

                    rootWindowPropsCollection.Add(playWindowProps);
                }

                virtualKeyboardType = skSimcodeSszOperatorDsProject is not null
                    ? (string) (skSimcodeSszOperatorDsProject.GetValue(VirtualKeyboardTypeValueName, "") ?? "")
                    : "";
            }
        }

        public static void SaveScreensInfo(IEnumerable<WindowProps> rootWindowPropsCollection,
            string virtualKeyboardType)
        {
            if (!DsProject.Instance.IsInitialized) return;

            RegistryKey? skSimcodeSszOperator = null;
            try
            {
                skSimcodeSszOperator = Registry.CurrentUser.CreateSubKey(SszOperatorSubKeyString);
            }
            catch (Exception)
            {
            }

            if (skSimcodeSszOperator is null)
            {
                MessageBoxHelper.ShowError(Resources.UnauthorizedAcessMessage);
                return;
            }

            RegistryKey? skSimcodeSszOperatorDsProject = null;
            try
            {
                skSimcodeSszOperatorDsProject =
                    Registry.CurrentUser.CreateSubKey(SszOperatorSubKeyString + @"\" +
                                                      GetDsProjectRegistryKeyString());
            }
            catch (Exception)
            {
            }

            if (skSimcodeSszOperatorDsProject is null)
            {
                MessageBoxHelper.ShowError(Resources.UnauthorizedAcessMessage);
                return;
            }

            StringBuilder sPlayWindowsInfo = new();
            foreach (WindowProps rootWindowProps in rootWindowPropsCollection)
            {
                sPlayWindowsInfo.Append(
                    NameValueCollectionValueSerializer<WindowProps>.Instance.ConvertToString(rootWindowProps, null));
                sPlayWindowsInfo.Append("|");
            }

            skSimcodeSszOperatorDsProject.SetValue(PlayWindowsInfoValueName, sPlayWindowsInfo.ToString(),
                RegistryValueKind.String);
            skSimcodeSszOperatorDsProject.SetValue(VirtualKeyboardTypeValueName, virtualKeyboardType ?? "",
                RegistryValueKind.String);
        }

        public static void SaveScreensInfo(Rect? touchScreen, TouchScreenMode touchScreenMode)
        {
            RegistryKey? skSimcodeSszOperator = null;
            try
            {
                skSimcodeSszOperator = Registry.CurrentUser.CreateSubKey(SszOperatorSubKeyString);
            }
            catch (Exception)
            {
            }

            if (skSimcodeSszOperator is null)
            {
                MessageBoxHelper.ShowError(Resources.UnauthorizedAcessMessage);
                return;
            }

            string sTouchScreenRect = "";
            if (touchScreen.HasValue) sTouchScreenRect = ObsoleteAnyHelper.ConvertTo<string>(touchScreen.Value, false);
            skSimcodeSszOperator.SetValue(TouchScreenRectValueName, sTouchScreenRect, RegistryValueKind.String);
            skSimcodeSszOperator.SetValue(TouchScreenModeValueName, touchScreenMode.ToString(),
                RegistryValueKind.String);
        }

        public static string? GetDsProjectValue(string key)
        {
            if (!DsProject.Instance!.IsInitialized) return null;

            try
            {
                var skSimcodeSszOperatorDsProject =
                    Registry.CurrentUser.OpenSubKey(SszOperatorSubKeyString + @"\" +
                                                    DsProject.Instance!.DsProjectFileFullName!.Replace(Path.DirectorySeparatorChar, '_'));

                if (skSimcodeSszOperatorDsProject is null) return null;

                return (string) (skSimcodeSszOperatorDsProject.GetValue(key, null) ?? "");
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
                return null;
            }
        }

        public static void SetDsProjectValue(string key, string value)
        {
            if (!DsProject.Instance.IsInitialized) return;

            try
            {
                var skSimcodeSszOperatorDsProject = GetOrCreateSimcodeSszOperatorDsProjectRegistryKey();

                if (skSimcodeSszOperatorDsProject is null) return;

                skSimcodeSszOperatorDsProject.SetValue(key, value, RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }
        }

        #endregion

        #region private functions

        private static RegistryKey? GetOrCreateSimcodeSszOperatorDsProjectRegistryKey()
        {
            RegistryKey? skSimcodeSszOperator = null;
            try
            {
                skSimcodeSszOperator = Registry.CurrentUser.CreateSubKey(SszOperatorSubKeyString);
            }
            catch (Exception)
            {
            }

            if (skSimcodeSszOperator is null) return null;

            RegistryKey? skSimcodeSszOperatorDsProject = null;
            try
            {
                skSimcodeSszOperatorDsProject =
                    Registry.CurrentUser.CreateSubKey(SszOperatorSubKeyString + @"\" +
                                                      GetDsProjectRegistryKeyString());
            }
            catch (Exception)
            {
            }

            return skSimcodeSszOperatorDsProject;
        }

        private static string GetDsProjectRegistryKeyString()
        {
            return DsProject.Instance.DsProjectFileFullName?.Replace(Path.DirectorySeparatorChar, '_') ?? @"";
        }

        #endregion
    }
}