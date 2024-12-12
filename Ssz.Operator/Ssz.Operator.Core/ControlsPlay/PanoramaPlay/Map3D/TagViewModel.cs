using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay.Map3D
{
    public class TagViewModel : ViewModelBase
    {
        #region private fields

        private string _valueToDisplay;

        #endregion

        #region construction and destruction

        public TagViewModel(string tag, string dsPageName)
        {
            Tag = tag;
            DsPageName = dsPageName;
            _valueToDisplay = tag + "\t(Point: " + dsPageName + ")";

            /*
            var genericDataEngine = DsProject.Instance.DataEngineObject as GenericDataEngines;
            if (genericDataEngine is not null)
            {
                if (!genericDataEngine.TagDescriptionInfo.IsConst)
                {
                    var id = genericDataEngine.TagDescriptionInfo.DataBindingItemsCollection[0].IdString;
                    id = id.Replace(DataEngineBase.ConstantConst, tag);
                    DataAccessProvider.Instance.ReadConstItem(id, any =>
                    {
                        var desc = any.ValueAsString(false);
                        ValueToDisplay = tag +
                                         ((String.IsNullOrWhiteSpace(desc) || Tag.Contains(desc))
                                             ? ""
                                             : "\t[" + desc + "]");
                    });
                }
            }*/
        }

        #endregion

        #region public functions

        public string Tag { get; }

        public string DsPageName { get; }

        public string ValueToDisplay
        {
            get => _valueToDisplay;
            set => SetValue(ref _valueToDisplay, value);
        }

        #endregion
    }
}