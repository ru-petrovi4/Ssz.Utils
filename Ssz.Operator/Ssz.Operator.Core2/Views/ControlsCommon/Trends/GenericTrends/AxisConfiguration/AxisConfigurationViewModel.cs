using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.AxisConfiguration
{
    public class AxisConfigurationViewModel : ViewModelBase
    {
        #region construction and destruction

        public AxisConfigurationViewModel(string variableId, double minimum, double maximum)
        {
            Title = string.Format("Установить Шкалу Y для {0}", variableId);
            Minimum = minimum;
            Maximum = maximum;
        }

        #endregion

        #region public functions

        public string Title { get; private set; }

        public double Minimum { get; set; }
        public double Maximum { get; set; }

        #endregion
    }

    public class AxisConfigurationDesignData : AxisConfigurationViewModel
    {
        #region construction and destruction

        public AxisConfigurationDesignData() :
            base("TCA2008/PID1/PV/CV", -50, 150)
        {
        }

        #endregion
    }
}