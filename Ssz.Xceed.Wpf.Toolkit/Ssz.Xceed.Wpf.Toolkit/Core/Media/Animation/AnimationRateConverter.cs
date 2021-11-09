/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace Ssz.Xceed.Wpf.Toolkit.Media.Animation
{
    public class AnimationRateConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            return t == typeof(string)
                   || t == typeof(double)
                   || t == typeof(int)
                   || t == typeof(TimeSpan);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor)
                   || destinationType == typeof(string)
                   || destinationType == typeof(double)
                   || destinationType == typeof(TimeSpan);
        }

        public override object ConvertFrom(
            ITypeDescriptorContext td,
            CultureInfo cultureInfo,
            object value)
        {
            var valueType = value.GetType();
            if (value is string)
            {
                var stringValue = value as string;
                if ((value as string).Contains(":"))
                {
                    var duration = TimeSpan.Zero;
                    duration = (TimeSpan) TypeDescriptor.GetConverter(duration).ConvertFrom(td, cultureInfo, value);
                    return new AnimationRate(duration);
                }

                double speed = 0;
                speed = (double) TypeDescriptor.GetConverter(speed).ConvertFrom(td, cultureInfo, value);
                return new AnimationRate(speed);
            }

            if (valueType == typeof(double))
                return (AnimationRate) (double) value;
            if (valueType == typeof(int))
                return (AnimationRate) (int) value;
            return (AnimationRate) (TimeSpan) value;
        }

        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo cultureInfo,
            object value,
            Type destinationType)
        {
            if (destinationType is not null && value is AnimationRate)
            {
                var rateValue = (AnimationRate) value;

                if (destinationType == typeof(InstanceDescriptor))
                {
                    MemberInfo mi;
                    if (rateValue.HasDuration)
                    {
                        mi = typeof(AnimationRate).GetConstructor(new[] {typeof(TimeSpan)});
                        return new InstanceDescriptor(mi, new object[] {rateValue.Duration});
                    }

                    if (rateValue.HasSpeed)
                    {
                        mi = typeof(AnimationRate).GetConstructor(new[] {typeof(double)});
                        return new InstanceDescriptor(mi, new object[] {rateValue.Speed});
                    }
                }
                else if (destinationType == typeof(string))
                {
                    return rateValue.ToString();
                }
                else if (destinationType == typeof(double))
                {
                    return rateValue.HasSpeed ? rateValue.Speed : 0.0d;
                }
                else if (destinationType == typeof(TimeSpan))
                {
                    return rateValue.HasDuration ? rateValue.Duration : TimeSpan.FromSeconds(0);
                }
            }

            return base.ConvertTo(context, cultureInfo, value, destinationType);
        }
    }
}