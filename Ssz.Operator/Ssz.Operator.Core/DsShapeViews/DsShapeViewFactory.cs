using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.DsShapeViews
{
    public static class DsShapeViewFactory
    {
        #region private fields

        private static readonly Dictionary<Guid, Func<DsShapeBase, Frame?, DsShapeViewBase>>
            CommonDsShapeViews = new()
            {
                {
                    ComplexDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new ComplexDsShapeView((ComplexDsShape) dsShape, frame)
                },
                {
                    ContentDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new ContentDsShapeView((ContentDsShape) dsShape, frame)
                },
                {
                    TextBlockDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new TextBlockDsShapeView((TextBlockDsShape) dsShape, frame)
                },
                {
                    UpDownDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new UpDownDsShapeView((UpDownDsShape) dsShape, frame)
                },
                {
                    ButtonDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new ButtonDsShapeView((ButtonDsShape) dsShape, frame)
                },
                {
                    ContentButtonDsShape.DsShapeTypeGuid,
                    (dsShape, frame) =>
                        new ContentButtonDsShapeView((ContentButtonDsShape) dsShape, frame)
                },
                {
                    GeometryButtonDsShape.DsShapeTypeGuid,
                    (dsShape, frame) =>
                        new GeometryButtonDsShapeView((GeometryButtonDsShape) dsShape, frame)
                },
                {
                    TextBlockButtonDsShape.DsShapeTypeGuid,
                    (dsShape, frame) =>
                        new TextBlockButtonDsShapeView((TextBlockButtonDsShape) dsShape, frame)
                },
                {
                    ToggleButtonDsShape.DsShapeTypeGuid,
                    (dsShape, frame) =>
                        new ToggleButtonDsShapeView((ToggleButtonDsShape) dsShape, frame)
                },
                {
                    TextBoxDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new TextBoxDsShapeView((TextBoxDsShape) dsShape, frame)
                },
                {
                    SliderDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new SliderDsShapeView((SliderDsShape) dsShape, frame)
                },
                {
                    GeometryDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new GeometryDsShapeView((GeometryDsShape) dsShape, frame)
                },
                {
                    MapDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new MapDsShapeView((MapDsShape) dsShape, frame)
                },
                {
                    CommandListenerDsShape.DsShapeTypeGuid,
                    (dsShape, frame) =>
                        new CommandListenerDsShapeView((CommandListenerDsShape) dsShape, frame)
                },
                {
                    ContextMenuDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new ContextMenuDsShapeView((ContextMenuDsShape) dsShape, frame)
                },
                {
                    ComboBoxDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new ComboBoxDsShapeView((ComboBoxDsShape) dsShape, frame)
                },
                {
                    TabDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new TabDsShapeView((TabDsShape) dsShape, frame)
                },
                {
                    VarComboBoxDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new VarComboBoxDsShapeView((VarComboBoxDsShape) dsShape, frame)
                },
                {
                    EditableComboBoxDsShape.DsShapeTypeGuid,
                    (dsShape, frame) =>
                        new EditableComboBoxDsShapeView((EditableComboBoxDsShape) dsShape, frame)
                },
                {
                    TrendGroupDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new TrendGroupDsShapeView((TrendGroupDsShape) dsShape, frame)
                },
                {
                    ChartDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new ChartDsShapeView((ChartDsShape) dsShape, frame)
                },
                {
                    MultiChartDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new MultiChartDsShapeView((MultiChartDsShape) dsShape, frame)
                },
                {
                    FrameDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new FrameDsShapeView((FrameDsShape) dsShape, frame)
                },
                {
                    WindowDragDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new WindowDragDsShapeView((WindowDragDsShape) dsShape, frame)
                },
                {
                    BrowserDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new BrowserDsShapeView((BrowserDsShape) dsShape, frame)
                },
                {
                    AlarmListDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new AlarmListDsShapeView((AlarmListDsShape) dsShape, frame)
                },
                {
                    Top3AlarmListDsShape.DsShapeTypeGuid,
                    (dsShape, frame) =>
                        new Top3AlarmListDsShapeView((Top3AlarmListDsShape) dsShape, frame)
                },
                {
                    ComplexDsShapeCenterPointDsShape.DsShapeTypeGuid,
                    (dsShape, frame) =>
                        new ComplexDsShapeCenterPointDsShapeView((ComplexDsShapeCenterPointDsShape) dsShape,
                            frame)
                },
                {
                    ConnectionPointDsShape.DsShapeTypeGuid,
                    (dsShape, frame) =>
                        new ConnectionPointDsShapeView((ConnectionPointDsShape) dsShape, frame)
                },
                {
                    ConnectorDsShape.DsShapeTypeGuid,
                    (dsShape, frame) => new ConnectorDsShapeView((ConnectorDsShape) dsShape, frame)
                }
            };

        #endregion

        #region public functions

        public static DsShapeViewBase? New(DsShapeBase dsShape, Frame? frame)
        {
            DsShapeViewBase? newDsShapeView = null;

            try
            {
                newDsShapeView = NewInternal(dsShape, frame);
                if (newDsShapeView is null)
                    DsProject.LoggersSet.Logger.LogCritical(
                        Resources.CannotCreateDsShapeView + @" " + dsShape.GetDsShapeTypeNameToDisplay());
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogCritical(ex,
                    Resources.CannotCreateDsShapeView + @" " + dsShape.GetDsShapeTypeNameToDisplay());
            }

            return newDsShapeView;
        }

        #endregion

        #region private functions

        private static DsShapeViewBase? NewInternal(DsShapeBase dsShape, Frame? frame)
        {
            var dsShapeTypeGuid = dsShape.GetDsShapeTypeGuid();

            Func<DsShapeBase, Frame?, DsShapeViewBase>? factory;
            if (CommonDsShapeViews.TryGetValue(dsShapeTypeGuid, out factory)) return factory(dsShape, frame);

            // try to dynamically load dsShape view from addons            
            return AddonsHelper.NewDsShapeView(dsShape, frame);
        }

        #endregion
    }
}