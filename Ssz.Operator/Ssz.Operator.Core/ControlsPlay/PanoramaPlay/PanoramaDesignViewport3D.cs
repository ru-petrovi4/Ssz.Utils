using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.VisualEditors;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    public class PanoramaDesignViewport3D : Viewport3D, IDisposable
    {
        #region construction and destruction

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                DsProject.Instance.DesignModeInPlayOnChanging -= OnDesignModeInPlayOnChanging;
                DsProject.Instance.DesignModeInPlayChanged -= OnDesignModeInPlayChanged;

                // Release and Dispose managed resources.
                if (_panoramaVisual3D is not null)
                    _panoramaVisual3D.Visual = null;
                if (_designerDrawingCanvas is not null)
                {
                    _designerDrawingCanvas.Close();
                    _designerDrawingCanvas = null;
                }

                if (_designerDrawingViewModel is not null)
                {
                    DesignDsProjectViewModel.Instance.CloseDrawing(_designerDrawingViewModel);
                    _designerDrawingViewModel = null;
                }
            }

            // Release unmanaged resources.
            // Set large fields to null.            
            _disposed = true;
        }


        ~PanoramaDesignViewport3D()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public QuaternionRotation3D? PanoramaRotation3D { get; private set; }

        public void Init(PanoramaPlayControl panoramaPlayControl)
        {
            if (!DsProject.Instance.Review &&
                !DsProject.Instance.GetAddon<PanoramaAddon>().AllowDesignModeInPlay)
                return;

            _panoramaPlayControl = panoramaPlayControl;

            CutCommandBinding = new CommandBinding(ApplicationCommands.Cut,
                DesignDsProjectViewModel.Instance.CutExecuted, DesignDsProjectViewModel.Instance.CutEnabled);
            CopyCommandBinding = new CommandBinding(ApplicationCommands.Copy,
                DesignDsProjectViewModel.Instance.CopyExecuted, DesignDsProjectViewModel.Instance.CopyEnabled);
            PasteCommandBinding = new CommandBinding(ApplicationCommands.Paste,
                DesignDsProjectViewModel.Instance.PasteExecuted, DesignDsProjectViewModel.Instance.PasteEnabled);
            DeleteCommandBinding = new CommandBinding(ApplicationCommands.Delete,
                DesignDsProjectViewModel.Instance.DeleteExecuted, DesignDsProjectViewModel.Instance.DeleteEnabled);
            SaveCommandBinding = new CommandBinding(ApplicationCommands.Save, SaveExecuted);
            UndoCommandBinding = new CommandBinding(ApplicationCommands.Undo,
                DesignDsProjectViewModel.Instance.UndoExecuted, DesignDsProjectViewModel.Instance.UndoEnabled);
            RedoCommandBinding = new CommandBinding(ApplicationCommands.Redo,
                DesignDsProjectViewModel.Instance.RedoExecuted, DesignDsProjectViewModel.Instance.RedoEnabled);

            // Camera Initialize
            _camera = new PerspectiveCamera();
            _camera.Position = new Point3D(0, 0, 0);
            _camera.UpDirection = new Vector3D(0, 0, 1);
            _camera.LookDirection = new Vector3D(1, 0, 0);
            Camera = _camera;

            // Light Model Initialize
            var lightModel = new ModelVisual3D();
            lightModel.Content = new DirectionalLight(Colors.White, new Vector3D(1, 0, 0));
            Children.Add(lightModel);

            // PanoramaVisual3D Initialize
            _panoramaVisual3D = new Viewport2DVisual3D();
            _panoramaVisual3D.CacheMode = new BitmapCache(1.0);
            PanoramaRotation3D = new QuaternionRotation3D();

            var rotateTransform = new RotateTransform3D();
            rotateTransform.Rotation = PanoramaRotation3D;
            _panoramaVisual3D.Transform = rotateTransform;
            _panoramaVisual3D.Material = new DiffuseMaterial();
            Viewport2DVisual3D.SetIsVisualHostMaterial(_panoramaVisual3D.Material, true);
            Children.Add(_panoramaVisual3D);

            DsProject.Instance.DesignModeInPlayOnChanging += OnDesignModeInPlayOnChanging;
            DsProject.Instance.DesignModeInPlayChanged += OnDesignModeInPlayChanged;
            OnDesignModeInPlayChanged();
        }

        #endregion

        #region protected functions

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            if (_designerDrawingViewModel is null) throw new InvalidOperationException();
            if (_panoramaPlayControl is null) throw new InvalidOperationException();

            if (_designerDrawingViewModel.SelectionService.IsAnySelected
                || _panoramaPlayControl.CurrentPanoramaViewport3D is null) return;

            _panoramaPlayControl.CurrentPanoramaViewport3D.OnMouseDown(e.GetPosition(this));
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);

            _lastRightButtonClickDesignDrawingCanvasPosition = e.GetPosition(_designerDrawingCanvas);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            if (_panoramaPlayControl is null || _panoramaPlayControl.CurrentPanoramaViewport3D is null) return;
            _panoramaPlayControl.CurrentPanoramaViewport3D.OnMouseUp();
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (_designerDrawingViewModel is null) throw new InvalidOperationException();
            if (_panoramaPlayControl is null) throw new InvalidOperationException();

            if (_designerDrawingViewModel.SelectionService.IsAnySelected) return;

            if (e.LeftButton == MouseButtonState.Released) return;

            if (_panoramaPlayControl is null || _panoramaPlayControl.CurrentPanoramaViewport3D is null) return;
            _panoramaPlayControl.CurrentPanoramaViewport3D.OnMouseMove(e.GetPosition(this));

            SyncQuaternionAndFieldOfView(_panoramaPlayControl.CurrentPanoramaViewport3D);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (_panoramaPlayControl is null || _panoramaPlayControl.CurrentPanoramaViewport3D is null) return;
            _panoramaPlayControl.CurrentPanoramaViewport3D.OnMouseWheel(e.Delta);

            SyncQuaternionAndFieldOfView(_panoramaPlayControl.CurrentPanoramaViewport3D);
        }

        #endregion

        #region private functions

        private void AddCommandBindings(CommandBindingCollection commandBindings)
        {
            commandBindings.Add(CutCommandBinding);
            commandBindings.Add(CopyCommandBinding);
            commandBindings.Add(PasteCommandBinding);
            commandBindings.Add(DeleteCommandBinding);
            commandBindings.Add(SaveCommandBinding);
            commandBindings.Add(UndoCommandBinding);
            commandBindings.Add(RedoCommandBinding);

            DesignDsProjectViewModel.Instance.AddCommandBindings(commandBindings);
        }

        private void RemoveCommandBindings(CommandBindingCollection commandBindings)
        {
            commandBindings.Remove(CutCommandBinding);
            commandBindings.Remove(CopyCommandBinding);
            commandBindings.Remove(PasteCommandBinding);
            commandBindings.Remove(DeleteCommandBinding);
            commandBindings.Remove(SaveCommandBinding);
            commandBindings.Remove(UndoCommandBinding);
            commandBindings.Remove(RedoCommandBinding);

            DesignDsProjectViewModel.Instance.RemoveCommandBindings(commandBindings);
        }

        private void OnDesignModeInPlayOnChanging(bool newValue, DesignModeInPlayOnChangingEventArgs e)
        {
            e.CanHandle = true;
            if (!newValue)
            {
                if (Visibility == Visibility.Hidden) return;

                var cancelled = DesignDsProjectViewModel.Instance.CloseDrawing(_designerDrawingViewModel);
                if (cancelled)
                    e.Cancel = true;
                else
                    _designerDrawingViewModel = null;
            }
        }

        private void OnDesignModeInPlayChanged()
        {
            if (DsProject.Instance.DesignModeInPlay)
            {
                if (Visibility == Visibility.Visible) return;
                Visibility = Visibility.Visible;

                if (_panoramaPlayControl is null || _panoramaPlayControl.CurrentPanoramaViewport3D is null) return;
                _panoramaPlayControl.FirstPanoramaViewport3D.Visibility = Visibility.Hidden;
                _panoramaPlayControl.SecondPanoramaViewport3D.Visibility = Visibility.Hidden;

                if (_panoramaVisual3D is null) return;
                _panoramaVisual3D.Geometry = _panoramaPlayControl.CurrentPanoramaViewport3D.PanoramaVisual3D.Geometry;
                SyncQuaternionAndFieldOfView(_panoramaPlayControl.CurrentPanoramaViewport3D);

                if (_panoramaPlayControl.DsPageDrawing is null) return;
                var designerDrawingBorder = new DesignDrawingBorder();
                _designerDrawingCanvas = designerDrawingBorder.DesignDrawingCanvas;
                var drawing =
                    DsProject.ReadDrawing(new FileInfo(_panoramaPlayControl.DsPageDrawing.FileFullName), true,
                        true) as DsPageDrawing;
                if (drawing is not null)
                {
                    _designerDrawingViewModel = new DesignDrawingViewModel(drawing, 0.0, 0.0);
                    designerDrawingBorder.DataContext = _designerDrawingViewModel;
                    designerDrawingBorder.Padding =
                        _panoramaPlayControl.CurrentPanoramaViewport3D.GetDrawingCanvasPadding(drawing.Width,
                            drawing.Height);
                }
                else
                {
                    designerDrawingBorder.Padding = new Thickness(0);
                }

                _designerDrawingCanvas.Initialize();
                _designerDrawingCanvas.ClipToBounds = true;
                _panoramaVisual3D.Visual = designerDrawingBorder;

                if (_designerDrawingViewModel is not null)
                {
                    _designerDrawingViewModel.Initialize(new DesignControlsInfo(_designerDrawingCanvas));

                    DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel = _designerDrawingViewModel;
                    DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels.Add(_designerDrawingViewModel);
                }

                InvalidateVisual();

                if (drawing is not null)
                {
                    UpdateFileInfoTextbox(drawing);
                    drawing.DataChangedFromLastSaveEvent += () =>
                    {
                        if (drawing.DataChangedFromLastSave)
                        {
                            if (!_panoramaPlayControl.InfoTextBlock.Text.EndsWith(@"*"))
                            {
                                var text = _panoramaPlayControl.InfoTextBlock.Text;
                                _panoramaPlayControl.InfoTextBlock.Text = text + @"*";
                            }
                        }
                        else
                        {
                            if (_panoramaPlayControl.InfoTextBlock.Text.EndsWith(@"*"))
                            {
                                var text = _panoramaPlayControl.InfoTextBlock.Text;
                                _panoramaPlayControl.InfoTextBlock.Text = text.Substring(0, text.Length - 1);
                            }
                        }
                    };
                }

                var disposable = _panoramaPlayControl.Content as IDisposable;
                if (disposable is not null) disposable.Dispose();
                _panoramaPlayControl.Content = _panoramaPlayControl.MainGrid;

                DrawingInfo[] drawingInfos =
                    DsProject.Instance.GetAllComplexDsShapesDrawingInfos().Values.ToArray();
                var contextMenu = new ContextMenu();
                foreach (DrawingInfo di in drawingInfos.OrderBy(di => di.Name))
                {
                    var vm = new EntityInfoViewModel(di);
                    var menuItem = new MenuItem
                    {
                        Icon = new Image
                        {
                            Source = vm.PreviewImage
                        },
                        Header = vm.GetDescOrName()
                    };
                    if (_designerDrawingViewModel is null) throw new InvalidOperationException();
                    menuItem.Click += (sender, args) =>
                        _designerDrawingViewModel.AddDsShape(vm.EntityInfo,
                            _lastRightButtonClickDesignDrawingCanvasPosition);
                    contextMenu.Items.Add(menuItem);
                }

                _designerDrawingCanvas.ContextMenu = contextMenu;

                if (!_panoramaDesignModeMessageShown)
                {
                    _panoramaDesignModeMessageShown = true;
                    MessageBoxHelper.ShowInfo(Properties.Resources.PanoramaPlayDesignModeMessage);
                }

                AddCommandBindings(((Window) _panoramaPlayControl.PlayWindow).CommandBindings);
            }
            else
            {
                if (Visibility == Visibility.Hidden) return;
                Visibility = Visibility.Hidden;

                if (_panoramaPlayControl is null) throw new InvalidOperationException();
                RemoveCommandBindings(((Window) _panoramaPlayControl.PlayWindow).CommandBindings);

                _panoramaPlayControl.FirstPanoramaViewport3D.Visibility = Visibility.Visible;
                _panoramaPlayControl.SecondPanoramaViewport3D.Visibility = Visibility.Visible;

                if (_panoramaVisual3D is null) throw new InvalidOperationException();
                if (_designerDrawingCanvas is null) throw new InvalidOperationException();
                _panoramaVisual3D.Visual = null;
                _designerDrawingCanvas.Close();
                _designerDrawingCanvas = null;

                if (_panoramaPlayControl.DsPageDrawing is null) 
                    return;                

                var dsPageDrawingInfo = DsProject.ReadDrawingInfo(new FileInfo(_panoramaPlayControl.DsPageDrawing.FileFullName), false) as DsPageDrawingInfo;
                if (dsPageDrawingInfo is null)
                    return;

                _panoramaPlayControl.Jump(
                    new JumpInfo(DsProject.Instance.GetFileRelativePath(_panoramaPlayControl.DsPageDrawing.FileFullName), DsProject.Instance), 
                    dsPageDrawingInfo);
            }
        }

        private void UpdateFileInfoTextbox(DrawingBase drawing)
        {
            if (_panoramaPlayControl is null) throw new InvalidOperationException();
            _panoramaPlayControl.InfoTextBlock.Text = Path.GetFileName(drawing.FileFullName) + " " +
                                                      Properties.Resources.PanoramaPlayDesignModeLabel;
        }

        private void SyncQuaternionAndFieldOfView(PanoramaViewport3D panoramaViewport3D)
        {
            if (PanoramaRotation3D is null) throw new InvalidOperationException();
            if (_camera is null) throw new InvalidOperationException();
            PanoramaRotation3D.Quaternion =
                panoramaViewport3D.PanoramaRotation3D.Quaternion;

            _camera.FieldOfView =
                ((PerspectiveCamera) panoramaViewport3D.Camera).FieldOfView;
        }

        private void SaveExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (Visibility == Visibility.Hidden) return;
            if (_designerDrawingViewModel is null) return;
            _designerDrawingViewModel.CheckDataChangedFromLastSaveIfNeeded();
            if (_designerDrawingViewModel.Drawing.DataChangedFromLastSave)
            {
                var errorMessages = new List<string>();
                DsProject.Instance.SaveUnconditionally(_designerDrawingViewModel.Drawing,
                    DsProject.IfFileExistsActions.AskNewFileName, true, errorMessages);

                UpdateFileInfoTextbox(_designerDrawingViewModel.Drawing);

                if (errorMessages.Count > 0) MessageBoxHelper.ShowError(string.Join("\n", errorMessages));
            }
        }

        #endregion

        #region private fields

        private bool _disposed;
        private bool _panoramaDesignModeMessageShown;
        private PanoramaPlayControl? _panoramaPlayControl;
        private PerspectiveCamera? _camera;
        private Viewport2DVisual3D? _panoramaVisual3D;
        private DesignDrawingViewModel? _designerDrawingViewModel;
        private DesignDrawingCanvas? _designerDrawingCanvas;
        private Point _lastRightButtonClickDesignDrawingCanvasPosition;

        private CommandBinding? CutCommandBinding;
        private CommandBinding? CopyCommandBinding;
        private CommandBinding? PasteCommandBinding;
        private CommandBinding? DeleteCommandBinding;
        private CommandBinding? SaveCommandBinding;
        private CommandBinding? UndoCommandBinding;
        private CommandBinding? RedoCommandBinding;

        #endregion
    }
}