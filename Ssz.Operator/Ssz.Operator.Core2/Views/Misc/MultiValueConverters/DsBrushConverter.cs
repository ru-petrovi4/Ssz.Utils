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
    public class DsBrushConverter : ValueConverterBase
    {
        #region construction and destruction

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                foreach (DsBrushStatement s in DataSourceToUiStatements) s.Dispose();
                DataSourceToUiStatements.Clear();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region private fields

        #endregion

        #region public functions

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        // For XAML serialization of collections
        public List<DsBrushStatement> DataSourceToUiStatements { get; protected set; } = new();

        public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is null || values.Count == 0 || values.Any(v => Equals(v, AvaloniaProperty.UnsetValue)))
                return BindingOperations.DoNothing;

            if (DisableUpdatingTarget) 
                return BindingOperations.DoNothing;

            object? evaluatedValue = null;

            var firstTrue =
                DataSourceToUiStatements.FirstOrDefault(
                    s => ObsoleteAnyHelper.ConvertTo<bool>(s.Condition.Evaluate(values.ToArray(), null, null), false));
            if (firstTrue is not null)
            {
                if (firstTrue.ParamNum.HasValue)
                {
                    var paramNum = firstTrue.ParamNum.Value;
                    if (paramNum >= 0 && paramNum < values.Count)
                        evaluatedValue = ObsoleteAnyHelper.ConvertTo(values[paramNum], targetType, false);
                }
                else
                {
                    if (firstTrue.ConstDsBrush is not null) evaluatedValue = firstTrue.ConstDsBrush.GetBrush(null);
                }
            }
            else
            {
                evaluatedValue = ObsoleteAnyHelper.ConvertTo(values[0], targetType, false);
            }

            if (StringHelper.IsNullOrEmptyString(evaluatedValue)) return NullOrEmptyValue;
            return evaluatedValue;
        }

        public override void ConvertBack(object? value, DataValueViewModel dataValueViewModel, object? parameter,
            CultureInfo culture)
        {
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                writer.WriteListOfOwnedDataSerializable(DataSourceToUiStatements, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            DataSourceToUiStatements = reader.ReadList<DsBrushStatement>();
                        }
                        catch (BlockEndingException)
                        {
                        }

                        break;
                    case 2:
                        DataSourceToUiStatements =
                            reader.ReadListOfOwnedDataSerializable(() => new DsBrushStatement(), context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override void FindConstants(HashSet<string> constants)
        {
            foreach (DsBrushStatement statement in DataSourceToUiStatements) statement.FindConstants(constants);
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            foreach (DsBrushStatement statement in DataSourceToUiStatements)
                ItemHelper.ReplaceConstants(statement, container);
        }

        #endregion
    }
}