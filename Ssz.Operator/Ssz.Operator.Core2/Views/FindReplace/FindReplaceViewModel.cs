using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Ssz.Utils.Avalonia;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.DataAccess;



using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Operator.Core.MultiValueConverters;

namespace Ssz.Operator.Core.FindReplace
{
    public class FindReplaceViewModel : ViewModels.ViewModelBase
    {
        #region construction and destruction

        protected FindReplaceViewModel()
        {
            ReplacementText = "";

            SearchIn = SearchScope.AllDsPageDrawings;
            SearchInProps = SearchScopeProps.ConstantsOnly;
            ShowSearchIn = true;
            ShowSearchInProps = true;
            WholeString = false;
            UseWildcards = false;

            SearchResultGroupsCollection = new ObservableCollection<SearchResultGroupViewModel>();
        }

        #endregion

        private class IsBusyCloser : IDisposable
        {
            #region private fields

            private readonly FindReplaceViewModel _viewModel;

            #endregion

            #region construction and destruction

            public IsBusyCloser(FindReplaceViewModel viewModel)
            {
                _viewModel = viewModel;
                _viewModel.IsBusy = true;
            }

            public void Dispose()
            {
                _viewModel.IsBusy = false;
            }

            #endregion
        }

        #region public functions

        public const string DuplicatesQueryString = @"***Duplicates***";
        public const string IncorrectDsPagesRefsQueryString = @"***Incorrect DsPages Refs***";
        public const string IncorrectOpcTagsQueryString = @"***Incorrect Opc Tags***";
        public const string IncorrectExpressionsQueryString = @"***Incorrect Expressions***";

        public static FindReplaceViewModel Instance { get; } = new();


        public IEnumerable? Editors
        {
            get => _editors;
            set => SetProperty(ref _editors, value);
        }


        public object? CurrentEditor
        {
            get => _currentEditor;
            set => SetProperty(ref _currentEditor, value);
        }


        public IValueConverter? InterfaceConverter
        {
            get => _interfaceConverter;
            set => SetProperty(ref _interfaceConverter, value);
        }

        public string TextToFind
        {
            get => _textToFind;
            set => SetProperty(ref _textToFind, value);
        }

        public string ReplacementText
        {
            get => _replacementText;
            set => SetProperty(ref _replacementText, value);
        }

        public bool UseWildcards
        {
            get => _useWildcards;
            set => SetProperty(ref _useWildcards, value);
        }

        public bool SearchUp
        {
            get => _searchUp;
            set => SetProperty(ref _searchUp, value);
        }

        public bool CaseSensitive
        {
            get => _caseSensitive;
            set => SetProperty(ref _caseSensitive, value);
        }

        public bool UseRegEx
        {
            get => _useRegEx;
            set => SetProperty(ref _useRegEx, value);
        }

        public bool WholeString
        {
            get => _wholeString;
            set => SetProperty(ref _wholeString, value);
        }

        public bool AcceptsReturn
        {
            get => _acceptsReturn;
            set => SetProperty(ref _acceptsReturn, value);
        }

        public SearchScope SearchIn
        {
            get => _searchIn;
            set => SetProperty(ref _searchIn, value);
        }

        public SearchScopeProps SearchInProps
        {
            get => _searchInProps;
            set => SetProperty(ref _searchInProps, value);
        }


        public bool ShowSearchIn
        {
            get => _showSearchIn;
            set => SetProperty(ref _showSearchIn, value);
        }


        public bool ShowSearchInProps
        {
            get => _showSearchInProps;
            set => SetProperty(ref _showSearchInProps, value);
        }


        public bool AllowReplace
        {
            get => _allowReplace;
            set => SetProperty(ref _allowReplace, value);
        }

        /// <summary>
        ///     Whether the options of the search are the author's to change. The WPF dialog carried a
        ///     Visibility here; a flag says the same thing and binds to IsVisible.
        /// </summary>
        public bool OptionsExpanderIsVisible
        {
            get => _optionsExpanderIsVisible;
            set => SetProperty(ref _optionsExpanderIsVisible, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(IsNotBusy));
            }
        }

        public bool IsNotBusy => !_isBusy;

        public ObservableCollection<SearchResultGroupViewModel> SearchResultGroupsCollection { get; }

        public bool ShowDsShapePropertiesButtonIsVisible
        {
            get => _showDsShapePropertiesButtonIsVisible;
            set => SetProperty(ref _showDsShapePropertiesButtonIsVisible, value);
        }

        public bool GoToDsPageButtonIsVisible
        {
            get => _goToDsPageButtonIsVisible;
            set => SetProperty(ref _goToDsPageButtonIsVisible, value);
        }

        public bool FindPathButtonIsVisible
        {
            get => _findPathButtonIsVisible;
            set => SetProperty(ref _findPathButtonIsVisible, value);
        }

        public string ProgressString
        {
            get => _progressString;
            set => SetProperty(ref _progressString, value);
        }

        public double ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }


        public async Task FindAsync(CancellationToken cancellationToken)
        {
            SearchResultGroupsCollection.Clear();

            DrawingInfo[]? drawingInfosForSearch = await GetDrawingInfosForSearchAsync();
            DrawingBase[] openedDrawings =
                DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels.Select(dvm => dvm.Drawing).ToArray();

            if (drawingInfosForSearch is not null && drawingInfosForSearch.Length > 0)
                switch (TextToFind)
                {
                    case DuplicatesQueryString:
                        await FindDuplicates(drawingInfosForSearch, openedDrawings, cancellationToken);
                        break;
                    case IncorrectDsPagesRefsQueryString:
                        await FindIncorrectDsPagesRefs(drawingInfosForSearch, openedDrawings, cancellationToken);
                        break;
                    case IncorrectOpcTagsQueryString:
                        await FindIncorrectOpcTags(drawingInfosForSearch, openedDrawings, cancellationToken);
                        break;
                    case IncorrectExpressionsQueryString:
                        await FindIncorrectExpressions(drawingInfosForSearch, openedDrawings, cancellationToken);
                        break;
                    default:
                        var regex = GetRegex();
                        if (regex is not null)
                            await FindGeneral(regex, SearchInProps, drawingInfosForSearch, openedDrawings,
                                cancellationToken);
                        break;
                }

            if (SearchResultGroupsCollection.Count == 0)
                SearchResultGroupsCollection.Add(new SearchResultGroupViewModel(SearchResultGroupsCollection,
                    new EntityInfo(Resources.NoSearchResults), null));
        }


        public Regex? GetRegex(bool forceLeftToRight = false)
        {
            Regex regex;
            var options = RegexOptions.None;
            if (SearchUp && !forceLeftToRight)
                options = options | RegexOptions.RightToLeft;
            if (!CaseSensitive)
                options = options | RegexOptions.IgnoreCase;

            if (UseRegEx)
            {
                try
                {
                    regex = new Regex(TextToFind, options);
                }
                catch (ArgumentException ex)
                {
                    MessageBoxHelper.ShowError(ex.Message);
                    return null;
                }
            }
            else
            {
                string textToFind = Regex.Escape(TextToFind);
                if (UseWildcards)
                    textToFind = textToFind.Replace("\\*", ".*").Replace("\\?", ".");
                if (WholeString)
                    textToFind = "^" + textToFind + "$";
                regex = new Regex(textToFind, options);
            }

            return regex;
        }

        public async Task ReplaceAllAsync(CancellationToken cancellationToken)
        {
            string replacementText = ReplacementText;
            if (replacementText is null) replacementText = @"";

            if (await MessageBoxHelper.AskYesNoCancelAsync(String.Format(
                    Resources.ReplaceConfirmationQuestion,
                    TextToFind, replacementText != @"" ? replacementText : @"<empty>")) != true) return;

            SearchResultGroupsCollection.Clear();

            DrawingInfo[]? drawingInfosForSearch = await GetDrawingInfosForSearchAsync();

            if (drawingInfosForSearch is not null && drawingInfosForSearch.Length > 0)
            {
                var regex = GetRegex();
                if (regex is not null)
                    await ReplaceAllAsync(regex, SearchInProps, drawingInfosForSearch, replacementText,
                        cancellationToken);
            }

            if (SearchResultGroupsCollection.Count == 0)
                SearchResultGroupsCollection.Add(new SearchResultGroupViewModel(SearchResultGroupsCollection,
                    new EntityInfo(Resources.NoSearchResults), null));
        }

        public IDisposable GetIsBusyCloser()
        {
            return new IsBusyCloser(this);
        }

        public async Task ExportResultsToCsvAsync()
        {
            if (SearchResultGroupsCollection.Count == 0 ||
                SearchResultGroupsCollection.Count == 1 && SearchResultGroupsCollection[0].SearchResults is null)
            {
                MessageBoxHelper.ShowInfo(Resources.NoSearchResultsToExport);
                return;
            }

            IStorageFile? file = await FileDialogHelper.SaveFileAsync(
                Resources.ExportResultsButton, null,
                FileDialogHelper.CsvFileType, FileDialogHelper.AllFilesFileType);
            if (file is null) return;

            var path = file.TryGetLocalPath();
            if (String.IsNullOrEmpty(path)) return;

            var data = GetData(SearchResultGroupsCollection);

            using (Stream stream = File.Create(path))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                foreach (var d in data) writer.WriteLine(CsvHelper.FormatForCsv(",", d));
            }

            MessageBoxHelper.ShowInfo(Resources.Done);
        }

        public async Task CopyResultsAsync()
        {
            if (SearchResultGroupsCollection.Count == 0 ||
                SearchResultGroupsCollection.Count == 1 && SearchResultGroupsCollection[0].SearchResults is null)
            {
                MessageBoxHelper.ShowInfo(Resources.NoSearchResultsToExport);
                return;
            }

            var data = GetData(SearchResultGroupsCollection);

            await ClipboardHelper.SetTextAsync(String.Join(Environment.NewLine,
                data.Select(d => CsvHelper.FormatForCsv(@"\t", d))));
        }

        #endregion

        #region protected functions

        #endregion

        #region private functions

        private static List<string[]> GetData(IEnumerable<SearchResultGroupViewModel> searchResultGroups)
        {
            var columnsWithData = new List<bool>();

            var data = new List<string[]>();
            foreach (SearchResultGroupViewModel searchResultGroup in searchResultGroups)
            foreach (SearchResultViewModel sr in searchResultGroup.SearchResults)
            {
                var d = new[]
                {
                    string.Concat(sr.PropertyValues),
                    sr.DrawingInfo.Name,
                    sr.RootDsShapeInfo is not null ? sr.RootDsShapeInfo.DsShapeTypeNameToDisplay : "",
                    sr.RootDsShapeInfo is not null ? sr.RootDsShapeInfo.Name : "",
                    sr.DsShapeInfo is not null && !sr.DsShapeInfo.IsRootDsShape
                        ? sr.DsShapeInfo.DsShapeTypeNameToDisplay
                        : "",
                    sr.DsShapeInfo is not null && !sr.DsShapeInfo.IsRootDsShape ? sr.DsShapeInfo.Name : "",
                    sr.PropertyPath
                }.Concat(sr.PropertyValues).ToArray();

                for (var i = 0; i < d.Length; i += 1)
                {
                    while (true)
                    {
                        if (i < columnsWithData.Count) break;
                        columnsWithData.Add(false);
                    }

                    if (!string.IsNullOrEmpty(d[i])) columnsWithData[i] = true;
                }

                data.Add(d);
            }

            var result = new List<string[]>();
            foreach (var d in data)
            {
                var resultRow = new List<string>();
                for (var i = 0; i < d.Length; i += 1)
                    if (columnsWithData[i])
                        resultRow.Add(d[i]);
                result.Add(resultRow.ToArray());
            }

            return result;
        }


        private static IEnumerable<Tuple<DsShapeInfo?, StringPropertyInfo>> FindIncorrectDsPagesRefs(object obj,
            Regex regex)
        {
            var result = new List<Tuple<DsShapeInfo?, StringPropertyInfo>>();

            var dsShape = obj as DsShapeBase;
            DsShapeInfo? dsShapeInfo;
            if (dsShape is not null) dsShapeInfo = dsShape.GetDsShapeInfo();
            else dsShapeInfo = null;

            foreach (
                StringPropertyInfo stringPropertyInfo in
                ObjectHelper.FindInStringBrowsableProperties(obj, regex))
                result.Add(Tuple.Create(dsShapeInfo, stringPropertyInfo));

            var c = obj as IDsContainer;
            if (c is not null)
                foreach (DsShapeBase sh in c.DsShapes)
                    result.AddRange(FindIncorrectDsPagesRefs(sh, regex));

            return result;
        }

        private static IEnumerable<Tuple<DsShapeInfo?, StringPropertyInfo>> FindIncorrectExpressions(object obj)
        {
            var result = new List<Tuple<DsShapeInfo?, StringPropertyInfo>>();

            var dsShape = obj as DsShapeBase;
            DsShapeInfo? dsShapeInfo;
            if (dsShape is not null) dsShapeInfo = dsShape.GetDsShapeInfo();
            else dsShapeInfo = null;

            foreach (var fi in ObjectHelper.GetAllFields(obj).Where(fi => typeof(IValueDataBinding).IsAssignableFrom(fi.FieldType)))
            {
                var valueDataBinding = fi.GetValue(obj) as IValueDataBinding;
                if (valueDataBinding is null || valueDataBinding.Converter is null) continue;

                var localizedConverter = valueDataBinding.Converter as LocalizedConverter;
                if (localizedConverter is not null)
                {
                    foreach (TextStatement statement in localizedConverter.DataSourceToUiStatements)
                    {
                        CheckExpression(statement.Condition, dsShapeInfo, fi, result);
                        CheckExpression(statement.Value, dsShapeInfo, fi, result);
                    }

                    foreach (TextStatement statement in localizedConverter.UiToDataSourceStatements)
                    {
                        CheckExpression(statement.Condition, dsShapeInfo, fi, result);
                        CheckExpression(statement.Value, dsShapeInfo, fi, result);
                    }

                    continue;
                }

                var dsBrushConverter = valueDataBinding.Converter as DsBrushConverter;
                if (dsBrushConverter is not null)
                {
                    foreach (DsBrushStatement statement in dsBrushConverter.DataSourceToUiStatements)
                        CheckExpression(statement.Condition, dsShapeInfo, fi, result);
                    continue;
                }

                var xamlConverter = valueDataBinding.Converter as XamlConverter;
                if (xamlConverter is not null)
                    foreach (XamlStatement statement in xamlConverter.DataSourceToUiStatements)
                        CheckExpression(statement.Condition, dsShapeInfo, fi, result);
            }

            var c = obj as IDsContainer;
            if (c is not null)
                foreach (DsShapeBase sh in c.DsShapes)
                    result.AddRange(FindIncorrectExpressions(sh));

            return result;
        }

        private static void CheckExpression(MultiValueConverters.Expression expression, DsShapeInfo? dsShapeInfo, FieldInfo fi,
            List<Tuple<DsShapeInfo?, StringPropertyInfo>> result)
        {
            if (!expression.IsValid)
                result.Add(Tuple.Create(dsShapeInfo,
                    new StringPropertyInfo {PropertyPath = fi.Name, PropertyValue = expression.ExpressionString}));
        }

        private async Task<DrawingInfo[]?> GetDrawingInfosForSearchAsync()
        {
            DrawingInfo[]? drawingInfos = null;

            switch (SearchIn)
            {
                case SearchScope.CurrentDrawing:
                    var drawingInfoViewModel =
                        DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
                    if (drawingInfoViewModel is not null)
                        drawingInfos = new[] {drawingInfoViewModel.Drawing.GetDrawingInfo()};
                    break;
                case SearchScope.AllOpenedDrawings:
                    drawingInfos = DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels.Select(
                        vm => vm.Drawing.GetDrawingInfo()).ToArray();
                    break;
                case SearchScope.AllDsPageDrawings:
                    drawingInfos =
                        DsProject.Instance.AllDsPagesCache.Values.Select(d => d.GetDrawingInfo())
                            .OfType<DsPageDrawingInfo>()
                            .OrderBy(di => di.DsPageTypeObject is not null ? di.DsPageTypeObject.IsFaceplate : false)
                            .ThenBy(di => di.Name).ToArray();
                    break;
                case SearchScope.AllDsShapeDrawings:
                    drawingInfos =
                        (await DsProject.Instance.GetAllComplexDsShapesDrawingInfosAsync()).Values.OrderBy(di => di.Name)
                            .ToArray();
                    break;
            }

            return drawingInfos;
        }

        private async Task FindDuplicates(DrawingInfo[] drawingInfosForSearch, DrawingBase[] openedDrawings,
            CancellationToken cancellationToken)
        {
            var drawingsCount = drawingInfosForSearch.Length;
            var i = 0;

            // Constant Type, Constant Value, List<ExtendedDsConstant>
            var valuesInfo = new CaseInsensitiveOrderedDictionary<Dictionary<string, List<ExtendedDsConstant>>>();

            foreach (DrawingInfo drawingInfoForSearch in drawingInfosForSearch)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await SetProgressLabels(string.Format("{0}/{1}", i, drawingsCount), 100.0 * i / drawingsCount);
                i += 1;

                var drawing = openedDrawings.FirstOrDefault(
                    d => FileSystemHelper.Compare(d.FileFullName, drawingInfoForSearch.FileFullName));
                if (drawing is null)
                    drawing = DsProject.Instance.AllDsPagesCache.TryGetValue(drawingInfoForSearch.Name);
                if (drawing is null) continue;

                foreach (ComplexDsShape rootComplexDsShape in drawing.DsShapes.OfType<ComplexDsShape>())
                foreach (DsConstant dsConstant in rootComplexDsShape.DsConstantsCollection)
                {
                    if (string.IsNullOrWhiteSpace(dsConstant.Type) ||
                        StringHelper.CompareIgnoreCase(dsConstant.Type, @"color") ||
                        StringHelper.CompareIgnoreCase(dsConstant.Type, @"dsPage") ||
                        StringHelper.CompareIgnoreCase(dsConstant.Type, @"dsShape") ||
                        dsConstant.Type.IndexOfAny(new[] {'\\', '/', '|'}) >= 0 ||
                        dsConstant.Value == @"" ||
                        ConstantsHelper.ContainsQuery(dsConstant.Value)) continue;

                    string dsShapeTypeAndConstantName = rootComplexDsShape.DsShapeDrawingName + @"," +
                                                          dsConstant.Name;
                    Dictionary<string, List<ExtendedDsConstant>>? constantValuesDictionary;
                    if (!valuesInfo.TryGetValue(dsShapeTypeAndConstantName, out constantValuesDictionary))
                    {
                        constantValuesDictionary =
                            new Dictionary<string, List<ExtendedDsConstant>>();
                        valuesInfo[dsShapeTypeAndConstantName] = constantValuesDictionary;
                    }

                    List<ExtendedDsConstant>? dsConstantsList;
                    if (
                        !constantValuesDictionary.TryGetValue(dsConstant.Value,
                            out dsConstantsList))
                    {
                        dsConstantsList = new List<ExtendedDsConstant>();
                        constantValuesDictionary[dsConstant.Value] = dsConstantsList;
                    }

                    dsConstantsList.Add(new ExtendedDsConstant(dsConstant, rootComplexDsShape));
                }
            }

            foreach (var typeAndConstants in valuesInfo)
            foreach (var valueAndConstants in typeAndConstants.Value)
            {
                if (valueAndConstants.Value.Count < 2) continue;

                var searchResultViewModelsCollection = new List<SearchResultViewModel>();

                foreach (ExtendedDsConstant gpi in
                    valueAndConstants.Value)
                {
                    var si = gpi.ComplexDsShape.GetDsShapeInfo();
                    searchResultViewModelsCollection.Add(
                        new SearchResultViewModel(
                            (gpi.ComplexDsShape.GetParentDrawing() ?? throw new InvalidOperationException())
                            .GetDrawingInfo(),
                            si, si, @" " + gpi.DsConstant.Name,
                            new[] {gpi.DsConstant.Value}));
                }

                SearchResultGroupsCollection.Add(new SearchResultGroupViewModel(SearchResultGroupsCollection,
                    new EntityInfo(
                        "Control Type: " +
                        valueAndConstants.Value[0].ComplexDsShape.DsShapeDrawingName + "; Value: " +
                        valueAndConstants.Key),
                    searchResultViewModelsCollection.OrderBy(g => g.DrawingInfo.Name)
                        .ThenBy(g => g.DsShapeTypeNameToDisplay)
                        .ThenBy(g => g.DsShapeName)));
            }
        }

        private async Task SetProgressLabels(string progressString, double progressPercent)
        {
            ProgressString = progressString;
            ProgressPercent = progressPercent;

            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        private async Task FindIncorrectDsPagesRefs(DrawingInfo[] drawingInfosForSearch, DrawingBase[] openedDrawings,
            CancellationToken cancellationToken)
        {
            var options = RegexOptions.None;
            options = options | RegexOptions.IgnoreCase;
            var regex = new Regex(@"^.*" + DsProject.DsPageFileExtension + "$", options);

            var drawingsCount = drawingInfosForSearch.Length;
            var i = 0;

            foreach (DrawingInfo drawingInfoForSearch in drawingInfosForSearch)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await SetProgressLabels(string.Format("{0}/{1}", i, drawingsCount), 100.0 * i / drawingsCount);
                i += 1;

                var drawing = openedDrawings.FirstOrDefault(
                    d => FileSystemHelper.Compare(d.FileFullName, drawingInfoForSearch.FileFullName));
                if (drawing is not null)
                    drawing = (DrawingBase) drawing.Clone();
                else
                    drawing = await DsProject.ReadDrawingAsync(drawingInfoForSearch.FileFullName, false, false);
                if (drawing is null) continue;

                drawing.ReplaceConstants(drawing);

                var searchResultViewModelsCollection = new List<SearchResultViewModel>();

                foreach (DsShapeBase rootDsShape in drawing.DsShapes)
                {
                    DsShapeInfo rootDsShapeInfo = rootDsShape.GetDsShapeInfo();
                    foreach (var tuple in FindIncorrectDsPagesRefs(rootDsShape, regex))
                    {
                        var dsPageFileInfo = DsProject.Instance.GetExistingDsPageFileFullNameOrNull(tuple.Item2.PropertyValue);
                        if (dsPageFileInfo is null)
                            searchResultViewModelsCollection.Add(
                                new SearchResultViewModel(drawing.GetDrawingInfo(),
                                    rootDsShapeInfo, tuple.Item1, tuple.Item2.PropertyPath,
                                    new[] {tuple.Item2.PropertyValue}));
                    }
                }

                if (searchResultViewModelsCollection.Count > 0)
                    SearchResultGroupsCollection.Add(new SearchResultGroupViewModel(SearchResultGroupsCollection,
                        drawingInfoForSearch,
                        searchResultViewModelsCollection.Distinct(
                                SearchResultViewModelEqualityComparer.Instance)
                            .OrderBy(g => g.DrawingInfo.Name)
                            .ThenBy(g => g.DsShapeTypeNameToDisplay)
                            .ThenBy(g => g.DsShapeName)));
            }
        }

        private async Task FindIncorrectOpcTags(DrawingInfo[] drawingInfosForSearch, DrawingBase[] openedDrawings,
            CancellationToken cancellationToken)
        {            
            var dispatcherWrapper = new WrapperDispatcher(Dispatcher.UIThread);
            await DsDataAccessProvider.StaticInitialize(
                DsProject.Instance.Mode,
                DsProject.Instance.ElementIdsMap,                
                DsProject.Instance.DefaultServerAddress, 
                @"Ssz.Operator", 
                DsProject.Instance.DefaultSystemNameToConnect, 
                new CaseInsensitiveOrderedDictionary<string?>(),
                dispatcherWrapper);            

            var drawingsCount = drawingInfosForSearch.Length;
            var i = 0;

            foreach (DrawingInfo drawingInfoForSearch in drawingInfosForSearch)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await SetProgressLabels(string.Format("{0}/{1}", i, drawingsCount), 100.0 * i / drawingsCount);
                i += 1;

                var drawing = openedDrawings.FirstOrDefault(
                    d => FileSystemHelper.Compare(d.FileFullName, drawingInfoForSearch.FileFullName));
                if (drawing is null) drawing = await DsProject.ReadDrawingAsync(drawingInfoForSearch.FileFullName, false, false);
                if (drawing is null) continue;

                var searchResultViewModelsCollection = new List<SearchResultViewModel>();

                foreach (DsShapeBase rootDsShape in drawing.DsShapes)
                {
                    DsShapeInfo rootDsShapeInfo = rootDsShape.GetDsShapeInfo();
                    foreach (var tuple in GetOpcTags(rootDsShape, rootDsShape.Container))
                    {
                        var searchResultViewModel = new SearchResultViewModel(drawing.GetDrawingInfo(),
                            rootDsShapeInfo, tuple.Item1, @" Incorrect OPC", new[] {tuple.Item2});

                        searchResultViewModelsCollection.Add(searchResultViewModel);
                    }
                }

                if (searchResultViewModelsCollection.Count > 0)
                {
                    SearchResultGroupsCollection.Add(new SearchResultGroupViewModel(SearchResultGroupsCollection,
                        drawingInfoForSearch,
                        searchResultViewModelsCollection.Distinct(
                                SearchResultViewModelEqualityComparer.Instance)
                            .OrderBy(g => g.DrawingInfo.Name)
                            .ThenBy(g => g.DsShapeTypeNameToDisplay)
                            .ThenBy(g => g.DsShapeName)));

                    foreach (SearchResultViewModel searchResultViewModel in searchResultViewModelsCollection)
                    {
                        SearchResultViewModel srvm = searchResultViewModel;
                        string id = searchResultViewModel.PropertyValues[0];
                        var subscription = new ReadOnceValueSubscription(DsDataAccessProvider.Instance,
                            id,
                            vst =>
                            {
                                if (StatusCodes.IsGood(vst.StatusCode) && 
                                        vst.Value.ValueTypeCode != Any.TypeCode.Empty &&
                                        vst.Value.ValueTypeCode != Any.TypeCode.DBNull &&
                                        srvm.ParentGroup is not null) 
                                    srvm.ParentGroup.Remove(srvm);
                            });
                        string modelId = subscription.MappedElementIdOrConst;

                        string tagAndPropSeparator = DsProject.Instance.DataEngine.TagAndPropSeparator;

                        var values = new string[3];
                        var ind = id.LastIndexOf(tagAndPropSeparator);
                        if (ind > 0 && ind < id.Length - 1)
                        {
                            values[0] = id.Substring(0, ind);
                            values[1] = id.Substring(ind);
                        }
                        else
                        {
                            values[0] = id;
                            values[1] = @"";
                        }

                        if (modelId != id) values[2] = modelId;

                        searchResultViewModel.PropertyValues = values;
                    }
                }
            }
        }

        private async Task FindIncorrectExpressions(DrawingInfo[] drawingInfosForSearch, DrawingBase[] openedDrawings,
            CancellationToken cancellationToken)
        {
            var drawingsCount = drawingInfosForSearch.Length;
            var i = 0;

            foreach (DrawingInfo drawingInfoForSearch in drawingInfosForSearch)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await SetProgressLabels(string.Format("{0}/{1}", i, drawingsCount), 100.0 * i / drawingsCount);
                i += 1;

                var drawing = openedDrawings.FirstOrDefault(
                    d => FileSystemHelper.Compare(d.FileFullName, drawingInfoForSearch.FileFullName));
                if (drawing is null) drawing = await DsProject.ReadDrawingAsync(drawingInfoForSearch.FileFullName, false, false);
                if (drawing is not null)
                {
                    var searchResultViewModelsCollection = new List<SearchResultViewModel>();

                    foreach (DsShapeBase rootDsShape in drawing.DsShapes)
                    {
                        DsShapeInfo rootDsShapeInfo = rootDsShape.GetDsShapeInfo();
                        foreach (var tuple in FindIncorrectExpressions(rootDsShape))
                        {
                            var dsPageFileInfo =
                                DsProject.Instance.GetExistingDsPageFileFullNameOrNull(tuple.Item2.PropertyValue);
                            if (dsPageFileInfo is null)
                                searchResultViewModelsCollection.Add(
                                    new SearchResultViewModel(drawing.GetDrawingInfo(),
                                        rootDsShapeInfo, tuple.Item1, tuple.Item2.PropertyPath,
                                        new[] {tuple.Item2.PropertyValue}));
                        }
                    }

                    if (searchResultViewModelsCollection.Count > 0)
                        SearchResultGroupsCollection.Add(new SearchResultGroupViewModel(SearchResultGroupsCollection,
                            drawingInfoForSearch,
                            searchResultViewModelsCollection.Distinct(
                                    SearchResultViewModelEqualityComparer.Instance)
                                .OrderBy(g => g.DrawingInfo.Name)
                                .ThenBy(g => g.DsShapeTypeNameToDisplay)
                                .ThenBy(g => g.DsShapeName)));
                }
            }
        }


        private async Task ReplaceAllAsync(Regex regex, SearchScopeProps searchInProps,
            DrawingInfo[] drawingInfosForSearch,
            string replacementText, CancellationToken cancellationToken)
        {
            var drawingsCount = drawingInfosForSearch.Length;
            var i = 0;

            try
            {
                DesignDsProjectViewModel.Instance.ForceDisableXamlToDsShapesConversion = true;

                foreach (DrawingInfo drawingInfoForSearch in drawingInfosForSearch)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    ProgressString = string.Format("{0}/{1}", i, drawingsCount);
                    ProgressPercent = 100.0 * i / drawingsCount;
                    i += 1;

                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

                    DesignDrawingViewModel? newlyOpenedDrawingViewModel = null;
                    var drawingViewModel =
                        DesignDsProjectViewModel.Instance.FindOpenedDrawingViewModel(new FileInfo(drawingInfoForSearch.FileFullName));
                    if (drawingViewModel is null)
                    {
                        newlyOpenedDrawingViewModel =
                            await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(new FileInfo(drawingInfoForSearch.FileFullName));
                        drawingViewModel = newlyOpenedDrawingViewModel;
                    }

                    if (drawingViewModel is null) continue;

                    List<SearchResultViewModel> searchResultViewModelsCollection = ReplaceAllInDrawing(
                        drawingViewModel.Drawing,
                        searchInProps, regex, replacementText);

                    if (searchResultViewModelsCollection.Count > 0)
                    {
                        drawingViewModel.Drawing.SetDataChanged();
                        drawingViewModel.Drawing.RefreshForPropertyGrid();

                        SearchResultGroupsCollection.Add(new SearchResultGroupViewModel(SearchResultGroupsCollection,
                            drawingInfoForSearch,
                            searchResultViewModelsCollection.Distinct(
                                    SearchResultViewModelEqualityComparer.Instance)
                                .OrderBy(g => g.DrawingInfo.Name)
                                .ThenBy(g => g.DsShapeTypeNameToDisplay)
                                .ThenBy(g => g.DsShapeName)));
                    }

                    if (newlyOpenedDrawingViewModel is not null)
                    {
                        await DsProject.Instance.SaveUnconditionallyAsync(newlyOpenedDrawingViewModel.Drawing,
                            DsProject.IfFileExistsActions.CreateBackup, false);

                        await DesignDsProjectViewModel.Instance.CloseDrawingAsync(newlyOpenedDrawingViewModel);
                    }
                }
            }
            finally
            {
                DesignDsProjectViewModel.Instance.ForceDisableXamlToDsShapesConversion = false;
            }
        }

        private async Task FindGeneral(Regex regex, SearchScopeProps searchInProps, DrawingInfo[] drawingInfosForSearch,
            DrawingBase[] openedDrawings, CancellationToken cancellationToken)
        {
            var drawingsCount = drawingInfosForSearch.Length;
            var i = 0;

            foreach (DrawingInfo drawingInfoForSearch in drawingInfosForSearch)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await SetProgressLabels(string.Format("{0}/{1}", i, drawingsCount), 100.0 * i / drawingsCount);
                i += 1;

                var drawing = openedDrawings.FirstOrDefault(
                    d => FileSystemHelper.Compare(d.FileFullName, drawingInfoForSearch.FileFullName));
                if (drawing is null && searchInProps == SearchScopeProps.ConstantsOnly)
                    drawing = DsProject.Instance.AllDsPagesCache.TryGetValue(drawingInfoForSearch.Name);
                if (drawing is null)
                    drawing = await DsProject.ReadDrawingAsync(drawingInfoForSearch.FileFullName, false, true);

                if (drawing is not null)
                {
                    List<SearchResultViewModel> searchResultViewModelsCollection = FindInDrawing(drawing, searchInProps,
                        regex);

                    if (searchResultViewModelsCollection.Count > 0)
                        SearchResultGroupsCollection.Add(new SearchResultGroupViewModel(SearchResultGroupsCollection,
                            drawingInfoForSearch,
                            searchResultViewModelsCollection.Distinct(
                                    SearchResultViewModelEqualityComparer.Instance)
                                .OrderBy(g => g.DrawingInfo.Name)
                                .ThenBy(g => g.DsShapeTypeNameToDisplay)
                                .ThenBy(g => g.DsShapeName)));
                }
            }
        }


        private List<SearchResultViewModel> FindInDrawing(DrawingBase drawing, SearchScopeProps searchInProps,
            Regex regex)
        {
            var result = new List<SearchResultViewModel>();

            switch (searchInProps)
            {
                case SearchScopeProps.AllProperties:
                {
                    foreach (StringPropertyInfo stringPropertyInfo in ObjectHelper.FindInStringBrowsableProperties(drawing, regex))
                    {
                        result.Add(new SearchResultViewModel(drawing.GetDrawingInfo(),
                                null, null, stringPropertyInfo.PropertyPath, new[] { stringPropertyInfo.PropertyValue }));
                    }

                    foreach (DsShapeBase rootDsShape in drawing.DsShapes)
                    {
                        var rootDsShapeInfo = rootDsShape.GetDsShapeInfo();
                        foreach (var tuple in FindInStringBrowsablePropertiesWithSubDsShapes(rootDsShape, regex))
                        {
                            result.Add(new SearchResultViewModel(drawing.GetDrawingInfo(),
                                    rootDsShapeInfo, tuple.Item1, tuple.Item2.PropertyPath,
                                    new[] { tuple.Item2.PropertyValue }));
                        }                            
                    }
                }
                    break;
                case SearchScopeProps.ConstantsOnly:
                {
                    foreach (var propertyValues in FindInConstantsOnly(drawing, regex))
                    {
                        result.Add(new SearchResultViewModel(drawing.GetDrawingInfo(),
                                null, null, "", propertyValues));
                    }

                    foreach (DsShapeBase rootDsShape in drawing.DsShapes)
                    {
                        var rootContainer = rootDsShape as IDsContainer;
                        if (rootContainer is not null)
                        {
                            var rootDsShapeInfo = rootDsShape.GetDsShapeInfo();
                            foreach (var tuple in FindInConstantsOnlyWithSubDsShapes(rootContainer, regex))
                            {
                                result.Add(new SearchResultViewModel(drawing.GetDrawingInfo(),
                                        rootDsShapeInfo, tuple.Item1, "",
                                        tuple.Item2));
                            }                                
                        }
                    }

                    break;
                }
            }

            return result;
        }

        private List<SearchResultViewModel> ReplaceAllInDrawing(DrawingBase drawing, SearchScopeProps searchInProps,
            Regex regex, string replacementText)
        {
            var result = new List<SearchResultViewModel>();

            switch (searchInProps)
            {
                case SearchScopeProps.AllProperties:
                {
                    foreach (
                        StringPropertyInfo stringPropertyInfo in
                        ObjectHelper.ReplaceInStringBrowsableProperties(drawing, regex, replacementText))
                        result.Add(new SearchResultViewModel(drawing.GetDrawingInfo(),
                            null, null, stringPropertyInfo.PropertyPath, new[] {stringPropertyInfo.PropertyValue}));

                    foreach (DsShapeBase rootDsShape in drawing.DsShapes)
                    foreach (
                        StringPropertyInfo stringPropertyInfo in
                        ReplaceInStringBrowsablePropertiesWithSubDsShapes(rootDsShape, regex, replacementText))
                        result.Add(new SearchResultViewModel(drawing.GetDrawingInfo(),
                            rootDsShape.GetDsShapeInfo(), null, stringPropertyInfo.PropertyPath,
                            new[] {stringPropertyInfo.PropertyValue}));
                }
                    break;
                case SearchScopeProps.ConstantsOnly:
                {
                    foreach (
                        var propertyValues in
                        ReplaceAllInConstantsOnly(drawing, regex, replacementText))
                        result.Add(new SearchResultViewModel(drawing.GetDrawingInfo(),
                            null, null, "", propertyValues));

                    foreach (DsShapeBase rootDsShape in drawing.DsShapes)
                    {
                        var rootContainer = rootDsShape as IDsContainer;
                        if (rootContainer is not null)
                            foreach (
                                var propertyValues in
                                ReplaceAllInConstantsOnlyWithSubDsShapes(rootContainer, regex, replacementText))
                                result.Add(new SearchResultViewModel(drawing.GetDrawingInfo(),
                                    rootDsShape.GetDsShapeInfo(), null, "",
                                    propertyValues));
                    }

                    break;
                }
            }

            return result;
        }

        private static IEnumerable<Tuple<DsShapeInfo?, StringPropertyInfo>>
            FindInStringBrowsablePropertiesWithSubDsShapes(object obj, Regex regex)
        {
            var dsShape = obj as DsShapeBase;
            DsShapeInfo? dsShapeInfo;
            if (dsShape is not null) dsShapeInfo = dsShape.GetDsShapeInfo();
            else dsShapeInfo = null;

            var result = new List<Tuple<DsShapeInfo?, StringPropertyInfo>>();
            foreach (var spi in ObjectHelper.FindInStringBrowsableProperties(obj, regex))
                result.Add(Tuple.Create(dsShapeInfo, spi));

            var container = obj as IDsContainer;
            if (container is not null)
                foreach (DsShapeBase sh in container.DsShapes)
                    result.AddRange(FindInStringBrowsablePropertiesWithSubDsShapes(sh, regex));

            return result;
        }

        private static IEnumerable<Tuple<DsShapeInfo?, string[]>> FindInConstantsOnlyWithSubDsShapes(
            IDsContainer container,
            Regex regex)
        {
            var dsShape = container as DsShapeBase;
            DsShapeInfo? dsShapeInfo;
            if (dsShape is not null) dsShapeInfo = dsShape.GetDsShapeInfo();
            else dsShapeInfo = null;

            var result = new List<Tuple<DsShapeInfo?, string[]>>();
            foreach (var s in FindInConstantsOnly(container, regex)) result.Add(Tuple.Create(dsShapeInfo, s));

            foreach (DsShapeBase sh in container.DsShapes)
            {
                var c = sh as IDsContainer;
                if (c is not null) result.AddRange(FindInConstantsOnlyWithSubDsShapes(c, regex));
            }

            return result;
        }

        private static IEnumerable<string[]> FindInConstantsOnly(IDsContainer container,
            Regex regex)
        {
            var result = new List<string[]>();

            foreach (DsConstant dsConstant in container.DsConstantsCollection)
            {
                if (dsConstant.Value == @"") continue;

                var strings = new[]
                {
                    dsConstant.Name,
                    dsConstant.Value,
                    dsConstant.Type,
                    dsConstant.Desc
                };

                var matched = false;
                foreach (string s in strings)
                {
                    if (s is null) continue;
                    Match match = regex.Match(s);
                    if (match.Success)
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched) result.Add(strings);
            }

            return result;
        }

        public static List<StringPropertyInfo> ReplaceInStringBrowsablePropertiesWithSubDsShapes(object obj,
            Regex regex,
            string replacement)
        {
            var result = new List<StringPropertyInfo>(ObjectHelper.ReplaceInStringBrowsableProperties(obj, regex, replacement));

            var container = obj as IDsContainer;
            if (container is not null)
                foreach (DsShapeBase dsShape in container.DsShapes)
                    result.AddRange(ReplaceInStringBrowsablePropertiesWithSubDsShapes(dsShape, regex, replacement));

            return result;
        }

        private static IEnumerable<string[]> ReplaceAllInConstantsOnlyWithSubDsShapes(
            IDsContainer container, Regex regex, string replacementText)
        {
            var result = new List<string[]>(ReplaceAllInConstantsOnly(container, regex, replacementText));

            foreach (DsShapeBase dsShape in container.DsShapes)
            {
                var c = dsShape as IDsContainer;
                if (c is not null) result.AddRange(ReplaceAllInConstantsOnlyWithSubDsShapes(c, regex, replacementText));
            }

            return result;
        }

        private static IEnumerable<string[]> ReplaceAllInConstantsOnly(
            IDsContainer container, Regex regex, string replacementText)
        {
            var result = new List<string[]>();

            foreach (DsConstant dsConstant in container.DsConstantsCollection)
            {
                var matched = false;
                string newStringValue;
                if (!string.IsNullOrEmpty(dsConstant.Name))
                {
                    newStringValue = regex.Replace(dsConstant.Name, replacementText);
                    if (newStringValue != dsConstant.Name)
                    {
                        dsConstant.Name = newStringValue;
                        matched = true;
                    }
                }

                if (!string.IsNullOrEmpty(dsConstant.Value))
                {
                    newStringValue = regex.Replace(dsConstant.Value, replacementText);
                    if (newStringValue != dsConstant.Value)
                    {
                        dsConstant.Value = newStringValue;
                        matched = true;
                    }
                }

                if (!string.IsNullOrEmpty(dsConstant.Type))
                {
                    newStringValue = regex.Replace(dsConstant.Type, replacementText);
                    if (newStringValue != dsConstant.Type)
                    {
                        dsConstant.Type = newStringValue;
                        matched = true;
                    }
                }

                if (!string.IsNullOrEmpty(dsConstant.Desc))
                {
                    newStringValue = regex.Replace(dsConstant.Desc, replacementText);
                    if (newStringValue != dsConstant.Desc)
                    {
                        dsConstant.Desc = newStringValue;
                        matched = true;
                    }
                }

                if (matched)
                {
                    var strings = new[]
                    {
                        dsConstant.Name,
                        dsConstant.Value,
                        dsConstant.Type,
                        dsConstant.Desc
                    };

                    result.Add(strings);
                }
            }

            return result;
        }

        private IEnumerable<Tuple<DsShapeInfo?, string>> GetOpcTags(object? obj, IDsContainer? container)
        {
            if (obj is null) return new Tuple<DsShapeInfo?, string>[0];

            var dsShape = obj as DsShapeBase;
            DsShapeInfo? dsShapeInfo;
            if (dsShape is not null) dsShapeInfo = dsShape.GetDsShapeInfo();
            else dsShapeInfo = null;

            var result = new List<Tuple<DsShapeInfo?, string>>();

            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(obj)
                .OfType<PropertyDescriptor>()
                .Where(item => item.IsBrowsable))
            {
                object? propertyValue = propertyDescriptor.GetValue(obj);

                if (propertyValue is null) continue;

                if (typeof(IValueDataBinding).IsAssignableFrom(propertyDescriptor.PropertyType))
                {
                    var valueDataBinding = propertyValue as IValueDataBinding;
                    if (valueDataBinding is not null)
                        foreach (DataBindingItem i in valueDataBinding.DataBindingItemsCollection)
                            if (i.Type == DataSourceType.OpcVariable)
                            {
                                var opcTag = ConstantsHelper.ComputeValue(container, i.IdString);
                                if (!string.IsNullOrWhiteSpace(opcTag)) result.Add(Tuple.Create(dsShapeInfo, opcTag));
                            }

                    continue;
                }

                var enumerable = propertyValue as IEnumerable;
                if (enumerable is not null)
                {
                    foreach (object item in enumerable) result.AddRange(GetOpcTags(item, container));
                    continue;
                }

                result.AddRange(GetOpcTags(propertyValue, container));
            }

            var c = obj as IDsContainer;
            if (c is not null)
                foreach (DsShapeBase s in c.DsShapes)
                    result.AddRange(GetOpcTags(s, s.Container));

            return result;
        }

        #endregion

        #region private fields

        private IEnumerable? _editors = null;
        private object? _currentEditor = null;
        private IValueConverter? _interfaceConverter = null;
        private string _textToFind = @"";
        private string _replacementText = @"";
        private bool _useWildcards = false;
        private bool _searchUp = false;
        private bool _caseSensitive = false;
        private bool _useRegEx = false;
        private bool _wholeString = false;
        private bool _acceptsReturn = false;
        private SearchScope _searchIn = SearchScope.CurrentDrawing;
        private SearchScopeProps _searchInProps = SearchScopeProps.AllProperties;
        private bool _showSearchIn = true;
        private bool _showSearchInProps = true;
        private bool _allowReplace = true;
        private bool _optionsExpanderIsVisible = true;
        private bool _showDsShapePropertiesButtonIsVisible;
        private bool _goToDsPageButtonIsVisible;
        private bool _findPathButtonIsVisible;


        private bool _isBusy;
        private string _progressString = @"";
        private double _progressPercent;

        #endregion
    }

    public interface IEditorControl
    {
        string Text { get; }
        int SelectionStart { get; }
        int SelectionLength { get; }


        void Select(int start, int length);

        void Replace(int start, int length, string replaceWith);


        void BeginChange();


        void EndChange();
    }
}