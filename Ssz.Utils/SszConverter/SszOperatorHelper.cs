using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;

namespace Ssz.Utils
{
    public static class SszOperatorHelper
    {
        public readonly static (string, SszOperator)[] Operators = new[]
                   {                        
                        (@"==", SszOperator.Equal),
                        (@"!=", SszOperator.NotEqual),
                        (@"<>", SszOperator.NotEqual),
                        (@"<=", SszOperator.LessThanOrEqual),
                        (@">=", SszOperator.GreaterThanOrEqual),
                        (@"=", SszOperator.Equal),
                        (@"<", SszOperator.LessThan),
                        (@">", SszOperator.GreaterThan),
                        (@"Contains|", SszOperator.Contains),
                        (@"!Contains|", SszOperator.NotContains),
                        (@"StartsWith|", SszOperator.StartsWith),
                        (@"!StartsWith|", SszOperator.NotStartsWith),
                        (@"EndsWith|", SszOperator.EndsWith),
                        (@"!EndsWith|", SszOperator.NotEndsWith),
                    };

#if NET5_0_OR_GREATER
        /// <summary>
        ///     If no operator, then returns element
        /// </summary>
        /// <param name="elementAndOperatorAndValue"></param>
        /// <returns></returns>
        public static (string, SszOperator, SszOperatorOptions, string[]) Parse(string? elementAndOperatorAndOptionsAndValues)
        {
            if (String.IsNullOrEmpty(elementAndOperatorAndOptionsAndValues))
                return (@"", SszOperator.None, SszOperatorOptions.None, [@""]);
            var r = Operators
                        .Select(t => (t.Item1, t.Item2, elementAndOperatorAndOptionsAndValues!.IndexOf(t.Item1)))
                        .FirstOrDefault(t => t.Item3 >= 0);
            if (String.IsNullOrEmpty(r.Item1))
            {
                return (elementAndOperatorAndOptionsAndValues!, SszOperator.None, SszOperatorOptions.None, [@""]);
            }
            else
            {
                var optionsAndValue = elementAndOperatorAndOptionsAndValues!.Substring(r.Item3 + r.Item1.Length).Trim();
                SszOperatorOptions operatorOptions = SszOperatorOptions.None;
                var allOeratorOptionsArray = Enum.GetValues<SszOperatorOptions>();
                while (true)
                {
                    bool found = false;
                    foreach (var operatorOption in allOeratorOptionsArray)
                    {
                        var operatorOptionString = operatorOption.ToString() + "|";
                        if (optionsAndValue.StartsWith(operatorOptionString, StringComparison.InvariantCultureIgnoreCase))
                        {
                            operatorOptions |= operatorOption;
                            optionsAndValue = optionsAndValue.Substring(0, operatorOptionString.Length);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        break;
                }
                return (elementAndOperatorAndOptionsAndValues!.Substring(0, r.Item3).Trim(),
                    r.Item2,
                    operatorOptions,
                    optionsAndValue.Split(ValuesSeparator, StringSplitOptions.None));
            }
        }

        /// <summary>
        ///     If no operator, then returns values
        /// </summary>
        /// <param name="elementAndOperatorAndValue"></param>
        /// <returns></returns>
        public static (SszOperator, SszOperatorOptions, string[]) Parse2(string? operatorAndOptionsAndValues)
        {
            if (String.IsNullOrEmpty(operatorAndOptionsAndValues))
                return (SszOperator.None, SszOperatorOptions.None, [@""]);
            var r = Operators                        
                        .FirstOrDefault(t => operatorAndOptionsAndValues!.IndexOf(t.Item1) == 0);
            if (String.IsNullOrEmpty(r.Item1))
            {
                return (SszOperator.None, SszOperatorOptions.None, operatorAndOptionsAndValues!.Split(ValuesSeparator, StringSplitOptions.None));
            }
            else
            {
                var optionsAndValue = operatorAndOptionsAndValues!.Substring(r.Item1.Length).Trim();
                SszOperatorOptions operatorOptions = SszOperatorOptions.None;
                var allOeratorOptionsArray = Enum.GetValues<SszOperatorOptions>();
                while (true)
                {
                    bool found = false;
                    foreach (var operatorOption in allOeratorOptionsArray)
                    {
                        var operatorOptionString = operatorOption.ToString() + "|";
                        if (optionsAndValue.StartsWith(operatorOptionString, StringComparison.InvariantCultureIgnoreCase))
                        {
                            operatorOptions |= operatorOption;
                            optionsAndValue = optionsAndValue.Substring(0, operatorOptionString.Length);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        break;
                }
                return (r.Item2,
                    operatorOptions,
                    optionsAndValue.Split(ValuesSeparator, StringSplitOptions.None));
            }
        }
#endif

        public static string ToString(SszOperator sszOperator)
        {
            return Operators.FirstOrDefault(it => it.Item2 == sszOperator).Item1;
            //switch (sszOperator)
            //{
            //    case SszOperator.Equal:
            //        return @"==";
            //    case SszOperator.NotEqual:
            //        return @"!=";
            //    case SszOperator.LessThan:
            //        return @"<";
            //    case SszOperator.LessThanOrEqual:
            //        return @"<=";
            //    case SszOperator.GreaterThan:
            //        return @">";
            //    case SszOperator.GreaterThanOrEqual:
            //        return @">=";
            //    default:
            //        return @"";
            //}
        }        

        public static SszOperator FromString(string? value, SszOperator defaultSszOperator)
        {
            var it = Operators.FirstOrDefault(it => String.Equals(it.Item1, value, StringComparison.InvariantCultureIgnoreCase));
            if (String.IsNullOrEmpty(it.Item1))
                return defaultSszOperator;
            else
                return it.Item2;
            //switch (value)
            //{
            //    case @"=":
            //    case @"==":
            //        return SszOperator.Equal;
            //    case @"!=":
            //    case @"<>":
            //        return SszOperator.NotEqual;
            //    case @"<":
            //        return SszOperator.LessThan;
            //    case @"<=":
            //    case @"=<":
            //        return SszOperator.LessThanOrEqual;
            //    case @">":
            //        return SszOperator.GreaterThan;
            //    case @">=":
            //    case @"=>":
            //        return SszOperator.GreaterThanOrEqual;
            //    default:
            //        return defaultSszOperator;            
        }        

        public static bool Compare(float left, SszOperator sszOperator, float right, float? customTolerance = null)
        {
            float tolerance;
            if (customTolerance is not null && customTolerance.Value > FloatTolerance)
                tolerance = customTolerance.Value;
            else
                tolerance = FloatTolerance;

            switch (sszOperator)
            {
                case SszOperator.Equal:
                    return left >= right - tolerance && left <= right + tolerance;
                case SszOperator.NotEqual:
                    return left < right - tolerance || left > right + tolerance;
                case SszOperator.LessThan:
                    return left < right - tolerance;
                case SszOperator.LessThanOrEqual:
                    return left <= right + tolerance;
                case SszOperator.GreaterThan:
                    return left > right + tolerance;
                case SszOperator.GreaterThanOrEqual:
                    return left >= right - tolerance;
                default:
                    return left >= -tolerance && left <= tolerance;
            }
        }

        public static bool Compare(float left, SszOperator sszOperator, SszOperatorOptions sszOperatorOptions, string[]? rightValues)
        {
            switch (sszOperator)
            {
                case SszOperator.Equal:
                    if (rightValues is null)
                        return false;
                    return rightValues.Select(GetFloatOperand).Any(rv =>
                        (rv.LowerInclusive ? left >= rv.Lower - FloatTolerance : left > rv.Lower + FloatTolerance) &&
                        rv.UpperInclusive ? left <= rv.Upper + FloatTolerance : left < rv.Upper - FloatTolerance);
                case SszOperator.NotEqual:
                    if (rightValues is null)
                        return true;
                    return rightValues.Select(GetFloatOperand).All(rv =>
                        (rv.LowerInclusive ? left < rv.Lower - FloatTolerance : left <= rv.Lower + FloatTolerance) &&
                        rv.UpperInclusive ? left > rv.Upper + FloatTolerance : left >= rv.Upper - FloatTolerance);
                case SszOperator.LessThan:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left < new Any(rightValues[0]).ValueAsSingle(false) - FloatTolerance;
                case SszOperator.LessThanOrEqual:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left <= new Any(rightValues[0]).ValueAsSingle(false) + FloatTolerance;
                case SszOperator.GreaterThan:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left > new Any(rightValues[0]).ValueAsSingle(false) + FloatTolerance;
                case SszOperator.GreaterThanOrEqual:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left >= new Any(rightValues[0]).ValueAsSingle(false) - FloatTolerance;
                default:
                    return left >= -FloatTolerance && left <= FloatTolerance;
            }
        }        

        public static bool Compare(DateTime left, SszOperator sszOperator, SszOperatorOptions sszOperatorOptions, string[]? rightValues)
        {
            switch (sszOperator)
            {
                case SszOperator.Equal:
                    if (rightValues is null)
                        return false;
                    return rightValues.Select(GetDateTimeOperand).Any(rv =>
                        (rv.LowerInclusive ? left >= rv.Lower - TimeSpanTolerance : left > rv.Lower + TimeSpanTolerance) &&
                        rv.UpperInclusive ? left <= rv.Upper + TimeSpanTolerance : left < rv.Upper - TimeSpanTolerance);
                case SszOperator.NotEqual:
                    if (rightValues is null)
                        return true;
                    return rightValues.Select(GetDateTimeOperand).All(rv =>
                        (rv.LowerInclusive ? left < rv.Lower - TimeSpanTolerance : left <= rv.Lower + TimeSpanTolerance) &&
                        rv.UpperInclusive ? left > rv.Upper + TimeSpanTolerance : left >= rv.Upper - TimeSpanTolerance);
                case SszOperator.LessThan:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left < DateTimeHelper.GetDateTimeUtc(rightValues[0]) - TimeSpanTolerance;
                case SszOperator.LessThanOrEqual:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left <= DateTimeHelper.GetDateTimeUtc(rightValues[0]) + TimeSpanTolerance;
                case SszOperator.GreaterThan:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left > DateTimeHelper.GetDateTimeUtc(rightValues[0]) + TimeSpanTolerance;
                case SszOperator.GreaterThanOrEqual:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left >= DateTimeHelper.GetDateTimeUtc(rightValues[0]) - TimeSpanTolerance;
                default:
                    return false;
            }
        }

        public static bool Compare(TimeSpan left, SszOperator sszOperator, SszOperatorOptions sszOperatorOptions, string[]? rightValues)
        {
            switch (sszOperator)
            {
                case SszOperator.Equal:
                    if (rightValues is null)
                        return false;
                    return rightValues.Select(GetTimeSpanOperand).Any(rv =>
                        (rv.LowerInclusive ? left >= rv.Lower - TimeSpanTolerance : left > rv.Lower + TimeSpanTolerance) &&
                        rv.UpperInclusive ? left <= rv.Upper + TimeSpanTolerance : left < rv.Upper - TimeSpanTolerance);
                case SszOperator.NotEqual:
                    if (rightValues is null)
                        return true;
                    return rightValues.Select(GetTimeSpanOperand).All(rv =>
                        (rv.LowerInclusive ? left < rv.Lower - TimeSpanTolerance : left <= rv.Lower + TimeSpanTolerance) &&
                        rv.UpperInclusive ? left > rv.Upper + TimeSpanTolerance : left >= rv.Upper - TimeSpanTolerance);
                case SszOperator.LessThan:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left < DateTimeHelper.GetTimeSpan(rightValues[0]) - TimeSpanTolerance;
                case SszOperator.LessThanOrEqual:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left <= DateTimeHelper.GetTimeSpan(rightValues[0]) + TimeSpanTolerance;
                case SszOperator.GreaterThan:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left > DateTimeHelper.GetTimeSpan(rightValues[0]) + TimeSpanTolerance;
                case SszOperator.GreaterThanOrEqual:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return left >= DateTimeHelper.GetTimeSpan(rightValues[0]) - TimeSpanTolerance;
                default:
                    return false;
            }
        }

#if NET5_0_OR_GREATER
        public static bool Compare(string? left, SszOperator sszOperator, SszOperatorOptions sszOperatorOptions, string[]? rightValues)
        {
            switch (sszOperator)
            {
                case SszOperator.Equal:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.Any(rv => left == rv);
                    else
                        return rightValues.Any(rv => String.Equals(left, rv, StringComparison.InvariantCultureIgnoreCase));                            
                case SszOperator.NotEqual:
                    if (rightValues is null)
                        return true;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.All(rv => left != rv);
                    else
                        return rightValues.All(rv => !String.Equals(left, rv, StringComparison.InvariantCultureIgnoreCase));
                case SszOperator.LessThan:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return new Any(left).ValueAsSingle(false) < new Any(rightValues[0]).ValueAsSingle(false) - FloatTolerance;
                case SszOperator.LessThanOrEqual:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return new Any(left).ValueAsSingle(false) <= new Any(rightValues[0]).ValueAsSingle(false) + FloatTolerance;
                case SszOperator.GreaterThan:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return new Any(left).ValueAsSingle(false) > new Any(rightValues[0]).ValueAsSingle(false) + FloatTolerance;
                case SszOperator.GreaterThanOrEqual:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return new Any(left).ValueAsSingle(false) >= new Any(rightValues[0]).ValueAsSingle(false) - FloatTolerance;
                case SszOperator.Contains:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left.Contains(rv) : false);
                    else
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left.Contains(rv, StringComparison.InvariantCultureIgnoreCase) : false);
                case SszOperator.NotContains:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left.Contains(rv) : true);
                    else
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left.Contains(rv, StringComparison.InvariantCultureIgnoreCase) : true);
                case SszOperator.StartsWith:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left.StartsWith(rv) : false);
                    else
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left.StartsWith(rv, StringComparison.InvariantCultureIgnoreCase) : false);
                case SszOperator.NotStartsWith:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left.StartsWith(rv) : true);
                    else
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left.StartsWith(rv, StringComparison.InvariantCultureIgnoreCase) : true);
                case SszOperator.EndsWith:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left.EndsWith(rv) : false);
                    else
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left.EndsWith(rv, StringComparison.InvariantCultureIgnoreCase) : false);
                case SszOperator.NotEndsWith:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left.EndsWith(rv) : true);
                    else
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left.EndsWith(rv, StringComparison.InvariantCultureIgnoreCase) : true);
                default:
                    return new Any(left).ValueAsBoolean(false);
            }
        }
#else
        public static bool Compare(string? left, SszOperator sszOperator, SszOperatorOptions sszOperatorOptions, string[]? rightValues)
        {
            switch (sszOperator)
            {
                case SszOperator.Equal:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.Any(rv => left == rv);
                    else
                        return rightValues.Any(rv => String.Equals(left, rv, StringComparison.InvariantCultureIgnoreCase));                            
                case SszOperator.NotEqual:
                    if (rightValues is null)
                        return true;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.All(rv => left != rv);
                    else
                        return rightValues.All(rv => !String.Equals(left, rv, StringComparison.InvariantCultureIgnoreCase));
                case SszOperator.LessThan:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return new Any(left).ValueAsSingle(false) < new Any(rightValues[0]).ValueAsSingle(false) - FloatTolerance;
                case SszOperator.LessThanOrEqual:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return new Any(left).ValueAsSingle(false) <= new Any(rightValues[0]).ValueAsSingle(false) + FloatTolerance;
                case SszOperator.GreaterThan:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return new Any(left).ValueAsSingle(false) > new Any(rightValues[0]).ValueAsSingle(false) + FloatTolerance;
                case SszOperator.GreaterThanOrEqual:
                    if (rightValues is null || rightValues.Length == 0)
                        return false;
                    return new Any(left).ValueAsSingle(false) >= new Any(rightValues[0]).ValueAsSingle(false) - FloatTolerance;
                case SszOperator.Contains:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left!.Contains(rv) : false);
                    else
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? StringHelper.ContainsIgnoreCase(left!, rv) : false);
                case SszOperator.NotContains:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left!.Contains(rv) : true);
                    else
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !StringHelper.ContainsIgnoreCase(left!, rv) : true);
                case SszOperator.StartsWith:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left!.StartsWith(rv) : false);
                    else
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left!.StartsWith(rv, StringComparison.InvariantCultureIgnoreCase) : false);
                case SszOperator.NotStartsWith:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left!.StartsWith(rv) : true);
                    else
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left!.StartsWith(rv, StringComparison.InvariantCultureIgnoreCase) : true);
                case SszOperator.EndsWith:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left!.EndsWith(rv) : false);
                    else
                        return rightValues.Any(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? left!.EndsWith(rv, StringComparison.InvariantCultureIgnoreCase) : false);
                case SszOperator.NotEndsWith:
                    if (rightValues is null)
                        return false;
                    if (sszOperatorOptions == SszOperatorOptions.CaseSensitive)
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left!.EndsWith(rv) : true);
                    else
                        return rightValues.All(rv => (!String.IsNullOrEmpty(left) && !String.IsNullOrEmpty(rv)) ? !left!.EndsWith(rv, StringComparison.InvariantCultureIgnoreCase) : true);
                default:
                    return new Any(left).ValueAsBoolean(false);
            }
        }
#endif

        public static bool Compare(string? left, SszOperator sszOperator, SszOperatorOptions sszOperatorOptions, string[]? rightValues, string paramDataType)
        {
            switch (paramDataType)
            {
                case nameof(SByte):
                case nameof(Byte):
                case nameof(Int16):
                case nameof(UInt16):
                case nameof(Int32):
                case nameof(UInt32):
                case nameof(Int64):
                case nameof(UInt64):
                case nameof(Double):
                case nameof(Single):
                case nameof(Decimal):                
                    return Compare(new Any(left).ValueAsSingle(false), sszOperator, sszOperatorOptions, rightValues);
                case nameof(DateTime):
                    return Compare(DateTimeHelper.GetDateTimeUtc(left), sszOperator, sszOperatorOptions, rightValues);
                case nameof(TimeSpan):
                    return Compare(DateTimeHelper.GetTimeSpan(left), sszOperator, sszOperatorOptions, rightValues);
                default:
                    return Compare(left, sszOperator, sszOperatorOptions, rightValues);
            }
        }

        #region private functions

        private static FloatOperand GetFloatOperand(string operand)
        {
            int separatorIndex = operand.IndexOf("..");
            if (separatorIndex > -1)
            {
                FloatOperand result = new();    
                
                if (operand[0] == '[')
                {
                    result.LowerInclusive = true;
                    result.Lower = new Any(operand.Substring(1, separatorIndex - 1).Trim()).ValueAsSingle(false);
                }
                else if (operand[0] == '(')
                {
                    result.LowerInclusive = false;
                    result.Lower = new Any(operand.Substring(1, separatorIndex - 1).Trim()).ValueAsSingle(false);
                }
                else
                {
                    result.LowerInclusive = true;
                    result.Lower = new Any(operand.Substring(0, separatorIndex).Trim()).ValueAsSingle(false);
                }

                if (operand[operand.Length - 1] == ']')
                {
                    result.UpperInclusive = true;
                    result.Upper = new Any(operand.Substring(separatorIndex + 2, operand.Length - separatorIndex - 3).Trim()).ValueAsSingle(false);
                }
                else if (operand[operand.Length - 1] == ')')
                {
                    result.UpperInclusive = false;
                    result.Upper = new Any(operand.Substring(separatorIndex + 2, operand.Length - separatorIndex - 3).Trim()).ValueAsSingle(false);
                }
                else
                {
                    result.UpperInclusive = true;
                    result.Upper = new Any(operand.Substring(separatorIndex + 2, operand.Length - separatorIndex - 2).Trim()).ValueAsSingle(false);
                }

                return result;
            }
            else
            {
                float operandFloat = new Any(operand).ValueAsSingle(false);
                return new FloatOperand()
                {
                    LowerInclusive = true,
                    Lower = operandFloat,
                    UpperInclusive = true,
                    Upper = operandFloat,
                };
            }
        }

        private static DateTimeOperand GetDateTimeOperand(string operand)
        {
            int separatorIndex = operand.IndexOf("..");
            if (separatorIndex > -1)
            {
                DateTimeOperand result = new();

                if (operand[0] == '[')
                {
                    result.LowerInclusive = true;
                    result.Lower = DateTimeHelper.GetDateTimeUtc(operand.Substring(1, separatorIndex - 1).Trim());
                }
                else if (operand[0] == '(')
                {
                    result.LowerInclusive = false;
                    result.Lower = DateTimeHelper.GetDateTimeUtc(operand.Substring(1, separatorIndex - 1).Trim());
                }
                else
                {
                    result.LowerInclusive = true;
                    result.Lower = DateTimeHelper.GetDateTimeUtc(operand.Substring(0, separatorIndex).Trim());
                }

                if (operand[operand.Length - 1] == ']')
                {
                    result.UpperInclusive = true;
                    result.Upper = DateTimeHelper.GetDateTimeUtc(operand.Substring(separatorIndex + 2, operand.Length - separatorIndex - 3).Trim());
                }
                else if (operand[operand.Length - 1] == ')')
                {
                    result.UpperInclusive = false;
                    result.Upper = DateTimeHelper.GetDateTimeUtc(operand.Substring(separatorIndex + 2, operand.Length - separatorIndex - 3).Trim());
                }
                else
                {
                    result.UpperInclusive = true;
                    result.Upper = DateTimeHelper.GetDateTimeUtc(operand.Substring(separatorIndex + 2, operand.Length - separatorIndex - 2).Trim());
                }

                return result;
            }
            else
            {
                DateTime operandDateTime = DateTimeHelper.GetDateTimeUtc(operand);
                return new DateTimeOperand()
                {
                    LowerInclusive = true,
                    Lower = operandDateTime,
                    UpperInclusive = true,
                    Upper = operandDateTime,
                };
            }
        }

        private static TimeSpanOperand GetTimeSpanOperand(string operand)
        {
            int separatorIndex = operand.IndexOf("..");
            if (separatorIndex > -1)
            {
                TimeSpanOperand result = new();

                if (operand[0] == '[')
                {
                    result.LowerInclusive = true;
                    result.Lower = DateTimeHelper.GetTimeSpan(operand.Substring(1, separatorIndex - 1).Trim());
                }
                else if (operand[0] == '(')
                {
                    result.LowerInclusive = false;
                    result.Lower = DateTimeHelper.GetTimeSpan(operand.Substring(1, separatorIndex - 1).Trim());
                }
                else
                {
                    result.LowerInclusive = true;
                    result.Lower = DateTimeHelper.GetTimeSpan(operand.Substring(0, separatorIndex).Trim());
                }

                if (operand[operand.Length - 1] == ']')
                {
                    result.UpperInclusive = true;
                    result.Upper = DateTimeHelper.GetTimeSpan(operand.Substring(separatorIndex + 2, operand.Length - separatorIndex - 3).Trim());
                }
                else if (operand[operand.Length - 1] == ')')
                {
                    result.UpperInclusive = false;
                    result.Upper = DateTimeHelper.GetTimeSpan(operand.Substring(separatorIndex + 2, operand.Length - separatorIndex - 3).Trim());
                }
                else
                {
                    result.UpperInclusive = true;
                    result.Upper = DateTimeHelper.GetTimeSpan(operand.Substring(separatorIndex + 2, operand.Length - separatorIndex - 2).Trim());
                }

                return result;
            }
            else
            {
                TimeSpan operandTimeSpan = DateTimeHelper.GetTimeSpan(operand);
                return new TimeSpanOperand()
                {
                    LowerInclusive = true,
                    Lower = operandTimeSpan,
                    UpperInclusive = true,
                    Upper = operandTimeSpan,
                };
            }
        }

        #endregion

        private static readonly string[] ValuesSeparator = ["||"];

        private const float FloatTolerance = 0.000001f;

        private static TimeSpan TimeSpanTolerance = TimeSpan.FromMilliseconds(1);

        private struct FloatOperand
        {
            public bool LowerInclusive;
            public float Lower;
            public bool UpperInclusive;
            public float Upper;
        }

        private struct DateTimeOperand
        {
            public bool LowerInclusive;
            public DateTime Lower;
            public bool UpperInclusive;
            public DateTime Upper;
        }

        private struct TimeSpanOperand
        {
            public bool LowerInclusive;
            public TimeSpan Lower;
            public bool UpperInclusive;
            public TimeSpan Upper;
        }
    }
}

//public static (string, SszConverter?) GetConverter(string element_Operator_Value)
//{
//    if (String.IsNullOrWhiteSpace(element_Operator_Value))
//        return (@"", null);

//    var r = OperatorsToFind
//        .Select(t => (t.Item1, t.Item2, element_Operator_Value.IndexOf(t.Item1)))
//        .FirstOrDefault(t => t.Item3 >= 0);
//    if (r.Item2 == SszOperator.None)
//    {
//        return (element_Operator_Value, null);
//    }
//    else
//    {
//        var sszConverter = new SszConverter();

//        var elementId = element_Operator_Value.Substring(0, r.Item3).Trim();
//        var operator_ = r.Item2;
//        var operand = Any.ConvertToBestType(element_Operator_Value.Substring(r.Item3 + r.Item1.Length).Trim(), false);
//        switch (AnyHelper.GetTransportType(operand))
//        {
//            case TransportType.Double:
//                sszConverter.Statements.Add(new SszStatement(@"true", "d[0]" + ToString(operator_) + operand.ValueAsString(false), 0));
//                break;
//            case TransportType.UInt32:
//                sszConverter.Statements.Add(new SszStatement(@"true", "i[0]" + ToString(operator_) + operand.ValueAsString(false), 0));
//                break;
//            case TransportType.Object:
//                sszConverter.Statements.Add(new SszStatement(@"true", "s[0]" + ToString(operator_) + "\"" + operand.ValueAsString(false) + "\"", 0));
//                break;
//        }

//        if (sszConverter is not null && sszConverter.Statements.Count == 0)
//            sszConverter = null;

//        return (elementId, sszConverter);
//    }
//}
