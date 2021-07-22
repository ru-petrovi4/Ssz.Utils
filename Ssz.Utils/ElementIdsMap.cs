using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class ElementIdsMap
    {
        #region construction and destruction

        public ElementIdsMap(CaseInsensitiveDictionary<List<string?>> map,
            CaseInsensitiveDictionary<List<string?>> tagInfos)
        {
            //_logger = logger;

            Map = map;
            TagInfos = tagInfos;

            var values = Map.TryGetValue("GenericTag");
            if (values != null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                GenericTag = values[1] ?? @"";

            values = Map.TryGetValue("TagTypeSeparator");
            if (values != null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                TagTypeSeparator = values[1] ?? @"";

            values = Map.TryGetValue("TagAndPropertySeparator");
            if (values != null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                TagAndPropertySeparator = values[1] ?? @"";
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Can be configured in mapDictionary, 'GenericTag' key
        /// </summary>
        public string GenericTag { get; } = @"%(TAG)";

        /// <summary>
        ///     Can be configured in mapDictionary, 'TagTypeSeparator' key
        /// </summary>
        public string TagTypeSeparator { get; } = @":";

        /// <summary>
        ///     Can be configured in mapDictionary, 'TagAndPropertySeparator' key
        /// </summary>
        public string TagAndPropertySeparator { get; } = @".";

        public CaseInsensitiveDictionary<List<string?>> Map { get; }

        public CaseInsensitiveDictionary<List<string?>> TagInfos { get; }

        /// <summary>
        ///     result.Count > 1
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="csvDb"></param>
        /// <returns></returns>
        public List<string?> GetFromMap(string elementId, CsvDb? csvDb)
        {
            string? tag;
            string? propertyPath;
            string? tagType;

            var separatorIndex = elementId.LastIndexOf(TagAndPropertySeparator);
            if (separatorIndex > 0 && separatorIndex < elementId.Length - 1)
            {
                tag = elementId.Substring(0, separatorIndex);
                propertyPath = elementId.Substring(separatorIndex);
                tagType = GetTagType(tag);
            }
            else
            {
                tag = elementId;
                propertyPath = null;
                tagType = null;
            }

            return GetFromMap(tag, propertyPath, tagType, csvDb);
        }

        /// <summary>
        ///     result.Count > 1
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="propertyPath"></param>
        /// <param name="tagType"></param>
        /// <param name="csvDb"></param>
        /// <returns></returns>
        public List<string?> GetFromMap(string? tag, string? propertyPath, string? tagType, CsvDb? csvDb)
        {
            string elementId = tag + propertyPath;
            if (elementId == @"") return new List<string?> { @"", @"" };

            var values = Map.TryGetValue(elementId);
            if (values != null)
            {
                if (values.Count == 1) values.Add("");
                return values;
            }

            var result = new List<string?> { elementId };

            if (!string.IsNullOrEmpty(propertyPath))
            {
                string? newTag = null;

                if (!string.IsNullOrEmpty(tag))
                {
                    values = Map.TryGetValue(tag);
                    if (values != null && values.Count > 1 && values[1] != "") newTag = values[1];
                }

                values = null;
                if (!string.IsNullOrEmpty(tagType))
                    values = Map.TryGetValue(tagType + TagTypeSeparator +
                                                          GenericTag + propertyPath);
                if (values == null)
                    values = Map.TryGetValue(GenericTag + propertyPath);
                if (values != null)
                {
                    if (values.Count > 1)
                    {
                        for (var i = 1; i < values.Count; i++)
                        {
                            string? v = SszQueryHelper.ComputeValueOfSszQueries(values[i], constant => (newTag ?? tag) ?? "", csvDb);                            
                            result.Add(v ?? @"");
                        }
                    }
                    else
                    {
                        result.Add("");
                    }
                }
                else
                {
                    result.Add((newTag ?? tag) + propertyPath);
                }
            }
            else
            {
                result.Add(elementId);
            }

            return result;
        }

        public Any? TryGetConstValue(string? mappedElementIdOrConst)
        {
            if (string.IsNullOrEmpty(mappedElementIdOrConst) || mappedElementIdOrConst.StartsWith(TagAndPropertySeparator) ||
                mappedElementIdOrConst.EndsWith(TagAndPropertySeparator)) return new Any(DBNull.Value);

            if (mappedElementIdOrConst.StartsWith("\"") && mappedElementIdOrConst.EndsWith("\""))
            {
                mappedElementIdOrConst = mappedElementIdOrConst.Substring(1, mappedElementIdOrConst.Length - 2);
                return new Any(mappedElementIdOrConst);
            }

            var any = Any.ConvertToBestType(mappedElementIdOrConst, false);
            if (any.ValueTypeCode != TypeCode.String) return any;

            return null;
        }

        public string GetTagType(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return "";
            var tagInfoValues = TagInfos.TryGetValue(tag);
            if (tagInfoValues == null) return "";
            if (tagInfoValues.Count < 2) return "";
            return tagInfoValues[1] ?? @"";
        }

        #endregion

        #region private fields

        //private ILogger<CsvMap>? _logger;       

        #endregion
    }
}
