using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core.MultiValueConverters
{
    public class LocalizedConverter : ValueConverterBase
    {
        #region private fields

        private object?[]? _dataSourceValues;

        #endregion

        #region construction and destruction

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                foreach (TextStatement s in DataSourceToUiStatements)
                {
                    s.Dispose();
                }
                DataSourceToUiStatements.Clear();

                foreach (TextStatement s in UiToDataSourceStatements)
                {
                    s.Dispose();
                }
                UiToDataSourceStatements.Clear();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public static LocalizedConverter EmptyConverter = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        // For XAML serialization of collections
        public List<TextStatement> DataSourceToUiStatements { get; protected set; } = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        // For XAML serialization of collections
        public List<TextStatement> UiToDataSourceStatements { get; protected set; } = new();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string Format { get; set; } = @"";

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string? ValueIfNullOrEmptyString { get; set; }

        public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is null || values.Count == 0 || values.Any(v => Equals(v, AvaloniaProperty.UnsetValue)))
                return BindingOperations.DoNothing;

            if (UiToDataSourceStatements.Count > 0) 
                    _dataSourceValues = (object[]) 
                values.ToArray();
            if (DisableUpdatingTarget) 
                return BindingOperations.DoNothing;

            object? evaluatedValue;

            var firstTrue =
                DataSourceToUiStatements.FirstOrDefault(
                    s => ObsoleteAnyHelper.ConvertTo<bool>(s.Condition.Evaluate(values.ToArray(), null), false));
            if (firstTrue is not null)
                evaluatedValue = firstTrue.Value.Evaluate(values.ToArray(), null);
            else
                evaluatedValue = values[0];

            if (StringHelper.IsNullOrEmptyString(evaluatedValue)) return NullOrEmptyValue;
            if (targetType == typeof(string))
                evaluatedValue = (string) ObsoleteAnyHelper.ConvertTo(evaluatedValue, typeof(string), true, Format);
            else
                evaluatedValue = ObsoleteAnyHelper.ConvertTo(evaluatedValue, targetType, false, Format);
            if (StringHelper.IsNullOrEmptyString(evaluatedValue)) return NullOrEmptyValue;
            return evaluatedValue;
        }

        /// <summary>
        ///      Uses DataSourceStrings
        /// </summary>
        /// <param name="value"></param>
        /// <param name="dataValueViewModel"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        public override void ConvertBack(object? value, DataValueViewModel dataValueViewModel, object? parameter,
            CultureInfo culture)
        {
            if (DataSourceStrings is null)
                return;

            var sourcesCount = DataSourceStrings.Count;

            var sourcesValues = new object?[sourcesCount];

            if (UiToDataSourceStatements.Count == 0)
            {
                if (value is string sValue)
                    value = Any.ConvertToBestType(sValue, true);

                sourcesValues[0] = value;
                if (_dataSourceValues is not null && 0 < _dataSourceValues.Length)
                    _dataSourceValues[0] = value;
                for (var paramNum = 1; paramNum < sourcesCount; paramNum += 1)
                {
                    sourcesValues[paramNum] = BindingOperations.DoNothing;
                }
            }
            else
            {
                var conditionResults = new bool[sourcesCount];

                foreach (TextStatement statement in UiToDataSourceStatements)
                {
                    var paramNum = statement.ParamNum;
                    if (paramNum >= 0 && paramNum < sourcesCount)
                        if (!conditionResults[paramNum])
                        {
                            if (ObsoleteAnyHelper.ConvertTo<bool>(statement.Condition.Evaluate(_dataSourceValues, value), false))
                            {
                                sourcesValues[paramNum] = statement.Value.Evaluate(_dataSourceValues, value);
                                conditionResults[paramNum] = true;
                            }
                        }
                }

                for (var paramNum = 0; paramNum < sourcesCount; paramNum += 1)
                {
                    if (conditionResults[paramNum])
                    {
                        if (_dataSourceValues is not null && paramNum < _dataSourceValues.Length)
                            _dataSourceValues[paramNum] = sourcesValues[paramNum];
                    }
                    else
                    {
                        sourcesValues[paramNum] = BindingOperations.DoNothing;
                    }
                }
            }

            for (var paramNum = 0; paramNum < sourcesCount; paramNum += 1)
            {
                var sourceValue = sourcesValues[paramNum];
                if (sourceValue != BindingOperations.DoNothing)
                {
                    dataValueViewModel[DataSourceStrings[paramNum]] = sourceValue;
                }
                else
                {
                    // TEMPCODE                    
                }
            }
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(3))
            {
                writer.WriteListOfOwnedDataSerializable(DataSourceToUiStatements, context);
                writer.WriteListOfOwnedDataSerializable(UiToDataSourceStatements, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {  
                    case 3:
                        DataSourceToUiStatements =
                            reader.ReadListOfOwnedDataSerializable(() => new TextStatement(), context);
                        UiToDataSourceStatements =
                            reader.ReadListOfOwnedDataSerializable(() => new TextStatement(), context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override void FindConstants(HashSet<string> constants)
        {
            foreach (TextStatement statement in DataSourceToUiStatements) statement.FindConstants(constants);
            foreach (TextStatement statement in UiToDataSourceStatements) statement.FindConstants(constants);
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            foreach (TextStatement statement in DataSourceToUiStatements)
                ItemHelper.ReplaceConstants(statement, container);
            foreach (TextStatement statement in UiToDataSourceStatements)
                ItemHelper.ReplaceConstants(statement, container);
        }

        #endregion
    }
}