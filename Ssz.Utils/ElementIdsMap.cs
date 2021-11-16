﻿using Microsoft.Extensions.Logging;
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

        public const string StandardMapFileName = @"Map.csv";

        public const string StandardTagsFileName = @"Tags.csv";

        /// <summary>
        ///     Can be configured in map, 'GenericTag' key
        /// </summary>
        public string GenericTag { get; private set; } = @"%(TAG)";

        /// <summary>
        ///     Can be configured in map, 'TagTypeSeparator' key
        /// </summary>
        public string TagTypeSeparator { get; private set; } = @":";

        /// <summary>
        ///     Can be configured in map, 'TagAndPropertySeparator' key
        /// </summary>
        public string TagAndPropertySeparator { get; private set; } = @".";

        public CaseInsensitiveDictionary<List<string?>> Map { get; private set; } = null!;

        public CaseInsensitiveDictionary<List<string?>> Tags { get; private set; } = null!;

        /// <summary>
        ///     Can be called multiple times. Other methods calls must be after this itinialization.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="tags"></param>
        public void Initialize(CaseInsensitiveDictionary<List<string?>> map,
            CaseInsensitiveDictionary<List<string?>> tags)
        {            
            Map = map;
            Tags = tags;

            var values = Map.TryGetValue("GenericTag");
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                GenericTag = values[1] ?? @"";

            values = Map.TryGetValue("TagTypeSeparator");
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                TagTypeSeparator = values[1] ?? @"";

            values = Map.TryGetValue("TagAndPropertySeparator");
            if (values is not null && values.Count > 1 && !String.IsNullOrEmpty(values[1]))
                TagAndPropertySeparator = values[1] ?? @"";
        }

        /// <summary>
        ///     result.Count > 1
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="csvDb"></param>
        /// <returns></returns>
        public List<string?> GetFromMap(string elementId, CsvDb? csvDb = null)
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
        public List<string?> GetFromMap(string? tag, string? propertyPath, string? tagType, CsvDb? csvDb = null)
        {
            string elementId = tag + propertyPath;
            if (elementId == @"") return new List<string?> { @"", @"" };

            var values = Map.TryGetValue(elementId);
            if (values is not null)
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
                    if (values is not null && values.Count > 1 && values[1] != "") newTag = values[1];
                }

                values = null;
                if (!string.IsNullOrEmpty(tagType))
                    values = Map.TryGetValue(tagType + TagTypeSeparator +
                                                          GenericTag + propertyPath);
                if (values is null)
                    values = Map.TryGetValue(GenericTag + propertyPath);
                if (values is not null)
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

        public string GetTagType(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return "";
            var tagValues = Tags.TryGetValue(tag);
            if (tagValues is null) return "";
            if (tagValues.Count < 2) return "";
            return tagValues[1] ?? @"";
        }

        public static Any? TryGetConstValue(string? elementIdOrConst)
        {
            if (string.IsNullOrEmpty(elementIdOrConst)) return new Any(DBNull.Value);

            if (elementIdOrConst.StartsWith("\"") && elementIdOrConst.EndsWith("\""))
            {
                elementIdOrConst = elementIdOrConst.Substring(1, elementIdOrConst.Length - 2);
                return new Any(elementIdOrConst);
            }

            var any = Any.ConvertToBestType(elementIdOrConst, false);
            if (any.ValueTypeCode != TypeCode.String) return any;

            return null;
        }

        #endregion

        #region private fields

        private ILogger<ElementIdsMap>? _logger;       

        #endregion
    }
}
