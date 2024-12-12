using System;
using System.Diagnostics.CodeAnalysis;
using Ssz.Operator.Core.Constants;

namespace Ssz.Operator.Core
{
    public static class DataItemHelper
    {
        #region public functions

        public static void ParseDataSourceString(string dataSourceString,
            out DataSourceType dataSourceType, out string dataSourceIdString, out string defaultValue)
        {
            dataSourceType = DataSourceType.Constant;
            dataSourceIdString = @"";
            defaultValue = @"";
            if (string.IsNullOrEmpty(dataSourceString)) return;
            string[] dataBindingItemStringParts =
                dataSourceString.Split(new[] {DataBindingItem.DataSourceStringSeparator},
                    StringSplitOptions.None);
            if (dataBindingItemStringParts.Length < 3) return;
            Enum.TryParse(dataBindingItemStringParts[0], out dataSourceType);
            dataSourceIdString = DeEscapeChars(dataBindingItemStringParts[1]);
            defaultValue = DeEscapeChars(dataBindingItemStringParts[2]);
        }

#if NET5_0_OR_GREATER
        [return: NotNullIfNotNull("idString")]
#endif
        public static string? ComputeDataSourceIdString(DataSourceType type,
            string? idString, IDsContainer? container)
        {
            switch (type)
            {
                case DataSourceType.ParamType:
                    return ConstantsHelper.GetConstantType(idString, container) ?? "";
                case DataSourceType.WindowVariable:
                case DataSourceType.GlobalVariable:
                    return ConstantsHelper.ComputeValueOfQueries(container, idString, new Ssz.Utils.IterationInfo());
                default:
                    return ConstantsHelper.ComputeValue(container, idString);
            }
        }


        public static string GetDataSourceString(DataSourceType type,
            string idString, string defaultValue, IDsContainer? container)
        {
            idString = ComputeDataSourceIdString(type, idString, container) ?? @"";
            defaultValue = ConstantsHelper.ComputeValue(container, defaultValue) ?? @"";
            return type +
                   DataBindingItem.DataSourceStringSeparator +
                   EscapeChars(idString) +
                   DataBindingItem.DataSourceStringSeparator +
                   EscapeChars(defaultValue);
        }

        #endregion

        #region private functions

        private static string EscapeChars(string path)
        {
            if (path is null) return @"";
            // TODO: Verify escaping / deescaping
            path = path.Replace(@",", @"^,");
            return path;
        }


        private static string DeEscapeChars(string path)
        {
            if (path is null) return @"";
            // TODO: Verify escaping / deescaping
            path = path.Replace(@"^,", @",");
            return path;
        }

        #endregion
    }
}