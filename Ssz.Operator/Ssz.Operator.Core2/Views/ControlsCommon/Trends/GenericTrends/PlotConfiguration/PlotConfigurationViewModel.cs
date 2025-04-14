using System.Collections.Generic;
using System.Linq;
using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Utils;
using Ssz.Operator.Core.DsShapes.Trends;
using Ssz.Operator.Core;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.PlotConfiguration
{
    public class PlotConfigurationViewModel : ViewModelBase
    {
        #region construction and destruction

        public PlotConfigurationViewModel()
        {
            TrendConfigurationViewModels = new[]
            {
                new TrendConfigurationViewModel(Colors.Yellow),
                new TrendConfigurationViewModel(Colors.White),
                new TrendConfigurationViewModel(Colors.Cyan),
                new TrendConfigurationViewModel(Colors.DarkViolet),
                new TrendConfigurationViewModel(Colors.DarkGoldenrod),
                new TrendConfigurationViewModel(Colors.DarkGreen),
                new TrendConfigurationViewModel(Colors.DimGray),
                new TrendConfigurationViewModel(Colors.LimeGreen)
            };
        }

        public PlotConfigurationViewModel(TrendsViewModel currentTrendsConfiguration) :
            this()
        {
            List<TrendViewModel> trendViewModels = currentTrendsConfiguration.Items.ToList();
            for (int i = 0; i < 8; ++i)
            {
                if (i >= trendViewModels.Count)
                    break;

                TrendConfigurationViewModels[i].Color = trendViewModels[i].Color;
                TrendConfigurationViewModels[i].HdaId = trendViewModels[i].Source.HdaId;
            }
        }

        #endregion

        #region public functions

        public TrendConfigurationViewModel[] TrendConfigurationViewModels { get; private set; }

        public TrendConfigurationViewModel? FirstUnassignedTrend()
        {
            return TrendConfigurationViewModels.FirstOrDefault(vm => !vm.IsAssigned);
        }

        public DsTrendItem[] CreateTrendsInfo()
        {
            var trendsInfo = new List<DsTrendItem>();
            foreach (TrendConfigurationViewModel trendConfigurationViewModel in TrendConfigurationViewModels)
            {
                if (!trendConfigurationViewModel.IsAssigned)
                    continue;

                var trendInfo = new DsTrendItem(trendConfigurationViewModel.HdaId)
                {
                    DsBrush = new BrushDataBinding
                    {
                        ConstValue = new SolidDsBrush(trendConfigurationViewModel.Color)
                    }
                };

                trendsInfo.Add(trendInfo);
            }

            return trendsInfo.ToArray();
        }

        #endregion
    }
}