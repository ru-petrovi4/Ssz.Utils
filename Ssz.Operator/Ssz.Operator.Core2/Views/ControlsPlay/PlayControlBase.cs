using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
//using Ssz.Utils.Wpf;

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

        //public EventWaitHandle DsPageDrawingIsLoadedEventWaitHandle => _dsPageDrawingIsLoadedEventWaitHandle;
        public SemaphoreSlim DsPageDrawingIsLoadedSemaphoreSlim { get; } = new SemaphoreSlim(0);

        public DsPageDrawing? DsPageDrawing
        {
            get => _dsPageDrawing;
            protected set
            {
                _dsPageDrawing = value;
                if (_dsPageDrawing is not null)
                    DsPageDrawingIsLoadedSemaphoreSlim.Release();                
                //if (_dsPageDrawing is null)
                //    _dsPageDrawingIsLoadedEventWaitHandle.Reset();
                //else
                //    _dsPageDrawingIsLoadedEventWaitHandle.Set();
            }
        }

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

        public virtual PixelRect? GetChildWindowsPixelRect(string frameName)
        {
            PixelRect? result;
            Control? control = null;
            if (string.IsNullOrEmpty(frameName))
                control = TreeHelper.FindChild<FrameDsShapeView>(this,
                    fsv => string.IsNullOrEmpty(fsv.FrameName) && fsv.IsVisible && fsv.IsEnabled);
            else
                control = TreeHelper.FindChild<FrameDsShapeView>(this,
                    fsv => StringHelper.CompareIgnoreCase(fsv.FrameName, frameName) && fsv.IsVisible && fsv.IsEnabled);
            if (control is not null)
            {                
                if (!double.IsNaN(control.Bounds.Width) &&
                        !double.IsNaN(control.Bounds.Height))
                    result = ScreenHelper.GetPixelRect(control);
                else
                    result = null;                
            }
            else
            {                
                if (!double.IsNaN(Bounds.Width) &&
                        !double.IsNaN(Bounds.Height))
                    result = ScreenHelper.GetPixelRect(this);
                else
                    result = null;                
            }
            return result;
        }

        public PixelRect? GetControlPixelRect(DsShapeBase? dsShapeBase)
        {
            PixelRect? result = null;
            if (dsShapeBase is not null)
            {
                Control? control = TreeHelper.FindChild<DsShapeViewBase>(this,
                    sv => ReferenceEquals(sv.DsShapeViewModel.DsShape, dsShapeBase));
                if (control is not null)
                {
                    if (!double.IsNaN(control.Bounds.Width) &&
                            !double.IsNaN(control.Bounds.Height))
                        result = ScreenHelper.GetPixelRect(control);
                }
            }
            return result;
        }

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        #region private fields

        private DsPageDrawing? _dsPageDrawing;

        //private readonly ManualResetEvent _dsPageDrawingIsLoadedEventWaitHandle = new ManualResetEvent(false);

        #endregion        
    }    
}