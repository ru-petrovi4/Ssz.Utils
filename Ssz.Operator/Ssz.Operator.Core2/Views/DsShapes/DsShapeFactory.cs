using Ssz.Operator.Core.Addons;
using System;
using System.Collections.Generic;

namespace Ssz.Operator.Core.DsShapes
{
    public static class DsShapeFactory
    {
        #region private fields

        private static readonly Dictionary<Guid, Func<bool, bool, DsShapeBase>> CommonDsShapes = new()
        {
            {
                EmptyComplexDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new EmptyComplexDsShape(visualDesignMode, loadXamlContent)
            },
            {
                ComplexDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new ComplexDsShape(visualDesignMode, loadXamlContent)
            },
            {
                ContentDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new ContentDsShape(visualDesignMode, loadXamlContent)
            },
            {
                TextBlockDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new TextBlockDsShape(visualDesignMode, loadXamlContent)
            },
            {
                UpDownDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new UpDownDsShape(visualDesignMode, loadXamlContent)
            },
            {
                ButtonDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new ButtonDsShape(visualDesignMode, loadXamlContent)
            },
            {
                ContentButtonDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new ContentButtonDsShape(visualDesignMode, loadXamlContent)
            },
            {
                GeometryButtonDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new GeometryButtonDsShape(visualDesignMode, loadXamlContent)
            },
            {
                TextBlockButtonDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new TextBlockButtonDsShape(visualDesignMode, loadXamlContent)
            },
            {
                ToggleButtonDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new ToggleButtonDsShape(visualDesignMode, loadXamlContent)
            },
            {
                TextBoxDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new TextBoxDsShape(visualDesignMode, loadXamlContent)
            },
            {
                SliderDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new SliderDsShape(visualDesignMode, loadXamlContent)
            },
            {
                GeometryDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new GeometryDsShape(visualDesignMode, loadXamlContent)
            },
            {
                MapDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new MapDsShape(visualDesignMode, loadXamlContent)
            },
            {
                CommandListenerDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new CommandListenerDsShape(visualDesignMode, loadXamlContent)
            },
            {
                ContextMenuDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new ContextMenuDsShape(visualDesignMode, loadXamlContent)
            },
            {
                ComboBoxDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new ComboBoxDsShape(visualDesignMode, loadXamlContent)
            },
            {
                TabDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new TabDsShape(visualDesignMode, loadXamlContent)
            },
            {
                VarComboBoxDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new VarComboBoxDsShape(visualDesignMode, loadXamlContent)
            },
            {
                EditableComboBoxDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new EditableComboBoxDsShape(visualDesignMode, loadXamlContent)
            },
            {
                TrendGroupDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new TrendGroupDsShape(visualDesignMode, loadXamlContent)
            },
            {
                ChartDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new ChartDsShape(visualDesignMode, loadXamlContent)
            },
            {
                MultiChartDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new MultiChartDsShape(visualDesignMode, loadXamlContent)
            },
            {
                FrameDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new FrameDsShape(visualDesignMode, loadXamlContent)
            },
            {
                WindowDragDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new WindowDragDsShape(visualDesignMode, loadXamlContent)
            },
            {
                BrowserDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new BrowserDsShape(visualDesignMode, loadXamlContent)
            },
            {
                AlarmListDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new AlarmListDsShape(visualDesignMode, loadXamlContent)
            },
            {
                Top3AlarmListDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new Top3AlarmListDsShape(visualDesignMode, loadXamlContent)
            },
            {
                ConnectionPointDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new ConnectionPointDsShape(visualDesignMode, loadXamlContent)
            },
            {
                ConnectorDsShape.DsShapeTypeGuid,
                (visualDesignMode, loadXamlContent) => new ConnectorDsShape(visualDesignMode, loadXamlContent)
            }
        };

        #endregion

        #region public functions

        public static DsShapeBase? NewDsShape(Guid dsShapeTypeGuid, bool visualDesignMode, bool loadXamlContent)
        {
            if (dsShapeTypeGuid == EmptyDsShape.DsShapeTypeGuid)
                return new EmptyDsShape(visualDesignMode, loadXamlContent);

            Func<bool, bool, DsShapeBase>? factory;
            if (CommonDsShapes.TryGetValue(dsShapeTypeGuid, out factory))
                return factory(visualDesignMode, loadXamlContent);

            // try to dynamically load dsShape from addons
            return AddonsManager.NewDsShape(dsShapeTypeGuid, visualDesignMode, loadXamlContent);
        }

        #endregion
    }
}