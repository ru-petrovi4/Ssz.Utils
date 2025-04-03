using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.Settings
{
    public class SettingsViewModel : ViewModelBase
    {
        #region construction and destruction

        public SettingsViewModel(TrendsPlotView plot)
        {
            _plot = plot;

            _initialPlotSettings = ColorPreset.FromPlotValues(plot);
            _selectedPreset = ColorPresets.First();
        }

        #endregion

        #region public functions

        public IEnumerable<ColorPreset> ColorPresets
        {
            get { return ColorPreset.Presets; }
        }

        public ColorPreset SelectedPreset
        {
            get { return _selectedPreset; }
            set
            {
                if (SetValue(ref _selectedPreset, value) && value != null)
                    _selectedPreset.Apply(_plot);
            }
        }

        public ColorPreset CustomPreset
        {
            get { return _customPreset; }
            private set { SetValue(ref _customPreset, value); }
        }

        public void ApplySelectedPredefinedPreset()
        {
            if (_selectedPreset != null)
                _selectedPreset.Apply(_plot);
        }

        public void ApplyCustomPreset()
        {
            CustomPreset.Apply(_plot);
        }

        public void RestoreInitialPlotSettings()
        {
            _initialPlotSettings.Apply(_plot);
        }

        public void ChangeCustomPresetPlotBackgroundColor(Color color)
        {
            CustomPreset = CustomPreset.WithPlotBackgroundColor(color);
            ApplyCustomPreset();
        }

        public void ChangeCustomPresetPlotAreaBackgroundColor(Color color)
        {
            CustomPreset = CustomPreset.WithPlotAreaBackgroundColor(color);
            ApplyCustomPreset();
        }

        #endregion

        #region private fields

        private ColorPreset _selectedPreset;
        private readonly TrendsPlotView _plot;

        private ColorPreset _customPreset = ColorPreset.Custom();
        private readonly ColorPreset _initialPlotSettings;

        #endregion
    }
}