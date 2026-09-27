using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Design.Controls;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.VisualEditors;

namespace Ssz.Operator.Core.Design.Views;

/// <summary>
///     The palette the author drags shapes from: the shapes built into the editor, the ones the addons
///     bring, and the complex shapes of the project.
///     <para>
///         Ported from the WPF editor's DsShapesListDockControl.
///     </para>
/// </summary>
public partial class DsShapesListView : UserControl
{
    #region construction and destruction

    public DsShapesListView()
    {
        InitializeComponent();

        DataContext = ViewModel;

        DsProject.Instance.DsShapeDrawingsListChanged += OnDsShapeDrawingsListChanged;

        _ = RefreshAsync(DsProject.Instance.DsProjectFileFullName);
    }

    #endregion

    #region public functions

    public DsShapesListViewModel ViewModel { get; } = new();

    #endregion

    #region protected functions

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DsProject.Instance.DsShapeDrawingsListChanged -= OnDsShapeDrawingsListChanged;

        base.OnDetachedFromVisualTree(e);
    }

    #endregion

    #region private functions

    private void OnDsShapeDrawingsListChanged()
    {
        Dispatcher.UIThread.Post(() => _ = RefreshAsync(DsProject.Instance.DsProjectFileFullName));
    }

    private void RefreshDsShapesButtonOnClick(object? sender, RoutedEventArgs e)
    {
        DsProject.Instance.AllComplexDsShapesCacheDelete();

        DsProject.Instance.OnDsShapeDrawingsListChanged();
    }

    private void SyncWithActiveDrawingButtonOnClick(object? sender, RoutedEventArgs e)
    {
        DesignMainView.Instance?.SyncWithActiveDrawing();
    }

    private async Task RefreshAsync(string? dsProjectFile)
    {
        if (_refreshIsStarted) return;
        _refreshIsStarted = true;

        // Let the other calls that came with this change arrive, and refresh once for all of them.
        await Task.Delay(400);

        _refreshIsStarted = false;

        DrawingInfo[] onDriveDsShapeDrawingInfos;
        if (ViewModel.OpenDsShapeDrawingsErrorMessages is null)
        {
            ViewModel.OpenDsShapeDrawingsErrorMessages = new List<string>();
            onDriveDsShapeDrawingInfos =
                (await DsProject.Instance.GetAllComplexDsShapesDrawingInfosAsync(
                    ViewModel.OpenDsShapeDrawingsErrorMessages)).Values.ToArray();

            if (ViewModel.OpenDsShapeDrawingsErrorMessages is null) return;

            if (ViewModel.OpenDsShapeDrawingsErrorMessages.Count > 0)
                MessageBoxHelper.ShowWarning(String.Join("\n", ViewModel.OpenDsShapeDrawingsErrorMessages));
        }
        else
        {
            onDriveDsShapeDrawingInfos =
                (await DsProject.Instance.GetAllComplexDsShapesDrawingInfosAsync()).Values.ToArray();
        }

        using (DesignDsProjectViewModel.BusyCloser busyCloser = DesignDsProjectViewModel.Instance.GetBusyCloser())
        {
            await DsProject.Instance.CheckDrawingsBinSerializationVersionAsync(onDriveDsShapeDrawingInfos, null);
        }

        if (dsProjectFile != DsProject.Instance.DsProjectFileFullName) return;

        ViewModel.DsShapesTreeViewItemsSource = null;

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
            AddonsManager.GetAddonsDsShapeTypes(), entityInfo => new EntityInfoViewModel(entityInfo));

        var comlexDsShapesGroupViewModel = new GroupViewModel
        {
            Header = Properties.Resources.ComplexDsShapesTreeViewItemHeader
        };
        DsDrawingsListHelper.FillGroupViewModelWithDsShapes(comlexDsShapesGroupViewModel,
            onDriveOrOpenedDrawingInfos, entityInfo => new DrawingInfoViewModel((DrawingInfo) entityInfo));

        ViewModel.DsShapeDrawingInfosSelectionService.Clear();
        comlexDsShapesGroupViewModel.InitializeSelectionService(ViewModel.DsShapeDrawingInfosSelectionService);

        var dsShapesTreeViewItemsSource = new List<object>();
        dsShapesTreeViewItemsSource.Add(simpleDsShapesGroupViewModel);
        if (!addonsSimpleDsShapesGroupViewModel.IsEmpty())
            dsShapesTreeViewItemsSource.Add(addonsSimpleDsShapesGroupViewModel);
        dsShapesTreeViewItemsSource.Add(comlexDsShapesGroupViewModel);
        ViewModel.DsShapesTreeViewItemsSource = dsShapesTreeViewItemsSource;
    }

    private void DsShapeOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;

        if (control.DataContext is DrawingInfoViewModel drawingInfoViewModel)
        {
            // A right press keeps a selection of several shapes, so the menu can act on all of them.
            if (e.GetCurrentPoint(control).Properties.IsRightButtonPressed)
            {
                if (!drawingInfoViewModel.IsSelected)
                    ViewModel.DsShapeDrawingInfosSelectionService.SelectOne(drawingInfoViewModel);
                else
                    ViewModel.DsShapeDrawingInfosSelectionService.MakeFirstSelected(drawingInfoViewModel);
                return;
            }

            ViewModel.DsShapeDrawingInfosSelectionService.UpdateSelection(drawingInfoViewModel, e.KeyModifiers);
        }

        // Dragging a shape onto the drawing is how it is added there.
        if (control.DataContext is EntityInfoViewModel entityInfoViewModel)
            _ = StartDragAsync(e, entityInfoViewModel);
    }

    private async Task StartDragAsync(PointerPressedEventArgs e, EntityInfoViewModel entityInfoViewModel)
    {
        try
        {
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(DesignDrawingCanvas.EntityInfoDataFormat,
                entityInfoViewModel.EntityInfo));

            await DragDrop.DoDragDropAsync(e, dataTransfer, DragDropEffects.Copy);
        }
        catch (Exception)
        {
            // A drag the platform refuses is not worth reporting.
        }
    }

    private async void DsShapeOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not DrawingInfoViewModel drawingInfoViewModel) return;

        e.Handled = true;

        await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
            new FileInfo(drawingInfoViewModel.DrawingInfo.FileFullName));
    }

    #endregion

    #region private fields

    private bool _refreshIsStarted;

    #endregion
}
