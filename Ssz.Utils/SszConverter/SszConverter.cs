using Microsoft.Extensions.Logging;
using Ssz.Utils.Logging;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <param name="loggersSet"></param>
        /// <returns></returns>
        public object? Convert(object?[]? values, ILoggersSet loggersSet)
        {
            if (values is null || values.Length == 0)
                return DoNothing;

            if (BackStatements.Count > 0)
                _values = (object[])values.Clone();            

            object? resultValue;

            var firstTrue =
                Statements.FirstOrDefault(
                    s => new Any(s.Condition.Evaluate(values, null, loggersSet)).ValueAsBoolean(false));
            if (firstTrue is not null)
                resultValue = firstTrue.Value.Evaluate(values, null, loggersSet);
            else
                resultValue = values[0];
            
            return resultValue;
        }

        /// <summary>
        ///     Writes to userFriendlyLogger with Information level.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="resultCount"></param>
        /// <param name="loggersSet"></param>        
        /// <returns></returns>
        public object?[] ConvertBack(object? value, int resultCount, ILoggersSet loggersSet)
        {
            if (resultCount <= 0 || resultCount > 0xFFFF) return new object[0];

            var resultValues = new object?[resultCount];
            var conditionResults = new bool[resultCount];

            if (BackStatements.Count > 0)
            {
                foreach (SszStatement statement in BackStatements)
                {
                    var paramNum = statement.ParamNum;
                    if (paramNum >= 0 && paramNum < resultCount)
                    {                        
                        if (!conditionResults[paramNum] &&
                            new Any(statement.Condition.Evaluate(_values, value, loggersSet)).ValueAsBoolean(false))
                        {
                            resultValues[paramNum] = statement.Value.Evaluate(_values, value, loggersSet);
                            conditionResults[paramNum] = true;
                        }
                    }                        
                }

                for (var paramNum = 0; paramNum < resultCount; paramNum++)
                {                    
                    if (conditionResults[paramNum])
                    {                        
                        if (_values is not null && paramNum < _values.Length)
                            _values[paramNum] = resultValues[paramNum];
                    }
                    else
                    {
                        resultValues[paramNum] = DoNothing;
                    }
                }
            }
            else
            {
                for (var paramNum = 0; paramNum < resultCount; paramNum++)
                {
                    resultValues[paramNum] = value;
                    if (_values is not null)
                        _values[paramNum] = value;
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
