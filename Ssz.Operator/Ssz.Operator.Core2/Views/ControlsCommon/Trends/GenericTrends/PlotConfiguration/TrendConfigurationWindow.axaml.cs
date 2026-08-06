using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.PlotConfiguration
{
    public partial class TrendConfigurationWindow : Window
    {
        #region construction and destruction

        public TrendConfigurationWindow()
        {
            InitializeComponent();
        }

        public TrendConfigurationWindow(TrendConfigurationViewModel viewModel) :
            this()
        {
            DataContext = _viewModel = viewModel;
        }

        #endregion

        #region private functions

        private async void OnChangeTrendColorClicked(object? sender, RoutedEventArgs e)
        {
            if (_viewModel is null)
                return;

            Color? newColor = await ColorDialogHelper.ShowAsync(this, _viewModel.Color);

            if (newColor is not null)
                _viewModel.Color = newColor.Value;
        }

        private void OnOkButtonClicked(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void OnCancelButtonClicked(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        #endregion

        #region private fields

        private readonly TrendConfigurationViewModel? _viewModel;

        #endregion
    }
}
