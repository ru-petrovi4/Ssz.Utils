using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.MonitoredUndo;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.Constants
{
    public static class ConstantsHelper
    {
        #region public functions

        public static void FindConstants(string? originalString, HashSet<string> constants)
        {
            FindConstantsInternal(originalString, constants);
        }

        public static void FindConstants(IEnumerable<DsConstant> dsConstantsCollection,
            HashSet<string> constants)
        {
            foreach (DsConstant dsConstant in dsConstantsCollection)
                if (dsConstant.Value == @"")
                {
                    var constantString = dsConstant.Name;
                    if (!string.IsNullOrEmpty(dsConstant.Type) || !string.IsNullOrEmpty(dsConstant.Desc))
                        constantString += DataBindingItem.DataSourceStringSeparator + dsConstant.Type +
                                          DataBindingItem.DataSourceStringSeparator + dsConstant.Desc;
                    constants.Add(constantString);
                }
                else
                {
                    FindConstantsInternal(dsConstant.Value, constants);
                }
        }

        public static void FindConstants<T>(List<T> list, HashSet<string> constants)
            where T : IDsItem
        {
            foreach (IDsItem dsItem in list)
            {
                dsItem.FindConstants(constants);
            }
        }

        public static bool ContainsQuery(string? originalString)
        {
            return FindFirstLevelQueries(originalString, true).Count > 0;
        }

        public static List<string> FindFirstLevelQueries(string? value, bool returnFirstOnly = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return new List<string>();
            return SszQueryHelper.FindFirstLevelSpecialText(value, '%', returnFirstOnly);
        }

        /// <summary>
        ///     Computes SSZ Queries only
        /// </summary>
        /// <param name="container"></param>
        /// <param name="originalString"></param>
        /// <param name="iterationInfo"></param>
        /// <returns></returns>
#if NET5_0_OR_GREATER
        [return: NotNullIfNotNull("originalString")]
#endif
        public static string? ComputeValueOfQueries(IDsContainer? container, string? originalString, IterationInfo iterationInfo)
        {
            if (string.IsNullOrWhiteSpace(originalString)) 
                return originalString;
            
            return SszQueryHelper.ComputeValueOfSszQueries(originalString,
                (constant, ii) => GetConstantValue(constant, container ?? DsProject.Instance, ii), DsProject.Instance.CsvDb, iterationInfo);
        }

        /// <summary>
        ///     Computes SSZ Queries and Variables
        /// </summary>
        /// <param name="container"></param>
        /// <param name="originalString"></param>
        /// <returns></returns>
#if NET5_0_OR_GREATER
        [return: NotNullIfNotNull("originalString")]
#endif
        public static string? ComputeValue(IDsContainer? container, string? originalString)
        {
            if (string.IsNullOrWhiteSpace(originalString)) 
                return originalString;

            IPlayWindowBase? playWindow = container?.PlayWindow;            

            if (playWindow is not null)
                originalString = ComputeValueOfVariables(originalString!, playWindow.WindowVariables);
            originalString = ComputeValueOfVariables(originalString!, DsProject.Instance.GlobalVariables);

            originalString = SszQueryHelper.ComputeValueOfSszQueries(originalString,
                (constant, ii) => GetConstantValue(constant, container ?? DsProject.Instance, ii), DsProject.Instance.CsvDb);
            
            if (playWindow is not null)
                originalString = ComputeValueOfVariables(originalString!, playWindow.WindowVariables);
            originalString = ComputeValueOfVariables(originalString!, DsProject.Instance.GlobalVariables);

            return originalString;
        }

        public static void ComputeFont(IDsContainer? container, DsFont? dsFont,
            out FontFamily? fontFamily, out double fontSize, out FontStyle? fontStyle, out FontStretch? fontStretch,
            out FontWeight? fontWeight)
        {
            DsFont resultDsFont;
            if (dsFont is null) dsFont = new DsFont();
            var mappedDsFont = NameValueCollectionValueSerializer<DsFont>.Instance.ConvertFromString(
                    DsProject.Instance.CsvDb.GetValue(
                        DsProject.Instance.DataEngine.FontsMapFileName,
                        dsFont.Family is not null ? dsFont.Family.ToString() : "", 1),
                    null) as
                DsFont;
            if (mappedDsFont is not null)
                resultDsFont = new DsFont
                {
                    Family = mappedDsFont.Family is not null ? mappedDsFont.Family : dsFont.Family,
                    Size = !string.IsNullOrWhiteSpace(dsFont.Size) ? dsFont.Size : mappedDsFont.Size,
                    Style = dsFont.Style is not null ? dsFont.Style : mappedDsFont.Style,
                    Stretch = dsFont.Stretch is not null ? dsFont.Stretch : mappedDsFont.Stretch,
                    Weight = dsFont.Weight is not null ? dsFont.Weight : mappedDsFont.Weight
                };
            else
                resultDsFont = dsFont;

            fontFamily = resultDsFont.Family;
            var computedSize = ComputeValue(container, resultDsFont.Size);
            if (!string.IsNullOrWhiteSpace(computedSize))
                double.TryParse(computedSize, NumberStyles.Any, CultureInfo.InvariantCulture, out fontSize);
            else
                fontSize = 0;
            fontStyle = resultDsFont.Style;
            fontStretch = resultDsFont.Stretch;
            fontWeight = resultDsFont.Weight;
        }

        public static string GetConstantValue(string? constant, IDsContainer container, IterationInfo iterationInfo)
        {
            if (string.IsNullOrWhiteSpace(constant) || constant!.Length < 4) 
                return @"";

            if (constant.StartsWith(@"%(U+"))
            {
                ushort code;
                if (ushort.TryParse(constant.Substring(4, constant.Length - 5).Trim(),
                    NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out code))
                    try
                    {
                        return char.ToString((char)code);
                    }
                    catch
                    {
                    }
            }

            var dsConstant = container.DsConstantsCollection
                .FirstOrDefault(gpi => StringHelper.CompareIgnoreCase(gpi.Name, constant));
            if (dsConstant is null)
            {
                var hiddenDsConstantsCollection = container.HiddenDsConstantsCollection;
                if (hiddenDsConstantsCollection is not null)
                    dsConstant = hiddenDsConstantsCollection.FirstOrDefault(
                            gpi => StringHelper.CompareIgnoreCase(gpi.Name, constant));
            }                               

            if (dsConstant is not null && dsConstant.Value != @"")
                return ComputeValueOfQueries(container, dsConstant.Value, iterationInfo) ?? @"";

            var parentContainer = container.ParentItem.Find<IDsContainer>();

            if (parentContainer is not null)
                return GetConstantValue(constant, parentContainer, iterationInfo);

            return @"";
        }

        public static string? GetConstantType(string? constant,
            IDsContainer? container)
        {
            if (container is null || string.IsNullOrWhiteSpace(constant) || constant!.Length < 4) return null;

            var dsConstant =
                container.DsConstantsCollection.FirstOrDefault(
                    gpi => StringHelper.CompareIgnoreCase(gpi.Name, constant));
            if (dsConstant is not null && dsConstant.Value != @"") return dsConstant.Type ?? @"";

            var parentContainer = container.ParentItem.Find<IDsContainer>();
            return GetConstantType(constant, parentContainer);
        }

        public static void FindConstantsInFields(object constantsHolder,
            HashSet<string> constants)
        {
            IEnumerable<FieldInfo> fields = ObjectHelper.GetAllFields(constantsHolder);
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    var propValueString = field.GetValue(constantsHolder) as string;
                    FindConstants(propValueString, constants);
                    continue;
                }

                if (typeof(IConstantsHolder).IsAssignableFrom(field.FieldType))
                {
                    var gph = field.GetValue(constantsHolder) as IConstantsHolder;
                    if (gph is not null) gph.FindConstants(constants);
                    continue;
                }

                if (field.FieldType.IsGenericType &&
                    field.FieldType.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
                {
                    var enumerable = field.GetValue(constantsHolder) as IEnumerable;
                    if (enumerable is not null)
                        foreach (object i in enumerable)
                        {
                            var gph = i as IConstantsHolder;
                            if (gph is not null)
                                gph.FindConstants(constants);
                        }
                }
            }
        } 

        public static bool UpdateDsConstants(ObservableCollection<DsConstant> dsConstants, DsConstant[] newDsConstants)
        {
            bool equals;
            if (newDsConstants.Length != dsConstants.Count)
            {
                equals = false;
            }
            else
            {
                equals = true;
                for (var i = 0; i < newDsConstants.Length; i += 1)
                    if (!newDsConstants[i].Equals(dsConstants[i]))
                    {
                        equals = false;
                        break;
                    }
            }

            if (!equals)
            {
                UndoService.Current.BeginChangeSetBatch("Generic Params", true);

                for (var i = dsConstants.Count - 1; i >= 0; i--)
                {
                    var constantValueViewModel = GetConstantValueViewModel(dsConstants[i]);
                    if (constantValueViewModel is not null) constantValueViewModel.DecrementUseCount();

                    dsConstants.RemoveAt(i);
                }

                foreach (DsConstant newDsConstant in newDsConstants)
                {
                    var constantValueViewModel = GetConstantValueViewModel(newDsConstant);
                    if (constantValueViewModel is not null) constantValueViewModel.IncrementUseCount();

                    dsConstants.Add(new DsConstant(newDsConstant));
                }

                UndoService.Current.EndChangeSetBatch();
            }

            return equals;
        }

        public static void ParseVariableNameAndIndex(string v, out string variableName, out int variableIndex)
        {
            v = v.Trim();
            variableName = v;
            variableIndex = 0;
            if (v == @"") return;
            var i = v.IndexOf('[');
            if (i < 1 || i > v.Length - 3) return;
            if (v[v.Length - 1] != ']') return;
            variableName = v.Substring(0, i).TrimEnd();
            variableIndex = ObsoleteAnyHelper.ConvertTo<int>(v.Substring(i + 1, v.Length - 2 - i).Trim(), false);
        }

        #endregion

        #region private functions

#if NET5_0_OR_GREATER
        [return: NotNullIfNotNull("originalString")]
#endif
        private static string? FindConstantsInternal(string? originalString, HashSet<string> constants)
        {
            if (string.IsNullOrWhiteSpace(originalString)) return originalString;

            var firstLevelQueries = SszQueryHelper.FindFirstLevelSpecialText(originalString, '%', false);
            foreach (string firstLevelQuery in firstLevelQueries)
            {
                string q = firstLevelQuery.Substring(2, firstLevelQuery.Length - 3);
                q = FindConstantsInternal(q, constants) ?? "";
                var i = q.IndexOf("=>");
                if (i >= 0)
                {
                    foreach (var p in q.Substring(0, i).Split(','))
                    {
                        string param = p.Trim();
                        if (param != @"") constants.Add(@"%(" + param + @")");
                    }
                }
                else
                {
                    string param = "";
                    i = q.IndexOf(',');
                    if (i >= 0)
                        param = q.Substring(0, i).Trim();
                    else
                        param = q.Trim();
                    if (param != @"" &&
                        !StringHelper.StartsWithIgnoreCase(param, @"U+") &&
                        !StringHelper.EndsWithIgnoreCase(param, @".csv"))
                        constants.Add(@"%(" + param + @")");
                }

                originalString = originalString!.Replace(firstLevelQuery, @"");
            }

            return originalString;
        }

        private static ConstantValueViewModel? GetConstantValueViewModel(DsConstant dsConstant)
        {
            if (string.IsNullOrEmpty(dsConstant.Value)) return null;

            ConstantValueViewModel[] constantValueViewModels =
                DsProject.Instance.GetConstantValuesForDropDownList(dsConstant.Type);

            return constantValueViewModels.FirstOrDefault(
                gpv => gpv.Value == dsConstant.Value);
        }                      

        private static string ComputeValueOfVariables(string originalString,
            CaseInsensitiveDictionary<List<object?>> variables)
        {
            var firstLevelVariables = SszQueryHelper.FindFirstLevelSpecialText(originalString, '$', false);
            foreach (string firstLevelVariable in firstLevelVariables)
            {
                var valueOfVariable = ComputeValueOfVariable(firstLevelVariable, variables);
                if (valueOfVariable is not null)
                    originalString = originalString.Replace(firstLevelVariable, valueOfVariable);
            }

            return originalString;
        }

        private static string? ComputeValueOfVariable(string variable,
            CaseInsensitiveDictionary<List<object?>> variables)
        {
            string v = variable.Substring(2, variable.Length - 3);

            var firstLevelVariables = SszQueryHelper.FindFirstLevelSpecialText(v, '$', false);
            foreach (string firstLevelVariable in firstLevelVariables)
            {
                var valueOfVariable = ComputeValueOfVariable(firstLevelVariable, variables);
                if (valueOfVariable is not null) v = v.Replace(firstLevelVariable, valueOfVariable);
            }

            string variableName;
            int variableIndex;
            ParseVariableNameAndIndex(v, out variableName, out variableIndex);
            if (variableName == @"") return null;

            List<object?>? variableValues;
            if (!variables.TryGetValue(variableName, out variableValues)) return null;
            if (variableIndex < 0 || variableIndex >= variableValues.Count) return null;
            return ObsoleteAnyHelper.ConvertTo<string>(variableValues[variableIndex], false);
        }        

        #endregion
    }
}


//if (ReferenceEquals(dsConstant, currentDsConstant))
//{
//    if (parentContainer is null)
//        return @"";

//    dsConstant = parentContainer.DsConstantsCollection
//        .FirstOrDefault(gpi => StringHelper.CompareIgnoreCase(gpi.Name, constant));
//    if (dsConstant is null)
//    {
//        var hiddenDsConstantsCollection = parentContainer.HiddenDsConstantsCollection;
//        if (hiddenDsConstantsCollection is not null)
//            dsConstant = hiddenDsConstantsCollection.FirstOrDefault(
//                    gpi => StringHelper.CompareIgnoreCase(gpi.Name, constant));
//    }
//}     