namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.Settings
{
    public class SettingsDesignData : SettingsViewModel
    {
        #region construction and destruction

        public SettingsDesignData() : base(plotWithTemplate())
        {
        }

        #endregion

        #region private functions

        private static GenericTrendsPlotView plotWithTemplate()
        {
            var plot = new GenericTrendsPlotView();
            plot.ApplyTemplate();

            return plot;
        }

        #endregion
    }
}