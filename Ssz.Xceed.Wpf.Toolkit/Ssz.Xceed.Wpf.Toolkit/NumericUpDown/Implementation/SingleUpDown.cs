﻿/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Windows;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class SingleUpDown : CommonNumericUpDown<float>
    {
        #region Constructors

        static SingleUpDown()
        {
            UpdateMetadata(typeof(SingleUpDown), 1f, float.NegativeInfinity, float.PositiveInfinity);
        }

        public SingleUpDown()
            : base(float.Parse, decimal.ToSingle, (v1, v2) => v1 < v2, (v1, v2) => v1 > v2)
        {
        }

        #endregion //Constructors

        #region Properties

        #region AllowInputSpecialValues

        public static readonly DependencyProperty AllowInputSpecialValuesProperty =
            DependencyProperty.Register("AllowInputSpecialValues", typeof(AllowedSpecialValues), typeof(SingleUpDown),
                new UIPropertyMetadata(AllowedSpecialValues.None));

        public AllowedSpecialValues AllowInputSpecialValues
        {
            get => (AllowedSpecialValues) GetValue(AllowInputSpecialValuesProperty);
            set => SetValue(AllowInputSpecialValuesProperty, value);
        }

        #endregion //AllowInputSpecialValues

        #endregion

        #region Base Class Overrides

        protected override float? OnCoerceIncrement(float? baseValue)
        {
            if (baseValue.HasValue && float.IsNaN(baseValue.Value))
                throw new ArgumentException("NaN is invalid for Increment.");

            return base.OnCoerceIncrement(baseValue);
        }

        protected override float? OnCoerceMaximum(float? baseValue)
        {
            if (baseValue.HasValue && float.IsNaN(baseValue.Value))
                throw new ArgumentException("NaN is invalid for Maximum.");

            return base.OnCoerceMaximum(baseValue);
        }

        protected override float? OnCoerceMinimum(float? baseValue)
        {
            if (baseValue.HasValue && float.IsNaN(baseValue.Value))
                throw new ArgumentException("NaN is invalid for Minimum.");

            return base.OnCoerceMinimum(baseValue);
        }

        protected override float IncrementValue(float value, float increment)
        {
            return value + increment;
        }

        protected override float DecrementValue(float value, float increment)
        {
            return value - increment;
        }

        protected override void SetValidSpinDirection()
        {
            if (Value.HasValue && float.IsInfinity(Value.Value) && Spinner is not null)
                Spinner.ValidSpinDirection = ValidSpinDirections.None;
            else
                base.SetValidSpinDirection();
        }

        protected override float? ConvertTextToValue(string text)
        {
            var result = base.ConvertTextToValue(text);

            if (result is not null)
            {
                if (float.IsNaN(result.Value))
                    TestInputSpecialValue(AllowInputSpecialValues, AllowedSpecialValues.NaN);
                else if (float.IsPositiveInfinity(result.Value))
                    TestInputSpecialValue(AllowInputSpecialValues, AllowedSpecialValues.PositiveInfinity);
                else if (float.IsNegativeInfinity(result.Value))
                    TestInputSpecialValue(AllowInputSpecialValues, AllowedSpecialValues.NegativeInfinity);
            }

            return result;
        }

        #endregion
    }
}