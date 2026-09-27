using Dock.Model.Mvvm.Controls;
using Ssz.Operator.Core.ControlsDesign;

namespace Ssz.Operator.Core.Design.Controls.Docking;

/// <summary>
///     One opened drawing as a document of the docking layout.
///     <para>
///         Ported from the WPF editor, where AvalonDock showed the same drawings as its documents.
///         The drawing itself is the view model the rest of the editor already works with; this only
///         says where it sits.
///     </para>
/// </summary>
public class DrawingDocument : Document
{
    #region construction and destruction

    public DrawingDocument(DesignDrawingViewModel drawingViewModel)
    {
        DrawingViewModel = drawingViewModel;

        Id = drawingViewModel.Drawing.FileFullName;
        Title = drawingViewModel.Title;

        drawingViewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(DesignDrawingViewModel.Title))
                Title = drawingViewModel.Title;
        };
    }

    #endregion

    #region public functions

    public DesignDrawingViewModel DrawingViewModel { get; }

    #endregion
}

/// <summary>
///     The list of the pages of the project, as a pane of the layout.
/// </summary>
public class DsPagesListTool : Tool
{
}

/// <summary>
///     The list of the complex shapes of the project, as a pane of the layout.
/// </summary>
public class DsShapesListTool : Tool
{
}

/// <summary>
///     The shapes of the drawing being edited, as a pane of the layout.
/// </summary>
public class DrawingDsShapesTool : Tool
{
}
