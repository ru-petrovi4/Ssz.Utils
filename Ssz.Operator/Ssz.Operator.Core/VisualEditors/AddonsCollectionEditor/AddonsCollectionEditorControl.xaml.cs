using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.VisualEditors.AddonsCollectionEditor
{
    public partial class AddonsCollectionEditorControl : UserControl
    {
        #region construction and destruction

        public AddonsCollectionEditorControl()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public List<GuidAndName> DesiredAdditionalAddonsInfo
        {
            get => GetAddonsInfo(((AddonsCollectionEditorViewModel) DataContext).ItemsSource);
            set => RefreshAddonsCollectionControl(value);
        }

        #endregion

        #region private functions

        private List<GuidAndName> GetAddonsInfo(IEnumerable<AddonViewModel> itemsSource)
        {
            var addonsInfo = new List<GuidAndName>();
            foreach (AddonViewModel pvm in itemsSource)
                if (pvm.IsChecked)
                {
                    if (pvm.IsAvailable)
                        addonsInfo.Add(new GuidAndName
                        {
                            Guid = pvm.Addon!.Guid,
                            Name = pvm.Addon.Name
                        });
                    else
                        addonsInfo.Add(new GuidAndName
                        {
                            Guid = pvm.UnavailableAddonGuid!.Value,
                            Name = pvm.UnavailableAddonNameToDisplay
                        });
                }

            return addonsInfo;
        }

        private void RefreshAddonsCollectionButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var oldViewModel = (AddonsCollectionEditorViewModel) DataContext;

            List<GuidAndName> desiredAdditionalAddonsInfo = GetAddonsInfo(oldViewModel.ItemsSource);

            RefreshAddonsCollectionControl(desiredAdditionalAddonsInfo);
        }

        private void RefreshAddonsCollectionControl(List<GuidAndName> desiredAdditionalAddonsInfo)
        {
            var viewModel = new AddonsCollectionEditorViewModel();

            AddonsManager.ResetAvailableAdditionalAddonsCache();
            AddonBase[] availableAdditionalAddons = AddonsManager.GetAvailableAdditionalAddonsCache();

            foreach (AddonBase availableAdditionalAddon in availableAdditionalAddons.OrderBy(p => p.Name))
                viewModel.ItemsSource.Add(new AddonViewModel(availableAdditionalAddon)
                {
                    IsChecked =
                        desiredAdditionalAddonsInfo.Any(i => i.Guid == availableAdditionalAddon.Guid)
                });

            foreach (GuidAndName guidAndName in desiredAdditionalAddonsInfo.OrderBy(p => p.Name))
            {
                if (availableAdditionalAddons.Any(p => p.Guid == guidAndName.Guid)) continue;

                viewModel.ItemsSource.Add(
                    new AddonViewModel(guidAndName.Guid, guidAndName.Name ?? "")
                    {
                        IsChecked = true
                    });
            }

            DataContext = viewModel;
        }

        #endregion
    }
}