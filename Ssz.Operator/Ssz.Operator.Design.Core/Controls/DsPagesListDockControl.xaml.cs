using Ssz.Operator.Core;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Drawings;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ssz.Operator.Design.Core.Controls
{
    /// <summary>
    /// Interaction logic for DsPagesListDockControl.xaml
    /// </summary>
    public partial class DsPagesListDockControl : UserControl
    {
        #region construction and destruction

        public DsPagesListDockControl()
        {
            InitializeComponent();

            DsProject.Instance.DsPageDrawingsListChanged +=
                () => Dispatcher.BeginInvoke(new Action(() => RefreshDsPageDrawingsListAsync(DsProject.Instance.DsProjectFileFullName)));

            RefreshDsPageDrawingsListAsync(DsProject.Instance.DsProjectFileFullName);
        }

        #endregion        

        #region private functions

        private SelectionService<DsPageDrawingInfoViewModel> DsPageDrawingInfosSelectionService
            => ((DsPagesListDockViewModel)DataContext).DsPageDrawingInfosSelectionService;

        private SelectionService<DrawingInfoViewModel> DsShapeDrawingInfosSelectionService
            => DockingManagerViewModel.Instance.DsShapesListDockViewModel.DsShapeDrawingInfosSelectionService;

        private async void RefreshDsPagesButtonClickAsync(object sender, RoutedEventArgs e)
        {
            await DesignDsProjectViewModel.Instance.AllDsPagesCacheUpdateAsync();

            DsProject.Instance.OnDsPageDrawingsListChanged();
        }

        private async void RefreshDsPageDrawingsListAsync(string? dsProjectFile)
        {
            if (String.IsNullOrEmpty(dsProjectFile)) return;

            if (_refreshDsPageDrawingsListIsDisabled) return;
            _refreshDsPageDrawingsListIsDisabled = true;

            await Task.Delay(400);
            // Wait for other calls to RefreshDsPageDrawingsListAsync(), and then execute refreshing only once

            _refreshDsPageDrawingsListIsDisabled = false;

            var onDriveDrawingInfos =
                        DsProject.Instance.AllDsPagesCache.Select(kvp => kvp.Value.GetDrawingInfo()).ToArray();

            if (dsProjectFile != DsProject.Instance.DsProjectFileFullName) return;

            ((DsPagesListDockViewModel)DataContext).DsPagesTreeViewItemsSource = null;

            IEnumerable<DrawingInfo>? onDriveOrOpenedDrawingInfos =
                DesignDsProjectViewModel.Instance.GetOnDriveOrOpenedDrawingInfos(onDriveDrawingInfos);

            if (onDriveOrOpenedDrawingInfos is null) return;

            var rootGroupViewModel = new GroupViewModel();
            DsDrawingsListHelper.FillGroupViewModelWithDsPages(rootGroupViewModel,
                onDriveOrOpenedDrawingInfos.OfType<DsPageDrawingInfo>(),
                new DsPagesGroupingFilter { GroupByStyle = true, GroupByGroup = true });

            var originallySelectedItems = DsPageDrawingInfosSelectionService.SelectedItems;
            DsPageDrawingInfosSelectionService.Clear();
            rootGroupViewModel.InitializeSelectionService(DsPageDrawingInfosSelectionService);
            foreach (var originallySelectedItem in originallySelectedItems)
            {
                DsPageDrawingInfoViewModel? item = DsPageDrawingInfosSelectionService.AllItems.FirstOrDefault(
                    i => FileSystemHelper.Compare(i.DrawingInfo.FileInfo.FullName,
                        originallySelectedItem.DrawingInfo.FileInfo.FullName));
                if (item is not null) DsPageDrawingInfosSelectionService.AddToSelection(item);
            }

            var dsPagesTreeViewItemsSource = new List<object>();
            //dsPagesTreeViewItemsSource.Add(DsProjectInfoViewModel.Instance);
            dsPagesTreeViewItemsSource.AddRange(rootGroupViewModel.Items);
            ((DsPagesListDockViewModel)DataContext).DsPagesTreeViewItemsSource = dsPagesTreeViewItemsSource;

            MainWindow.Instance.OnStartDsPageChanged();
        }

        private void DsPagesTreeViewItemOnMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var drawingInfos = DsPageDrawingInfosSelectionService.SelectedItems
                        .Select(vm => vm.DrawingInfo).ToArray();
                    if (drawingInfos.Length > 0)
                    {
                        DragDrop.DoDragDrop(DsPagesTreeView,
                            drawingInfos,
                            DragDropEffects.Move);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void DsPagesTreeViewItemOnDrop(object sender, DragEventArgs e)
        {
            var drawingInfos = e.Data.GetData(typeof(DrawingInfo[])) as DrawingInfo[];
            if (drawingInfos is null) return;

            var dsPageGroupViewModel = ((FrameworkElement)sender).DataContext as DsPageDrawingInfosGroupViewModel;
            if (dsPageGroupViewModel is not null)
            {
                foreach (var drawingInfo in drawingInfos)
                {
                    DesignDrawingViewModel? designerDrawingViewModel =
                        DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels.FirstOrDefault(
                            dvm => FileSystemHelper.Compare(dvm.Drawing.FileFullName, drawingInfo.FileInfo.FullName));

                    if (designerDrawingViewModel is not null)
                    {
                        var dsPageDrawing = designerDrawingViewModel.Drawing as DsPageDrawing;
                        if (dsPageDrawing is null) return;

                        DsDrawingsListHelper.UpdateDsPageDrawingProps(dsPageDrawing, dsPageGroupViewModel);
                    }
                    else
                    {
                        var dsPageDrawing = DsProject.ReadDrawing(drawingInfo.FileInfo, true, true) as DsPageDrawing;
                        if (dsPageDrawing is null) return;

                        DsDrawingsListHelper.UpdateDsPageDrawingProps(dsPageDrawing, dsPageGroupViewModel);

                        if (dsPageDrawing.DataChangedFromLastSave)
                        {
                            DsProject.Instance.SaveUnconditionally(dsPageDrawing, DsProject.IfFileExistsActions.CreateBackup, true);
                        }
                    }
                }

                DsProject.Instance.OnDsPageDrawingsListChanged();
            }

            e.Handled = true;
        }

        private void DsPageTreeViewItemOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = (TreeViewItem)sender;

            treeViewItem.IsSelected = true;

            DsPageDrawingInfosSelectionService.UpdateSelection(treeViewItem.DataContext as DsPageDrawingInfoViewModel);

            e.Handled = true;
        }

        private void DsPageTreeViewItemOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                MainWindow.Instance.ShowDsPageTypeObjectProperties(((FrameworkElement)sender).DataContext as DsPageDrawingInfoViewModel);

                e.Handled = true;
            }
        }

        private async void DsPageTreeViewItemOnMouseDoubleClickAsync(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var dsPageDrawingInfoViewModel = ((FrameworkElement)sender).DataContext as DsPageDrawingInfoViewModel;

                if (dsPageDrawingInfoViewModel is not null)
                {
                    await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
                        dsPageDrawingInfoViewModel.DrawingInfo.FileInfo);
                }
            }

            e.Handled = true;
        }

        private void SyncWithActiveDrawingButtonOnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SyncWithActiveDrawingButtonOnClick();
        }

        #endregion

        #region private fields        

        private bool _refreshDsPageDrawingsListIsDisabled;

        #endregion        
    }
}
