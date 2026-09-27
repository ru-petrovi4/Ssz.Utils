using System;
using Avalonia.Media;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Design.Properties;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.VisualEditors;

namespace Ssz.Operator.Core.Design.Controls
{
    /// <summary>
    ///     One drawing in a list of the editor. The hint is the coloured badge in front of the name that
    ///     warns about a drawing this build cannot fully read.
    /// </summary>
    public class DrawingInfoViewModel : EntityInfoViewModel
    {
        #region construction and destruction

        public DrawingInfoViewModel(DrawingInfo drawingInfo) :
            base(drawingInfo)
        {
        }

        #endregion

        #region public functions

        public DrawingInfo DrawingInfo => (DrawingInfo) EntityInfo;

        public string? HintText
        {
            get => _hintText;
            set => SetProperty(ref _hintText, value);
        }

        public IBrush? HintBackground
        {
            get => _hintBackground;
            set => SetProperty(ref _hintBackground, value);
        }

        #endregion

        #region protected functions

        protected override void OnEntityInfoChanged()
        {
            base.OnEntityInfoChanged();

            if (DrawingInfo.SerializationVersionDateTime > DrawingBase.CurrentSerializationVersionDateTime)
            {
                HintText = " ! ";
                HintBackground = Brushes.Red;
                ToolTip = Resources.FileSavedInNewerVersionOfDesign;
            }
            else
            {
                string[] unSupportedAddonsNameToDisplays =
                    AddonsManager.GetNotInAddonsCollection(DrawingInfo.ActuallyUsedAddonsInfo);
                if (unSupportedAddonsNameToDisplays.Length > 0)
                {
                    HintText = " ! ";
                    HintBackground = Brushes.OrangeRed;
                    ToolTip = Resources.UnSupportedAddons + ": " + String.Join(",", unSupportedAddonsNameToDisplays);
                }
                else
                {
                    HintText = "";
                    HintBackground = Brushes.DeepSkyBlue;
                }
            }
        }

        #endregion

        #region private fields

        private string? _hintText;
        private IBrush? _hintBackground;

        #endregion
    }
}
