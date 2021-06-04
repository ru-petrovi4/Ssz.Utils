using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Ssz.Utils.Wpf.Converters;

namespace Ssz.Utils.Wpf.EventSourceModel
{
    public class AlarmTypeBrushesBase
    {
        #region public functions

        public SolidColorBrush AlarmCategory0Brush { get; set; } = null!;
        public SolidColorBrush AlarmCategory1Brush { get; set; } = null!;
        public SolidColorBrush AlarmCategory2Brush { get; set; } = null!;
        public Brush AlarmCategory0BlinkingBrush { get; set; } = null!;
        public Brush AlarmCategory1BlinkingBrush { get; set; } = null!;
        public Brush AlarmCategory2BlinkingBrush { get; set; } = null!;

        #endregion
    }

}