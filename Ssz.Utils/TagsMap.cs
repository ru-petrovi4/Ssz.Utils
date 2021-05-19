using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class TagsMap
    {
        #region construction and destruction

        public TagsMap(CaseInsensitiveDictionary<List<string?>> mapDictionary,
            CaseInsensitiveDictionary<List<string?>> tagsDictionary)
        {
            //_logger = logger;

            _mapDictionary = mapDictionary;
            _tagsDictionary = tagsDictionary;
        }

        #endregion

        #region public functions

        public string GenericTag { get; set; } = "%(TAG)";

        public string TagTypeSeparator { get; set; } = ":";

        public string TagAndPropertySeparator { get; set; } = ".";

        public string GetFromMap(string elementId)
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

            List<string?> mapValues = GetFromOpcMap(tag, propertyPath, tagType);

            return mapValues[1] ?? "";
        }

        #endregion

        #region private functions

        private string GetTagType(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return "";
            var line = _tagsDictionary.TryGetValue(tag);
            if (line == null) return "";
            if (line.Count < 2) return "";
            return line[1] ?? @"";
        }

        /// <summary>
        ///     result.Count > 1
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="propertyPath"></param>
        /// <param name="tagType"></param>
        /// <returns></returns>
        private List<string?> GetFromOpcMap(string? tag, string? propertyPath, string? tagType)
        {
            string uiId = tag + propertyPath;
            if (uiId == @"") return new List<string?> { @"", @"" };

            var values = _mapDictionary.TryGetValue(uiId);
            if (values != null)
            {
                if (values.Count == 1) values.Add("");
                return values;
            }

            var result = new List<string?> { uiId };

            if (!string.IsNullOrEmpty(propertyPath))
            {
                string? newTag = null;

                if (!string.IsNullOrEmpty(tag))
                {
                    values = _mapDictionary.TryGetValue(tag);
                    if (values != null && values.Count > 1 && values[1] != "") newTag = values[1];
                }

                values = null;
                if (!string.IsNullOrEmpty(tagType))
                    values = _mapDictionary.TryGetValue(tagType + TagTypeSeparator +
                                                          GenericTag + propertyPath);
                if (values == null)
                    values = _mapDictionary.TryGetValue(GenericTag + propertyPath);
                if (values != null)
                {
                    if (values.Count > 1)
                    {                        
                        for (var i = 1; i < values.Count; i++)
                        {                            
                            string v = values[i] ?? "";
                            StringHelper.ReplaceIgnoreCase(ref v, GenericTag, (newTag ?? tag) ?? "");
                            result.Add(v);
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
                result.Add(uiId);
            }

            return result;
        }

        #endregion

        #region private fields

        //private ILogger<CsvMap>? _logger;       

        private CaseInsensitiveDictionary<List<string?>> _mapDictionary;

        private CaseInsensitiveDictionary<List<string?>> _tagsDictionary;

        #endregion
    }
}
