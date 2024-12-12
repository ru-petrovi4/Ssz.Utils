using System;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.VisualEditors.AddonsCollectionEditor
{
    internal class AddonViewModel : ViewModelBase
    {
        #region construction and destruction

        public AddonViewModel(AddonBase addon)
        {
            Addon = addon;
        }


        public AddonViewModel(Guid unavailableAddonGuid, string unavailableAddonNameToDisplay)
        {
            UnavailableAddonGuid = unavailableAddonGuid;
            UnavailableAddonNameToDisplay = unavailableAddonNameToDisplay;
        }

        #endregion

        #region public functions

        public AddonBase? Addon { get; }

        public Guid? UnavailableAddonGuid { get; }

        public string? UnavailableAddonNameToDisplay { get; }

        public bool IsAvailable => Addon is not null;

        public bool IsChecked { get; set; }

        public string Header
        {
            get
            {
                if (Addon is not null)
                {
                    string result = Addon.Name;
                    if (!string.IsNullOrWhiteSpace(Addon.Desc)) result = result + @"; " + Addon.Desc;
                    return result;
                }

                return UnavailableAddonNameToDisplay + @" (" + Resources.AddonUnavailable + @")";
            }
        }

        public string ToolTip
        {
            get
            {
                if (Addon is not null)
                {
                    string result = @"Addon Version: " + Addon.Version + "\n";
                    result += @"Ssz.Operator.Play Version: " + Addon.SszOperatorVersion + "\n";
                    result += @"Full Path: " + Addon.DllFileFullName;
                    return result;
                }

                return "";
            }
        }

        #endregion
    }
}