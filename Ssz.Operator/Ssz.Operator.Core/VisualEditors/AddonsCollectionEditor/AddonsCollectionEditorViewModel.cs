using System.Collections.ObjectModel;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.VisualEditors.AddonsCollectionEditor
{
    internal class AddonsCollectionEditorViewModel : ViewModelBase
    {
        #region public functions

        public ObservableCollection<AddonViewModel> ItemsSource { get; } = new();

        #endregion

        #region private fields

        #endregion
    }
}