using Microsoft.Extensions.Logging;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class SszExpression : ViewModelBase
    {
        #region construction and destruction

        public SszExpression()
        {            
            IsValidInternal = false;
            _isUiToDataSourceWarning = false;
            _expressionType = ExpressinType.Const;
            _lambdaExpression = null;
            _delegate = null;
        }

        public SszExpression(string expressionString)
        {
            ExpressionString = expressionString;
        }

        public SszExpression(SszExpression that)
        {
            _expressionString = that._expressionString;
            IsValidInternal = that.IsValidInternal;
            _isUiToDataSourceWarning = that._isUiToDataSourceWarning;
            _expressionType = that._expressionType;
            _lambdaExpression = that._lambdaExpression;
            _delegate = that._delegate;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Writes to logger with Debug level.
        /// </summary>
        /// <param name="dataSourceValues"></param>
        /// <param name="userValue"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public object? Evaluate(object?[]? dataSourceValues, object? userValue, ILogger? logger)
        {
            if (!IsValidInternal) return null;

            if (_expressionType == ExpressinType.Const && _hasLastResult) return _lastResult;

            if (_expressionType == ExpressinType.Function && _hasLastResult && dataSourceValues != null &&
                _lastDataSourceValues != null &&
                dataSourceValues.SequenceEqual(_lastDataSourceValues) &&
                Equals(userValue, _lastUserValue))
                return _lastResult;

            double[] dDataSourceValues;
            int[] iDataSourceValues;
            uint[] uDataSourceValues;
            bool[] bDataSourceValues;
            string[] sDataSourceValues;

            _lastDataSourceValues = dataSourceValues != null ? (object[])dataSourceValues.Clone() : null;
            _lastUserValue = userValue;

            if (dataSourceValues == null || dataSourceValues.Length == 0)
            {
                dDataSourceValues = new double[0];
                iDataSourceValues = new int[0];
                uDataSourceValues = new uint[0];
                bDataSourceValues = new bool[0];
                sDataSourceValues = new string[0];
            }
            else
            {
                dDataSourceValues = dataSourceValues.Select(
                        v => new Any(v).ValueAsDouble(false))
                    .ToArray();
                iDataSourceValues =
                    dataSourceValues.Select(
                            v => new Any(v).ValueAsInt32(false))
                        .ToArray();
                uDataSourceValues =
                    dataSourceValues.Select(
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

            if (_delegate == null)
                if (!_delegates.TryGetValue(_expressionString, out _delegate))
                {
                    PrepareLambdaExpression(logger);
                    if (_lambdaExpression != null)
                        try
                        {
                            _delegate = _lambdaExpression.Compile();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogDebug(ex, @"");
                        }

                    _delegates.Add(_expressionString, _delegate);
                }

            if (_delegate != null)
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

        public string ExpressionString
        {
            get => _expressionString;
            set
            {                
                if (!SetValue(ref _expressionString, value)) return;
                IsValidInternal = _expressionString != @"";
                _lambdaExpression = null;
                _delegate = null;
                if (_expressionString.Contains(@"Now") ||
                    _expressionString.Contains(@"Random"))
                    _expressionType = ExpressinType.Dynamic;
                else if (_expressionString.Contains(@"[") ||
                         _expressionString.Contains(@"user"))
                    _expressionType = ExpressinType.Function;
                else
                    _expressionType = ExpressinType.Const;
                if (!string.IsNullOrEmpty(_expressionString))
                    IsUiToDataSourceWarning = _expressionString.Contains('[') && _expressionString.Contains(']');
                else
                    IsUiToDataSourceWarning = false;
                OnPropertyChanged(@"IsValid");
            }
        }
        
        public virtual bool IsValid
        {
            get
            {
                PrepareLambdaExpression(null);

                return IsValidInternal;
            }            
        }

        public virtual bool IsUiToDataSourceWarning
        {
            get => _isUiToDataSourceWarning;
            set => SetValue(ref _isUiToDataSourceWarning, value);
        }

        #endregion

        #region protected functions

        protected bool IsValidInternal;

        protected bool Equals(SszExpression other)
        {
            return string.Equals(ExpressionString, other.ExpressionString);
        }

        #endregion

        #region private functions

        private static LambdaExpression? GetLambdaExpression(string expressionString, ILogger? logger)
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

                return DynamicExpression.ParseLambda(new[] { p1, p2, p3, p4, p5, p6, p7, p8, p9, p10 },
                    null,
                    expressionString);
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "");
                return null;
            }
        }

        private void PrepareLambdaExpression(ILogger? logger)
        {
            if (_lambdaExpression != null || !IsValidInternal) return;

            _hasLastResult = false;
            _lambdaExpression = GetLambdaExpression(_expressionString, logger);
            IsValidInternal = _lambdaExpression != null;
        }

        #endregion

        #region private fields

        private static readonly Dictionary<string, Delegate?> _delegates = new();
        private bool _hasLastResult;
        private object? _lastResult;
        private object[]? _lastDataSourceValues;
        private object? _lastUserValue;

        private string _expressionString = @"";
        private bool _isUiToDataSourceWarning;
        private ExpressinType _expressionType;
        private LambdaExpression? _lambdaExpression;
        private Delegate? _delegate;

        #endregion

        private enum ExpressinType
        {
            Dynamic, // DateTime.Now or Random
            Function, // Function of inputs only
            Const // Const value
        }
    }
}
