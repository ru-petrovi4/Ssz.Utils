using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;
using Ssz.Utils;
using Ssz.Utils.Serialization;

namespace Ssz.WpfHmi.Common.ModelEngines
{
    public class AlarmTypeBrushes
    {
        public AlarmTypeBrushes()
        {
            AlarmCategory0Brush = Brushes.Lime;
            AlarmCategory1Brush = Brushes.Yellow;
            AlarmCategory2Brush = Brushes.Red;
            AlarmCategory0BlinkingBrush = GetBrush(Colors.Lime, Colors.Transparent);
            AlarmCategory1BlinkingBrush = GetBrush(Colors.Yellow, Colors.Transparent);
            AlarmCategory2BlinkingBrush = GetBrush(Colors.Red, Colors.Transparent);
        }

        #region public functions

        public SolidColorBrush AlarmCategory0Brush { get; set; }
        public SolidColorBrush AlarmCategory1Brush { get; set; }
        public SolidColorBrush AlarmCategory2Brush { get; set; }
        public Brush AlarmCategory0BlinkingBrush { get; set; }
        public Brush AlarmCategory1BlinkingBrush { get; set; }
        public Brush AlarmCategory2BlinkingBrush { get; set; }

        #endregion

        private static SolidColorBrush GetBrush(Color firstColor, Color secondColor)
        {
            SolidColorBrush? brush;
            _Brushes.TryGetValue(Tuple.Create(firstColor, secondColor), out brush);
            if (brush is null)
            {
                brush = new SolidColorBrush(firstColor);

                // TODO:
                //BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding
                //{
                //    Source =
                //        Project.Instance,
                //    Path =
                //        new PropertyPath(
                //            @"GlobalUITimerPhase"),
                //    Converter =
                //        Int32ToColorConverter
                //            .Instanse,
                //    ConverterParameter =
                //        new[]
                //        {firstColor, secondColor}
                //});

                _Brushes.Add(Tuple.Create(firstColor, secondColor), brush);
            }
            return brush;
        }

        private static readonly Dictionary<Tuple<Color, Color>, SolidColorBrush> _Brushes =
            new Dictionary<Tuple<Color, Color>, SolidColorBrush>();
    }

}