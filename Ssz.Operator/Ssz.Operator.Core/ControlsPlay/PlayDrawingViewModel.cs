using System;
using Ssz.Operator.Core.Drawings;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay
{
    public class PlayDrawingViewModel : DisposableViewModelBase
    {
        #region private fields

        private bool _drawingUpdatingIsEnabled;

        #endregion

        #region construction and destruction

        public PlayDrawingViewModel(DrawingBase drawing)
        {
            Drawing = drawing;
        }


        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                DrawingUpdatingIsEnabled = false;
                Drawing.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public DrawingBase Drawing { get; }


        public bool DrawingUpdatingIsEnabled
        {
            get => _drawingUpdatingIsEnabled;
            set => SetValue(ref _drawingUpdatingIsEnabled, value);
        }

        #endregion
    }
}