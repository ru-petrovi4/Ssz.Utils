using System.Collections.Generic;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Ssz.Operator.Core.Design.Properties;

namespace Ssz.Operator.Core.Design.Controls.Docking;

/// <summary>
///     The layout of the editor: the opened drawings on the left, and beside them the one pane that
///     holds the three lists.
///     <para>
///         This is the layout the WPF editor started with - a documents pane and one anchorable pane
///         four hundred wide - and, as there, the author may drag the panes elsewhere, tab them
///         together or float them off.
///     </para>
/// </summary>
public class DesignDockFactory : Factory
{
    #region public functions

    public IDocumentDock DocumentDock { get; private set; } = null!;

    public IToolDock ToolDock { get; private set; } = null!;

    public DsPagesListTool DsPagesListTool { get; } = new()
    {
        Id = @"DsPagesList",
        Title = Resources.DsPagesTabItemHeader,
        CanClose = false
    };

    public DsShapesListTool DsShapesListTool { get; } = new()
    {
        Id = @"DsShapesList",
        Title = Resources.DsShapesTabItemHeader,
        CanClose = false
    };

    public DrawingDsShapesTool DrawingDsShapesTool { get; } = new()
    {
        Id = @"DrawingDsShapes",
        Title = Resources.DrawingTabItemHeader,
        CanClose = false
    };

    public override IRootDock CreateLayout()
    {
        DocumentDock = new DocumentDock
        {
            Id = @"Drawings",
            Title = Resources.DsPagesTabItemHeader,
            IsCollapsable = false,
            Proportion = 0.72,
            CanCreateDocument = false,
            VisibleDockables = CreateList<IDockable>()
        };

        ToolDock = new ToolDock
        {
            Id = @"Lists",
            Title = Resources.DsPagesTabItemHeader,
            Proportion = 0.28,
            Alignment = Alignment.Right,
            VisibleDockables = CreateList<IDockable>(
                DsPagesListTool, DsShapesListTool, DrawingDsShapesTool),
            ActiveDockable = DsPagesListTool
        };

        var mainLayout = new ProportionalDock
        {
            Id = @"MainLayout",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                DocumentDock,
                new ProportionalDockSplitter(),
                ToolDock)
        };

        var rootDock = CreateRootDock();
        rootDock.Id = @"Root";
        rootDock.IsCollapsable = false;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;

        return rootDock;
    }

    public override void InitLayout(IDockable layout)
    {
        // The panes are told what to look for by name, which is how a layout read back from disc
        // finds its contents again.
        DockableLocator = new Dictionary<string, System.Func<IDockable?>>
        {
            [@"DsPagesList"] = () => DsPagesListTool,
            [@"DsShapesList"] = () => DsShapesListTool,
            [@"DrawingDsShapes"] = () => DrawingDsShapesTool,
            [@"Drawings"] = () => DocumentDock,
            [@"Lists"] = () => ToolDock
        };

        base.InitLayout(layout);
    }

    #endregion
}
