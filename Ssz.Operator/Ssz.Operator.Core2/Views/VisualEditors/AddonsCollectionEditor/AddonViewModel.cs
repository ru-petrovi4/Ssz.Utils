using System;
using System.Collections.ObjectModel;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.VisualEditors.AddonsCollectionEditor
{
    /// <summary>
    ///     One addon offered to the project: whether it is wanted, and what is known about it.
    ///     <para>
    ///         An addon a project asks for that is not on this machine is still listed, so that saving
    ///         the project does not quietly drop it.
    ///     </para>
    /// </summary>
    public class AddonViewModel : ViewModelBase
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
                    var result = Addon.Name;
                    if (!String.IsNullOrWhiteSpace(Addon.Desc)) result = result + @"; " + Addon.Desc;
                    return result;
                }

                return UnavailableAddonNameToDisplay + @" (" + Resources.AddonUnavailable + @")";
            }
        }

        public string ToolTip
        {
            get
            {
                if (Addon is null) return @"";

                var result = @"Addon Version: " + Addon.Version + Environment.NewLine;
                result += @"Ssz.Operator.Play Version: " + Addon.CoreLibraryVersion + Environment.NewLine;
                result += @"Full Path: " + Addon.DllFileFullName;
                return result;
            }
        }

        #endregion
    }

    /// <summary>
    ///     The addons a project may be given.
    /// </summary>
    public class AddonsCollectionEditorViewModel : ViewModelBase
    {
        #region public functions

        public ObservableCollection<AddonViewModel> ItemsSource { get; } = new();

        #endregion
    }
}
