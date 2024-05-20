using Microsoft.Extensions.Logging;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class ElementIdsMap
    {
        #region public functions

        public const string StandardMapFileName = @"map.csv";

        public const string StandardTagsFileName = @"tags.csv";

        public const string GenericTagMapOptionParamName = @"%(GenericTagMapOption)";

        public const string GenericPropMapOptionParamName = @"%(GenericPropMapOption)";

        public const string TagTypeSeparatorMapOptionParamName = @"%(TagTypeSeparatorMapOption)";

        public const string TagAndPropSeparatorMapOptionParamName = @"%(TagAndPropSeparatorMapOption)";

        public const string CommonEventMessageFieldsToAddParamName = @"%(CommonEventMessageFieldsToAdd)";

        /// <summary>
        ///     Can be configured in map, '%(GenericTagMapOption)' key
        /// </summary>
        public string GenericTag { get; private set; } = @"%(TAG)";

        public string GenericTagNum { get; private set; } = @"%(TAGNUM)";

        /// <summary>
        ///     Can be configured in map, '%(GenericPropMapOption)' key
        /// </summary>
        public string GenericProp { get; private set; } = @"%(PROP)";

        /// <summary>
        ///     Can be configured in map, '%(TagTypeSeparatorMapOption)' key
        /// </summary>
        public string TagTypeSeparator { get; private set; } = @":";

        /// <summary>
        ///     Can be configured in map, '%(TagAndPropSeparatorMapOption)' key
        /// </summary>
        public string TagAndPropSeparator { get; private set; } = @".";

        /// <summary>
        ///     Not null after Initialize(...)
        /// </summary>
        public CaseInsensitiveDictionary<List<string?>> Map { get; private set; } = null!;

        /// <summary>
        ///     Not null after Initialize(...)
        /// </summary>
        public CaseInsensitiveDictionary<List<string?>> Tags { get; private set; } = null!;

        /// <summary>
        ///     Can be configured in map, '%(CommonEventMessageFieldsToAdd)' key
        /// </summary>
        public CaseInsensitiveDictionary<string?> CommonEventMessageFieldsToAdd { get; private set; } = new();

        public bool IsEmpty => Map.Count == 0;

        /// <summary>
        ///     Can be called multiple times. Other methods calls must be after this initialization.
        ///     csvDb is used for queries resolving.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="tags"></param>
        /// <param name="csvDb"></param>
        public void Initialize(CaseInsensitiveDictionary<List<string?>> map,
            CaseInsensitiveDictionary<List<string?>> tags, CsvDb? csvDb = null)
        {            
            Map = map;
            Tags = tags;
            _csvDb = csvDb;

            var values = Map.TryGetValue(GenericTagMapOptionParamName);
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                GenericTag = values[1] ?? @"";

            values = Map.TryGetValue(GenericPropMapOptionParamName);
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                GenericProp = values[1] ?? @"";

            values = Map.TryGetValue(TagTypeSeparatorMapOptionParamName);
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                TagTypeSeparator = values[1] ?? @"";

            values = Map.TryGetValue(TagAndPropSeparatorMapOptionParamName);
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                TagAndPropSeparator = values[1] ?? @"";

            values = Map.TryGetValue(CommonEventMessageFieldsToAddParamName);
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                CommonEventMessageFieldsToAdd = NameValueCollectionHelper.Parse(values[1]);
        }

        /// <summary>
        ///     Returns null if not found in map file, otherwise result.Count > 1
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="getConstantValue"></param>
        /// <returns></returns>
        public List<string?>? GetFromMap(string elementId, Func<string, IterationInfo, string>? getConstantValue = null)
        {
            if (elementId == @"" || Map.Count == 0) 
                return null;

            var values = Map.TryGetValue(elementId);
            if (values is not null)
            {
                if (values.Count == 1)
                    values.Add("");
                return values;
            }
            
            var separatorIndex = elementId.IndexOf(TagAndPropSeparator);
            if (separatorIndex < 1)
                return null;
            string tagName = elementId.Substring(0, separatorIndex);
            string prop = elementId.Substring(separatorIndex + 1);

            string tagType = GetTagType(tagName);

            return GetFromMapInternal(elementId, tagName, prop, tagType, getConstantValue);
        }

        /// <summary>
        ///     Returns null if not found in map file, otherwise result.Count > 1
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="prop"></param>
        /// <param name="tagType"></param>
        /// <param name="getConstantValue"></param>
        /// <returns></returns>
        public List<string?>? GetFromMap(string? tagName, string? prop, Func<string, IterationInfo, string>? getConstantValue = null)
        {
            string elementId = tagName + TagAndPropSeparator + prop;
            if (elementId == @"" || Map.Count == 0)
                return null;

            var values = Map.TryGetValue(elementId);
            if (values is not null)
            {
                if (values.Count == 1) 
                    values.Add("");
                return values;
            }

            if (String.IsNullOrEmpty(tagName))
                return null;

            string tagType = GetTagType(tagName);

            return GetFromMapInternal(elementId, tagName!, prop ?? @"", tagType, getConstantValue);
        }


        /// <summary>
        ///     Returns null if not found in map file, otherwise result.Count > 1
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="prop"></param>
        /// <param name="tagType"></param>
        /// <param name="getConstantValue"></param>
        /// <returns></returns>
        public List<string?>? GetFromMap(string? tagName, string? prop, string? tagType, Func<string, IterationInfo, string>? getConstantValue = null)
        {
            string elementId = tagName + TagAndPropSeparator + prop;
            if (elementId == @"" || Map.Count == 0) 
                return null;

            var values = Map.TryGetValue(elementId);
            if (values is not null)
            {
                if (values.Count == 1) 
                    values.Add("");
                return values;
            }

            if (String.IsNullOrEmpty(tagName))
                return null;

            return GetFromMapInternal(elementId, tagName!, prop ?? @"", tagType, getConstantValue);
        }

        public string GetTagType(string? tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return "";
            var tagValues = Tags.TryGetValue(tagName!);
            if (tagValues is null) return "";
            if (tagValues.Count < 2) return "";
            return tagValues[1] ?? @"";
        }

        public static Any? TryGetConstValue(string? elementIdOrConst)
        {
            if (String.IsNullOrEmpty(elementIdOrConst))
                return null;

            if (elementIdOrConst!.StartsWith("\"") && elementIdOrConst.EndsWith("\"") && elementIdOrConst.Length >= 2)
            {
                elementIdOrConst = elementIdOrConst.Substring(1, elementIdOrConst.Length - 2);
                return new Any(elementIdOrConst);
            }

            var any = Any.ConvertToBestType(elementIdOrConst, false);
            if (any.ValueTypeCode != Any.TypeCode.String)
                return any;

            return null;
        }

        /// <summary>
        ///     Returns same EventMessagesCollectione, if any.
        /// </summary>
        /// <param name="eventMessage"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull("eventMessagesCollection")]
        public EventMessagesCollection? AddCommonFieldsToEventMessagesCollection(EventMessagesCollection? eventMessagesCollection)
        {
            if (eventMessagesCollection is null)
                return null;

            if (eventMessagesCollection.CommonFields is null)
            {
                if (CommonEventMessageFieldsToAdd.Count > 0)
                    eventMessagesCollection.CommonFields = new CaseInsensitiveDictionary<string?>(CommonEventMessageFieldsToAdd);
            }
            else
            {
                foreach (var kvp in CommonEventMessageFieldsToAdd)
                {
                    eventMessagesCollection.CommonFields[kvp.Key] = kvp.Value;
                }
            }            

            return eventMessagesCollection;
        }

        #endregion        

        #region private functions

        /// <summary>
        ///     Preconditions: tagName != String.Empty
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="tagName"></param>
        /// <param name="prop"></param>
        /// <param name="tagType"></param>
        /// <param name="getConstantValue"></param>
        /// <returns></returns>
        private List<string?>? GetFromMapInternal(string elementId, string tagName, string prop, string? tagType, Func<string, IterationInfo, string>? getConstantValue)
        {
            List<string?>? values = null;
            if (!String.IsNullOrEmpty(tagType))
                values = Map.TryGetValue(tagType + TagTypeSeparator + GenericTag + TagAndPropSeparator + prop);
            if (values is null)
                values = Map.TryGetValue(GenericTag + TagAndPropSeparator + prop);
            if (values is null && !String.IsNullOrEmpty(tagType))
                values = Map.TryGetValue(tagType + TagTypeSeparator + GenericTag + TagAndPropSeparator + GenericProp);
            if (values is null)
                values = Map.TryGetValue(GenericTag + TagAndPropSeparator + GenericProp);
            if (values is null)
                values = Map.TryGetValue(tagName + TagAndPropSeparator + GenericProp);
            if (values is null)
                return null;

            var result = new List<string?> { elementId };

            if (values.Count > 1)
            {
                for (var i = 1; i < values.Count; i++)
                {
                    string? v = SszQueryHelper.ComputeValueOfSszQueries(values[i], (constant, iterationInfo) =>
                    {
                        if (String.Equals(constant, GenericTag, StringComparison.InvariantCultureIgnoreCase))
                            return tagName;
                        if (String.Equals(constant, GenericTagNum, StringComparison.InvariantCultureIgnoreCase))
                        {
                            int index = tagName.IndexOfAny(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);
                            if (index > 0)
                                return tagName.Substring(index);
                            else
                                return @"";
                        }
                        if (String.Equals(constant, GenericProp, StringComparison.InvariantCultureIgnoreCase))
                            return prop;
                        if (getConstantValue != null)
                            return getConstantValue(constant, iterationInfo);
                        return @"";
                    }, _csvDb);
                    result.Add(v ?? @"");
                }
            }
            else
            {
                result.Add("");
            }

            return result;
        }

        #endregion

        #region private fields

        private CsvDb? _csvDb;

        #endregion
    }
}


//public List<string?>? GetFromMap(string? tagName, string? propertyPath, string? tagType)
//{
//    string elementId = tag + propertyPath;
//    if (elementId == @"") return null;

//    var values = Map.TryGetValue(elementId);
//    if (values is not null)
//    {
//        if (values.Count == 1) values.Add("");
//        return values;
//    }

//    var result = new List<string?> { elementId };

//    if (String.IsNullOrEmpty(propertyPath))
//        return null;

//    string? newTag = null;

//    if (!string.IsNullOrEmpty(tag))
//    {
//        values = Map.TryGetValue(tag);
//        if (values is not null && values.Count > 1 && values[1] != "") newTag = values[1];
//    }

//    values = null;
//    if (!string.IsNullOrEmpty(tagType))
//        values = Map.TryGetValue(tagType + TagTypeSeparator +
//                                                GenericTag + propertyPath);
//    if (values is null)
//        values = Map.TryGetValue(GenericTag + propertyPath);
//    if (values is not null)
//    {
//        if (values.Count > 1)
//        {
//            for (var i = 1; i < values.Count; i++)
//            {
//                string? v = SszQueryHelper.ComputeValueOfSszQueries(values[i], constant => (newTag ?? tag) ?? "", _csvDb);
//                result.Add(v ?? @"");
//            }
//        }
//        else
//        {
//            result.Add("");
//        }
//    }
//    else
//    {
//        if (newTag is null)
//            return null;
//        result.Add(newTag + propertyPath);
//    }

//    return result;
//}