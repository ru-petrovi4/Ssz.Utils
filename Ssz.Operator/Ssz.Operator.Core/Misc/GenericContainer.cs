using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core
{
    public class GenericContainer : IDsContainer
    {
        #region private fields

        #endregion

        #region public functions

        public ObservableCollection<DsConstant> DsConstantsCollection { get; } = new();

        public DsConstant[]? HiddenDsConstantsCollection => null;

        public DsShapeBase[] DsShapes
        {
            get => new DsShapeBase[0];
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
    }
}