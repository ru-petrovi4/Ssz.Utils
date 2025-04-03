using System.Windows;
using System.Windows.Media;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.PlotConfiguration
{
    public partial class TrendConfigurationWindow
    {
        #region construction and destruction

        public TrendConfigurationWindow(TrendConfigurationViewModel viewModel)
        {
            InitializeComponent();

            DataContext = _viewModel = viewModel;
        }

        #endregion

        #region private functions

        private void onChangeTrendColorClicked(object sender, RoutedEventArgs e)
        {
            Color? newColor = WpfColorDialog.Show(_viewModel.Color);

            if (newColor != null)
                _viewModel.Color = newColor.Value;
        }

        private void onOkButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void onCancelButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        #endregion

        #region private fields

        private readonly TrendConfigurationViewModel _viewModel;

        #endregion
    }
}