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
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Design.Controls;
using Ssz.Operator.Core.Drawings;
using Ssz.Utils;

namespace Ssz.Operator.Core.Design.Views;

/// <summary>
///     The list of the pages of the opened project, grouped by page type and then by the group the
///     author gave them.
///     <para>
///         Ported from the WPF editor's DsPagesListDockControl. It is a plain view for now: it moves
///         into a dock pane when the docking layout is ported, and opening a page on a double click
///         arrives with the drawing surface.
///     </para>
/// </summary>
public partial class DsPagesListView : UserControl
{
    #region construction and destruction

    public DsPagesListView()
    {
        InitializeComponent();

        DataContext = ViewModel;

        DsProject.Instance.DsPageDrawingsListChanged += OnDsPageDrawingsListChanged;

        _ = RefreshAsync(DsProject.Instance.DsProjectFileFullName);
    }

    #endregion

    #region public functions

    public DsPagesListViewModel ViewModel { get; } = new();

    /// <summary>
    ///     Marks the page the project starts with, which the list shows with a '&gt;' badge.
    /// </summary>
    public void OnStartDsPageChanged()
    {
        if (!DsProject.Instance.IsInitialized) return;

        string? startDsPageFileFullName =
            DsProject.Instance.GetExistingDsPageFileFullNameOrNull(
                DsProject.Instance.RootWindowProps.FileRelativePath);

        foreach (DsPageDrawingInfoViewModel dsPageDrawingInfoViewModel in
                 ViewModel.DsPageDrawingInfosSelectionService.AllItems)
        {
            dsPageDrawingInfoViewModel.IsStartDsPage =
                startDsPageFileFullName is not null &&
                FileSystemHelper.Compare(dsPageDrawingInfoViewModel.DrawingInfo.FileFullName,
                    startDsPageFileFullName);
        }
    }

    #endregion

    #region protected functions

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DsProject.Instance.DsPageDrawingsListChanged -= OnDsPageDrawingsListChanged;

        base.OnDetachedFromVisualTree(e);
    }

    #endregion

    #region private functions

    private void OnDsPageDrawingsListChanged()
    {
        Dispatcher.UIThread.Post(() => _ = RefreshAsync(DsProject.Instance.DsProjectFileFullName));
    }

    private async void RefreshDsPagesButtonOnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await DesignDsProjectViewModel.Instance.AllDsPagesCacheUpdateAsync();

            DsProject.Instance.OnDsPageDrawingsListChanged();
        }
        catch (Exception ex)
        {
            DsProject.LoggersSet.Logger.LogError(ex, @"Refreshing the pages list failed.");
        }
    }

    /// <summary>
    ///     A press on a page selects it, and a press with Ctrl or Shift extends the selection - which
    ///     is what every command over the pages list works from. A right press selects too, so that
    ///     the menu that follows acts on what was pointed at.
    /// </summary>
    private void DsPageOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not DsPageDrawingInfoViewModel dsPageDrawingInfoViewModel) return;

        PointerPointProperties properties = e.GetCurrentPoint(control).Properties;

        if (properties.IsRightButtonPressed)
        {
            // Right on something already selected keeps the selection, so a menu can act on all of it.
            if (!dsPageDrawingInfoViewModel.IsSelected)
                ViewModel.DsPageDrawingInfosSelectionService.SelectOne(dsPageDrawingInfoViewModel);
            else
                ViewModel.DsPageDrawingInfosSelectionService.MakeFirstSelected(dsPageDrawingInfoViewModel);
            return;
        }

        if (!properties.IsLeftButtonPressed) return;

        ViewModel.DsPageDrawingInfosSelectionService.UpdateSelection(dsPageDrawingInfoViewModel,
            e.KeyModifiers);
    }

    /// <summary>
    ///     The middle button opens the properties of the page type object, as it did in WPF.
    /// </summary>
    private void DsPageOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Middle) return;
        if (sender is not Control control) return;

        DesignMainView.Instance?.ShowDsPageTypeObjectProperties(
            control.DataContext as DsPageDrawingInfoViewModel);

        e.Handled = true;
    }

    private async void DsPageOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not DsPageDrawingInfoViewModel dsPageDrawingInfoViewModel) return;

        e.Handled = true;

        await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
            new FileInfo(dsPageDrawingInfoViewModel.DrawingInfo.FileFullName));
    }

    private void SyncWithActiveDrawingButtonOnClick(object? sender, RoutedEventArgs e)
    {
        DesignMainView.Instance?.SyncWithActiveDrawing();
    }

    /// <param name="dsProjectFile">
    ///     The project this refresh was asked for. Another one may be opened while it waits, and then
    ///     its result belongs to a project that is no longer there.
    /// </param>
    private async Task RefreshAsync(string? dsProjectFile)
    {
        if (String.IsNullOrEmpty(dsProjectFile)) return;

        if (_refreshIsDisabled) return;
        _refreshIsDisabled = true;

        // Let the other calls that came with this change arrive, and refresh once for all of them.
        await Task.Delay(400);

        _refreshIsDisabled = false;

        DrawingInfo[] onDriveDrawingInfos =
            DsProject.Instance.AllDsPagesCache.Select(kvp => kvp.Value.GetDrawingInfo()).ToArray();

        if (dsProjectFile != DsProject.Instance.DsProjectFileFullName) return;

        ViewModel.DsPagesTreeViewItemsSource = null;

        IEnumerable<DrawingInfo>? onDriveOrOpenedDrawingInfos =
            DesignDsProjectViewModel.Instance.GetOnDriveOrOpenedDrawingInfos(onDriveDrawingInfos);
        if (onDriveOrOpenedDrawingInfos is null) return;

        var rootGroupViewModel = new GroupViewModel();
        DsDrawingsListHelper.FillGroupViewModelWithDsPages(rootGroupViewModel,
            onDriveOrOpenedDrawingInfos.OfType<DsPageDrawingInfo>(),
            new DsPagesGroupingFilter { GroupByStyle = true, GroupByGroup = true });

        // The pages are new objects, so the selection is carried over by file name.
        DsPageDrawingInfoViewModel[] originallySelectedItems =
            ViewModel.DsPageDrawingInfosSelectionService.SelectedItems;
        ViewModel.DsPageDrawingInfosSelectionService.Clear();
        rootGroupViewModel.InitializeSelectionService(ViewModel.DsPageDrawingInfosSelectionService);
        foreach (DsPageDrawingInfoViewModel originallySelectedItem in originallySelectedItems)
        {
            DsPageDrawingInfoViewModel? item = ViewModel.DsPageDrawingInfosSelectionService.AllItems.FirstOrDefault(
                i => FileSystemHelper.Compare(i.DrawingInfo.FileFullName,
                    originallySelectedItem.DrawingInfo.FileFullName));
            if (item is not null) ViewModel.DsPageDrawingInfosSelectionService.AddToSelection(item);
        }

        var dsPagesTreeViewItemsSource = new List<object>();
        dsPagesTreeViewItemsSource.AddRange(rootGroupViewModel.Items);
        ViewModel.DsPagesTreeViewItemsSource = dsPagesTreeViewItemsSource;

        OnStartDsPageChanged();
    }

    #endregion

    #region private fields

    private bool _refreshIsDisabled;

    #endregion
}
