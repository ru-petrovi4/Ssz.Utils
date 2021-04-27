/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit
{
    [TemplatePart(Name = PART_SpectrumDisplay, Type = typeof(Rectangle))]
    public class ColorSpectrumSlider : Slider
    {
        private const string PART_SpectrumDisplay = "PART_SpectrumDisplay";

        #region Constructors

        static ColorSpectrumSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorSpectrumSlider),
                new FrameworkPropertyMetadata(typeof(ColorSpectrumSlider)));
        }

        #endregion //Constructors

        #region Methods

        private void CreateSpectrum()
        {
            _pickerBrush = new LinearGradientBrush();
            _pickerBrush.StartPoint = new Point(0.5, 0);
            _pickerBrush.EndPoint = new Point(0.5, 1);
            _pickerBrush.ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation;

            var colorsList = ColorUtilities.GenerateHsvSpectrum();

            var stopIncrement = (double) 1 / colorsList.Count;

            int i;
            for (i = 0; i < colorsList.Count; i++)
                _pickerBrush.GradientStops.Add(new GradientStop(colorsList[i], i * stopIncrement));

            _pickerBrush.GradientStops[i - 1].Offset = 1.0;
            _spectrumDisplay.Fill = _pickerBrush;
        }

        #endregion //Methods

        #region Private Members

        private Rectangle _spectrumDisplay;
        private LinearGradientBrush _pickerBrush;

        #endregion //Private Members

        #region Dependency Properties

        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register("SelectedColor",
            typeof(Color), typeof(ColorSpectrumSlider), new PropertyMetadata(Colors.Transparent));

        public Color SelectedColor
        {
            get => (Color) GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        #endregion //Dependency Properties

        #region Base Class Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _spectrumDisplay = (Rectangle) GetTemplateChild(PART_SpectrumDisplay);
            CreateSpectrum();
            OnValueChanged(double.NaN, Value);
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);

            var color = ColorUtilities.ConvertHsvToRgb(360 - newValue, 1, 1);
            SelectedColor = color;
        }

        #endregion //Base Class Overrides
    }
}