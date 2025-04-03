using System.Windows.Media;
using Ssz.Operator.Core.ControlsCommon.Trends;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends.Settings
{
    public class ColorPreset
    {
        #region public functions

        public static ColorPreset[] Presets =
        {
            new ColorPreset
            {
                Name = "Default"                
            },
            new ColorPreset
            {
                Name = "Dark",
                PlotBackground = new SolidColorBrush(Color.FromArgb(255, 160, 178, 193)),
                PlotAreaBackground = Brushes.Black
            },
            new ColorPreset
            {
                Name = "Middle",
                PlotBackground = Brushes.SandyBrown,
                PlotAreaBackground = Brushes.LightSeaGreen
            },
            new ColorPreset
            {
                Name = "Light",
                PlotBackground = Brushes.White,
                PlotAreaBackground = Brushes.LightGray
            }
        };

        public static ColorPreset Custom()
        {
            return new ColorPreset
            {
                Name = "custom preset",
                PlotAreaBackground = Brushes.DarkGray,
                PlotBackground = Brushes.LightGray
            };
        }

        public static ColorPreset FromPlotValues(TrendsPlotView plot)
        {
            return new ColorPreset
            {
                Name = "from plot values",
                PlotBackground = plot.Plot.Background,
                PlotAreaBackground = plot.Plot.PlotAreaBackground
            };
        }

        public string Name { get; private set; }
        public Brush PlotAreaBackground { get; private set; }
        public Brush PlotBackground { get; private set; }

        public void Apply(TrendsPlotView plot)
        {
            if (Name == @"Default") return;
            plot.Plot.Background = PlotBackground;
            plot.Plot.PlotAreaBackground = PlotAreaBackground;
        }

        public ColorPreset WithPlotBackgroundColor(Color color)
        {
            return new ColorPreset
            {
                Name = Name,
                PlotAreaBackground = PlotAreaBackground,
                PlotBackground = new SolidColorBrush(color)
            };
        }

        public ColorPreset WithPlotAreaBackgroundColor(Color color)
        {
            return new ColorPreset
            {
                Name = Name,
                PlotAreaBackground = new SolidColorBrush(color),
                PlotBackground = PlotBackground
            };
        }

        public override bool Equals(object obj)
        {
            var that = obj as ColorPreset;
            if (that == null)
                return false;

            return PlotAreaBackground == that.PlotAreaBackground &&
                   PlotBackground == that.PlotBackground;
        }

        public override int GetHashCode()
        {
            return PlotBackground.GetHashCode() ^ PlotAreaBackground.GetHashCode();
        }

        #endregion
    }
}