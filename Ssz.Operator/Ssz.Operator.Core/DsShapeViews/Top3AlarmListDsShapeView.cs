using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class Top3AlarmListDsShapeView : ControlDsShapeView<GenericAlarmListControl>
    {
        #region construction and destruction

        public Top3AlarmListDsShapeView(Top3AlarmListDsShape dsShape, Frame? frame)
            : base(
                new GenericAlarmListControl(DsProject.Instance.GetAddon<GenericEmulationAddon>().AlarmsListViewModel
                    .Last5UnackedAlarms), dsShape,
                frame)
        {
        }

        #endregion
    }
}