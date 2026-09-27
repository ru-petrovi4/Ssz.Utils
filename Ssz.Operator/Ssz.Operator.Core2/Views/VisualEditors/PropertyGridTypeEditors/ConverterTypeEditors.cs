using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.MultiValueConverters;
using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.VisualEditors.Windows;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    /// <summary>
    ///     What every converter looks like in the grid: the rules it holds, written out, in a colour
    ///     that says whether they all make sense.
    /// </summary>
    public abstract class ConverterTypeEditorBase : DialogTypeEditorBase, IPropertyGridItem
    {
        #region public functions

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        /// <summary>
        ///     A converter belongs to the binding it converts for; with nothing bound there is nothing
        ///     to convert, so the converter goes.
        /// </summary>
        public void RefreshForPropertyGrid()
        {
            if (DataSourceInfo is not null && DataSourceInfo.DataBindingItemsCollection.Count == 0)
                DataSourceInfo.Converter = null;
        }

        public void EndEditInPropertyGrid()
        {
            RefreshForPropertyGrid();
        }

        #endregion

        #region protected functions

        protected IValueDataBinding? DataSourceInfo { get; set; }

        protected override IValueConverter ValueConverter => ValueConverterToTextConverter.Instance;

        protected void BindForeground()
        {
            MainButton.Bind(ForegroundProperty, new Binding(nameof(PropertyItemViewModel.Value))
            {
                Source = PropertyItem,
                Converter = ValueConverterToBrushConverter.Instance
            });
        }

        #endregion
    }

    /// <summary>
    ///     A converter that works both ways: from the data source to the shape and back.
    /// </summary>
    public class StructConverterTypeEditor : ConverterTypeEditorBase
    {
        #region public functions

        public override Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataSourceInfo = propertyItem.Owner as IValueDataBinding;
            base.ResolveEditor(propertyItem);
            BindForeground();
            return this;
        }

        #endregion

        #region protected functions

        protected override async Task OnButtonClickAsync()
        {
            var originalValueConverter = PropertyItem.Value as ValueConverterBase;

            var dialog = new StructConverterDialog(true, true)
            {
                LocalizedConverter = originalValueConverter
            };

            if (await ShowDialogAsync(dialog)) PropertyItem.Value = dialog.LocalizedConverter;
        }

        #endregion
    }

    /// <summary>
    ///     A converter that only works from the data source to the shape.
    /// </summary>
    public class OneWayStructConverterTypeEditor : ConverterTypeEditorBase
    {
        #region public functions

        public override Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataSourceInfo = propertyItem.Owner as IValueDataBinding;
            base.ResolveEditor(propertyItem);
            BindForeground();
            return this;
        }

        #endregion

        #region protected functions

        protected override async Task OnButtonClickAsync()
        {
            var originalValueConverter = PropertyItem.Value as ValueConverterBase;

            var dialog = new StructConverterDialog(true, false)
            {
                LocalizedConverter = originalValueConverter
            };

            if (await ShowDialogAsync(dialog))
                if (!Equals(originalValueConverter, dialog.LocalizedConverter))
                    PropertyItem.Value = dialog.LocalizedConverter;
        }

        #endregion
    }

    /// <summary>
    ///     A converter that only works from what the author does back to the data source.
    /// </summary>
    public class OneWayToSourceStructConverterTypeEditor : ConverterTypeEditorBase
    {
        #region public functions

        public override Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataSourceInfo = (propertyItem.Owner as SetValueDsCommandOptions)?.ValueInfo;
            base.ResolveEditor(propertyItem);
            BindForeground();
            return this;
        }

        #endregion

        #region protected functions

        protected override async Task OnButtonClickAsync()
        {
            var originalValueConverter = PropertyItem.Value as ValueConverterBase;

            var dialog = new StructConverterDialog(false, true)
            {
                LocalizedConverter = originalValueConverter
            };

            if (await ShowDialogAsync(dialog))
                if (!Equals(originalValueConverter, dialog.LocalizedConverter))
                    PropertyItem.Value = dialog.LocalizedConverter;
        }

        #endregion
    }

    /// <summary>
    ///     A converter that chooses a piece of content.
    /// </summary>
    public class XamlConverterTypeEditor : ConverterTypeEditorBase
    {
        #region public functions

        public override Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataSourceInfo = propertyItem.Owner as IValueDataBinding;
            base.ResolveEditor(propertyItem);
            BindForeground();
            return this;
        }

        #endregion

        #region protected functions

        protected override async Task OnButtonClickAsync()
        {
            var originalValueConverter = PropertyItem.Value as ValueConverterBase;

            var dialog = new XamlConverterDialog
            {
                XamlConverter = originalValueConverter
            };

            if (await ShowDialogAsync(dialog))
                if (!Equals(originalValueConverter, dialog.XamlConverter))
                    PropertyItem.Value = dialog.XamlConverter;
        }

        #endregion
    }

    /// <summary>
    ///     A converter that chooses a brush.
    /// </summary>
    public class BrushConverterTypeEditor : ConverterTypeEditorBase
    {
        #region public functions

        public override Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataSourceInfo = propertyItem.Owner as IValueDataBinding;
            base.ResolveEditor(propertyItem);
            BindForeground();
            return this;
        }

        #endregion

        #region protected functions

        protected override async Task OnButtonClickAsync()
        {
            var originalValueConverter = PropertyItem.Value as ValueConverterBase;

            var dialog = new BrushConverterDialog
            {
                DsBrushConverter = originalValueConverter
            };

            if (await ShowDialogAsync(dialog))
                if (!Equals(originalValueConverter, dialog.DsBrushConverter))
                    PropertyItem.Value = dialog.DsBrushConverter;
        }

        #endregion
    }

    /// <summary>
    ///     An object whose own properties are edited in a dialog of their own.
    /// </summary>
    public class CloneableObjectTypeEditor : DialogTypeEditorBase
    {
        #region protected functions

        protected override async Task OnButtonClickAsync()
        {
            if (PropertyItem.Value is not ICloneable valueCloneable) return;

            ICloneable? valueResult =
                await CloneableObjectPropertiesDialog.ShowDialogAsync(OwnerWindow, valueCloneable);
            if (valueResult is not null) PropertyItem.Value = valueResult;
        }

        #endregion
    }
}
