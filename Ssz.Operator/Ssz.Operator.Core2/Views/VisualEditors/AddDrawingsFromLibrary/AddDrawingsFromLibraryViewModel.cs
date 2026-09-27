using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.ViewModels;
using Ssz.Utils;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.VisualEditors.AddDrawingsFromLibrary
{
    /// <summary>
    ///     What a library of pages and shapes holds, as a tree: folder, group, drawing.
    /// </summary>
    public class AddDrawingsFromLibraryViewModel : ViewModels.ViewModelBase
    {
        #region public functions

        public List<ItemViewModel>? MainTreeViewItemsSource
        {
            get => _mainTreeViewItemsSource;
            set => SetProperty(ref _mainTreeViewItemsSource, value);
        }

        public async Task GoLibraryAsync(DirectoryInfo? libraryDirectoryInfo)
        {
            var rootItem = new ItemViewModel(new EntityInfo(Res.LibraryDsPagesAndDsShapes))
            {
                IsInitiallySelected = true
            };

            try
            {
                if (libraryDirectoryInfo is not null)
                {
                    var directories = new List<DirectoryInfo> { libraryDirectoryInfo };
                    directories.AddRange(libraryDirectoryInfo.GetDirectories(@"*", SearchOption.AllDirectories));

                    var categoryItems = new List<ItemViewModel>();

                    foreach (DirectoryInfo di in directories)
                    {
                        var drawingInfos = new CaseInsensitiveOrderedDictionary<DrawingInfo>();

                        foreach (FileInfo fi in di.GetFiles(@"*" + DsProject.DsPageFileExtension,
                                     SearchOption.TopDirectoryOnly))
                        {
                            DrawingInfo? drawingInfo = await DsProject.ReadDrawingInfoAsync(fi.FullName, false);
                            if (drawingInfo is not null) drawingInfos[drawingInfo.Name] = drawingInfo;
                        }

                        foreach (FileInfo fi in di.GetFiles(@"*" + DsProject.DsShapeFileExtension,
                                     SearchOption.TopDirectoryOnly))
                        {
                            DrawingInfo? drawingInfo = await DsProject.ReadDrawingInfoAsync(fi.FullName, false);
                            if (drawingInfo is not null) drawingInfos[drawingInfo.Name] = drawingInfo;
                        }

                        if (drawingInfos.Count == 0) continue;

                        var categoryName = di.FullName == libraryDirectoryInfo.FullName
                            ? @"\"
                            : di.FullName.Substring(libraryDirectoryInfo.FullName.Length);

                        var categoryItem = new ItemViewModel(new EntityInfo(categoryName));
                        categoryItems.Add(categoryItem);

                        Dictionary<string, List<EntityInfo>> entityInfosDictionary =
                            DsProject.GetEntityInfosDictionary(drawingInfos.Values.OfType<EntityInfo>());

                        if (entityInfosDictionary.Count == 1)
                        {
                            foreach (EntityInfo entityInfo in entityInfosDictionary.First().Value)
                                categoryItem.Children.Add(new ItemViewModel(entityInfo));
                        }
                        else
                        {
                            foreach (var group in entityInfosDictionary.Keys.OrderBy(key => key))
                            {
                                var groupName = String.IsNullOrWhiteSpace(group) ? Res.MainGroup : group;

                                var groupItem = new ItemViewModel(new EntityInfo(groupName));
                                categoryItem.Children.Add(groupItem);

                                foreach (EntityInfo entityInfo in entityInfosDictionary[group])
                                    groupItem.Children.Add(new ItemViewModel(entityInfo));
                            }
                        }
                    }

                    rootItem.Children.AddRange(categoryItems.OrderBy(ci => ci.Header));
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }

            rootItem.Initialize();

            MainTreeViewItemsSource = new List<ItemViewModel> { rootItem };
        }

        #endregion

        #region private fields

        private List<ItemViewModel>? _mainTreeViewItemsSource;

        #endregion
    }
}
