using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.PlotConfiguration
{
    public partial class AssignTrendToVariableWindow : Window
    {
        #region construction and destruction

        public AssignTrendToVariableWindow()
        {
            InitializeComponent();
        }

        public AssignTrendToVariableWindow(TrendConfigurationViewModel viewModel) :
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
