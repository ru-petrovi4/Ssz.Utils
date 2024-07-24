using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils
{
    public enum SszOperator
    {
        None = 0,
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
    }

    public static class SszOperatorHelper
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
