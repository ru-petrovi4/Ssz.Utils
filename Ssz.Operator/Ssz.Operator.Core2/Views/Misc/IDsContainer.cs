using System.Collections.ObjectModel;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core
{
    public interface IDsContainer : IDsItem
    {
        ObservableCollection<DsConstant> DsConstantsCollection { get; }

        DsConstant[]? HiddenDsConstantsCollection { get; }

        DsShapeBase[] DsShapes { get; set; }

        IPlayWindowBase? PlayWindow { get; }
    }
}