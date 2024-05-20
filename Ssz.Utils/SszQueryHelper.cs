using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public static class SszQueryHelper
    {
        #region public functions

        /// <summary>
        ///     Searches special text like %(...) or $(...) in string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="symbol"></param>
        /// <param name="returnFirstOnly"></param>
        /// <returns></returns>
        public static List<string> FindFirstLevelSpecialText(string? value, char symbol, bool returnFirstOnly)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(value) || value!.Length < 4) return result;

            var firstOpenIndex = -1;
            var openCount = 0;
            for (var i = 0; i < value.Length; i++)
                if (firstOpenIndex == -1)
                {
                    if (i < value.Length - 1 && value[i] == symbol && value[i + 1] == '(')
                    {
                        openCount = 1;
                        firstOpenIndex = i;
                        i++;
                    }
                }
                else
                {
                    if (value[i] == '(')
                    {
                        openCount++;
                    }
                    else if (value[i] == ')')
                    {
                        openCount--;
                        if (openCount == 0)
                        {
                            if (i - firstOpenIndex > 2)
                            {
                                string r = value.Substring(firstOpenIndex, i - firstOpenIndex + 1);
                                result.Add(r);
                                if (returnFirstOnly) return result;
                            }

                            firstOpenIndex = -1;
                        }
                    }
                }

            return result;
        }

        /// <summary>
        ///     Compute SSZ queries in string.
        ///     E.g. %(TAG), %(TAG => s[0].Substring(1)), %(tags.csv,%(TAG),1)
        /// </summary>
        /// <param name="originalString"></param>
        /// <param name="getConstantValue"></param>        
        /// <param name="csvDb"></param>
        /// <param name="iterationInfo"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull("originalString")]
        public static string? ComputeValueOfSszQueries(string? originalString, Func<string, IterationInfo, string>? getConstantValue, CsvDb? csvDb = null, IterationInfo? iterationInfo = null)
        {
            if (string.IsNullOrWhiteSpace(originalString) || getConstantValue is null) 
                return originalString;
            
            originalString = ComputeValueOfSszQueriesInternal(originalString!, getConstantValue, csvDb, iterationInfo ?? new IterationInfo());

            return originalString;
        }

        #endregion

        #region private functions

        private static string EscapeForSszQuery(string value)
        {
            if (value == @"")
                return @"";
            return value.Replace(@",", @"###Comma###").Replace(@"=>", @"###Lambda###");
        }

        private static string UnescapeForSszQuery(string value)
        {
            if (value == @"") 
                return @"";
            return value.Replace(@"###Comma###", @",").Replace(@"###Lambda###", @"=>");
        }

        private static string ComputeValueOfSszQueriesInternal(
            string originalString,
            Func<string, IterationInfo, string> getConstantValue, CsvDb? csvDb,
            IterationInfo iterationInfo)
        {
            var firstLevelSszQueries = FindFirstLevelSpecialText(originalString, '%', false);
            foreach (string firstLevelSszQuery in firstLevelSszQueries)
            {
                string valueOfQuery =
                    ComputeValueOfSszQuery(firstLevelSszQuery, getConstantValue, csvDb, iterationInfo);
                originalString = originalString.Replace(firstLevelSszQuery, valueOfQuery);
            }

            return originalString;
        }

        private static string ComputeValueOfSszQuery(
            string sszQuery, 
            Func<string, IterationInfo, string> getConstantValue,
            CsvDb? csvDb,
            IterationInfo iterationInfo)
        {
            iterationInfo.IterationN += 1;
            if (iterationInfo.IterationN > 64)
                return sszQuery;

            string q = sszQuery.Substring(2, sszQuery.Length - 3);

            var firstLevelSszQueries = FindFirstLevelSpecialText(q, '%', false);
            foreach (string firstLevelSszQuery in firstLevelSszQueries)
            {
                string valueOfQuery =
                    ComputeValueOfSszQuery(firstLevelSszQuery, getConstantValue, csvDb, iterationInfo);
                q = q.Replace(firstLevelSszQuery, EscapeForSszQuery(valueOfQuery));
            }

            var i = q.IndexOf("=>");
            if (i >= 0)
            {
                var dataSourceValues = new List<object>();
                string args = UnescapeForSszQuery(q.Substring(0, i).Trim());
                string lambda = UnescapeForSszQuery(q.Substring(i + 2).Trim());
                foreach (var a in args.Split(','))
                {
                    string constant = a.Trim();
                    if (constant == @"") return @""; // Query is not resolved
                    constant = @"%(" + constant + @")";
                    string valueOfQuery = getConstantValue(constant, iterationInfo) ?? @""; // For safe getConstantValue call
                    dataSourceValues.Add(valueOfQuery);
                }

                var evaluated = new SszExpression(lambda).Evaluate(dataSourceValues.ToArray(), null, null, null);
                return new Any(evaluated).ValueAsString(false);
            }

            string[] parts = q.Split(',');
            if (parts.Length == 1 || parts.Length == 2)
            {
                string constant = UnescapeForSszQuery(parts[0].Trim());
                if (constant == @"") return @""; // Query is not resolved
                constant = @"%(" + constant + @")";
                string valueOfSszQuery = getConstantValue(constant, iterationInfo) ?? @"";
                if (valueOfSszQuery == @"" && parts.Length == 2)
                    valueOfSszQuery = UnescapeForSszQuery(parts[1].Trim());
                return valueOfSszQuery;
            }

            if ((parts.Length == 3 || parts.Length == 4) && csvDb is not null)
            {
                string fileName = UnescapeForSszQuery(parts[0].Trim());
                string key = UnescapeForSszQuery(parts[1].Trim());
                int column;
                if (!int.TryParse(UnescapeForSszQuery(parts[2].Trim()), out column))
                    return @""; // Query is not resolved
                string valueOfSszQuery = csvDb.GetValue(fileName, key, column) ?? @"";
                if (valueOfSszQuery == @"" && parts.Length == 4)
                    valueOfSszQuery = UnescapeForSszQuery(parts[3].Trim());
                return valueOfSszQuery;
            }

            return sszQuery; // Query is not resolved
        }        

        #endregion
    }
}
