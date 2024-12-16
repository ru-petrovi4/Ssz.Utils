using Ssz.Operator.Core;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.VisualEditors;
using System;
using System.Collections.Generic;
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

namespace Ssz.Operator.Design.Controls
{
    /// <summary>
    /// Interaction logic for DsShapesListDockControl.xaml
    /// </summary>
    public partial class DsShapesListDockControl : UserControl
    {
        #region construction and destruction

        public DsShapesListDockControl()
        {
            InitializeComponent();

            DsProject.Instance.DsShapeDrawingsListChanged +=
                () => Dispatcher.BeginInvoke(new Action(() => RefreshDsShapesListAsync(DsProject.Instance.DsProjectFileFullName)));

            RefreshDsShapesListAsync(DsProject.Instance.DsProjectFileFullName);
        }

        #endregion        

        #region private functions

        private List<string>? OpenDsShapeDrawingsErrorMessages
        {
            get => ((DsShapesListDockViewModel)DataContext).OpenDsShapeDrawingsErrorMessages;
            set => ((DsShapesListDockViewModel)DataContext).OpenDsShapeDrawingsErrorMessages = value;
        }

        private SelectionService<DrawingInfoViewModel> DsShapeDrawingInfosSelectionService
            => ((DsShapesListDockViewModel)DataContext).DsShapeDrawingInfosSelectionService;

        private void RefreshDsShapesButtonClick(object sender, RoutedEventArgs e)
        {
            DsProject.Instance.AllComplexDsShapesCacheDelete();

            DsProject.Instance.OnDsShapeDrawingsListChanged();
        }

        private async void RefreshDsShapesListAsync(string? dsProjectFile)
        {
            if (_refreshDsShapesListIsStarted) return;
            _refreshDsShapesListIsStarted = true;

            await Task.Delay(400);
            // Wait for other calls to RefreshDsShapesListAsync(), and then execute refreshing only once

            _refreshDsShapesListIsStarted = false;

            DrawingInfo[] onDriveDsShapeDrawingInfos;
            if (OpenDsShapeDrawingsErrorMessages is null)
            {
                OpenDsShapeDrawingsErrorMessages = new List<string>();
                onDriveDsShapeDrawingInfos =
                        DsProject.Instance.GetAllComplexDsShapesDrawingInfos(
                            OpenDsShapeDrawingsErrorMessages).Values.ToArray();

                if (OpenDsShapeDrawingsErrorMessages is null) return;

                if (OpenDsShapeDrawingsErrorMessages.Count > 0)
                {
                    MessageBoxHelper.ShowWarning(String.Join("\n", OpenDsShapeDrawingsErrorMessages));
                }
            }
            else
            {
                onDriveDsShapeDrawingInfos =
                    DsProject.Instance.GetAllComplexDsShapesDrawingInfos().Values.ToArray();
            }

            using (DesignDsProjectViewModel.BusyCloser busyCloser = DesignDsProjectViewModel.Instance.GetBusyCloser())
            {
                await DsProject.Instance.CheckDrawingsBinSerializationVersionAsync(onDriveDsShapeDrawingInfos, null);
            }

            if (dsProjectFile != DsProject.Instance.DsProjectFileFullName) return;

            ((DsShapesListDockViewModel)DataContext).DsShapesTreeViewItemsSource = null;

            IEnumerable<DrawingInfo>? onDriveOrOpenedDrawingInfos =
                DesignDsProjectViewModel.Instance.GetOnDriveOrOpenedDrawingInfos(onDriveDsShapeDrawingInfos);

            if (onDriveOrOpenedDrawingInfos is null) return;

            var simpleDsShapesGroupViewModel = new GroupViewModel
            {
                Header = Properties.Resources.SimpleDsShapesTreeViewItemHeader
            };
            DsDrawingsListHelper.FillGroupViewModelWithStandardSimpleDsShapes(simpleDsShapesGroupViewModel);

            var addonsSimpleDsShapesGroupViewModel = new GroupViewModel
            {
                Header = Properties.Resources.AddonSimpleDsShapesTreeViewItemHeader
            };
            DsDrawingsListHelper.FillGroupViewModelWithDsShapes(addonsSimpleDsShapesGroupViewModel,
                AddonsHelper.GetAddonsDsShapeTypes(), entityInfo => new EntityInfoViewModel(entityInfo));

            var comlexDsShapesGroupViewModel = new GroupViewModel
            {
                Header = Properties.Resources.ComplexDsShapesTreeViewItemHeader
            };
            DsDrawingsListHelper.FillGroupViewModelWithDsShapes(comlexDsShapesGroupViewModel,
                onDriveOrOpenedDrawingInfos, entityInfo => new DrawingInfoViewModel((DrawingInfo)entityInfo));

            DsShapeDrawingInfosSelectionService.Clear();
            comlexDsShapesGroupViewModel.InitializeSelectionService(DsShapeDrawingInfosSelectionService);

            var dsShapesTreeViewItemsSource = new List<object>();
            dsShapesTreeViewItemsSource.Add(simpleDsShapesGroupViewModel);
            if (!addonsSimpleDsShapesGroupViewModel.IsEmpty())
                dsShapesTreeViewItemsSource.Add(addonsSimpleDsShapesGroupViewModel);
            dsShapesTreeViewItemsSource.Add(comlexDsShapesGroupViewModel);
            ((DsShapesListDockViewModel)DataContext).DsShapesTreeViewItemsSource = dsShapesTreeViewItemsSource;
        }

        private void DsShapeTreeViewItemOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = (TreeViewItem)sender;

            treeViewItem.IsSelected = true;

            DsShapeDrawingInfosSelectionService.UpdateSelection(
                    treeViewItem.DataContext as DrawingInfoViewModel);

            e.Handled = true;
        }

        private async void DsShapeTreeViewItemOnMouseDoubleClickAsync(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var drawingInfoViewModel = ((FrameworkElement)sender).DataContext as DrawingInfoViewModel;

                if (drawingInfoViewModel is not null)
                {
                    await
                        DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
                            drawingInfoViewModel.DrawingInfo.FileInfo);
                }
            }

            e.Handled = true;
        }

        private void DsShapesTreeViewItemOnMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var entityInfoViewModel = DsShapesTreeView.SelectedItem as EntityInfoViewModel;
                    if (entityInfoViewModel is not null)
                    {
                        DragDrop.DoDragDrop(DsShapesTreeView,
                            entityInfoViewModel.EntityInfo,
                            DragDropEffects.Copy);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void SyncWithActiveDrawingButtonOnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SyncWithActiveDrawingButtonOnClick();
        }

        #endregion

        #region private fields        

        private bool _refreshDsShapesListIsStarted;

        #endregion        
    }
}
