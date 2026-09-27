using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Core.Events;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Design.Controls.Docking;

namespace Ssz.Operator.Core.Design.Views;

/// <summary>
///     The panes of the editor and what is in them.
///     <para>
///         Ported from the WPF editor's DockingManagerViewModel: the opened drawings are the
///         documents, the three lists are the panes beside them, and the document the author is on is
///         the drawing every command works from. As in WPF the author may drag a pane elsewhere, tab
///         panes together or float them off.
///     </para>
/// </summary>
public partial class DesignMainView
{
    #region public functions

    /// <summary>
    ///     The three lists are made once and kept, so that a pane moved elsewhere is still the same
    ///     list, with the same selection, and so that the rest of the editor may reach them by name,
    ///     as it did when they were three tabs of the window.
    /// </summary>
    public DsPagesListView DsPagesListView => _dsPagesListView ??= new DsPagesListView();

    public DsShapesListView DsShapesListView => _dsShapesListView ??= new DsShapesListView();

    public DrawingDsShapesView DrawingDsShapesView => _drawingDsShapesView ??= new DrawingDsShapesView();

    #endregion

    #region private functions

    /// <summary>
    ///     Builds the layout and puts the drawings that are already open into it.
    /// </summary>
    private void InitializeDocking()
    {
        // What a pane or a document shows. The lists are the ones kept above, and a drawing keeps its
        // own surface, so that moving its tab does not throw away what is drawn.
        DataTemplates.Add(new FuncDataTemplate<object>((data, _) => data switch
        {
            DsPagesListTool => DsPagesListView,
            DsShapesListTool => DsShapesListView,
            DrawingDsShapesTool => DrawingDsShapesView,
            DrawingDocument document => GetDrawingView(document),
            _ => null
        }, true));

        _dockFactory = new DesignDockFactory();

        IRootDock layout = _dockFactory.CreateLayout();
        _dockFactory.InitLayout(layout);

        _dockFactory.ActiveDockableChanged += DockFactoryOnActiveDockableChanged;
        _dockFactory.FocusedDockableChanged += DockFactoryOnFocusedDockableChanged;
        _dockFactory.DockableClosing += DockFactoryOnDockableClosing;

        MainDockControl.Factory = _dockFactory;
        MainDockControl.Layout = layout;

        DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels.CollectionChanged +=
            OpenedDrawingsOnCollectionChanged;

        foreach (DesignDrawingViewModel drawingViewModel in
                 DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels)
            AddDocument(drawingViewModel);
    }

    /// <summary>
    ///     Brings the list of the pages or the list of the complex shapes to the front, which is what
    ///     the editor asks for when it shows where the drawing being edited came from.
    /// </summary>
    private void ShowListTool(bool dsPages)
    {
        if (_dockFactory is null) return;

        _dockFactory.SetActiveDockable(dsPages
            ? _dockFactory.DsPagesListTool
            : _dockFactory.DsShapesListTool);
    }

    /// <summary>
    ///     The surface of a drawing is made once and kept, because it is what the drawing is edited on
    ///     and it is told the controls of the drawing only the first time it is shown.
    /// </summary>
    private Control GetDrawingView(DrawingDocument document)
    {
        if (_drawingViews.TryGetValue(document.DrawingViewModel, out DesignDrawingView? view)) return view;

        view = new DesignDrawingView { DataContext = document.DrawingViewModel };
        _drawingViews.Add(document.DrawingViewModel, view);
        return view;
    }

    private void OpenedDrawingsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (DesignDrawingViewModel drawingViewModel in e.OldItems.OfType<DesignDrawingViewModel>())
                RemoveDocument(drawingViewModel);

        if (e.NewItems is not null)
            foreach (DesignDrawingViewModel drawingViewModel in e.NewItems.OfType<DesignDrawingViewModel>())
                AddDocument(drawingViewModel);
    }

    private void AddDocument(DesignDrawingViewModel drawingViewModel)
    {
        if (_dockFactory is null) return;
        if (FindDocument(drawingViewModel) is not null) return;

        var document = new DrawingDocument(drawingViewModel);

        _documentsBeingChanged = true;
        try
        {
            _dockFactory.AddDockable(_dockFactory.DocumentDock, document);
            _dockFactory.SetActiveDockable(document);
            _dockFactory.SetFocusedDockable(_dockFactory.DocumentDock, document);
        }
        finally
        {
            _documentsBeingChanged = false;
        }
    }

    private void RemoveDocument(DesignDrawingViewModel drawingViewModel)
    {
        if (_dockFactory is null) return;

        DrawingDocument? document = FindDocument(drawingViewModel);
        if (document is null) return;

        _documentsBeingChanged = true;
        try
        {
            _dockFactory.RemoveDockable(document, true);
        }
        finally
        {
            _documentsBeingChanged = false;
        }

        _drawingViews.Remove(drawingViewModel);
    }

    private DrawingDocument? FindDocument(DesignDrawingViewModel drawingViewModel)
    {
        return _dockFactory?.DocumentDock.VisibleDockables?
            .OfType<DrawingDocument>()
            .FirstOrDefault(d => ReferenceEquals(d.DrawingViewModel, drawingViewModel));
    }

    /// <summary>
    ///     The drawing of the document the author is on is the one every command works from. This is
    ///     what the WPF editor did with the active content of its docking manager.
    /// </summary>
    private void DockFactoryOnActiveDockableChanged(object? sender, ActiveDockableChangedEventArgs e)
    {
        if (_documentsBeingChanged) return;

        switch (e.Dockable)
        {
            case DrawingDocument document when !document.DrawingViewModel.IsDisposed:
                DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel = document.DrawingViewModel;
                break;
            case null when _dockFactory is null || _dockFactory.DocumentDock.VisibleDockables is null ||
                           _dockFactory.DocumentDock.VisibleDockables.Count == 0:
                DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel = null;
                break;
        }
    }

    private void DockFactoryOnFocusedDockableChanged(object? sender, FocusedDockableChangedEventArgs e)
    {
        if (_documentsBeingChanged) return;

        if (e.Dockable is DrawingDocument document && !document.DrawingViewModel.IsDisposed)
            DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel = document.DrawingViewModel;
    }

    /// <summary>
    ///     Closing the tab of a drawing closes the drawing, which is where the author is asked about
    ///     what they have not saved. The tab is kept until that is answered, and goes when the drawing
    ///     leaves the opened ones.
    /// </summary>
    private void DockFactoryOnDockableClosing(object? sender, DockableClosingEventArgs e)
    {
        if (_documentsBeingChanged) return;
        if (e.Dockable is not DrawingDocument document) return;

        e.Cancel = true;

        _ = DesignDsProjectViewModel.Instance.CloseDrawingAsync(document.DrawingViewModel);
    }

    /// <summary>
    ///     Tells the drawings which of them is the one on show, as the WPF editor did by binding the
    ///     selection of a tab to its drawing. A drawing remembers when it was last on show, which is
    ///     how the editor chooses what to show next when the author closes a drawing, and a drawing
    ///     left behind is looked over for changes not yet saved.
    /// </summary>
    private void UpdateSelectedDrawing()
    {
        DesignDrawingViewModel? focused = DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
        if (ReferenceEquals(_selectedDrawingViewModel, focused)) return;

        if (_selectedDrawingViewModel is not null && !_selectedDrawingViewModel.IsDisposed)
            _selectedDrawingViewModel.IsSelected = false;

        _selectedDrawingViewModel = focused;

        if (focused is not null && !focused.IsDisposed)
            focused.IsSelected = true;
    }

    /// <summary>
    ///     Keeps the tab the author is on in step with the drawing the editor works on, for when that
    ///     is changed from elsewhere - opening a page from a list, say.
    /// </summary>
    private void SyncActiveDocument()
    {
        if (_dockFactory is null || _documentsBeingChanged) return;

        DesignDrawingViewModel? focused = DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
        if (focused is null || focused.IsDisposed) return;

        DrawingDocument? document = FindDocument(focused);
        if (document is null || ReferenceEquals(_dockFactory.DocumentDock.ActiveDockable, document)) return;

        _documentsBeingChanged = true;
        try
        {
            _dockFactory.SetActiveDockable(document);
        }
        finally
        {
            _documentsBeingChanged = false;
        }
    }

    #endregion

    #region private fields

    private DesignDockFactory? _dockFactory;
    private bool _documentsBeingChanged;

    private DesignDrawingViewModel? _selectedDrawingViewModel;

    private DsPagesListView? _dsPagesListView;
    private DsShapesListView? _dsShapesListView;
    private DrawingDsShapesView? _drawingDsShapesView;

    private readonly Dictionary<DesignDrawingViewModel, DesignDrawingView> _drawingViews = new();

    #endregion
}
