using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class GenericContainer : IDsContainer
    {
        #region public functions

        public ObservableCollection<DsConstant> DsConstantsCollection => _dsConstantsCollection;

        public DsConstant[]? HiddenDsConstantsCollection => null;

        public DsShapeBase[] DsShapes
        {
            get => _dsShapes;
            set
            {                
            }
        }

        public IDsItem? ParentItem { get; set; }

        public IPlayWindowBase? PlayWindow { get; set; }

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(this);
        }

        public void EndEditInPropertyGrid()
        {
        }

        public void ReplaceConstants(IDsContainer? container)
        {
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public void FindConstants(HashSet<string> constants)
        {
        }

        #endregion

        #region private fields       

        [Searchable(false)] private readonly ObservableCollection<DsConstant> _dsConstantsCollection = new();

        [Searchable(false)] private readonly DsShapeBase[] _dsShapes = new DsShapeBase[0];

        #endregion
    }
}