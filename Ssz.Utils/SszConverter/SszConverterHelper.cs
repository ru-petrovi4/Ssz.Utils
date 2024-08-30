using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ssz.Utils
{
    public static class SszConverterHelper
    {
        public readonly static (string, SszOperator)[] OperatorsToFind = new[]
           {
                (@"==", SszOperator.Equal),
                (@"!=", SszOperator.NotEqual),
                (@"<>", SszOperator.NotEqual),
                (@"<=", SszOperator.LessThanOrEqual),
                (@">=", SszOperator.GreaterThanOrEqual),
                (@"=", SszOperator.Equal),
                (@"<", SszOperator.LessThan),
                (@">", SszOperator.GreaterThan)
            };

        public static (string, SszOperator, Any) Parse(string? element_Operator_Value)
        {
            if (String.IsNullOrEmpty(element_Operator_Value))
                return (@"", SszOperator.None, new Any());
            var r = OperatorsToFind
                        .Select(t => (t.Item1, t.Item2, element_Operator_Value!.IndexOf(t.Item1)))
                        .FirstOrDefault(t => t.Item3 >= 0);
            if (r.Item2 == SszOperator.None)
                return (element_Operator_Value!, SszOperator.None, new Any());
            else
                return (element_Operator_Value!.Substring(0, r.Item3).Trim(),
                    r.Item2,
                    Any.ConvertToBestType(element_Operator_Value!.Substring(r.Item3 + r.Item1.Length).Trim(), false));
        }

        public static (string, SszConverter?) GetConverter(string element_Operator_Value)
        {
            if (String.IsNullOrWhiteSpace(element_Operator_Value))
                return (@"", null);

            var r = OperatorsToFind
                .Select(t => (t.Item1, t.Item2, element_Operator_Value.IndexOf(t.Item1)))
                .FirstOrDefault(t => t.Item3 >= 0);
            if (r.Item2 == SszOperator.None)
            {
                return (element_Operator_Value, null);
            }
            else
            {
                var sszConverter = new SszConverter();

                var elementId = element_Operator_Value.Substring(0, r.Item3).Trim();
                var operator_ = r.Item2;
                var operand = Any.ConvertToBestType(element_Operator_Value.Substring(r.Item3 + r.Item1.Length).Trim(), false);
                switch (AnyHelper.GetTransportType(operand))
                {
                    case TransportType.Double:
                        sszConverter.Statements.Add(new SszStatement(@"true", "d[0]" + ToString(operator_) + operand.ValueAsString(false), 0));
                        break;
                    case TransportType.UInt32:
                        sszConverter.Statements.Add(new SszStatement(@"true", "i[0]" + ToString(operator_) + operand.ValueAsString(false), 0));
                        break;
                    case TransportType.Object:
                        sszConverter.Statements.Add(new SszStatement(@"true", "s[0]" + ToString(operator_) + "\"" + operand.ValueAsString(false) + "\"", 0));
                        break;
                }

                if (sszConverter is not null && sszConverter.Statements.Count == 0)
                    sszConverter = null;

                return (elementId, sszConverter);
            }
        }

        public static string ToString(SszOperator sszOperator)
        {
            switch (sszOperator)
            {
                case SszOperator.Equal:
                    return @"==";
                case SszOperator.NotEqual:
                    return @"!=";
                case SszOperator.LessThan:
                    return @"<";
                case SszOperator.LessThanOrEqual:
                    return @"<=";
                case SszOperator.GreaterThan:
                    return @">";
                case SszOperator.GreaterThanOrEqual:
                    return @">=";
                default:
                    return @"";
            }
        }
    }
}


    //public static SszOperator FromString(string value)
    //{
    //    switch (value)
    //    {
    //        case @"=":
    //        case @"==":
    //            return SszOperator.Equal;
    //        case @"!=":
    //        case @"<>":
    //            return SszOperator.NotEqual;
    //        case @"<":
    //            return SszOperator.LessThan;
    //        case @"<=":
    //        case @"=<":
    //            return SszOperator.LessThanOrEqual;
    //        case @">":
    //            return SszOperator.GreaterThan;
    //        case @">=":
    //        case @"=>":
    //            return SszOperator.GreaterThanOrEqual;
    //        default:
    //            return SszOperator.None;
    //    }
    //}

