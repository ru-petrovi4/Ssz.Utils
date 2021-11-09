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

        public object? Convert(object?[]? values, ILogger? logger)
        {
            if (values is null || values.Length == 0)
                return DoNothing;

            if (BackStatements.Count > 0)
                _values = (object[])values.Clone();            

            object? resultValue;

            var firstTrue =
                Statements.FirstOrDefault(
                    s => new Any(s.Condition.Evaluate(values, null, logger)).ValueAsBoolean(false));
            if (firstTrue is not null)
                resultValue = firstTrue.Value.Evaluate(values, null, logger);
            else
                resultValue = values[0];
            
            return resultValue;
        }

        public object?[] ConvertBack(object? value, int resultCount, ILogger? logger)
        {
            if (resultCount <= 0 || resultCount > 0xFFFF) return new object[0];

            var resultValues = new object?[resultCount];
            var conditionResults = new bool?[resultCount];

            foreach (SszStatement statement in BackStatements)
            {
                var paramNum = statement.ParamNum;
                if (paramNum >= 0 && paramNum < resultCount)
                    if (conditionResults[paramNum] != true)
                    {
                        if (new Any(statement.Condition.Evaluate(_values, value, logger)).ValueAsBoolean(false))
                        {
                            resultValues[paramNum] = statement.Value.Evaluate(_values, value, logger);
                            conditionResults[paramNum] = true;
                        }
                        else
                        {
                            conditionResults[paramNum] = false;
                        }
                    }
            }            

            for (var paramNum = 0; paramNum < resultCount; paramNum++)
            {
                var conditionResult = conditionResults[paramNum];
                if (conditionResult is null)
                {
                    resultValues[paramNum] = value;
                    if (_values is not null && paramNum < _values.Length)
                        _values[paramNum] = value;
                }
                else
                {
                    if (!conditionResult.Value)
                    {
                        resultValues[paramNum] = DoNothing;
                    }
                    else
                    {
                        if (_values is not null && paramNum < _values.Length)
                            _values[paramNum] = resultValues[paramNum];
                    }
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
