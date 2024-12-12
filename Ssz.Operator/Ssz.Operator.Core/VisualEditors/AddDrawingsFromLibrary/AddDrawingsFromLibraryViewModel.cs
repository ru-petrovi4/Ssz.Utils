using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.VisualEditors.AddDrawingsFromLibrary
{
    public class AddDrawingsFromLibraryViewModel : ViewModelBase
    {
        #region private fields

        private List<ItemViewModel>? _mainTreeViewItemsSource;

        #endregion

        #region public functions

        public List<ItemViewModel>? MainTreeViewItemsSource
        {
            get => _mainTreeViewItemsSource;
            set => SetValue(ref _mainTreeViewItemsSource, value);
        }

        public void GoLibrary(DirectoryInfo? libraryDirectoryInfo)
        {
            var rootItem = new ItemViewModel(new EntityInfo(Resources.LibraryDsPagesAndDsShapes))
            {
                IsInitiallySelected = true
            };

            try
            {
                if (libraryDirectoryInfo is not null)
                {
                    List<DirectoryInfo> directories = new();
                    directories.Add(libraryDirectoryInfo);
                    directories.AddRange(libraryDirectoryInfo.GetDirectories("*", SearchOption.AllDirectories));

                    var categoryItems = new List<ItemViewModel>();
                    foreach (
                        DirectoryInfo di in directories)
                    {
                        var drawingInfos = new CaseInsensitiveDictionary<DrawingInfo>();

                        foreach (FileInfo fi in di.GetFiles(@"*" + DsProject.DsPageFileExtension, SearchOption.TopDirectoryOnly))
                        {
                            var drawingInfo = DsProject.ReadDrawingInfo(fi, false);
                            if (drawingInfo is not null) drawingInfos.Add(drawingInfo.Name, drawingInfo);
                        }

                        foreach (FileInfo fi in di.GetFiles(@"*" + DsProject.DsShapeFileExtension, SearchOption.TopDirectoryOnly))
                        {
                            var drawingInfo = DsProject.ReadDrawingInfo(fi, false);
                            if (drawingInfo is not null) drawingInfos.Add(drawingInfo.Name, drawingInfo);
                        }

                        if (drawingInfos.Count > 0)
                        {
                            string categoryName;
                            if (di == libraryDirectoryInfo)
                                categoryName = @"\";
                            else
                                categoryName = di.FullName.Substring(libraryDirectoryInfo.FullName.Length);
                            /*
                            if (di.Name.ToUpperInvariant() == dsShapesDirectoryInfo.Name.ToUpperInvariant())
                                categoryName = di.Parent.Name;
                            else categoryName = di.Name;*/
                            var categoryItem = new ItemViewModel(new EntityInfo(categoryName));
                            categoryItems.Add(categoryItem);

                            Dictionary<string, List<EntityInfo>> entityInfosDictionary =
                                DsProject.GetEntityInfosDictionary(drawingInfos.Values);

                            if (entityInfosDictionary.Count == 1)
                                foreach (EntityInfo entityInfo in entityInfosDictionary.First().Value)
                                {
                                    var leafItem = new ItemViewModel(entityInfo);
                                    categoryItem.Children.Add(leafItem);
                                }
                            else
                                foreach (
                                    string group in
                                    entityInfosDictionary.Keys.OrderBy(
                                        key => key))
                                {
                                    string groupName;
                                    if (string.IsNullOrWhiteSpace(@group)) groupName = Resources.MainGroup;
                                    else groupName = @group;
                                    var groupItem = new ItemViewModel(new EntityInfo(groupName));
                                    categoryItem.Children.Add(groupItem);

                                    foreach (EntityInfo entityInfo in entityInfosDictionary[@group])
                                    {
                                        var leafItem = new ItemViewModel(entityInfo);
                                        groupItem.Children.Add(leafItem);
                                    }
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
            MainTreeViewItemsSource = new List<ItemViewModel> {rootItem};
        }

        #endregion
    }
}