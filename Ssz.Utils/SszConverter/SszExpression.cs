using Microsoft.Extensions.Logging;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Ssz.Utils
{
    public class SszExpression : ViewModelBase
    {
        #region construction and destruction

        public SszExpression()
        {   
        }

        public SszExpression(string expressionString)
        {
            ExpressionString = expressionString;
        }

        public SszExpression(SszExpression that)
        {
            _expressionString = that._expressionString;
            IsValidInternal = that.IsValidInternal;            
            _expressionType = that._expressionType;
            _lambdaExpression = that._lambdaExpression;
            _delegate = that._delegate;
        }

        #endregion

        #region public functions

        /// <summary>        
        ///     Writes to logger with Debug level.
        ///     Writes to userFriendlyLogger with Warning and Error levels.
        /// </summary>
        /// <param name="dataSourceValues"></param>
        /// <param name="userValue"></param>
        /// <param name="logger"></param>
        /// <param name="userFriendlyLogger"></param>
        /// <returns></returns>
        public object? Evaluate(object?[]? dataSourceValues, object? userValue, ILogger? logger, ILogger? userFriendlyLogger)
        {
            if (!IsValidInternal)
                return null;

            if (_expressionType == ExpressinType.Const && _hasLastResult)
                return _lastResult;

            if (_expressionType == ExpressinType.PureFunction && _hasLastResult && dataSourceValues is not null &&
                    _lastDataSourceValues is not null &&
                    dataSourceValues.SequenceEqual(_lastDataSourceValues) &&
                    Equals(userValue, _lastUserValue))
                return _lastResult;

            double[] dDataSourceValues;
            int[] iDataSourceValues;
            uint[] uDataSourceValues;
            bool[] bDataSourceValues;
            string[] sDataSourceValues;

            _lastDataSourceValues = dataSourceValues is not null ? (object[])dataSourceValues.Clone() : null;
            _lastUserValue = userValue;

            if (dataSourceValues is null || dataSourceValues.Length == 0)
            {
                dDataSourceValues = Array.Empty<double>();
                iDataSourceValues = Array.Empty<int>();
                uDataSourceValues = Array.Empty<uint>();
                bDataSourceValues = Array.Empty<bool>();
                sDataSourceValues = Array.Empty<string>();
            }
            else
            {
                dDataSourceValues = dataSourceValues.Select(
                        v => new Any(v).ValueAsDouble(false))
                    .ToArray();
                iDataSourceValues = dataSourceValues.Select(
                        v => new Any(v).ValueAsInt32(false))
                    .ToArray();
                uDataSourceValues = dataSourceValues.Select(
                        v => new Any(v).ValueAsUInt32(false))
                    .ToArray();
                bDataSourceValues = dataSourceValues.Select(
                        v => new Any(v).ValueAsBoolean(false))
                    .ToArray();
                sDataSourceValues = dataSourceValues.Select(
                        v => new Any(v).ValueAsString(true))
                    .ToArray();
            }

            var dUserValue = new Any(userValue).ValueAsDouble(true);
            var iUserValue = new Any(userValue).ValueAsInt32(true);
            var uUserValue = new Any(userValue).ValueAsUInt32(true);
            var bUserValue = new Any(userValue).ValueAsBoolean(true);
            string sUserValue = new Any(userValue).ValueAsString(false);

            if (_delegate is null)
            {
                lock (_delegates)
                {
                    if (!_delegates.TryGetValue(_expressionString, out _delegate))
                    {
                        PrepareLambdaExpression(logger, userFriendlyLogger);
                        if (_lambdaExpression is not null)
                            try
                            {
                                _delegate = _lambdaExpression.Compile();
                            }
                            catch (Exception ex)
                            {
                                logger?.LogDebug(ex, @"PrepareLambdaExpression error");
                                userFriendlyLogger?.LogError(Properties.Resources.PrepareLambdaExpressionError + ": " + _expressionString);
                            }

                        _delegates.Add(_expressionString, _delegate);
                    }
                }
            }

            if (_delegate is not null)
            {
                try
                {
                    _lastResult = _delegate.DynamicInvoke(dDataSourceValues, iDataSourceValues, uDataSourceValues, bDataSourceValues,
                        sDataSourceValues,
                        dUserValue, iUserValue, uUserValue, bUserValue, sUserValue);
                }
                catch (Exception ex)
                {
                    _lastResult = null;
                    logger?.LogDebug(ex, @"");
                    userFriendlyLogger?.LogError(Properties.Resources.CalculationLambdaExpressionError + ": " + _expressionString);
                }

                _hasLastResult = true;
            }
            else
            {
                IsValidInternal = false;
            }

            return _lastResult;
        }

        public override bool Equals(object? obj)
        {
            var other = obj as Expression;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public virtual string ExpressionString
        {
            get => _expressionString;
            set
            {                
                if (!SetValue(ref _expressionString, value)) 
                    return;
                IsValidInternal = _expressionString != @"";
                _lambdaExpression = null;
                _delegate = null;
                if (_expressionString.Contains(@"Now") ||
                    _expressionString.Contains(@"Random"))
                    _expressionType = ExpressinType.Other;
                else if (_expressionString.Contains(@"[") ||
                         _expressionString.Contains(@"user"))
                    _expressionType = ExpressinType.PureFunction;
                else
                    _expressionType = ExpressinType.Const;                
                OnPropertyChanged(nameof(IsValid));
            }
        }
        
        public virtual bool IsValid
        {
            get
            {
                PrepareLambdaExpression(null, null);

                return IsValidInternal;
            }            
        }        

        public int GetInputsCount()
        {
            if (String.IsNullOrEmpty(_expressionString))
                return 0;
            Regex regex = new(@"d\[(?<n>[0-9]+)\]|i\[(?<n>[0-9]+)\]|u\[(?<n>[0-9]+)\]|b\[(?<n>[0-9]+)\]|s\[(?<n>[0-9]+)\]", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);                        
            var m = regex.Matches(_expressionString);
            int inputsCount = 0;
            foreach (int i in Enumerable.Range(0, m.Count))
            {
                var g = m[i].Groups["n"];
                if (g is not null)
                {
                    int c = new Any(g.Value).ValueAsInt32(false) + 1;
                    if (c > inputsCount)
                        inputsCount = c;
                }
            }
            return inputsCount;
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     If true - possible valid. If false - certainly invalid.
        /// </summary>
        protected bool IsValidInternal;

        protected bool Equals(SszExpression other)
        {
            return string.Equals(ExpressionString, other.ExpressionString);
        }

        #endregion

        #region private functions

        private static LambdaExpression? GetLambdaExpression(string expressionString, ILogger? logger, ILogger? userFriendlyLogger)
        {
            try
            {
                ParameterExpression p1 = Expression.Parameter(typeof(double[]), "d");
                ParameterExpression p2 = Expression.Parameter(typeof(int[]), "i");
                ParameterExpression p3 = Expression.Parameter(typeof(uint[]), "u");
                ParameterExpression p4 = Expression.Parameter(typeof(bool[]), "b");
                ParameterExpression p5 = Expression.Parameter(typeof(string[]), "s");
                ParameterExpression p6 = Expression.Parameter(typeof(double), "userD");
                ParameterExpression p7 = Expression.Parameter(typeof(int), "userI");
                ParameterExpression p8 = Expression.Parameter(typeof(uint), "userU");
                ParameterExpression p9 = Expression.Parameter(typeof(bool), "userB");
                ParameterExpression p10 = Expression.Parameter(typeof(string), "userS");

                return DynamicExpressionParser.ParseLambda(new[] { p1, p2, p3, p4, p5, p6, p7, p8, p9, p10 },
                    null,
                    expressionString);
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "");
                userFriendlyLogger?.LogError(Properties.Resources.PrepareLambdaExpressionError + ": " + expressionString);
                return null;
            }
        }

        private void PrepareLambdaExpression(ILogger? logger, ILogger? userFriendlyLogger)
        {
            if (_lambdaExpression is not null || !IsValidInternal) return;

            _hasLastResult = false;
            _lambdaExpression = GetLambdaExpression(_expressionString, logger, userFriendlyLogger);
            IsValidInternal = _lambdaExpression is not null;
        }

        #endregion

        #region private fields

        private static readonly Dictionary<string, Delegate?> _delegates = new();
        private bool _hasLastResult;
        private object? _lastResult;
        private object[]? _lastDataSourceValues;
        private object? _lastUserValue;

        private string _expressionString = @"";        
        private ExpressinType _expressionType;
        private LambdaExpression? _lambdaExpression;
        private Delegate? _delegate;

        #endregion

        private enum ExpressinType
        {
            Const = 0, // Const value
            PureFunction, // Function of inputs only   
            Other, // DateTime.Now or Random                     
        }
    }
}
