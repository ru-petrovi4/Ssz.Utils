namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.PlotConfiguration
{
    public class PlotConfigurationDesignData : PlotConfigurationViewModel
    {
        #region construction and destruction

        public PlotConfigurationDesignData()
        {
            TrendConfigurationViewModels[0].HdaId = "LCA2250/PID1/PV.CV";
            TrendConfigurationViewModels[1].HdaId = "LCA2250/PID1/SP.CV";
            TrendConfigurationViewModels[2].HdaId = "LCA2250/PID1/OUT.CV";
        }

        #endregion
    }
}