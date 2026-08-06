using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Ssz.Operator.Core.ControlsCommon.Trends;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.Settings
{
    public partial class SettingsWindow : Window
    {
        #region construction and destruction

        public SettingsWindow()
        {
            InitializeComponent();
        }

        public SettingsWindow(TrendsPlotView plot) :
            this()
        {
            DataContext = _viewModel = new SettingsViewModel(plot);
        }

        #endregion

        #region private functions

        private async void OnPlotBackgroundRectanglePointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_viewModel is null || e.InitialPressMouseButton != MouseButton.Left)
                return;

            Color initialColor = (_viewModel.CustomPreset.PlotBackground as SolidColorBrush)?.Color ?? Colors.White;

            Color? newColor = await ColorDialogHelper.ShowAsync(this, initialColor);
            if (newColor is not null)
            {
                _viewModel.ChangeCustomPresetPlotBackgroundColor(newColor.Value);
                btnApply.IsEnabled = true;
            }
        }

        private async void OnPlotAreaBackgroundRectanglePointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_viewModel is null || e.InitialPressMouseButton != MouseButton.Left)
                return;

            Color initialColor = (_viewModel.CustomPreset.PlotAreaBackground as SolidColorBrush)?.Color ?? Colors.White;

            Color? newColor = await ColorDialogHelper.ShowAsync(this, initialColor);
            if (newColor is not null)
            {
                _viewModel.ChangeCustomPresetPlotAreaBackgroundColor(newColor.Value);
                btnApply.IsEnabled = true;
            }
        }

        private void OnUsePredefinedPresetsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            // _viewModel is null when window is being initialized,
            // and it raises IsCheckedChanged, and here we are.
            if (_viewModel is null || btnUsePredefinedPresets.IsChecked != true)
                return;

            _viewModel.ApplySelectedPredefinedPreset();
            btnApply.IsEnabled = true;
        }

        private void OnUseCustomPresetCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (_viewModel is null || btnUseCustomPreset.IsChecked != true)
                return;

            _viewModel.ApplyCustomPreset();
            btnApply.IsEnabled = true;
        }

        private void OnTestClicked(object? sender, RoutedEventArgs e)
        {
            _viewModel?.ApplyCustomPreset();
        }

        private void OnOkClicked(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void OnCancelClicked(object? sender, RoutedEventArgs e)
        {
            _viewModel?.RestoreInitialPlotSettings();
            Close(false);
        }

        private void OnApplyClicked(object? sender, RoutedEventArgs e)
        {
            btnApply.IsEnabled = false;
        }

        #endregion

        #region private fields

        private readonly SettingsViewModel? _viewModel;

        #endregion
    }
}
