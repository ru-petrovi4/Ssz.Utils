using System;
using System.Windows;
using System.Windows.Data;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.MultiValueConverters;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.DsShapeViews
{
    public static class DependencyObjectExtention
    {
        #region public functions

        public static BindingExpressionBase? SetBindingOrConst(this DependencyObject dependencyObject,
            IDsContainer? container,
            DependencyProperty dependencyProperty, IValueDataBinding dataSourceInfo, BindingMode bindingMode,
            UpdateSourceTrigger updateSourceTrigger, bool forceSetConst = false)
        {
            if (dataSourceInfo.IsConst || forceSetConst)
            {
                try
                {
                    dependencyObject.SetValue(dependencyProperty, dataSourceInfo.GetParsedConstObject(container));
                }
                catch (Exception)
                {
                    dependencyObject.SetValue(dependencyProperty, DependencyProperty.UnsetValue);
                }

                return null;
            }

            MultiBinding multiBinding = CreateBindingWithoutConverter(container, dataSourceInfo, bindingMode,
                updateSourceTrigger);

            multiBinding.Converter = dataSourceInfo.GetConverterOrDefaultConverter(container);
            multiBinding.FallbackValue = dataSourceInfo.FallbackValue;

            RegisterMultiBinding(dependencyObject, multiBinding);

            return BindingOperations.SetBinding(dependencyObject, dependencyProperty, multiBinding);
        }

        public static BindingExpressionBase? SetVisibilityBindingOrConst(this DependencyObject dependencyObject,
            IDsContainer? container,
            DependencyProperty dependencyProperty, BooleanDataBinding dataSourceInfo, Visibility valueWhenInvisible,
            bool forceSetConst = false)
        {
            if (dataSourceInfo.IsConst || forceSetConst)
            {
                dependencyObject.SetValue(dependencyProperty,
                    dataSourceInfo.ConstValue ? Visibility.Visible : valueWhenInvisible);
                return null;
            }

            MultiBinding multiBinding = CreateBindingWithoutConverter(container, dataSourceInfo,
                BindingMode.OneWay, UpdateSourceTrigger.Default);

            multiBinding.Converter =
                new VisibilityConverter(dataSourceInfo.GetConverterOrDefaultConverter(container), valueWhenInvisible);
            multiBinding.FallbackValue = dataSourceInfo.FallbackValue;

            RegisterMultiBinding(dependencyObject, multiBinding);

            return BindingOperations.SetBinding(dependencyObject, dependencyProperty, multiBinding);
        }

        public static void SetConst(this DependencyObject dependencyObject,
            IDsContainer? container,
            DependencyProperty dependencyProperty,
            object? constObject)
        {
            if (ReferenceEquals(constObject, null))
                dependencyObject.SetValue(dependencyProperty, DependencyProperty.UnsetValue);
            else
                try
                {
                    var constString = constObject as string;
                    if (constString is not null)
                    {
                        constString = ConstantsHelper.ComputeValue(container, constString);
                        dependencyObject.SetValue(dependencyProperty,
                            ObsoleteAnyHelper.ConvertTo(constString, dependencyProperty.PropertyType, false));
                    }
                    else
                    {
                        dependencyObject.SetValue(dependencyProperty, constObject);
                    }
                }
                catch (Exception)
                {
                    dependencyObject.SetValue(dependencyProperty, DependencyProperty.UnsetValue);
                }
        }

        public static MultiBinding CreateBindingWithoutConverter(
            IDsContainer? container,
            IValueDataBinding dataSourceInfo,
            BindingMode bindingMode,
            UpdateSourceTrigger updateSourceTrigger)
        {
            var multiBinding = new MultiBinding();
            foreach (DataBindingItem dataBindingItem in dataSourceInfo.DataBindingItemsCollection)
            {
                var binding = new Binding
                {
                    Path =
                        new PropertyPath("[" +
                                         DataItemHelper.GetDataSourceString(dataBindingItem.Type,
                                             dataBindingItem.IdString, dataBindingItem.DefaultValue, container) + "]"),
                    Mode = bindingMode
                };
                multiBinding.Bindings.Add(binding);
            }

            multiBinding.Mode = bindingMode;
            multiBinding.UpdateSourceTrigger = updateSourceTrigger;
            return multiBinding;
        }

        public static void RegisterMultiBinding(DependencyObject dependencyObject, MultiBinding? multiBinding)
        {
            if (multiBinding is null) 
                return;

            var fe = dependencyObject as FrameworkElement;
            if (fe is null) return;
            var dataValueViewModel = fe.DataContext as DataValueViewModel;
            if (dataValueViewModel is null) 
                return;
            dataValueViewModel.RegisterMultiBinding(multiBinding);
        }

        #endregion
    }
}