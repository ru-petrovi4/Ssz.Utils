using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.Utils;
using Avalonia.Threading;
//using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay
{
    public abstract class FaceplatePlayControlBase : PlayControlBase
    {
        #region protected functions

        protected abstract PlayDsPageDrawingViewbox PlayDsPageDrawingViewbox { get; set; }

        #endregion

        #region public functions

        public override async void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
        {                    
            var dsPageDrawing = await DsProject.Instance.ReadDsPageInPlayAsync(
                jumpInfo.FileRelativePath,                 
                jumpInfo.SenderContainerCopy, 
                PlayWindow);            
            if (dsPageDrawing is null)
                return;

            DsPageDrawing = dsPageDrawing;

            await Task.Delay(1);
            
            PlayDsPageDrawingViewbox?.Dispose();

            PlayDsPageDrawingViewbox =
                new PlayDsPageDrawingViewbox(DsPageDrawing, PlayWindow.MainFrame);            
        }

        #endregion

        #region private functions

        private void OnGlobalUITimerEvent(int phase)
        {
            if (!PlayWindow.IsActive || phase != 0) 
                return;

            var withContextMenuOpen =
                TreeHelper.FindChildsOrSelf<Control>(this,
                    fe => fe.ContextMenu is not null && fe.ContextMenu.IsOpen).FirstOrDefault();
            if (withContextMenuOpen is not null) 
                return;

            Control? withDropDownOpen =
                TreeHelper.FindChildsOrSelf<ComboBox>(this,
                    fe => fe.IsDropDownOpen).FirstOrDefault();
            if (withDropDownOpen is not null) 
                return;

            //WindowsFormsHost? windowsFormsHost =
            //    TreeHelper.FindChildsOrSelf<WindowsFormsHost>(this).FirstOrDefault();
            //if (windowsFormsHost is not null)
            //    return;

            Dispatcher.UIThread.InvokeAsync(new Action(() =>
            {
                TextBoxDsShapeView[] textBoxDsShapeViews =
                    TreeHelper.FindChilds<TextBoxDsShapeView>(Content as Control,
                            sv => sv.Control.IsEnabled && !sv.Control.IsReadOnly)
                        .ToArray();
                if (textBoxDsShapeViews.Length == 0) return;
                var isAnyKeyboardFocused = false;
                foreach (TextBoxDsShapeView textBoxDsShapeView in textBoxDsShapeViews)
                    if (textBoxDsShapeView.Control.IsFocused)
                    {
                        _lastKeyboardFocusedTextBoxDsShapeView = textBoxDsShapeView;
                        isAnyKeyboardFocused = true;
                    }

                if (!isAnyKeyboardFocused && Content is Visual visual)
                    isAnyKeyboardFocused = TreeHelper.FindChild<InputElement>(visual, e => e.IsFocused) is not null;

                if (!isAnyKeyboardFocused)
                {
                    TextBoxDsShapeView textBoxDsShapeView;
                    if (_lastKeyboardFocusedTextBoxDsShapeView is not null &&
                        textBoxDsShapeViews.Contains(_lastKeyboardFocusedTextBoxDsShapeView))
                        textBoxDsShapeView = _lastKeyboardFocusedTextBoxDsShapeView;
                    else
                        textBoxDsShapeView = textBoxDsShapeViews[0];
                    textBoxDsShapeView.Control.Focus();
                    textBoxDsShapeView.Control.SelectAll();
                }
            }));
        }

        #endregion

        #region construction and destruction

        protected FaceplatePlayControlBase(IPlayWindow playWindow) :
            base(playWindow)
        {
            DsProject.Instance.GlobalUITimerEvent += OnGlobalUITimerEvent;
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
            {
                // Release and Dispose managed resources.

                DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEvent;

                var disposable = PlayDsPageDrawingViewbox as IDisposable;
                if (disposable is not null) disposable.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region private fields

        private TextBoxDsShapeView? _lastKeyboardFocusedTextBoxDsShapeView;

        #endregion
    }
}