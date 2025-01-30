using System.Linq;
using Ssz.Utils;

namespace Ssz.Operator.Core.DataEngines
{
    public static class DataEngineBaseExtensions
    {
        #region public functions

        public static ProcessModelPropertyInfo? FindPropertyInfo(this DataEngineBase dataEngine, string? tagType,
            string propertyPath)
        {
            ProcessModelPropertyInfo? result;
            if (string.IsNullOrEmpty(tagType))
            {
                result =
                    dataEngine.ModelTagPropertyInfosCollection.FirstOrDefault(
                        pi => (StringHelper.CompareIgnoreCase(pi.PropertyPath, propertyPath) || StringHelper.CompareIgnoreCase(pi.PropertyPathToDisplay, propertyPath)) &&
                              string.IsNullOrEmpty(pi.TagType));
            }
            else
            {
                result =
                    dataEngine.ModelTagPropertyInfosCollection.FirstOrDefault(
                        pi => (StringHelper.CompareIgnoreCase(pi.PropertyPath, propertyPath) || StringHelper.CompareIgnoreCase(pi.PropertyPathToDisplay, propertyPath)) &&
                              StringHelper.CompareIgnoreCase(pi.TagType, tagType));
                if (result is null)
                    result =
                        dataEngine.ModelTagPropertyInfosCollection.FirstOrDefault(
                            pi => (StringHelper.CompareIgnoreCase(pi.PropertyPath, propertyPath) || StringHelper.CompareIgnoreCase(pi.PropertyPathToDisplay, propertyPath)) &&
                                  string.IsNullOrEmpty(pi.TagType));
            }

            if (result is null) // It is better to return any result, than null
                result =
                    dataEngine.ModelTagPropertyInfosCollection.FirstOrDefault(
                        pi => StringHelper.CompareIgnoreCase(pi.PropertyPath, propertyPath) || StringHelper.CompareIgnoreCase(pi.PropertyPathToDisplay, propertyPath));

            return result;
        }

        public static TagInfo GetTagInfo(this DataEngineBase dataEngine, string? tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return new TagInfo
                {
                    TagName = @"",
                    TagType = @"",
                    DsPageFileRelativePath = @""
                };
            string tagType = DsProject.Instance.CsvDb.GetValue(dataEngine.TagsFileName, tagName!, 1) ?? @"";
            string dsPageFileRelativePath = DsProject.Instance.CsvDb.GetValue(dataEngine.TagsFileName, tagName!, 2) ?? @"";
            if (!string.IsNullOrEmpty(dsPageFileRelativePath) &&
                !StringHelper.EndsWithIgnoreCase(dsPageFileRelativePath, DsProject.DsPageFileExtension))
                dsPageFileRelativePath = dsPageFileRelativePath + DsProject.DsPageFileExtension;
            return new TagInfo
            {
                TagName = tagName!,
                TagType = tagType,
                DsPageFileRelativePath = dsPageFileRelativePath
            };
        }

        public static void SetTagInfo(this DataEngineBase dataEngine, TagInfo tagInfo)
        {
            DsProject.Instance.CsvDb.SetValue(dataEngine.TagsFileName, tagInfo.TagName, 1, tagInfo.TagType);
            DsProject.Instance.CsvDb.SetValue(dataEngine.TagsFileName, tagInfo.TagName, 2, tagInfo.DsPageFileRelativePath);
        }

        public static void TagInfosClear(this DataEngineBase dataEngine)
        {
            DsProject.Instance.CsvDb.FileClear(dataEngine.TagsFileName);
        }

        public static TagTypeInfo? GetTagTypeInfo(this DataEngineBase dataEngine, string tagType)
        {
            var tagTypeInCsvDbFile = DsProject.Instance.CsvDb.GetValue(dataEngine.TagTypesFileName, tagType, 0);
            if (tagTypeInCsvDbFile is null && !string.IsNullOrEmpty(tagType))
                tagTypeInCsvDbFile = DsProject.Instance.CsvDb.GetValue(dataEngine.TagTypesFileName, @"", 0);
            if (tagTypeInCsvDbFile is null) return null;

            return new TagTypeInfo
            {
                TagType = tagType,
                DsPageFileRelativePaths =
                    DsProject.Instance.CsvDb.GetValue(dataEngine.TagTypesFileName, tagTypeInCsvDbFile, 1),
                Constant = DsProject.Instance.CsvDb.GetValue(dataEngine.TagTypesFileName, tagTypeInCsvDbFile, 2)
            };
        }

        public static void SetTagTypeInfo(this DataEngineBase dataEngine, TagTypeInfo tagTypeInfo)
        {
            DsProject.Instance.CsvDb.SetValue(dataEngine.TagTypesFileName, tagTypeInfo.TagType ?? "", 1,
                tagTypeInfo.DsPageFileRelativePaths);
            DsProject.Instance.CsvDb.SetValue(dataEngine.TagTypesFileName, tagTypeInfo.TagType ?? "", 2, tagTypeInfo.Constant);
        }

        public static void TagTypeInfosClear(this DataEngineBase dataEngine)
        {
            DsProject.Instance.CsvDb.FileClear(dataEngine.TagTypesFileName);
        }

        #endregion
    }
}