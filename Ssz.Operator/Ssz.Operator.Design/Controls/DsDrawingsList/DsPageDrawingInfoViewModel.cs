using System;
using System.Windows.Media;
using Ssz.Operator.Core;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.VisualEditors;
using Ssz.Operator.Design.Properties;

namespace Ssz.Operator.Design.Controls
{
    public class DsPageDrawingInfoViewModel : DrawingInfoViewModel
    {
        #region construction and destruction

        public DsPageDrawingInfoViewModel(DsPageDrawingInfo dsPageDrawingInfo) :
            base(dsPageDrawingInfo)
        {
        }

        #endregion

        #region public functions
         
        public DsPageDrawingInfo DsPageDrawingInfo { get { return (DsPageDrawingInfo)DrawingInfo; } }

        public bool IsStartDsPage
        {
            get { return _isStartDsPage; }
            set
            {
                if (SetValue(ref _isStartDsPage, value))
                    OnEntityInfoChanged();
            }
        }

        public Brush? MarkBrush
        {
            get { return _markBrush; }
            set { SetValue(ref _markBrush, value); }
        }

        public bool IsExpanded { get; set; }

        /// <summary>
        ///     Number in DsPages Tree
        /// </summary>
        public int Number { get; set; }

        #endregion

        #region protected functions

        protected override void OnEntityInfoChanged()
        {
            base.OnEntityInfoChanged();
            
            if (DsPageDrawingInfo.DsPageTypeObject is not null)
            {
                string hint = DsPageDrawingInfo.DsPageTypeObject.Hint;
                if (!String.IsNullOrWhiteSpace(hint))
                {
                    Header += @": " + hint;
                    if (!String.IsNullOrEmpty(ToolTip)) ToolTip += "\n";
                    ToolTip += hint;
                }
            }            

            if (IsStartDsPage)
            {
                if (!String.IsNullOrEmpty(HintText)) HintText += " ";
                HintText += ">";

                if (!String.IsNullOrEmpty(ToolTip)) ToolTip += "\n";
                ToolTip += Resources.DsProjectStartDsPageToolTip;
            }
            
            switch (DrawingInfo.Mark)
            {
                case 1:
                    MarkBrush = new SolidColorBrush(Colors.Red);
                    break;
                case 2:
                    MarkBrush = new SolidColorBrush(Colors.Orange);
                    break;
                case 3:
                    MarkBrush = new SolidColorBrush(Colors.YellowGreen);
                    break;
                case 4:
                    MarkBrush = new SolidColorBrush(Colors.Blue);
                    break;
                case 5:
                    MarkBrush = new SolidColorBrush(Colors.Magenta);
                    break;
                case 6:
                    MarkBrush = new SolidColorBrush(Colors.Black);
                    break;
                default:
                    MarkBrush = null;
                    break;
            }
        }

        #endregion

        #region private fields

        private bool _isStartDsPage;
        private Brush? _markBrush;

        #endregion
    }
}