using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.AxisConfiguration
{
    public partial class AxisConfigurationWindow : Window
    {
        #region construction and destruction

        public AxisConfigurationWindow()
        {
            InitializeComponent();
        }

        public AxisConfigurationWindow(AxisConfigurationViewModel viewModel) :
            this()
        {
            DataContext = viewModel;
        }

        #endregion

        #region private functions

        private void OnOkButtonClicked(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void OnCancelButtonClicked(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        #endregion
    }
}
