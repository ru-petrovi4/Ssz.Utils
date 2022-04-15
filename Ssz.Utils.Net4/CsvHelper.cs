using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace Ssz.Utils.Net4
{
    public static class CsvHelper
    {
        #region public functions

        /// <summary>
        ///     values != null, result != null        
        /// </summary>
        public static string FormatForCsv(string separator, IEnumerable<string> values)
        {
            if (values is null) throw new ArgumentNullException(@"values");

            string result = @"";
            bool inited = false;
            foreach (var v in values)
            {
                if (inited)
                {
                    result += separator;
                }
                else
                {
                    inited = true;
                }
                result += FormatValueForCsv(separator, v);                
            }
            return result;
        }

        /// <summary>
        ///     values != null, result != null        
        ///     values converted using Any.ConvertTo String (obj, false).
        /// </summary>
        public static string FormatForCsv(string separator, IEnumerable<object> values)
        {
            if (values is null) throw new ArgumentNullException(@"values");
            
            return FormatForCsv(separator, values.Select(obj => Any.ConvertTo<string>(obj, false)));
        }

        /// <summary>
        ///     result != null
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string FormatValueForCsv(string separator, string sourceString)
        {
            if (sourceString is null) return @"";
            if (sourceString == @"") return "\"\"";

            sourceString = sourceString
                .Replace("\"", "\"\"");

            return sourceString.Contains(separator) || sourceString.Contains('\"') ||
                sourceString.StartsWith(" ") || sourceString.EndsWith(" ")
                ? "\"" + sourceString + "\""
                : sourceString;
        }

        /// <summary>
        ///     result != null
        ///     If empty field, value in array is null.
        ///     # parsed as ordinary symbol. All field values are trimmed.
        ///     Only first char is used as separator in separator param.
        /// </summary>
        public static string[] ParseCsvLine(string separator, string sourceString)
        {            
            if (String.IsNullOrEmpty(sourceString)) return new [] { @"" };
            if (String.IsNullOrEmpty(separator)) return new[] { sourceString };

            var result = new List<string>();
            bool inQuotes = false;
            bool fieldHasQuotes = false;
            int fieldBeginIndex = 0;
            for (int i = 0; i <= sourceString.Length; i++)
            {
                char ch;
                if (i < sourceString.Length) ch = sourceString[i];
                else ch = default (char);
                if (ch == '\"')
                {
                    fieldHasQuotes = true;
                    inQuotes = !inQuotes;
                    continue;
                }
                if (!inQuotes && (ch == separator[0] || ch == default(char)))
                {
                    if (fieldBeginIndex == i)
                    {
                        result.Add(@"");
                    }
                    else
                    {
                        string value = sourceString.Substring(fieldBeginIndex, i - fieldBeginIndex).Trim();
                        if (value == @"")
                        {
                            result.Add(null);
                        }
                        else
                        {
                            if (fieldHasQuotes)
                            {
                                if (value.StartsWith("\"")) value = value.Substring(1);
                                if (value.EndsWith("\"")) value = value.Substring(0, value.Length - 1);
                                value = value.Replace("\"\"", "\"");
                            }
                            result.Add(value);
                        }                        
                    }
                    fieldHasQuotes = false;
                    fieldBeginIndex = i + separator.Length;                    
                }
            }            
            return result.ToArray();
        }

        /// <summary>
        ///     result != null, arrays != null
        /// </summary>
        /// <param name="separator"></param>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        public static List<string[]> ParseCsv(string separator, string sourceString)
        {
            var result = new List<string[]>();
            if (String.IsNullOrEmpty(sourceString)) return result;            

            foreach (string line in sourceString.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {                
                result.Add(ParseCsvLine(separator, line));                
            }

            return result;
        }

        /// <summary>
        ///     First column in file is Key and must be unique.
        ///     Can contain include directives, defines and comments.
        ///     If file does not exist returns empty result.
        ///     All fields values are trimmed. 
        ///     result != null, lists != null
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <param name="includeFiles"></param>
        /// <param name="defines"></param>
        /// <returns></returns>
        public static CaseInsensitiveDictionary<List<string>> ParseCsvFile(string fileFullName, bool includeFiles, Dictionary<Regex, string> defines = null)
        {
            var fileData = new CaseInsensitiveDictionary<List<string>>();            
                
            if (File.Exists(fileFullName))
            {
                if (defines is null) defines = new Dictionary<Regex, string>();
                string filePath = Path.GetDirectoryName(fileFullName);
                using (var reader = new StreamReader(fileFullName, true))
                {
                    string line = "";
                    string l;
                    while ((l = reader.ReadLine()) != null)
                    {
                        l = l.Trim();                        
                        if (l.Length > 0 && l[l.Length - 1] == '\\')
                        {
                            line += l.Substring(0, l.Length - 1);
                            continue;
                        }
                        else
                        {
                            line += l;
                        }
                        if (line == "") continue;
                        if (includeFiles && StringHelper.StartsWithIgnoreCase(line, @"#include") && line.Length > 8)
                        {
                            var q1 = line.IndexOf('"', 8);
                            if (q1 != -1 && q1 + 1 < line.Length)
                            {
                                var q2 = line.IndexOf('"', q1 + 1);
                                if (q2 != -1 && q2 > q1 + 1)
                                {
                                    var includeFileName = line.Substring(q1 + 1, q2 - q1 - 1);
                                    foreach (var kvp in ParseCsvFile(filePath + @"\" + includeFileName, false, defines))
                                    {
                                        fileData[kvp.Key] = kvp.Value;
                                    }
                                }
                            }
                        }
                        else if (StringHelper.StartsWithIgnoreCase(line, @"#define") && line.Length > 7)
                        {
                            int q1 = 7;
                            for (; q1 < line.Length; q1++)
                            {
                                char ch = line[q1];
                                if (Char.IsWhiteSpace(ch)) continue;
                                else break;
                            }
                            if (q1 < line.Length)
                            {
                                int q2 = q1 + 1;
                                for (; q2 < line.Length; q2++)
                                {
                                    char ch = line[q2];
                                    if (Char.IsWhiteSpace(ch)) break;
                                    else continue;
                                }
                                string define = line.Substring(q1, q2 - q1);
                                string subst = @"";
                                if (q2 < line.Length - 1)
                                {                                    
                                    subst = ReplaceDefines(line.Substring(q2 + 1).Trim(), defines);
                                }
                                defines[new Regex(@"\b" + define + @"\b", RegexOptions.IgnoreCase)] = subst;
                            }
                        }
                        else if (line[0] == '#')
                        {
                            // Comment, skip                            
                        }
                        else
                        {
                            List<string> fields =
                                CsvHelper.ParseCsvLine(@",", ReplaceDefines(line, defines)).ToList();
                            if (fields.Count > 0)
                            {
                                if (String.IsNullOrEmpty(fields[0]))
                                {
                                    if (fields.Count > 1)
                                    {
                                        fileData[@""] = fields;
                                    }
                                }
                                else
                                {
                                    fileData[fields[0]] = fields;
                                }
                            }
                        }
                        line = "";
                    }
                }                                
            }            

            return fileData;
        }

        #endregion

        #region private functions

        /// <summary>
        ///     sourceString != null, defines != null, result != null
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="defines"></param>
        /// <returns></returns>
        private static string ReplaceDefines(string sourceString, Dictionary<Regex, string> defines)
        {  
            foreach (var d in defines)
            {
                sourceString = d.Key.Replace(sourceString, d.Value);
            }
            return sourceString;
        }

        #endregion
    }
}