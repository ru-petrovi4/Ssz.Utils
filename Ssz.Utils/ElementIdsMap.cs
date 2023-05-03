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
        #region construction and destruction

        /// <summary>
        ///     Must be initialized before first use.
        /// </summary>
        /// <param name="logger"></param>
        public ElementIdsMap(ILogger<ElementIdsMap>? logger = null)
        {
            _logger = logger;
        }

        #endregion

        #region public functions

        public const string StandardMapFileName = @"map.csv";

        public const string StandardTagsFileName = @"tags.csv";

        /// <summary>
        ///     Can be configured in map, '%(GenericTag)' key
        /// </summary>
        public string GenericTag { get; private set; } = @"%(TAG)";

        /// <summary>
        ///     Can be configured in map, '%(TagTypeSeparator)' key
        /// </summary>
        public string TagTypeSeparator { get; private set; } = @":";

        /// <summary>
        ///     Can be configured in map, '%(TagAndPropertySeparator)' key
        /// </summary>
        public string TagAndPropertySeparator { get; private set; } = @".";

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

            var values = Map.TryGetValue("%(GenericTag)");
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                GenericTag = values[1] ?? @"";

            values = Map.TryGetValue("%(TagTypeSeparator)");
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                TagTypeSeparator = values[1] ?? @"";

            values = Map.TryGetValue("%(TagAndPropertySeparator)");
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                TagAndPropertySeparator = values[1] ?? @"";

            values = Map.TryGetValue("%(CommonEventMessageFieldsToAdd)");
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                CommonEventMessageFieldsToAdd = NameValueCollectionHelper.Parse(values[1]);
        }

        /// <summary>
        ///     Returns null if not found in map file, otherwise result.Count > 1
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="getConstantValue"></param>
        /// <returns></returns>
        public List<string?>? GetFromMap(string elementId, Func<string, string>? getConstantValue = null)
        {
            if (elementId == @"" || Map.Count == 0) return null;

            var values = Map.TryGetValue(elementId);
            if (values is not null)
            {
                if (values.Count == 1) values.Add("");
                return values;
            }

            string? tagName;
            string? propertyPath;
            string? tagType;

            var separatorIndex = elementId.LastIndexOf(TagAndPropertySeparator);
            if (separatorIndex > 0 && separatorIndex < elementId.Length - 1)
            {
                tagName = elementId.Substring(0, separatorIndex);
                propertyPath = elementId.Substring(separatorIndex);
                tagType = GetTagType(tagName);
            }
            else
            {
                tagName = elementId;
                propertyPath = null;
                tagType = null;
            }

            if (String.IsNullOrEmpty(propertyPath))
                return null;

            var result = new List<string?> { elementId };

            values = null;
            if (!string.IsNullOrEmpty(tagType))
                values = Map.TryGetValue(tagType + TagTypeSeparator +
                                                        GenericTag + propertyPath);
            if (values is null)
                values = Map.TryGetValue(GenericTag + propertyPath);
            if (values is null)
                return null;

            if (values.Count > 1)
            {
                for (var i = 1; i < values.Count; i++)
                {
                    string? v = SszQueryHelper.ComputeValueOfSszQueries(values[i],
                        constant =>
                        {
                            if (String.Equals(constant, GenericTag, StringComparison.InvariantCultureIgnoreCase))
                                return tagName ?? @"";
                            if (getConstantValue != null)
                                return getConstantValue(constant);
                            return @"";
                        }
                        , _csvDb);
                    result.Add(v ?? @"");
                }
            }
            else
            {
                result.Add("");
            }

            return result;
        }

        /// <summary>
        ///     Returns null if not found in map file, otherwise result.Count > 1
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="propertyPath"></param>
        /// <param name="tagType"></param>
        /// <param name="getConstantValue"></param>
        /// <returns></returns>
        public List<string?>? GetFromMap(string? tagName, string? propertyPath, string? tagType, Func<string, string>? getConstantValue = null)
        {
            string elementId = tagName + propertyPath;
            if (elementId == @"" || Map.Count == 0) return null;

            var values = Map.TryGetValue(elementId);
            if (values is not null)
            {
                if (values.Count == 1) values.Add("");
                return values;
            }

            var result = new List<string?> { elementId };

            if (String.IsNullOrEmpty(propertyPath))
                return null;

            values = null;
            if (!string.IsNullOrEmpty(tagType))
                values = Map.TryGetValue(tagType + TagTypeSeparator +
                                                        GenericTag + propertyPath);
            if (values is null)
                values = Map.TryGetValue(GenericTag + propertyPath);
            if (values is null)
                return null;

            if (values.Count > 1)
            {
                for (var i = 1; i < values.Count; i++)
                {
                    string? v = SszQueryHelper.ComputeValueOfSszQueries(values[i], constant =>
                    {
                        if (String.Equals(constant, GenericTag, StringComparison.InvariantCultureIgnoreCase))
                            return tagName ?? @"";
                        if (getConstantValue != null)
                            return getConstantValue(constant);
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

        public string GetTagType(string? tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return "";
            var tagValues = Tags.TryGetValue(tagName!);
            if (tagValues is null) return "";
            if (tagValues.Count < 2) return "";
            return tagValues[1] ?? @"";
        }

        public static Any? TryGetConstValue(string? elementIdOrConst)
        {
            if (string.IsNullOrEmpty(elementIdOrConst)) return null;

            if (elementIdOrConst!.StartsWith("\"") && elementIdOrConst.EndsWith("\""))
            {
                elementIdOrConst = elementIdOrConst.Substring(1, elementIdOrConst.Length - 2);
                return new Any(elementIdOrConst);
            }

            var any = Any.ConvertToBestType(elementIdOrConst, false);
            if (any.ValueTypeCode != TypeCode.String) return any;

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

        #region private fields

        private ILogger<ElementIdsMap>? _logger;

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