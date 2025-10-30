using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Win32;

using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Utils.Wpf.WpfMessageBox;
using Ssz.Utils;
using Ssz.Utils.MonitoredUndo;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core.ControlsDesign
{
    public class DesignDrawingViewModel : DockViewModel
    {
        #region construction and destruction

        public DesignDrawingViewModel(DrawingBase drawing, double horizontalMargin, double verticalMargin) :
            base(true)
        {
            SelectionService = new SelectionService<DsShapeViewModel>();
            Drawing = drawing;
            _horizontalMargin = horizontalMargin;
            _verticalMargin = verticalMargin;
            Drawing.PropertyChanged += DrawingOnPropertyChanged;
            Drawing.DrawingPositionInTreeChanged += UpdateTitle;
            if (Drawing is DsPageDrawing)
                Drawing.DrawingPositionInTreeChanged += DsProject.Instance.OnDsPageDrawingsListChanged;
            if (Drawing is DsShapeDrawing)
                Drawing.DrawingPositionInTreeChanged += DsProject.Instance.OnDsShapeDrawingsListChanged;
            Drawing.DataChangedFromLastSaveEvent += UpdateTitle;
            UpdateTitle();
            CopyCurrentCursorPoint = new RelayCommand(parameter =>
                Clipboard.SetData(DataFormats.UnicodeText, CurrentCursorPoint.ToString()));           
            
            _viewScale = 1;
        }


        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                if (DesignControlsInfo is not null) DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEvent;
                Drawing.DrawingPositionInTreeChanged -= UpdateTitle;
                Drawing.DataChangedFromLastSaveEvent -= UpdateTitle;
                Drawing.PropertyChanged -= DrawingOnPropertyChanged;
                
                SelectionService.Dispose();

                Drawing.Dispose();

                DesignControlsInfo = null;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public readonly ManualResetEvent IsInitialized = new(false);

        public readonly ManualResetEvent IsXamlToDsShapesConverted = new(false);

        public event Action<double, double, Point?>? ViewScaleChanging;

        public DrawingBase Drawing { get; }        

        public double Width
        {
            get => Drawing.Width;
            set => Drawing.Width = value;
        }

        public double Height
        {
            get => Drawing.Height;
            set => Drawing.Height = value;
        }

        public double BorderWidth => Drawing.Width + _horizontalMargin;

        public double BorderHeight => Drawing.Height + _verticalMargin;

        public double ViewboxWidth => BorderWidth * ViewScale;

        public double ViewboxHeight => BorderHeight * ViewScale;

        public Point CurrentCursorPoint
        {
            get => _currentCursorPoint;
            set
            {
                value.X = Math.Round(value.X, 3);
                value.Y = Math.Round(value.Y, 3);
                if (SetValue(ref _currentCursorPoint, value)) OnPropertyChanged(() => CurrentCursorPointToDisplay);
            }
        }

        public string CurrentCursorPointToDisplay => Resources.ClickCoordinate + @" " + _currentCursorPoint;

        public string DrawingPropertiesMenuItemHeader => Drawing + @" " + Resources.PropertiesMenuItemHeader;

        public ICommand CopyCurrentCursorPoint { get; protected set; }        

        public SelectionService<DsShapeViewModel> SelectionService { get; }

        public DateTime SelectedDateTime { get; private set; }

        public override bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetValue(ref _isSelected, value))
                {
                    if (!_isSelected)
                    {
                        if (_propertiesWindowsCount == 0) Drawing.CheckDataChangedFromLastSave();
                    }
                    else
                    {
                        SelectedDateTime = DateTime.Now;
                    }
                }
            }
        }

        public Visibility ResizeDecoratorVisibility
        {
            get
            {
                if (Drawing is DsShapeDrawing) return Visibility.Visible;
                return Visibility.Hidden;
            }
        }

        public double ViewScale
        {
            get => _viewScale;
            private set
            {
                if (Math.Abs(_viewScale - value) < 0.001) return;

                _viewScale = value;

                OnPropertyChanged(nameof(ViewScale));
                OnPropertyChanged(nameof(ViewboxWidth));
                OnPropertyChanged(nameof(ViewboxHeight));
            }
        }

        public DesignControlsInfo? DesignControlsInfo { get; private set; }


        public void Initialize(DesignControlsInfo designerControlsInfo)
        {
            DesignControlsInfo = designerControlsInfo;

            IsInitialized.Set();

            DsProject.Instance.GlobalUITimerEvent += OnGlobalUITimerEvent;
        }

        public void PropertiesWindowsCountIncrement()
        {
            _propertiesWindowsCount += 1;
        }

        public void PropertiesWindowsCountDecrement()
        {
            _propertiesWindowsCount--;
            if (!_isSelected && _propertiesWindowsCount == 0) Drawing.CheckDataChangedFromLastSave();
        }


        public bool SaveUnconditionally(List<string>? errorMessages = null)
        {
            SelectionService.ClearSelection();

            var dsShapeDrawingPreviewBytesChanged = false;
            var dsShapeDrawing = Drawing as DsShapeDrawing;
            if (dsShapeDrawing is not null && DesignControlsInfo is not null)
            {
                dsShapeDrawing.CreatePreviewImage(DesignControlsInfo.DesignDrawingCanvas);
                dsShapeDrawingPreviewBytesChanged = true;
            }

            var cancelled =
                DsProject.Instance.SaveUnconditionally(Drawing, DsProject.IfFileExistsActions.AskNewFileName, true,
                    errorMessages);

            if (!cancelled)
            {
                Drawing.DeleteUnusedFiles(true);

                if (dsShapeDrawingPreviewBytesChanged)
                    DsProject.Instance.OnDsShapeDrawingsListChanged();
            }

            return cancelled;
        }

        public void AddDsShape(EntityInfo? entityInfo, Point centerPosition)
        {
            var newDsShape = DesignDsProjectViewModel.Instance.NewDsShape(entityInfo, centerPosition);
            if (newDsShape is null) return;
            SelectionService.ClearSelection();
            newDsShape.SelectWhenShow = true;
            newDsShape.FirstSelectWhenShow = true;
            using (new UndoBatch(Drawing, "Add DsShape", true))
            {
                Drawing.AddDsShapes(null, true, newDsShape);
                newDsShape.RefreshForPropertyGrid();
            }
        }

        public void ResizeHorizontalLeft(double horizontalChange)
        {
            if (DesignDsProjectViewModel.Instance.DiscreteMode)
            {
                var discreteModeStep = DesignDsProjectViewModel.Instance.DiscreteModeStep;
                horizontalChange = Math.Round(horizontalChange / discreteModeStep) * discreteModeStep;
            }

            horizontalChange = Math.Min(horizontalChange, Drawing.Width);
            Drawing.ResizeHorizontalFromLeft(horizontalChange);
        }

        public void ResizeHorizontalRight(double horizontalChange)
        {
            horizontalChange = Math.Max(horizontalChange, -Drawing.Width);
            Drawing.ResizeHorizontalFromRight(horizontalChange);
            if (DesignDsProjectViewModel.Instance.DiscreteMode)
            {
                var discreteModeStep = DesignDsProjectViewModel.Instance.DiscreteModeStep;
                Drawing.Width =
                    Math.Round(Drawing.Width / discreteModeStep) * discreteModeStep;
            }
        }

        public void ResizeVerticalTop(double verticalChange)
        {
            if (DesignDsProjectViewModel.Instance.DiscreteMode)
            {
                var discreteModeStep = DesignDsProjectViewModel.Instance.DiscreteModeStep;
                verticalChange = Math.Round(verticalChange / discreteModeStep) * discreteModeStep;
            }

            verticalChange = Math.Min(verticalChange, Drawing.Height);
            Drawing.ResizeVerticalFromTop(verticalChange);
        }

        public void ResizeVerticalBottom(double verticalChange)
        {
            verticalChange = Math.Max(verticalChange, -Drawing.Height);
            Drawing.ResizeVerticalFromBottom(verticalChange);
            if (DesignDsProjectViewModel.Instance.DiscreteMode)
            {
                var discreteModeStep = DesignDsProjectViewModel.Instance.DiscreteModeStep;
                Drawing.Height =
                    Math.Round(Drawing.Height / discreteModeStep) * discreteModeStep;
            }
        }

        public bool IsAlignEnabled()
        {
            return SelectionService.SelectedItems.Length > 1;
        }

        public void AlignTopExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 1)
                using (new UndoBatch(Drawing, "Align Top", true))
                {
                    var baseRect = selectedDsShapeViewModels[0].GetBoundingRect();
                    var index = 0;
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        if (index > 0)
                        {
                            var rect = selectedDsShapeViewModel.GetBoundingRect();
                            rect.Y = baseRect.Top;
                            selectedDsShapeViewModel.SetBoundingRect(rect);
                        }

                        index += 1;
                    }
                }
        }

        public void AlignRightExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 1)
                using (new UndoBatch(Drawing, "Align Right", true))
                {
                    var baseRect = selectedDsShapeViewModels[0].GetBoundingRect();
                    var index = 0;
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        if (index > 0)
                        {
                            var rect = selectedDsShapeViewModel.GetBoundingRect();
                            rect.X = baseRect.Right - rect.Width;
                            selectedDsShapeViewModel.SetBoundingRect(rect);
                        }

                        index += 1;
                    }
                }
        }

        public void AlignLeftExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 1)
                using (new UndoBatch(Drawing, "Align Left", true))
                {
                    var baseRect = selectedDsShapeViewModels[0].GetBoundingRect();
                    var index = 0;
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        if (index > 0)
                        {
                            var rect = selectedDsShapeViewModel.GetBoundingRect();
                            rect.X = baseRect.Left;
                            selectedDsShapeViewModel.SetBoundingRect(rect);
                        }

                        index += 1;
                    }
                }
        }

        public void AlignBottomExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 1)
                using (new UndoBatch(Drawing, "Align Bottom", true))
                {
                    var baseRect = selectedDsShapeViewModels[0].GetBoundingRect();
                    var index = 0;
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        if (index > 0)
                        {
                            var rect = selectedDsShapeViewModel.GetBoundingRect();
                            rect.Y = baseRect.Bottom - rect.Height;
                            selectedDsShapeViewModel.SetBoundingRect(rect);
                        }

                        index += 1;
                    }
                }
        }

        public void AlignHorizontalCentersExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 1)
                using (new UndoBatch(Drawing, "Align Horizontal Centers", true))
                {
                    var baseRect = selectedDsShapeViewModels[0].GetBoundingRect();
                    var index = 0;
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        if (index > 0)
                        {
                            var rect = selectedDsShapeViewModel.GetBoundingRect();
                            var delta = rect.Left +
                                        rect.Width / 2 - baseRect.Left -
                                        baseRect.Width / 2;
                            rect.Offset(-delta, 0);
                            selectedDsShapeViewModel.SetBoundingRect(rect);
                        }

                        index += 1;
                    }
                }
        }

        public void AlignVerticalCentersExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 1)
                using (new UndoBatch(Drawing, "Align Vertical Centers", true))
                {
                    var baseRect = selectedDsShapeViewModels[0].GetBoundingRect();
                    var index = 0;
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        if (index > 0)
                        {
                            var rect = selectedDsShapeViewModel.GetBoundingRect();
                            var delta = rect.Top +
                                        rect.Height / 2 - baseRect.Top -
                                        baseRect.Height / 2;
                            rect.Offset(0, -delta);
                            selectedDsShapeViewModel.SetBoundingRect(rect);
                        }

                        index += 1;
                    }
                }
        }

        public void EqualizeWidthExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 1)
                using (new UndoBatch(Drawing, "Equalize Width", true))
                {
                    var baseRect = selectedDsShapeViewModels[0].GetBoundingRect();
                    var index = 0;
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        if (index > 0)
                        {
                            var rect = selectedDsShapeViewModel.GetBoundingRect();
                            var widthDelta = baseRect.Width - rect.Width;
                            rect.Width = baseRect.Width;
                            rect.Offset(-widthDelta / 2, 0);
                            selectedDsShapeViewModel.SetBoundingRect(rect);
                        }

                        index += 1;
                    }
                }
        }

        public void EqualizeHeightExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 1)
                using (new UndoBatch(Drawing, "Equalize Height", true))
                {
                    var baseRect = selectedDsShapeViewModels[0].GetBoundingRect();
                    var index = 0;
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        if (index > 0)
                        {
                            var rect = selectedDsShapeViewModel.GetBoundingRect();
                            var heightDelta = baseRect.Height - rect.Height;
                            rect.Height = baseRect.Height;
                            rect.Offset(0, -heightDelta / 2);
                            selectedDsShapeViewModel.SetBoundingRect(rect);
                        }

                        index += 1;
                    }
                }
        }

        public void CropUnusedSpace()
        {
            using (new UndoBatch(Drawing, "Crop Unused Space", true))
            {
                Drawing.CropUnusedSpace();
            }
        }

        public bool IsDsShapeDockEnabled()
        {
            return SelectionService.SelectedItems.Length == 1;
        }

        public void DsShapeDockExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length == 1)
                using (new UndoBatch(Drawing, "Dock", true))
                {
                    DsShapeBase dsShape = selectedDsShapeViewModels[0].DsShape;
                    dsShape.WidthInitial = Drawing.Width;
                    dsShape.HeightInitial = Drawing.Height;
                    dsShape.LeftNotTransformed = 0;
                    dsShape.TopNotTransformed = 0;
                }
        }

        public void DsShapeDockLeftExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length == 1)
                using (new UndoBatch(Drawing, "DockLeft", true))
                {
                    DsShapeBase dsShape = selectedDsShapeViewModels[0].DsShape;
                    dsShape.HeightInitial = Drawing.Height;
                    dsShape.LeftNotTransformed = 0;
                    dsShape.TopNotTransformed = 0;
                }
        }

        public void DsShapeDockTopExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length == 1)
                using (new UndoBatch(Drawing, "DockTop", true))
                {
                    DsShapeBase dsShape = selectedDsShapeViewModels[0].DsShape;
                    dsShape.WidthInitial = Drawing.Width;
                    dsShape.LeftNotTransformed = 0;
                    dsShape.TopNotTransformed = 0;
                }
        }

        public void DsShapeDockRightExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length == 1)
                using (new UndoBatch(Drawing, "DockRight", true))
                {
                    DsShapeBase dsShape = selectedDsShapeViewModels[0].DsShape;
                    dsShape.HeightInitial = Drawing.Height;
                    dsShape.LeftNotTransformed = Drawing.Width - dsShape.WidthInitial;
                    dsShape.TopNotTransformed = 0;
                }
        }

        public void DsShapeDockBottomExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length == 1)
                using (new UndoBatch(Drawing, "DockBottom", true))
                {
                    DsShapeBase dsShape = selectedDsShapeViewModels[0].DsShape;
                    dsShape.WidthInitial = Drawing.Width;
                    dsShape.LeftNotTransformed = 0;
                    dsShape.TopNotTransformed = Drawing.Height - dsShape.HeightInitialNotRounded;
                }
        }

        public bool IsDistributeEnabled()
        {
            return SelectionService.SelectedItems.Length > 2;
        }

        public void DistributeVerticalExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems
                    .OrderBy(svm => svm.GetBoundingRect().Top + svm.GetBoundingRect().Height / 2)
                    .ToArray();

            if (selectedDsShapeViewModels.Length > 2)
                using (new UndoBatch(Drawing, "Distribute Vertical", true))
                {
                    var topRect = selectedDsShapeViewModels.First().GetBoundingRect();
                    var topCenter = topRect.Y + topRect.Height / 2;
                    var bottomRect = selectedDsShapeViewModels.Last().GetBoundingRect();
                    var bottomCenter = bottomRect.Y + bottomRect.Height / 2;
                    var delta = (bottomCenter - topCenter) / (selectedDsShapeViewModels.Length - 1);
                    var index = 0;
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        if (index > 0 && index < selectedDsShapeViewModels.Length - 1)
                        {
                            var rect = selectedDsShapeViewModel.GetBoundingRect();
                            rect.Y = topCenter + index * delta - rect.Height / 2;
                            selectedDsShapeViewModel.SetBoundingRect(rect);
                        }

                        index += 1;
                    }
                }
        }

        public void DistributeHorizontalExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems
                    .OrderBy(svm => svm.GetBoundingRect().Left + svm.GetBoundingRect().Width / 2)
                    .ToArray();

            if (selectedDsShapeViewModels.Length > 2)
                using (new UndoBatch(Drawing, "Distribute Vertical", true))
                {
                    var leftRect = selectedDsShapeViewModels.First().GetBoundingRect();
                    var leftCenter = leftRect.X + leftRect.Width / 2;
                    var rightRect = selectedDsShapeViewModels.Last().GetBoundingRect();
                    var rightCenter = rightRect.X + rightRect.Width / 2;
                    var delta = (rightCenter - leftCenter) / (selectedDsShapeViewModels.Length - 1);
                    var index = 0;
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        if (index > 0 && index < selectedDsShapeViewModels.Length - 1)
                        {
                            var rect = selectedDsShapeViewModel.GetBoundingRect();
                            rect.X = leftCenter + index * delta - rect.Width / 2;
                            selectedDsShapeViewModel.SetBoundingRect(rect);
                        }

                        index += 1;
                    }
                }
        }

        public string GetCurrentSelectionAsXaml()
        {
            using (var sw = new StringWriter())
            {
                using (
                    var xmlTextWriter = new XmlTextWriter(sw)
                )
                {
                    xmlTextWriter.Formatting = Formatting.Indented;

                    DsShapeBase[] selectedDsShapes = SelectionService.SelectedItems
                        .Where(svm => svm.DsShape.Index >= 0)
                        .Select(svm => svm.DsShape).OrderBy(sh => sh.Index).ToArray();
                    if (selectedDsShapes.Length == 0) return "";

                    XamlHelper.Save(new ArrayList(selectedDsShapes), xmlTextWriter);
                }

                return sw.ToString();
            }
        }

        public void Paste(string? xaml, Point position)
        {
            if (string.IsNullOrWhiteSpace(xaml)) return;

            try
            {
                DsShapeBase[] pastedDsShapes =
                    ((ArrayList) (XamlHelper.Load(xaml!) ?? new ArrayList())).OfType<DsShapeBase>().ToArray();

                if (pastedDsShapes.Length == 0) return;

                var minX = double.MaxValue;
                var minY = double.MaxValue;
                foreach (DsShapeBase dsShape in pastedDsShapes)
                {
                    var centerInitialPositionNotRounded = dsShape.CenterInitialPositionNotRounded;
                    if (centerInitialPositionNotRounded.X < minX) minX = centerInitialPositionNotRounded.X;
                    if (centerInitialPositionNotRounded.Y < minY) minY = centerInitialPositionNotRounded.Y;
                }

                foreach (DsShapeBase dsShape in pastedDsShapes)
                {
                    var centerInitialPositionNotRounded = dsShape.CenterInitialPositionNotRounded;
                    centerInitialPositionNotRounded.X = centerInitialPositionNotRounded.X - minX + position.X;
                    centerInitialPositionNotRounded.Y = centerInitialPositionNotRounded.Y - minY + position.Y;
                    dsShape.CenterInitialPosition = centerInitialPositionNotRounded;

                    dsShape.SelectWhenShow = true;
                }

                SelectionService.ClearSelection();

                using (new UndoBatch(Drawing, "Paste DsShapes", true))
                {
                    Drawing.AddDsShapes(null, true, pastedDsShapes);

                    foreach (DsShapeBase dsShape in pastedDsShapes) dsShape.RefreshForPropertyGrid();
                }
            }
            catch (Exception)
            {
            }
        }

        public void DeleteCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Delete DsShapes", true))
            {
                Drawing.RemoveDsShapes(SelectionService.SelectedItems
                    .Where(svm => svm.DsShape.Index >= 0)
                    .Select(svm => svm.DsShape).ToArray());
            }
        }

        public void Group(DsShapeBase[] originalDsShapes)
        {
            using (new UndoBatch(Drawing, "Group Current Selection", true))
            {
                var originalDsShapeIndex = originalDsShapes[0].Index;

                string xaml = XamlHelper.Save(new ArrayList(originalDsShapes));
                DsShapeBase[] copyDsShapes =
                    ((ArrayList) (XamlHelper.Load(xaml) ?? new ArrayList())).OfType<DsShapeBase>().ToArray();

                Drawing.RemoveDsShapes(originalDsShapes);
                SelectionService.ClearSelection();

                ComplexDsShape newComplexDsShape = DsProject.Instance.NewComplexDsShape(copyDsShapes);

                Drawing.AddDsShapes(originalDsShapeIndex, true, newComplexDsShape);

                newComplexDsShape.RefreshForPropertyGrid();
            }
        }

        public void Ungroup(ComplexDsShape[] complexDsShapes, bool replaceConstants)
        {
            using (new UndoBatch(Drawing, "Ungroup", true))
            {
                foreach (var complexDsShape in complexDsShapes)
                {
                    DsShapeBase[] copyDsShapes =
                        complexDsShape.Ungroup(replaceConstants);

                    var complexDsShapeIndex = complexDsShape.Index;
                    Drawing.RemoveDsShapes(complexDsShape);
                    SelectionService.ClearSelection();
                    Drawing.AddDsShapes(complexDsShapeIndex, true, copyDsShapes);
                    foreach (DsShapeBase dsShape in copyDsShapes) dsShape.RefreshForPropertyGrid();
                }
            }
        }


        public async Task TryConvertContentDsShapeToComplexDsShapeAsync(ContentDsShape[] contentDsShapes)
        {
            try
            {
                DesignDsProjectViewModel.Instance.ForceEnableXamlToDsShapesConversion = true;

                using (new UndoBatch(Drawing, "Convert", true))
                {
                    foreach (var contentDsShape in contentDsShapes)
                    {
                        var contentXaml = contentDsShape.ContentInfo.ConstValue?.Xaml;
                        var canExtract = XamlHelper.CheckXamlForDsShapeExtraction(contentXaml);
                        if (!canExtract) continue;

                        var originalDsShapeIndex = contentDsShape.Index;
                        var originalDsShapeRect = contentDsShape.GetBoundingRect();

                        var tempDirPath = DsProject.Instance.DsProjectPath + @"\Temp";
                        Directory.CreateDirectory(tempDirPath);
                        var drawing = new DsPageDrawing(false, true)
                        {
                            Width = contentDsShape.WidthInitial,
                            Height = contentDsShape.HeightInitial,
                            FileFullName = tempDirPath + @"\" + Drawing.Name + @"_temp" + DsProject.DsPageFileExtension
                        };
                        drawing.UnderlyingXaml.Xaml = contentXaml ?? "";
                        DsProject.Instance.SaveUnconditionally(drawing, DsProject.IfFileExistsActions.CreateBackup,
                            false);

                        var designerDrawingViewModel =
                            await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
                                new FileInfo(drawing.FileFullName));
                        if (designerDrawingViewModel is null) return;

                        if (IsDisposed) return;

                        var dsShapesArray = designerDrawingViewModel.Drawing.DsShapesArray as ArrayList;
                        if (dsShapesArray is null) throw new InvalidOperationException();

                        if (dsShapesArray.Count > 0)
                        {
                            string xaml = XamlHelper.Save(dsShapesArray);
                            DsShapeBase[] extractedDsShapes =
                                ((ArrayList) (XamlHelper.Load(xaml) ?? new ArrayList())).OfType<DsShapeBase>()
                                .ToArray();

                            SelectionService.ClearSelection();

                            foreach (DsShapeBase extractedDsShape in extractedDsShapes)
                            {
                                var centerInitialPositionNotRounded =
                                    extractedDsShape.CenterInitialPositionNotRounded;
                                centerInitialPositionNotRounded.X =
                                    centerInitialPositionNotRounded.X + originalDsShapeRect.X;
                                centerInitialPositionNotRounded.Y =
                                    centerInitialPositionNotRounded.Y + originalDsShapeRect.Y;
                                extractedDsShape.CenterInitialPosition = centerInitialPositionNotRounded;
                            }

                            DsShapeBase? dsShapeToAdd = null;
                            if (extractedDsShapes.Length == 1)
                                dsShapeToAdd = extractedDsShapes[0];
                            else if (extractedDsShapes.Length > 1)
                                dsShapeToAdd = DsProject.Instance.NewComplexDsShape(extractedDsShapes);
                            if (dsShapeToAdd is not null)
                            {
                                Drawing.RemoveDsShapes(contentDsShape);

                                dsShapeToAdd.IsVisibleInfo =
                                    (BooleanDataBinding) contentDsShape.IsVisibleInfo.Clone();
                                dsShapeToAdd.OpacityInfo = (DoubleDataBinding) contentDsShape.OpacityInfo.Clone();
                                dsShapeToAdd.AngleInitial = contentDsShape.AngleInitial;

                                Drawing.AddDsShapes(originalDsShapeIndex, true, dsShapeToAdd);

                                dsShapeToAdd.RefreshForPropertyGrid();
                            }
                        }

                        DesignDsProjectViewModel.Instance.CloseDrawingUnconditionally(designerDrawingViewModel);
                    }
                }
            }
            finally
            {
                DesignDsProjectViewModel.Instance.ForceEnableXamlToDsShapesConversion = false;
            }
        }

        public void BringForwardCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Bring Forward Current Selection", true))
            {
                DsShapeViewModel[] currentSelection =
                    SelectionService.SelectedItems
                        .Where(svm => svm.DsShape.Index >= 0)
                        .OrderByDescending(svm => svm.DsShape.Index)
                        .ToArray();

                foreach (DsShapeViewModel dsShapeViewModel in currentSelection)
                {
                    if (dsShapeViewModel.DsShape.Index >= Drawing.DsShapesCount - 1) break;
                    Drawing.ReorderDsShape(dsShapeViewModel.DsShape.Index,
                        dsShapeViewModel.DsShape.Index + 1);
                }

                //if (currentSelection.Length == 1) SelectionService.ClearSelection();
            }
        }

        public void BringToFrontCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Bring to Front Current Selection", true))
            {
                DsShapeViewModel[] currentSelection =
                    SelectionService.SelectedItems.Where(svm => svm.DsShape.Index >= 0)
                        .OrderBy(svm => svm.DsShape.Index).ToArray();

                foreach (DsShapeViewModel dsShapeViewModel in currentSelection)
                    Drawing.ReorderDsShape(dsShapeViewModel.DsShape.Index, Drawing.DsShapesCount - 1);

                //if (currentSelection.Length == 1) SelectionService.ClearSelection();
            }
        }

        public void SendBackwardCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Send Backward Current Selection", true))
            {
                DsShapeViewModel[] currentSelection =
                    SelectionService.SelectedItems.Where(svm => svm.DsShape.Index >= 0)
                        .OrderBy(svm => svm.DsShape.Index).ToArray();

                foreach (DsShapeViewModel dsShapeViewModel in currentSelection)
                {
                    if (dsShapeViewModel.DsShape.Index == 0) break;
                    Drawing.ReorderDsShape(dsShapeViewModel.DsShape.Index,
                        dsShapeViewModel.DsShape.Index - 1);
                }

                //if (currentSelection.Length == 1) SelectionService.ClearSelection();
            }
        }

        public void SendToBackCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Send to Back Current Selection", true))
            {
                DsShapeViewModel[] currentSelection =
                    SelectionService.SelectedItems
                        .Where(svm => svm.DsShape.Index >= 0)
                        .OrderByDescending(svm => svm.DsShape.Index)
                        .ToArray();

                foreach (DsShapeViewModel dsShapeViewModel in currentSelection)
                    Drawing.ReorderDsShape(dsShapeViewModel.DsShape.Index, 0);

                //if (currentSelection.Length == 1) SelectionService.ClearSelection();
            }
        }

        public void EditGeometryCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Geometry Editing Mode", true))
            {
                var dsShapeViewModels =
                    SelectionService.SelectedItems
                        .Where(svm => svm.DsShape.Index >= 0);
                foreach (var dsShapeViewModel in dsShapeViewModels)
                    dsShapeViewModel.GeometryEditingMode = !dsShapeViewModel.GeometryEditingMode;
            }
        }

        public void DsShapeMoveLeftExecuted()
        {
            using (new UndoBatch(Drawing, "Move DsShapes Left", true))
            {
                DsShapeViewModel[] currentSelection =
                    SelectionService.SelectedItems;

                foreach (DsShapeViewModel dsShapeViewModel in currentSelection)
                {
                    var position = dsShapeViewModel.DsShape.CenterInitialPositionNotRounded;
                    position.X = (int) position.X - 1;
                    dsShapeViewModel.DsShape.CenterInitialPosition = position;
                }
            }
        }

        public void DsShapeMoveUpExecuted()
        {
            using (new UndoBatch(Drawing, "Move DsShapes Up", true))
            {
                DsShapeViewModel[] currentSelection =
                    SelectionService.SelectedItems;

                foreach (DsShapeViewModel dsShapeViewModel in currentSelection)
                {
                    var position = dsShapeViewModel.DsShape.CenterInitialPositionNotRounded;
                    position.Y = (int) position.Y - 1;
                    dsShapeViewModel.DsShape.CenterInitialPosition = position;
                }
            }
        }

        public void DsShapeMoveRightExecuted()
        {
            using (new UndoBatch(Drawing, "Move DsShapes Right", true))
            {
                DsShapeViewModel[] currentSelection =
                    SelectionService.SelectedItems;

                foreach (DsShapeViewModel dsShapeViewModel in currentSelection)
                {
                    var position = dsShapeViewModel.DsShape.CenterInitialPositionNotRounded;
                    position.X = (int) position.X + 1;
                    dsShapeViewModel.DsShape.CenterInitialPosition = position;
                }
            }
        }

        public void DsShapeMoveDownExecuted()
        {
            using (new UndoBatch(Drawing, "Move DsShapes Down", true))
            {
                DsShapeViewModel[] currentSelection =
                    SelectionService.SelectedItems;

                foreach (DsShapeViewModel dsShapeViewModel in currentSelection)
                {
                    var position = dsShapeViewModel.DsShape.CenterInitialPositionNotRounded;
                    position.Y = (int) position.Y + 1;
                    dsShapeViewModel.DsShape.CenterInitialPosition = position;
                }
            }
        }

        public void RotateCounterClockwiseCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Rotate Counter-Clockwise", true))
            {
                var currentSelection =
                    SelectionService.SelectedItems.FirstOrDefault();

                if (currentSelection is not null)
                    currentSelection.DsShape.AngleInitial = currentSelection.DsShape.AngleInitialNotRounded - 90;
            }
        }

        public void RotateClockwiseCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Rotate Clockwise", true))
            {
                var currentSelection =
                    SelectionService.SelectedItems.FirstOrDefault();

                if (currentSelection is not null)
                    currentSelection.DsShape.AngleInitial = currentSelection.DsShape.AngleInitialNotRounded + 90;
            }
        }

        public void FlipHorizontalCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Flip Horizontal", true))
            {
                var currentSelection =
                    SelectionService.SelectedItems.FirstOrDefault();

                if (currentSelection is not null)
                    currentSelection.DsShape.IsFlipped = !currentSelection.DsShape.IsFlipped;
            }
        }

        public void RotateXRightCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Rotate X Right", true))
            {
                var currentSelection =
                    SelectionService.SelectedItems.FirstOrDefault();

                if (currentSelection is not null)
                    currentSelection.DsShape.RotationX = currentSelection.DsShape.RotationXNotRounded + 1;
            }
        }

        public void RotateXLeftCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Rotate X Left", true))
            {
                var currentSelection =
                    SelectionService.SelectedItems.FirstOrDefault();

                if (currentSelection is not null)
                    currentSelection.DsShape.RotationX = currentSelection.DsShape.RotationXNotRounded - 1;
            }
        }

        public void RotateYRightCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Rotate Y Right", true))
            {
                var currentSelection =
                    SelectionService.SelectedItems.FirstOrDefault();

                if (currentSelection is not null)
                    currentSelection.DsShape.RotationY = currentSelection.DsShape.RotationYNotRounded + 1;
            }
        }

        public void RotateYLeftCurrentSelection()
        {
            using (new UndoBatch(Drawing, "Rotate Y Left", true))
            {
                var currentSelection =
                    SelectionService.SelectedItems.FirstOrDefault();

                if (currentSelection is not null)
                    currentSelection.DsShape.RotationY = currentSelection.DsShape.RotationYNotRounded - 1;
            }
        }

        public void DsShapeFieldOfViewIncreaseCurrentSelection()
        {
            using (new UndoBatch(Drawing, "FieldOfView Increase", true))
            {
                var currentSelection =
                    SelectionService.SelectedItems.FirstOrDefault();

                if (currentSelection is not null)
                    currentSelection.DsShape.FieldOfView = currentSelection.DsShape.FieldOfViewNotRounded + 1;
            }
        }

        public void DsShapeFieldOfViewDecreaseCurrentSelection()
        {
            using (new UndoBatch(Drawing, "FieldOfView Decrease", true))
            {
                var currentSelection =
                    SelectionService.SelectedItems.FirstOrDefault();

                if (currentSelection is not null)
                    currentSelection.DsShape.FieldOfView = currentSelection.DsShape.FieldOfViewNotRounded - 1;
            }
        }

        public bool IsUndoEnabled()
        {
            return UndoService.Current[Drawing.GetUndoRoot()].CanUndo;
        }

        public void UndoExecuted()
        {
            UndoRoot undoRoot = UndoService.Current[Drawing.GetUndoRoot()];
            undoRoot.Undo();

            Drawing.SetDataChangedUnknown();
        }

        public bool IsRedoEnabled()
        {
            return UndoService.Current[Drawing.GetUndoRoot()].CanRedo;
        }

        public void RedoExecuted()
        {
            UndoService.Current[Drawing.GetUndoRoot()].Redo();

            Drawing.SetDataChangedUnknown();
        }

        public IEnumerable<DsShapeViewModel> GetRootDsShapeViewModels()
        {
            return SelectionService.AllItems;
        }

        public DsShapeViewModel? GetRootDsShapeViewModel(DsShapeInfo? dsShapeInfo)
        {
            if (dsShapeInfo is null) return null;
            var allRootDsShapeViewModels = SelectionService.AllItems.ToArray();
            if (!String.IsNullOrEmpty(dsShapeInfo.Name))
            {
                var sameNameRootDsShapeViewModels = allRootDsShapeViewModels
                        .Where(svm => StringHelper.CompareIgnoreCase(svm.DsShape.Name, dsShapeInfo.Name)).ToArray();
                if (sameNameRootDsShapeViewModels.Length == 0) return null;
                if (sameNameRootDsShapeViewModels.Length == 1) return sameNameRootDsShapeViewModels[0];
            }            
            return allRootDsShapeViewModels.FirstOrDefault(svm => svm.DsShape.Index == dsShapeInfo.Index);
        }

        public void UpdateSelection(Rect rubberBand)
        {
            foreach (DsShapeViewModel dsShapeViewModel in SelectionService.AllItems.Where(svm =>
                !svm.DsShape.IsLocked))
            {
                var itemDesignRect =
                    dsShapeViewModel.GetBoundingRect();

                if (rubberBand.Contains(itemDesignRect))
                {
                    if (!dsShapeViewModel.IsSelected)
                        SelectionService.AddToSelection(dsShapeViewModel);
                }
                else
                {
                    if (dsShapeViewModel.IsSelected)
                        SelectionService.RemoveFromSelection(dsShapeViewModel);
                }
            }
        }

        public bool IsDsShapeLockEnabled()
        {
            return SelectionService.SelectedItems.Any(svm => !svm.DsShape.IsLocked);
        }

        public void DsShapeLockExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 0)
                using (new UndoBatch(Drawing, "Lock", true))
                {
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                        selectedDsShapeViewModel.DsShape.IsLocked = true;
                }
        }

        public bool IsDsShapeUnlockEnabled()
        {
            return SelectionService.SelectedItems.Any(svm => svm.DsShape.IsLocked);
        }

        public void DsShapeUnlockExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 0)
                using (new UndoBatch(Drawing, "Unlock", true))
                {
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                        selectedDsShapeViewModel.DsShape.IsLocked = false;
                }
        }

        public bool IsDsShapeExportPropertiesEnabled()
        {
            return SelectionService.SelectedItems.Length == 1;
        }

        public void DsShapeExportPropertiesExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length == 1)
            {
                var dialog = new SaveFileDialog
                {
                    Title = Resources.DsShapeExportPropertiesSaveAsDialogTitle,
                    Filter = @"Save file (*.csv)|*.csv|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() != true) return;

                using (Stream stream = dialog.OpenFile())
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    foreach (var spi in ObjectHelper.FindInStringBrowsableProperties(selectedDsShapeViewModels[0].DsShape, null))
                        writer.WriteLine(CsvHelper.FormatForCsv(",", new[] {spi.PropertyPath, spi.PropertyValue}));
                }

                MessageBoxHelper.ShowInfo(Resources.Done);
            }
        }

        public bool IsDsShapeImportPropertiesEnabled()
        {
            return SelectionService.SelectedItems.Length > 0;
        }

        public void DsShapeImportPropertiesExecuted()
        {
            DsShapeViewModel[] selectedDsShapeViewModels =
                SelectionService.SelectedItems;

            if (selectedDsShapeViewModels.Length > 0)
            {
                var dlg = new OpenFileDialog
                {
                    Title = Resources.DsShapeImportPropertiesDialogTitle,
                    Filter = @"Open file (*.csv)|*.csv|All files (*.*)|*.*"
                };

                if (dlg.ShowDialog() != true) return;

                CaseInsensitiveOrderedDictionary<List<string?>> fileData;
                try
                {
                    fileData = CsvHelper.LoadCsvFile(dlg.FileName, true);
                }
                catch (Exception ex)
                {
                    var message = Resources.ReadCsvFileError + @" " + dlg.FileName;
                    MessageBoxHelper.ShowError(message + ". " + Resources.SeeErrorLogForDetails);
                    DsProject.LoggersSet.Logger.LogError(ex, message);
                    return;
                }

                using (new UndoBatch(Drawing, "DsShape Import Properties", true))
                {
                    foreach (DsShapeViewModel selectedDsShapeViewModel in selectedDsShapeViewModels)
                    {
                        foreach (var kvp in fileData)
                            if (kvp.Value.Count > 1)
                                ObjectHelper.SetValue(selectedDsShapeViewModel.DsShape, @"." + kvp.Value[0], kvp.Value[1]);
                        selectedDsShapeViewModel.OnDsShapeChanged();
                    }
                }
            }
        }

        public void DsShapeOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (IsDisposed || _dsShapeOnPropertyChangedExecuting || sender is null) return;

            var propertyName = args.PropertyName ?? "";
            switch (propertyName)
            {
                case @"CenterInitialPosition":
                case @"CenterInitialPositionAdvanced":
                case @"WidthInitial":
                case @"WidthInitialAdvanced":
                case @"HeightInitial":
                case @"HeightInitialAdvanced":
                case @"AngleInitialAdvanced":
                case @"Index":
                case @"Name":
                case @"":
                    return;
            }

            var selectedItems = SelectionService.SelectedItems;
            if (selectedItems.Length == 0 ||
                selectedItems.Length == 1 && ReferenceEquals(selectedItems[0].DsShape, sender)) return;

            PropertyDescriptor? propertyDescriptor = TypeDescriptor.GetProperties(sender.GetType())[propertyName];
            if (propertyDescriptor is null || !propertyDescriptor.IsBrowsable || propertyDescriptor.IsReadOnly) return;
            if (propertyDescriptor.Attributes.OfType<ReadOnlyInEditorAttribute>().Any()) return;

            var value = propertyDescriptor.GetValue(sender);
            var valueDataBinding = value as IValueDataBinding;
            if (valueDataBinding is not null && !valueDataBinding.IsConst) return;

            _dsShapeOnPropertyChangedExecuting = true;

            var result = new List<Tuple<DsShapeBase, PropertyDescriptor>>();
            foreach (var sh in selectedItems.Select(svm => svm.DsShape))
            {
                if (ReferenceEquals(sh, sender)) continue;

                foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(sh.GetType())
                    .OfType<PropertyDescriptor>())
                    if (pd.IsBrowsable && !pd.IsReadOnly &&
                        pd.Name == propertyName && pd.PropertyType == propertyDescriptor.PropertyType)
                    {
                        if (!propertyDescriptor.Attributes.OfType<ReadOnlyInEditorAttribute>().Any())
                            result.Add(Tuple.Create(sh, pd));
                        break;
                    }
            }

            if (result.Count == 0)
            {
                _dsShapeOnPropertyChangedExecuting = false;
                return;
            }

            var messageBoxResult = WpfMessageBox.Show(MessageBoxHelper.GetRootWindow(),
                Resources.ApplyToOtherDsShapesQuestion,
                Resources.QuestionMessageBoxCaption,
                WpfMessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (messageBoxResult == WpfMessageBoxResult.Yes)
            {
                if (valueDataBinding is not null)
                {
                    foreach (var r in result)
                    {
                        var vdsi = r.Item2.GetValue(r.Item1) as IValueDataBinding;
                        if (vdsi is not null)
                        {
                            for (var i = vdsi.DataBindingItemsCollection.Count - 1; i >= 0; i--)
                                vdsi.DataBindingItemsCollection.RemoveAt(i);
                            vdsi.Converter = null;
                            var cl = valueDataBinding.ConstObject as ICloneable;
                            if (cl is not null)
                                vdsi.ConstObject = cl.Clone();
                            else
                                vdsi.ConstObject = valueDataBinding.ConstObject;
                        }
                    }

                    _dsShapeOnPropertyChangedExecuting = false;
                    return;
                }

                var clonable = value as ICloneable;
                if (clonable is not null)
                {
                    foreach (var r in result) r.Item2.SetValue(r.Item1, clonable.Clone());
                    _dsShapeOnPropertyChangedExecuting = false;
                    return;
                }

                foreach (var r in result) r.Item2.SetValue(r.Item1, value);
            }

            _dsShapeOnPropertyChangedExecuting = false;
        }

        public void TryConvertUnderlyingContentXamlToDsShapes()
        {
            var undoRoot = Drawing.GetUndoRoot();
            if ((undoRoot is null || !UndoService.Current[undoRoot].IsUndoingOrRedoing) &&
                    Drawing is DsPageDrawing &&
                    (DesignDsProjectViewModel.Instance.ForceEnableXamlToDsShapesConversion || DsProject.Instance.TryConvertXamlToDsShapes) &&
                    !DesignDsProjectViewModel.Instance.ForceDisableXamlToDsShapesConversion)                
            {
                Application.Current.Dispatcher.BeginInvoke(async () =>
                {
                    var designControlsInfo = DesignControlsInfo;
                    if (designControlsInfo is not null)
                        using (new UndoBatch(Drawing, "Underlying Xaml Processing", true))
                        {
                            await designControlsInfo.DesignDrawingCanvas
                                .TryConvertUnderlyingContentXamlToDsShapesAsync();
                        }

                    Drawing.CheckDataChangedFromLastSave();

                    IsXamlToDsShapesConverted.Set();
                });
            }
            else
            {
                IsXamlToDsShapesConverted.Set();
            }            
        }

        public void CheckDataChangedFromLastSaveIfNeeded()
        {
            if (_isSelected || _propertiesWindowsCount > 0) Drawing.CheckDataChangedFromLastSave();
        }

        #endregion

        #region internal functions

        internal void SetViewScale(double oldValue, double newValue, Point? immovableRelativePoint)
        {
            ViewScaleChanging?.Invoke(oldValue, newValue, immovableRelativePoint);
            ViewScale = newValue;
        }

        #endregion        

        #region protected functions

        protected override void OnCloseCommandExecuted()
        {
            DesignDsProjectViewModel.Instance.CloseDrawing(this);
        }        

        #endregion

        #region private functions

        private void OnGlobalUITimerEvent(int phase)
        {
            CheckDataChangedFromLastSaveIfNeeded();
        }

        private void DrawingOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case @"Width":
                    OnPropertyChanged(@"Width");
                    OnPropertyChanged(@"BorderWidth");
                    OnPropertyChanged(@"ViewboxWidth");
                    break;
                case @"Height":
                    OnPropertyChanged(@"Height");
                    OnPropertyChanged(@"BorderHeight");
                    OnPropertyChanged(@"ViewboxHeight");
                    break;
            }
        }                    

        private void UpdateTitle()
        {
            string title = Drawing.Name;
            if (Drawing.DataChangedFromLastSave) title += "*";

            Title = title;
        }

        #endregion

        #region private fields

        private int _propertiesWindowsCount;
        private bool _isSelected;
        private readonly double _horizontalMargin;
        private readonly double _verticalMargin;
        private double _viewScale;        
        private Point _currentCursorPoint;
        private bool _dsShapeOnPropertyChangedExecuting;        

        #endregion
    }
}