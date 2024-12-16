using System;
using System.Windows.Media;
using Ssz.Operator.Core;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.VisualEditors;
using Ssz.Operator.Design.Properties;

namespace Ssz.Operator.Design.Controls
{
    public class DrawingInfoViewModel : EntityInfoViewModel
    {
        #region construction and destruction

        public DrawingInfoViewModel(DrawingInfo drawingInfo) :
            base(drawingInfo)
        {
        }

        #endregion

        #region public functions

        public DrawingInfo DrawingInfo
        {
            get { return (DrawingInfo) EntityInfo; }
        }

        public string? HintText
        {
            get { return _hintText; }
            set { SetValue(ref _hintText, value); }
        }

        public Brush? HintBackground
        {
            get { return _hintBackground; }
            set { SetValue(ref _hintBackground, value); }
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
                    AddonsHelper.GetNotInAddonsCollection(DrawingInfo.ActuallyUsedAddonsInfo);
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
        private Brush? _hintBackground;        

        #endregion
    }
}