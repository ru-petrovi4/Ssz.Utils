using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ssz.Operator.Core.Constants;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection
{
    public class DsConstantsCollectionViewModel
    {
        #region private fields

        private readonly List<DsConstant> _sourceCollectionCopy = new();

        #endregion

        #region construction and destruction

        public DsConstantsCollectionViewModel(ObservableCollection<DsConstant> sourceCollection)
        {
            SourceCollection = sourceCollection;
            EditedCollection = new ObservableCollection<DsConstantViewModel>();

            InitializeCollections();
        }

        #endregion

        #region private functions

        private void InitializeCollections()
        {
            _sourceCollectionCopy.Clear();
            EditedCollection.Clear();
            foreach (DsConstant dsConstant in SourceCollection)
            {
                _sourceCollectionCopy.Add(new DsConstant(dsConstant));
                EditedCollection.Add(new DsConstantViewModel(new DsConstant(dsConstant)));
            }
        }

        #endregion

        #region public fields

        public ObservableCollection<DsConstant> SourceCollection { get; }

        public ObservableCollection<DsConstantViewModel> EditedCollection { get; }

        public void Refresh()
        {
            bool equals;

            if (_sourceCollectionCopy.Count != SourceCollection.Count)
            {
                equals = false;
            }
            else
            {
                equals = true;
                for (var i = 0; i < _sourceCollectionCopy.Count; i += 1)
                {
                    var copy = _sourceCollectionCopy[i];
                    if (!copy.Equals(SourceCollection[i]))
                    {
                        equals = false;
                        break;
                    }
                }
            }

            if (!equals)
            {
                InitializeCollections();
                return;
            }

            equals = ConstantsHelper.UpdateDsConstants(SourceCollection, EditedCollection.Where(vm =>
                !string.IsNullOrWhiteSpace(vm.Name) && !vm.IsEmpty()).Select(vm => vm.DsConstant).ToArray());
            if (!equals)
            {
                _sourceCollectionCopy.Clear();
                foreach (DsConstant dsConstant in SourceCollection)
                    _sourceCollectionCopy.Add(new DsConstant(dsConstant));
            }
        }

        #endregion
    }
}