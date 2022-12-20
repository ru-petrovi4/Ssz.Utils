using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Ssz.Utils.Logging;

namespace Ssz.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class CsvHelper
    {
        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="separator"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string FormatForCsv(string separator, IEnumerable<string?> values)
        {
            if (separator.Length != 1) throw new InvalidOperationException();

            string result = @"";
            bool inited = false;
            foreach (string? v in values)
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
        ///     Formats with default separator - comma.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string FormatForCsv(params string?[] values)
        {            
            return FormatForCsv(@",", values);
        }

        /// <summary>        
        ///     values converted using Any.ConvertTo String (obj, false).
        /// </summary>
        public static string FormatForCsv(string separator, IEnumerable<object?> values)
        {
            if (separator.Length != 1) throw new InvalidOperationException();

            return FormatForCsv(separator, values.Select(obj => obj is null ? null : new Any(obj).ValueAsString(false)));
        }

        /// <summary>
        ///     Formats with default separator - comma.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string FormatForCsv(params object?[] values)
        {
            return FormatForCsv(@",", values);
        }

        /// <summary>        
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string FormatValueForCsv(string separator, string? sourceString)
        {
            if (separator.Length != 1) throw new InvalidOperationException();

            if (sourceString is null) return @"";
            if (sourceString == @"") return "\"\"";

            sourceString = sourceString
                .Replace("\"", "\"\"");

            return sourceString.Contains(separator) || sourceString.Contains('\"') ||
                sourceString.Contains('\n') ||
                sourceString.StartsWith(" ") || sourceString.EndsWith(" ")
                ? "\"" + sourceString + "\""
                : sourceString;
        }

        /// <summary>        
        ///     If empty field, value in array is null.
        ///     # parsed as ordinary symbol. All field values are trimmed.
        ///     Only first char is used as separator in separator param.
        ///     result.Length > 0
        /// </summary>
        public static string?[] ParseCsvLine(string separator, string? sourceString)
        {
            if (separator.Length != 1) throw new InvalidOperationException();

            if (sourceString is null || sourceString == @"") return new string?[] { null };

            bool inQuotes = false;
            return ParseCsvLineInternal(separator, sourceString, ref inQuotes).ToArray();            
        }

        /// <summary>   
        ///     Child list.Count > 0
        ///     No procerssing #include, #define, comments.
        /// </summary>
        /// <param name="separator"></param>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        public static List<List<string?>> ParseCsv(string separator, string? sourceString)
        {
            if (separator.Length != 1) throw new InvalidOperationException();            

            var result = new List<List<string?>>();

            if (String.IsNullOrEmpty(sourceString))
                return result;

            using (var reader = new StringReader(sourceString))
            {
                string line = "";
                string? l;
                List<string?>? beginFields = null;
                bool inQuotes = false;
                while ((l = reader.ReadLine()) is not null)
                {
                    l = l.Trim();

                    line += l;

                    if (line == @"")
                    {
                        result.Add(new List<string?> { @"" });
                    }
                    else
                    {
                        List<string?> fields;

                        if (beginFields is null)
                        {
                            fields = ParseCsvLineInternal(@",", line, ref inQuotes);
                            if (inQuotes)
                            {
                                beginFields = fields;
                                line = "";
                                continue;
                            }
                        }
                        else
                        {
                            fields = ParseCsvLineInternal(@",", line, ref inQuotes);
                            beginFields[beginFields.Count - 1] = beginFields[beginFields.Count - 1] + fields[0];
                            beginFields.AddRange(fields.Skip(1));
                            if (inQuotes)
                            {
                                line = "";
                                continue;
                            }

                            fields = beginFields;
                            beginFields = null;
                        }

                        result.Add(fields);
                        line = "";
                    }                    
                }
            }

            return result;
        }

        /// <summary>
        ///     First column in file is Key and must be unique.
        ///     With procerssing #include, #define, comments.
        ///     If file does not exist, returns empty result.
        ///     userFriendlyLogger: Messages are localized. Priority is Information, Error, Warning.
        ///     includeFileNames: File names in Upper-Case.
        ///     Result: List.Count >= 1, List[0] is not null
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <param name="includeFiles"></param>
        /// <param name="defines"></param>
        /// <param name="userFriendlyLogger"></param>
        /// <param name="includeFileNames"></param>
        /// <returns></returns>
        public static CaseInsensitiveDictionary<List<string?>> LoadCsvFile(string fileFullName, bool includeFiles, Dictionary<Regex, string>? defines = null,
            IUserFriendlyLogger? userFriendlyLogger = null, List<string>? includeFileNames = null)
        {
            var fileData = new CaseInsensitiveDictionary<List<string?>>();

            using var fileFullNameScope = userFriendlyLogger?.BeginScope("FileName: " + Path.GetFileName(fileFullName));
            
            if (!File.Exists(fileFullName))
            {
                userFriendlyLogger?.LogError(Properties.Resources.CsvHelper_CsvFileDoesNotExist);
                return fileData;
            }

            try
            {
                if (defines is null) defines = new Dictionary<Regex, string>();
                string? filePath = Path.GetDirectoryName(fileFullName);
                using (var reader = new StreamReader(fileFullName, true))
                {
                    string line = "";
                    string? l;
                    List<string?>? beginFields = null;
                    bool inQuotes = false;
                    while ((l = reader.ReadLine()) is not null)
                    {                            
                        l = l.Trim();

                        if (l.Length > 0 && l[l.Length - 1] == '\\')
                        {
                            if (line != @"")
                            {
                                if (StringHelper.StartsWithIgnoreCase(line, @"#define"))
                                {
                                    line += @" " + l.Substring(0, l.Length - 1);
                                    continue;
                                }                                    
                            }
                            else
                            {
                                if (StringHelper.StartsWithIgnoreCase(l, @"#define"))
                                {
                                    line = l.Substring(0, l.Length - 1);
                                    continue;
                                }                                    
                            }                                
                        }

                        line += l;

                        if (line == "") continue;

                        List<string?> fields;

                        if (beginFields is null)
                        {
                            if (includeFiles && StringHelper.StartsWithIgnoreCase(line, @"#include") && line.Length > 8)
                            {
                                var q1 = line.IndexOf('"', 8);
                                if (q1 != -1 && q1 + 1 < line.Length)
                                {
                                    var q2 = line.IndexOf('"', q1 + 1);
                                    if (q2 != -1 && q2 > q1 + 1)
                                    {
                                        var includeFileName = line.Substring(q1 + 1, q2 - q1 - 1);
                                        if (includeFileNames is not null)
                                            includeFileNames.Add(includeFileName.ToUpperInvariant());
                                        foreach (var kvp in LoadCsvFile(filePath + @"\" + includeFileName, false, defines, userFriendlyLogger))
                                        {
                                            if (fileData.ContainsKey(kvp.Key))
                                            {
                                                userFriendlyLogger?.LogError(Properties.Resources.CsvHelper_CsvFileDuplicateKey + ", Key='" + kvp.Key + "'");
                                            }
                                            fileData[kvp.Key] = kvp.Value;
                                        }
                                    }
                                }

                                line = "";
                                continue;
                            }
                            if (StringHelper.StartsWithIgnoreCase(line, @"#define") && line.Length > 7)
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

                                line = "";
                                continue;
                            }
                            if (line[0] == '#')
                            {
                                // Comment, skip

                                line = "";
                                continue;
                            }

                            fields = ParseCsvLineInternal(@",", ReplaceDefines(line, defines), ref inQuotes);
                            if (inQuotes)
                            {
                                beginFields = fields;
                                line = "";
                                continue;
                            }
                        }
                        else
                        {
                            fields = ParseCsvLineInternal(@",", ReplaceDefines(line, defines), ref inQuotes);
                            beginFields[beginFields.Count - 1] = beginFields[beginFields.Count - 1] + '\n' + fields[0];
                            beginFields.AddRange(fields.Skip(1));
                            if (inQuotes)
                            {
                                line = "";
                                continue;
                            }
                                
                            fields = beginFields;
                            beginFields = null;                                
                        }

                        string? field0 = fields[0];
                        if (field0 is null)
                        {
                            fields[0] = @"";
                            field0 = @"";
                        }
                        if (field0 == @"")
                        {
                            if (!fields.All(f => String.IsNullOrEmpty(f)))
                            {
                                if (fileData.ContainsKey(@""))
                                {
                                    userFriendlyLogger?.LogError(Properties.Resources.CsvHelper_CsvFileDuplicateKey + ", Key=''");
                                }
                                fileData[@""] = fields;
                            }
                        }
                        else
                        {
                            if (fileData.ContainsKey(field0))
                            {
                                userFriendlyLogger?.LogError(Properties.Resources.CsvHelper_CsvFileDuplicateKey + ", Key='" + field0 + "'");
                            }
                            fileData[field0] = fields;
                        }

                        line = "";
                    }
                }
            }
            catch (Exception ex)
            {
                userFriendlyLogger?.LogError(ex, Properties.Resources.CsvHelper_CsvFileReadingError);
            }                      

            return fileData;
        }        

        /// <summary>
        ///     Saves data to file.
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <param name="fileData"></param>
        public static void SaveCsvFile(string fileFullName, CaseInsensitiveDictionary<List<string?>> fileData)
        {
            using (var writer = new StreamWriter(File.Create(fileFullName), new UTF8Encoding(true)))
            {
                foreach (var fileLine in fileData.OrderBy(kvp => kvp.Key))
                {
                    writer.WriteLine(FormatForCsv(",", fileLine.Value));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <param name="fileData"></param>
        public static void SaveCsvFile(string fileFullName, IEnumerable<IEnumerable<string?>> fileData)
        {
            using (var writer = new StreamWriter(File.Create(fileFullName), new UTF8Encoding(true)))
            {
                foreach (var fileLine in fileData)
                {
                    writer.WriteLine(FormatForCsv(",", fileLine));
                }
            }
        }        

        #endregion

        #region private functions

        /// <summary>
        ///     sourceString is not null, defines is not null, result is not null
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

        /// <summary>
        ///     Preconditions: separator.Length == 1, sourceString != String.Empty
        ///     Returns whether last quote is not closed, so new line must be appended to this.
        ///     result.Length > 0
        /// </summary>
        /// <param name="separator"></param>
        /// <param name="sourceString"></param>
        /// <param name="inQuotes"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static List<string?> ParseCsvLineInternal(string separator, string sourceString, ref bool inQuotes)
        {            
            var separatorChar = separator[0];
            var result = new List<string?>();            
            bool fieldHasQuotes = false;
            int fieldBeginIndex = 0;
            for (int i = 0; i <= sourceString.Length; i++)
            {
                char ch;
                if (i < sourceString.Length) ch = sourceString[i];
                else ch = default;
                if (ch == '\"')
                {
                    fieldHasQuotes = true;
                    inQuotes = !inQuotes;
                    continue;
                }
                if ((!inQuotes && ch == separatorChar) || ch == default)
                {
                    if (fieldBeginIndex == i)
                    {
                        result.Add(null);
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
                    fieldBeginIndex = i + 1;
                }
            }            
            return result;
        }        

        #endregion
    }
}