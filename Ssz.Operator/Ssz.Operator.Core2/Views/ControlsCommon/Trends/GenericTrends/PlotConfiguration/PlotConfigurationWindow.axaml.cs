
namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.PlotConfiguration
{
    //public partial class PlotConfigurationWindow
    //{
    //    #region construction and destruction

    //    public PlotConfigurationWindow(PlotConfigurationViewModel viewModel)
    //    {
    //        InitializeComponent();

    //        DataContext = _viewModel = viewModel;
    //    }

    //    #endregion

    //    #region private functions

    //    private void onColorButtonClicked(object sender, RoutedEventArgs e)
    //    {
    //        var fe = sender as FrameworkElement;
    //        if (fe == null)
    //            return;

    //        var trendConfigurationViewModel = fe.DataContext as TrendConfigurationViewModel;
    //        if (trendConfigurationViewModel == null)
    //            return;

    //        Color? newColor = WpfColorDialog.Show(trendConfigurationViewModel.Color);
    //        if (newColor != null)
    //            trendConfigurationViewModel.Color = newColor.Value;
    //    }

    //    private void onAssignTrendButtonClicked(object sender, RoutedEventArgs e)
    //    {
    //        TrendConfigurationViewModel firstUnassignedTrend = _viewModel.FirstUnassignedTrend();

    //        // All trends are assigned to variables. Can't add new trend?
    //        if (firstUnassignedTrend == null)
    //            return;

    //        var unassignedTrendCopy = new TrendConfigurationViewModel(firstUnassignedTrend);
    //        bool? result = new AssignTrendToVariableWindow(unassignedTrendCopy) {Owner = this}.ShowDialog();

    //        if (result == true)
    //            firstUnassignedTrend.CopyFrom(unassignedTrendCopy);
    //    }

    //    private void onTrendDetailsButtonClicked(object sender, RoutedEventArgs e)
    //    {
    //        var trendConfiguration = TrendsListBox.SelectedItem as TrendConfigurationViewModel;
    //        if (trendConfiguration == null)
    //            return;

    //        var trendConfigurationCopy = new TrendConfigurationViewModel(trendConfiguration);

    //        bool? result = new TrendConfigurationWindow(trendConfigurationCopy) {Owner = this}.ShowDialog();

    //        if (result == true)
    //            trendConfiguration.CopyFrom(trendConfigurationCopy);
    //    }

    //    private void onClearTrendButtonClicked(object sender, RoutedEventArgs e)
    //    {
    //        var trendConfigurationViewModel = TrendsListBox.SelectedItem as TrendConfigurationViewModel;
    //        if (trendConfigurationViewModel == null)
    //            return;

    //        trendConfigurationViewModel.Unassign();
    //    }

    //    private void onOkButtonClicked(object sender, RoutedEventArgs e)
    //    {
    //        DialogResult = true;
    //    }

    //    private void onCancelButtonClicked(object sender, RoutedEventArgs e)
    //    {
    //        DialogResult = false;
    //    }

    //    #endregion

    //    #region private fields

    //    private readonly PlotConfigurationViewModel _viewModel;

    //    #endregion
    //}
}