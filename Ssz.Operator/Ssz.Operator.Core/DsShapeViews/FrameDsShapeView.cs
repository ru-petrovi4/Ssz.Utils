using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Utils.MonitoredUndo;
using static Ssz.Operator.Core.ControlsPlay.PlayControlWrapper;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class FrameDsShapeView : DsShapeViewBase, ISupportsUndo
    {
        #region construction and destruction

        public FrameDsShapeView(FrameDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            if (VisualDesignMode)
            {
                DesignModeTextBlock = new TextBlock();
                var border = new Border();
                border.Opacity = 0.3;
                border.BorderThickness = new Thickness(5);
                border.BorderBrush = new SolidColorBrush(Colors.White);
                border.Background = new SolidColorBrush(Colors.Gray);
                border.Padding = new Thickness(5);
                border.Child =
                    new Viewbox
                    {
                        Stretch = Stretch.Uniform,
                        Child = DesignModeTextBlock
                    };
                Content = border;
            }            
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UndoService.Current.Clear(this);

                var disposable = Content as IDisposable;
                if (disposable is not null) disposable.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion                        

        #region public functions

        public string FrameName { get; private set; } = @"";

        public string StartDsPageFileRelativePath { get; private set; } = @"";

        public DsPageDrawing? DsPageDrawing { get; private set; }

        public JumpInfo? CurrentJumpInfo
        {
            get => _currentJumpInfo;
            set
            {
                DefaultChangeFactory.Instance.OnChanging(this, nameof(CurrentJumpInfo),
                    _currentJumpInfo, value);
                _currentJumpInfo = value;
                OnCurrentJumpInfoChanged();
            }
        }

        public void JumpBack()
        {
            UndoService.Current[this].Undo();
        }

        public void JumpForward()
        {
            UndoService.Current[this].Redo();
        }

        public object GetUndoRoot()
        {
            return this;
        }

        #endregion

        #region protected functions

        protected TextBlock? DesignModeTextBlock { get; }

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (FrameDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.FrameName))
            {
                FrameName = ConstantsHelper.ComputeValue(dsShape.Container, dsShape.FrameName)!;
                if (VisualDesignMode)
                {
                    if (DesignModeTextBlock is null) throw new InvalidOperationException();
                    DesignModeTextBlock.Text = @"Frame: " + (!string.IsNullOrEmpty(FrameName) ? FrameName : @"<Main>");
                }
            }
            if (propertyName is null || propertyName == nameof(dsShape.StartDsPageFileRelativePath))
            {
                StartDsPageFileRelativePath = ConstantsHelper.ComputeValue(dsShape.Container, dsShape.StartDsPageFileRelativePath)!;
                if (!VisualDesignMode)
                {
                    if (!string.IsNullOrEmpty(StartDsPageFileRelativePath))
                    {                       
                        _currentJumpInfo = new JumpInfo(
                            StartDsPageFileRelativePath,
                            PlayDsProjectView.GetGenericContainerCopy(dsShape.ParentItem));
                        OnCurrentJumpInfoChanged();
                    }
                }
            }
        }

        #endregion

        #region private functions

        private void OnCurrentJumpInfoChanged()
        {
            if (_currentJumpInfo is null) 
                return;

            var playWindow = Frame is not null ? Frame.PlayWindow : null;

            DsPageDrawing? dsPageDrawing = DsProject.Instance.ReadDsPageInPlay(_currentJumpInfo.FileRelativePath,
                    _currentJumpInfo.SenderContainerCopy, playWindow);

            if (dsPageDrawing is not null)
            {
                DsPageDrawing = dsPageDrawing;

                var disposable = Content as IDisposable;
                if (disposable is not null) disposable.Dispose();

                if (playWindow is not null)
                    Content = new PlayDsPageDrawingViewbox(dsPageDrawing,
                        new ControlsPlay.Frame(playWindow, FrameName));
            }
        }

        #endregion

        #region private fields

        private JumpInfo? _currentJumpInfo;

        #endregion
    }
}