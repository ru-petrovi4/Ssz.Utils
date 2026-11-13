using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.MultiValueConverters;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.DsShapeViews
{
    public static class StyledElementExtentions
    {
        #region public functions

        public static (BindingExpressionBase?, MultiBinding?) SetBindingOrConst(this StyledElement styledElement,            
            IDsContainer? container,
            AvaloniaProperty avaloniaProperty, 
            IValueDataBinding dataSourceInfo, 
            BindingMode bindingMode,
            UpdateSourceTrigger updateSourceTrigger, 
            bool forceSetConst = false)
        {
            if (dataSourceInfo.IsConst || forceSetConst)
            {
                try
                {
                    SetConstValue(styledElement, avaloniaProperty, dataSourceInfo, container);                    
                }
                catch (Exception)
                {
                    styledElement.SetValue(avaloniaProperty, AvaloniaProperty.UnsetValue);
                }

                return (null, null);
            }

            (MultiBinding multiBinding, List<string> dataSourceStrings) = CreateBindingWithoutConverter(                 
                container, 
                dataSourceInfo, 
                bindingMode,
                updateSourceTrigger);

            multiBinding.Converter = dataSourceInfo.GetConverterOrDefaultConverter(container, dataSourceStrings);
            if (dataSourceInfo.FallbackValue is not null)
                multiBinding.FallbackValue = dataSourceInfo.FallbackValue;

            (styledElement.DataContext as DataValueViewModel)?.RegisterMultiBinding(multiBinding);

            return (styledElement.Bind(avaloniaProperty, multiBinding), multiBinding);
        }        

        public static (BindingExpressionBase?, MultiBinding?) SetVisibilityBindingOrConst(this StyledElement styledElement,            
            IDsContainer? container,
            AvaloniaProperty avaloniaProperty, BooleanDataBinding dataSourceInfo, bool valueWhenInvisible,
            bool forceSetConst = false)
        {
            if (dataSourceInfo.IsConst || forceSetConst)
            {
                styledElement.SetValue(avaloniaProperty,
                    dataSourceInfo.ConstValue ? true : valueWhenInvisible);
                return (null, null);
            }

            (MultiBinding multiBinding, List<string> dataSourceStrings) = CreateBindingWithoutConverter(                
                container, 
                dataSourceInfo,
                BindingMode.OneWay, 
                UpdateSourceTrigger.Default);

            multiBinding.Converter =
                new VisibilityConverter(dataSourceInfo.GetConverterOrDefaultConverter(container), valueWhenInvisible);
            if (dataSourceInfo.FallbackValue is not null)
                multiBinding.FallbackValue = dataSourceInfo.FallbackValue;

            (styledElement.DataContext as DataValueViewModel)?.RegisterMultiBinding(multiBinding);

            return (styledElement.Bind(avaloniaProperty, multiBinding), multiBinding);
        }

        public static void SetConst(this StyledElement dependencyObject,
            IDsContainer? container,
            AvaloniaProperty dependencyProperty,
            object? constObject)
        {
            if (ReferenceEquals(constObject, null))
                dependencyObject.SetValue(dependencyProperty, AvaloniaProperty.UnsetValue);
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
                    dependencyObject.SetValue(dependencyProperty, AvaloniaProperty.UnsetValue);
                }
        }

        public static (MultiBinding, List<string>) CreateBindingWithoutConverter(            
            IDsContainer? container,
            IValueDataBinding dataSourceInfo,
            BindingMode bindingMode,
            UpdateSourceTrigger updateSourceTrigger)
        {
            var multiBinding = new MultiBinding();
            List<string> dataSourceStrings = new();
            foreach (DataBindingItem dataBindingItem in dataSourceInfo.DataBindingItemsCollection)
            {
                string dataSourceString = DataItemHelper.GetDataSourceString(dataBindingItem.Type,
                                             dataBindingItem.IdString, dataBindingItem.DefaultValue, container);
                CompiledBindingExtension binding =
                    new CompiledBindingExtension(new CompiledBindingPathBuilder().Property( // !!!WARNING CompiledBindingPathBuilder(1)
                        new ClrPropertyInfo("Item",
                            obj0 => ((DataValueViewModel)obj0)[dataSourceString],
                            (obj0, obj1) => ((DataValueViewModel)obj0)[dataSourceString] = obj1,
                            typeof(object)),
                        new Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor>(PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)).Build());
                binding.Mode = bindingMode;
                //binding.UpdateSourceTrigger = updateSourceTrigger;
                //var binding = new Binding
                //{                    
                //    Path = "[" + DataItemHelper.GetDataSourceString(dataBindingItem.Type,
                //                             dataBindingItem.IdString, dataBindingItem.DefaultValue, container) + "]",
                //    Mode = bindingMode
                //};
                multiBinding.Bindings.Add(binding);
                dataSourceStrings.Add(dataSourceString);
            }
            multiBinding.Mode = bindingMode;            
            return (multiBinding, dataSourceStrings);
        }        

        #endregion

        #region private functions

        /// <summary>
        ///     Puts the value a project holds into the property of the control.
        ///     <para>
        ///         What a project holds need not suit the property - a width that is not a number, a
        ///         colour that is not one. WPF threw there and the caller left the property as it was.
        ///         Here the value is awaited first, so a throw after that await reaches no caller and
        ///         ends the editor; it is answered here instead.
        ///     </para>
        /// </summary>
        private static async void SetConstValue(StyledElement styledElement, AvaloniaProperty avaloniaProperty, IValueDataBinding dataSourceInfo, IDsContainer? container)
        {
            try
            {
                styledElement.SetValue(avaloniaProperty, await dataSourceInfo.GetParsedConstObjectAsync(container));
            }
            catch (Exception)
            {
                try
                {
                    styledElement.SetValue(avaloniaProperty, AvaloniaProperty.UnsetValue);
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion        
    }
}