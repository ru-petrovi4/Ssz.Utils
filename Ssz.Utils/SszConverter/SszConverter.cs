using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class SszConverter
    {
        #region public functions

        public static SszConverter Empty { get; } = new();

        public static object DoNothing { get; } = new();

        public object? NullOrEmptyValue { get; set; }

        public string Format { get; set; } = @"";

        public object? Convert(object?[]? values, Type? targetType, ILogger? logger)
        {
            if (values == null || values.Length == 0)
                return DoNothing;

            if (BackStatements.Count > 0) _values = (object[])values.Clone();            

            object? evaluatedValue;

            var firstTrue =
                Statements.FirstOrDefault(
                    s => new Any(s.Condition.Evaluate(values, null, logger)).ValueAsBoolean(false));
            if (firstTrue != null)
                evaluatedValue = firstTrue.Value.Evaluate(values, null, logger);
            else
                evaluatedValue = values[0];

            if (StringHelper.IsNullOrEmptyString(evaluatedValue)) return NullOrEmptyValue;
            if (targetType == typeof(string))
                evaluatedValue = new Any(evaluatedValue).ValueAsString(true, Format);
            else
                evaluatedValue = new Any(evaluatedValue).ValueAs(targetType, false, Format);
            if (StringHelper.IsNullOrEmptyString(evaluatedValue)) return NullOrEmptyValue;
            return evaluatedValue;
        }

        public object?[] ConvertBack(object? value, Type?[] targetTypes, ILogger? logger)
        {
            if (targetTypes.Length == 0) return new object[0];

            var resultValues = new object?[targetTypes.Length];
            var resultValuesIsSet = new bool?[targetTypes.Length];

            foreach (SszStatement statement in BackStatements)
            {
                var paramNum = statement.ParamNum;
                if (paramNum >= 0 && paramNum < targetTypes.Length)
                    if (resultValuesIsSet[paramNum] != true)
                    {
                        if (new Any(statement.Condition.Evaluate(_values, value, logger)).ValueAsBoolean(false))
                        {
                            resultValues[paramNum] = statement.Value.Evaluate(_values, value, logger);
                            resultValuesIsSet[paramNum] = true;
                        }
                        else
                        {
                            resultValuesIsSet[paramNum] = false;
                        }
                    }
            }            

            for (var paramNum = 0; paramNum < targetTypes.Length; paramNum++)
            {
                if (resultValuesIsSet[paramNum] == null) resultValues[paramNum] = value;

                if (resultValues[paramNum] == null)
                {
                    resultValues[paramNum] = DoNothing;
                }
                else
                {
                    if (_values != null && paramNum < _values.Length)
                        _values[paramNum] = resultValues[paramNum];
                }
            }

            return resultValues;
        }

        public List<SszStatement> Statements { get; protected set; } = new();
        
        public List<SszStatement> BackStatements { get; protected set; } = new();

        #endregion   

        #region private fields

        private object?[]? _values;

        #endregion
    }
}
