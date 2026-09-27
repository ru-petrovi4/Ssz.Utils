using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.FindReplace
{
    /// <summary>
    ///     Names a scope of the search as the author knows it.
    ///     <para>
    ///         The WPF dialog listed the scopes as items of a combo box and bound their index; here the
    ///         list holds the values themselves, so the names come from this.
    ///     </para>
    /// </summary>
    public class SearchScopeToTextConverter : IValueConverter
    {
        #region public functions

        public static readonly SearchScopeToTextConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            switch (value)
            {
                case SearchScope.CurrentDrawing:
                    return Res.CurrentDrawingComboBoxItem;
                case SearchScope.AllOpenedDrawings:
                    return Res.AllOpenedDrawingsComboBoxItem;
                case SearchScope.AllDsPageDrawings:
                    return Res.AllDsPageDrawingsComboBoxItem;
                case SearchScope.AllDsShapeDrawings:
                    return Res.AllDsShapeDrawingsComboBoxItem;
                case SearchScopeProps.ConstantsOnly:
                    return Res.ConstantsValuesOnlyComboBoxItem;
                case SearchScopeProps.AllProperties:
                    return Res.AllPropertiesComboBoxItem;
                default:
                    return value?.ToString();
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
