using System.Windows.Media;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.PlotConfiguration
{
    public class TrendConfigurationViewModel : ViewModelBase
    {
        #region construction and destruction

        public TrendConfigurationViewModel(Color color, string hdaId = "")
        {
            _color = color;
            _hdaId = hdaId;
        }

        public TrendConfigurationViewModel(TrendConfigurationViewModel sourceToCopy)
        {
            CopyFrom(sourceToCopy);
        }

        #endregion

        #region public functions

        public Color Color
        {
            get { return _color; }
            set { SetValue(ref _color, value); }
        }

        public string HdaId
        {
            get { return _hdaId; }
            set { SetValue(ref _hdaId, value); }
        }

        public bool IsAssigned
        {
            get { return _hdaId != ""; }
        }

        public void CopyFrom(TrendConfigurationViewModel sourceToCopy)
        {
            Color = sourceToCopy.Color;
            HdaId = sourceToCopy.HdaId;
        }

        public void Unassign()
        {
            HdaId = "";
        }

        #endregion

        #region private fields

        private Color _color;
        private string _hdaId = "";

        #endregion
    }

    public class TrendConfigurationDesignData : TrendConfigurationViewModel
    {
        #region construction and destruction

        public TrendConfigurationDesignData() : base(Colors.Yellow, "LCA2250/PID1/PV.CV")
        {
        }

        #endregion
    }
}