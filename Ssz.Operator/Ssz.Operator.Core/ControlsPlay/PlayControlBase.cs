using System;
using System.Windows;
using System.Windows.Controls;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay
{
    public class PlayControlBase : UserControl, IDisposable
    {
        #region construction and destruction

        protected PlayControlBase(IPlayWindow playWindow)
        {
            PlayWindow = playWindow;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Disposed = true;
        }

        ~PlayControlBase()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public IPlayWindow PlayWindow { get; }

        public DsPageDrawing? DsPageDrawing { get; protected set; }

        public virtual void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
        {
        }

        public virtual bool Jump(JumpInfo jumpInfo)
        {
            return false;
        }

        public virtual bool PrepareWindow(IPlayWindow newWindow,
            ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            return false;
        }

        public virtual bool PrepareChildWindow(IPlayWindow newChildWindow,
            ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            return false;
        }

        public virtual Rect GetChildWindowsArea(string frameName)
        {
            Rect result;
            FrameworkElement? frameworkElement = null;
            if (string.IsNullOrEmpty(frameName))
                frameworkElement = TreeHelper.FindChild<FrameDsShapeView>(this,
                    fsv => string.IsNullOrEmpty(fsv.FrameName) && fsv.IsVisible && fsv.IsEnabled);
            else
                frameworkElement = TreeHelper.FindChild<FrameDsShapeView>(this,
                    fsv => StringHelper.CompareIgnoreCase(fsv.FrameName, frameName) && fsv.IsVisible && fsv.IsEnabled);
            if (frameworkElement is not null)
            {                
                if (!double.IsNaN(frameworkElement.ActualWidth) &&
                    !double.IsNaN(frameworkElement.ActualHeight))
                    result = ScreenHelper.GetRect(frameworkElement);
                else
                    result = Rect.Empty;                
            }
            else
            {                
                if (!double.IsNaN(ActualWidth) &&
                    !double.IsNaN(ActualHeight))
                    result = ScreenHelper.GetRect(this);
                else
                    result = Rect.Empty;                
            }
            return result;
        }

        public Rect GetControlArea(DsShapeBase? dsShapeBase)
        {
            Rect result = Rect.Empty;
            if (dsShapeBase is not null)
            {
                FrameworkElement? frameworkElement = TreeHelper.FindChild<DsShapeViewBase>(this,
                    sv => ReferenceEquals(sv.DsShapeViewModel.DsShape, dsShapeBase));
                if (frameworkElement is not null)
                {                    
                    if (!double.IsNaN(frameworkElement.ActualWidth) &&
                            !double.IsNaN(frameworkElement.ActualHeight))
                        result = ScreenHelper.GetRect(frameworkElement);                    
                }
            }
            return result;
        }

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion
    }    
}