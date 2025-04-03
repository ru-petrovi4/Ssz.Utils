using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Ssz.Operator.Core.ControlsCommon.Trends;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.Settings
{
    public partial class SettingsWindow : Window
    {
        #region construction and destruction

        public SettingsWindow(TrendsPlotView plot)
        {
            InitializeComponent();

            DataContext = _viewModel = new SettingsViewModel(plot);
        }

        #endregion

        #region private functions

        private void onPlotBackgroundRectangleClicked(object sender, MouseButtonEventArgs e)
        {
            var solidColorBrush = _viewModel.CustomPreset.PlotBackground as SolidColorBrush;
            Color? initialColor = null;

            if (solidColorBrush != null)
                initialColor = solidColorBrush.Color;

            Color? newColor = WpfColorDialog.Show(initialColor);
            if (newColor != null)
            {
                _viewModel.ChangeCustomPresetPlotBackgroundColor(newColor.Value);
                btnApply.IsEnabled = true;
            }
        }

        private void onPlotAreaBackgroundRectangleClicked(object sender, MouseButtonEventArgs e)
        {
            var solidColorBrush = _viewModel.CustomPreset.PlotAreaBackground as SolidColorBrush;
            Color? initialColor = null;

            if (solidColorBrush != null)
                initialColor = solidColorBrush.Color;

            Color? newColor = WpfColorDialog.Show(initialColor);
            if (newColor != null)
            {
                _viewModel.ChangeCustomPresetPlotAreaBackgroundColor(newColor.Value);
                btnApply.IsEnabled = true;
            }
        }

        private void onUsePredefinedPresetsChecked(object sender, RoutedEventArgs e)
        {
            _viewModel.ApplySelectedPredefinedPreset();
            btnApply.IsEnabled = true;
        }

        private void onUseCustomPresetChecked(object sender, RoutedEventArgs e)
        {
            // _viewModel is null when window is being initialized,
            // and it throws a Checked event, and here we are.
            if (_viewModel != null)
            {
                _viewModel.ApplyCustomPreset();
                btnApply.IsEnabled = true;
            }
        }

        private void onTestClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.ApplyCustomPreset();
        }

        private void onOkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void onCancelClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.RestoreInitialPlotSettings();
            DialogResult = false;
        }

        private void onApplyClicked(object sender, RoutedEventArgs e)
        {
            btnApply.IsEnabled = false;
        }

        #endregion

        #region private fields

        private readonly SettingsViewModel _viewModel;

        #endregion
    }
}