using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Design.Controls;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.Design.Views;

/// <summary>
///     The shapes of the drawing being edited, in the order the author chose. Clicking one selects it
///     on the drawing; a double click brings it into view and opens its properties.
///     <para>
///         Ported from the WPF editor's DrawingDsShapesDockControl. The WPF version used a TreeView
///         although the shapes it lists are flat, so this is a list.
///     </para>
/// </summary>
public partial class DrawingDsShapesView : UserControl
{
    #region construction and destruction

    public DrawingDsShapesView()
    {
        InitializeComponent();

        DataContext = ViewModel;

        // The list follows the drawing being edited for as long as the editor is open, and not only
        // while it is on show, because the pane it is in may be behind another pane while the author
        // moves from one drawing to another.
        DesignDsProjectViewModel.Instance.PropertyChanged += OnDesignDsProjectViewModelPropertyChanged;

        Refresh();
    }

    #endregion

    #region public functions

    public DrawingDsShapesViewModel ViewModel { get; } = new();

    #endregion

    #region private functions

    private void OnDesignDsProjectViewModelPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DesignDsProjectViewModel.FocusedDesignDrawingViewModel))
            Refresh();
    }

    /// <summary>
    ///     Re-orders the shapes of the focused drawing the way the author asked. The order is kept on
    ///     the selection service itself, because that is also the order a Shift-click extends over.
    /// </summary>
    private void Refresh()
    {
        DesignDrawingViewModel? focusedDesignDrawingViewModel =
            DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;

        if (focusedDesignDrawingViewModel is null)
        {
            ViewModel.DrawingDsShapesTreeViewItemsSource = null;
            return;
        }

        switch ((DsShapesOrderingEnum) DesignDsProjectViewModel.Instance.DsShapesOrdering)
        {
            case DsShapesOrderingEnum.DsShapeZIndex:
                focusedDesignDrawingViewModel.SelectionService.AllItems =
                    focusedDesignDrawingViewModel.SelectionService.AllItems.OrderBy(i => i.DsShape.Index);
                break;
            case DsShapesOrderingEnum.DsShapeType:
                focusedDesignDrawingViewModel.SelectionService.AllItems =
                    focusedDesignDrawingViewModel.SelectionService.AllItems
                        .OrderBy(i => i.DsShape.GetDsShapeTypeNameToDisplay())
                        .ThenBy(i => i.DsShape.Index);
                break;
            case DsShapesOrderingEnum.DsShapeName:
                focusedDesignDrawingViewModel.SelectionService.AllItems =
                    focusedDesignDrawingViewModel.SelectionService.AllItems.OrderBy(i => i.DsShape.Name)
                        .ThenBy(i => i.DsShape.Index);
                break;
        }

        ViewModel.DrawingDsShapesTreeViewItemsSource =
            focusedDesignDrawingViewModel.SelectionService.AllItems.ToArray();
    }

    private void DsShapeOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control || control.DataContext is not DsShapeViewModel dsShapeViewModel) return;

        DesignDrawingViewModel? focusedDesignDrawingViewModel =
            DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
        focusedDesignDrawingViewModel?.SelectionService.UpdateSelection(dsShapeViewModel, e.KeyModifiers);

        Utils.RoutedCommand.InvalidateRequerySuggested();

        e.Handled = true;
    }

    private void DsShapeOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control control || control.DataContext is not DsShapeViewModel dsShapeViewModel) return;

        Point center = dsShapeViewModel.DsShape.GetCenterInitialPositionOnDrawing();
        DesignDrawingView.ShowOnViewportCenter(
            DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel, center.X, center.Y);
        DesignDsProjectViewModel.Instance.ShowFirstSelectedDsShapePropertiesWindow(
            DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel);

        e.Handled = true;
    }

    #endregion
}
