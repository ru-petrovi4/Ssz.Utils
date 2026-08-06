using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.PlotConfiguration
{
    public partial class PlotConfigurationWindow : Window
    {
        #region construction and destruction

        public PlotConfigurationWindow()
        {
            InitializeComponent();
        }

        public PlotConfigurationWindow(PlotConfigurationViewModel viewModel) :
            this()
        {
            DataContext = _viewModel = viewModel;
        }

        #endregion

        #region private functions

        private async void OnColorButtonClicked(object? sender, RoutedEventArgs e)
        {
            var fe = sender as StyledElement;
            if (fe is null)
                return;

            var trendConfigurationViewModel = fe.DataContext as TrendConfigurationViewModel;
            if (trendConfigurationViewModel is null)
                return;

            Color? newColor = await ColorDialogHelper.ShowAsync(this, trendConfigurationViewModel.Color);
            if (newColor is not null)
                trendConfigurationViewModel.Color = newColor.Value;
        }

        private async void OnAssignTrendButtonClicked(object? sender, RoutedEventArgs e)
        {
            if (_viewModel is null)
                return;

            TrendConfigurationViewModel? firstUnassignedTrend = _viewModel.FirstUnassignedTrend();

            // All trends are assigned to variables. Can't add new trend?
            if (firstUnassignedTrend is null)
                return;

            var unassignedTrendCopy = new TrendConfigurationViewModel(firstUnassignedTrend);
            bool result = await new AssignTrendToVariableWindow(unassignedTrendCopy).ShowDialog<bool>(this);

            if (result)
                firstUnassignedTrend.CopyFrom(unassignedTrendCopy);
        }

        private async void OnTrendDetailsButtonClicked(object? sender, RoutedEventArgs e)
        {
            var trendConfiguration = TrendsListBox.SelectedItem as TrendConfigurationViewModel;
            if (trendConfiguration is null)
                return;

            var trendConfigurationCopy = new TrendConfigurationViewModel(trendConfiguration);

            bool result = await new TrendConfigurationWindow(trendConfigurationCopy).ShowDialog<bool>(this);

            if (result)
                trendConfiguration.CopyFrom(trendConfigurationCopy);
        }

        private void OnClearTrendButtonClicked(object? sender, RoutedEventArgs e)
        {
            var trendConfigurationViewModel = TrendsListBox.SelectedItem as TrendConfigurationViewModel;
            if (trendConfigurationViewModel is null)
                return;

            trendConfigurationViewModel.Unassign();
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

        private readonly PlotConfigurationViewModel? _viewModel;

        #endregion
    }
}
