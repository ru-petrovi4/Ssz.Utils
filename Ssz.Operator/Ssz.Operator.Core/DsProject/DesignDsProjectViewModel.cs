using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Operator.Core.Utils;
using Ssz.Utils.Wpf.WpfMessageBox;
using Ssz.Utils;
using Ssz.Utils.Wpf;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core
{
    public class DesignDsProjectViewModel : DisposableViewModelBase
    {
        public class BusyCloser : IProgressInfo, IDisposable
        {
            #region private fields

            private readonly DesignDsProjectViewModel? _designerDsProjectViewModel;

            #endregion

            #region construction and destruction

            public BusyCloser(DesignDsProjectViewModel designerMainWindowViewModel)
            {
                if (designerMainWindowViewModel.IsBusy) return;

                _designerDsProjectViewModel = designerMainWindowViewModel;
                _designerDsProjectViewModel._busyCancellationTokenSource = new CancellationTokenSource();
                _designerDsProjectViewModel.IsBusy = true;
                _designerDsProjectViewModel.BusyHeader = Resources.MainBusyIndicatorText;
                _designerDsProjectViewModel.BusyProgressBarPercent = 100.0;
                _designerDsProjectViewModel.BusyDescription = @"";
            }

            public void Dispose()
            {
                if (_designerDsProjectViewModel is null) return;

                _designerDsProjectViewModel.IsBusy = false;
            }

            #endregion

            #region public functions

            public void SetHeader(string text)
            {
                if (_designerDsProjectViewModel is null) return;

                _designerDsProjectViewModel.BusyHeader = text;
            }

            public void SetDescription(string text)
            {
                if (_designerDsProjectViewModel is null) return;

                _designerDsProjectViewModel.BusyDescription = text;
            }

            public void RefreshProgressBar()
            {
                if (_designerDsProjectViewModel is null) return;

                if (ProgressBarMaxValue > 0)
                {
                    int progressBarCurrentValue;
                    if (ProgressBarCurrentValue < 0) progressBarCurrentValue = 0;
                    else if (ProgressBarCurrentValue > ProgressBarMaxValue)
                        progressBarCurrentValue = ProgressBarMaxValue;
                    else progressBarCurrentValue = ProgressBarCurrentValue;
                    _designerDsProjectViewModel.BusyProgressBarPercent =
                        100.0 * progressBarCurrentValue / ProgressBarMaxValue;
                }
                else
                {
                    _designerDsProjectViewModel.BusyProgressBarPercent = 0.0;
                }
            }

            public void DebugInfo(string info)
            {
                if (_designerDsProjectViewModel is null) return;

                var instance = DebugWindow.Instance; // Create Window, if doesn't exist
                DsProject.LoggersSet.UserFriendlyLogger.LogInformation(info);
            }

            public async Task SetHeaderAsync(string text)
            {
                SetHeader(text);

                await Dispatcher.Yield(DispatcherPriority.Background);
            }

            public async Task SetDescriptionAsync(string text)
            {
                SetDescription(text);

                await Dispatcher.Yield(DispatcherPriority.Background);
            }

            public int ProgressBarCurrentValue { get; set; }

            public int ProgressBarMaxValue { get; set; }

            public async Task RefreshProgressBarAsync()
            {
                RefreshProgressBar();

                await Dispatcher.Yield(DispatcherPriority.Background);
            }

            public async Task RefreshProgressBarAsync(int currentValue, int maxValue)
            {
                ProgressBarCurrentValue = currentValue;
                ProgressBarMaxValue = maxValue;

                if (ProgressBarMaxValue > 0 && ProgressBarCurrentValue >= 0 &&
                    ProgressBarCurrentValue <= ProgressBarMaxValue)
                    SetDescription(ProgressBarCurrentValue + @"/" + ProgressBarMaxValue);
                else
                    SetDescription(@"");

                RefreshProgressBar();

                await Dispatcher.Yield(DispatcherPriority.Background);
            }

            public async Task DebugInfoAsync(string info)
            {
                DebugInfo(info);

                await Dispatcher.Yield(DispatcherPriority.Background);
            }

            public CancellationToken GetCancellationToken()
            {
                if (_designerDsProjectViewModel is null ||
                    _designerDsProjectViewModel._busyCancellationTokenSource is null) return new CancellationToken();

                return _designerDsProjectViewModel._busyCancellationTokenSource.Token;
            }

            #endregion
        }

        #region construction and destruction

        static DesignDsProjectViewModel()
        {
            DsShapeMoveLeft = new RoutedCommand();
            DsShapeMoveUp = new RoutedCommand();
            DsShapeMoveRight = new RoutedCommand();
            DsShapeMoveDown = new RoutedCommand();
            RotateCounterClockwise = new RoutedCommand();
            RotateClockwise = new RoutedCommand();
            FlipHorizontal = new RoutedCommand();
            BringForward = new RoutedCommand();
            BringToFront = new RoutedCommand();
            SendBackward = new RoutedCommand();
            SendToBack = new RoutedCommand();
            DsShapeRotateXRight = new RoutedCommand();
            DsShapeRotateXLeft = new RoutedCommand();
            DsShapeRotateYRight = new RoutedCommand();
            DsShapeRotateYLeft = new RoutedCommand();
            DsShapeFieldOfViewIncrease = new RoutedCommand();
            DsShapeFieldOfViewDecrease = new RoutedCommand();
            Group = new RoutedCommand();
            Ungroup = new RoutedCommand();
            UngroupAndReplaceConstants = new RoutedCommand();
            ConvertToComplexDsShape = new RoutedCommand();
            EditGeometry = new RoutedCommand();
            AlignTop = new RoutedCommand();
            AlignVerticalCenters = new RoutedCommand();
            AlignBottom = new RoutedCommand();
            AlignLeft = new RoutedCommand();
            AlignHorizontalCenters = new RoutedCommand();
            AlignRight = new RoutedCommand();
            EqualizeWidth = new RoutedCommand();
            EqualizeHeight = new RoutedCommand();
            CropUnusedSpace = new RoutedCommand();
            DsShapeLock = new RoutedCommand();
            DsShapeUnlock = new RoutedCommand();
            DsShapeExportProperties = new RoutedCommand();
            DsShapeImportProperties = new RoutedCommand();
            DsShapeDock = new RoutedCommand();
            DsShapeDockLeft = new RoutedCommand();
            DsShapeDockTop = new RoutedCommand();
            DsShapeDockRight = new RoutedCommand();
            DsShapeDockBottom = new RoutedCommand();
            DistributeHorizontal = new RoutedCommand();
            DistributeVertical = new RoutedCommand();            

            Instance = new DesignDsProjectViewModel();
        }

        protected DesignDsProjectViewModel()
        {
            RecentFilesCollectionManager =
                new RecentFilesCollectionManager(AppRegistryOptions.SszOperatorSubKeyString, 10, 150, null);

            DiscreteModeStep = 5;
            DsShapesInfoFontSizeScale = 0.4;
            DsShapesInfoOpacity = 0.7;

            DsShapeMoveLeftCommandBinding = new CommandBinding(DsShapeMoveLeft,
                DsShapeMoveLeftExecuted, DsShapeMoveEnabled);
            DsShapeMoveUpCommandBinding = new CommandBinding(DsShapeMoveUp,
                DsShapeMoveUpExecuted, DsShapeMoveEnabled);
            DsShapeMoveRightCommandBinding = new CommandBinding(DsShapeMoveRight,
                DsShapeMoveRightExecuted, DsShapeMoveEnabled);
            DsShapeMoveDownCommandBinding = new CommandBinding(DsShapeMoveDown,
                DsShapeMoveDownExecuted, DsShapeMoveEnabled);
            RotateCounterClockwiseCommandBinding = new CommandBinding(RotateCounterClockwise,
                RotateCounterClockwiseExecuted,
                DsShapeTransformEnabled);
            RotateClockwiseCommandBinding = new CommandBinding(RotateClockwise, RotateClockwiseExecuted,
                DsShapeTransformEnabled);
            FlipHorizontalCommandBinding = new CommandBinding(FlipHorizontal, FlipHorizontalExecuted,
                DsShapeTransformEnabled);
            BringForwardCommandBinding = new CommandBinding(BringForward, BringForwardExecuted,
                OrderEnabled);
            BringToFrontCommandBinding = new CommandBinding(BringToFront, BringToFrontExecuted,
                OrderEnabled);
            SendBackwardCommandBinding = new CommandBinding(SendBackward, SendBackwardExecuted,
                OrderEnabled);
            SendToBackCommandBinding = new CommandBinding(SendToBack, SendToBackExecuted,
                OrderEnabled);
            DsShapeRotateXRightCommandBinding = new CommandBinding(DsShapeRotateXRight,
                DsShapeRotateXRightExecuted,
                DsShapeTransformEnabled);
            DsShapeRotateXLeftCommandBinding = new CommandBinding(DsShapeRotateXLeft, DsShapeRotateXLeftExecuted,
                DsShapeTransformEnabled);
            DsShapeRotateYRightCommandBinding = new CommandBinding(DsShapeRotateYRight,
                DsShapeRotateYRightExecuted,
                DsShapeTransformEnabled);
            DsShapeRotateYLeftCommandBinding = new CommandBinding(DsShapeRotateYLeft, DsShapeRotateYLeftExecuted,
                DsShapeTransformEnabled);
            DsShapeFieldOfViewIncreaseCommandBinding = new CommandBinding(DsShapeFieldOfViewIncrease,
                DsShapeFieldOfViewIncreaseExecuted,
                DsShapeTransformEnabled);
            DsShapeFieldOfViewDecreaseCommandBinding = new CommandBinding(DsShapeFieldOfViewDecrease,
                DsShapeFieldOfViewDecreaseExecuted,
                DsShapeTransformEnabled);
            GroupCommandBinding = new CommandBinding(Group, GroupExecuted,
                GroupEnabled);
            UngroupCommandBinding = new CommandBinding(Ungroup, UngroupExecuted,
                UngroupEnabled);
            UngroupAndReplaceConstantsCommandBinding = new CommandBinding(UngroupAndReplaceConstants,
                UngroupAndReplaceConstantsExecuted,
                UngroupEnabled);
            ConvertToComplexDsShapeCommandBinding = new CommandBinding(ConvertToComplexDsShape,
                ConvertToComplexDsShapeExecutedAsync,
                ConvertToComplexDsShapeEnabled);
            EditGeometryCommandBinding = new CommandBinding(EditGeometry, EditGeometryExecuted,
                EditGeometryEnabled);
            AlignTopCommandBinding = new CommandBinding(AlignTop, AlignTopExecuted, AlignEnabled);
            AlignVerticalCentersCommandBinding = new CommandBinding(AlignVerticalCenters,
                AlignVerticalCentersExecuted, AlignEnabled);
            AlignBottomCommandBinding = new CommandBinding(AlignBottom, AlignBottomExecuted,
                AlignEnabled);
            AlignLeftCommandBinding = new CommandBinding(AlignLeft, AlignLeftExecuted,
                AlignEnabled);
            AlignHorizontalCentersCommandBinding = new CommandBinding(AlignHorizontalCenters,
                AlignHorizontalCentersExecuted,
                AlignEnabled);
            AlignRightCommandBinding = new CommandBinding(AlignRight, AlignRightExecuted,
                AlignEnabled);
            EqualizeWidthCommandBinding = new CommandBinding(EqualizeWidth, EqualizeWidthExecuted,
                AlignEnabled);
            EqualizeHeightCommandBinding = new CommandBinding(EqualizeHeight, EqualizeHeightExecuted,
                AlignEnabled);
            CropUnusedSpaceCommandBinding = new CommandBinding(CropUnusedSpace, CropUnusedSpaceExecuted,
                CropUnusedSpaceEnabled);
            DsShapeLockCommandBinding = new CommandBinding(DsShapeLock, DsShapeLockExecuted,
                DsShapeLockEnabled);
            DsShapeUnlockCommandBinding = new CommandBinding(DsShapeUnlock, DsShapeUnlockExecuted,
                DsShapeUnlockEnabled);
            DsShapeExportPropertiesCommandBinding = new CommandBinding(DsShapeExportProperties,
                DsShapeExportPropertiesExecuted,
                DsShapeExportPropertiesEnabled);
            DsShapeImportPropertiesCommandBinding = new CommandBinding(DsShapeImportProperties,
                DsShapeImportPropertiesExecuted,
                DsShapeImportPropertiesEnabled);
            DsShapeDockCommandBinding = new CommandBinding(DsShapeDock, DsShapeDockExecuted,
                DsShapeDockEnabled);
            DsShapeDockLeftCommandBinding = new CommandBinding(DsShapeDockLeft, DsShapeDockLeftExecuted,
                DsShapeDockEnabled);
            DsShapeDockTopCommandBinding = new CommandBinding(DsShapeDockTop, DsShapeDockTopExecuted,
                DsShapeDockEnabled);
            DsShapeDockRightCommandBinding = new CommandBinding(DsShapeDockRight, DsShapeDockRightExecuted,
                DsShapeDockEnabled);
            DsShapeDockBottomCommandBinding = new CommandBinding(DsShapeDockBottom, DsShapeDockBottomExecuted,
                DsShapeDockEnabled);
            DistributeHorizontalCommandBinding = new CommandBinding(DistributeHorizontal,
                DistributeHorizontalExecuted,
                DistributeEnabled);
            DistributeVerticalCommandBinding = new CommandBinding(DistributeVertical,
                DistributeVerticalExecuted, DistributeEnabled);

            DsShapeMoveLeft.InputGestures.Add(new KeyGesture(Key.Left));
            DsShapeMoveUp.InputGestures.Add(new KeyGesture(Key.Up));
            DsShapeMoveRight.InputGestures.Add(new KeyGesture(Key.Right));
            DsShapeMoveDown.InputGestures.Add(new KeyGesture(Key.Down));
            RotateCounterClockwise.InputGestures.Add(new KeyGesture(Key.Left, ModifierKeys.Control));
            RotateClockwise.InputGestures.Add(new KeyGesture(Key.Right, ModifierKeys.Control));
            BringForward.InputGestures.Add(new KeyGesture(Key.Down, ModifierKeys.Control));
            BringToFront.InputGestures.Add(new KeyGesture(Key.Down, ModifierKeys.Control | ModifierKeys.Shift));
            SendBackward.InputGestures.Add(new KeyGesture(Key.Up, ModifierKeys.Control));
            SendToBack.InputGestures.Add(new KeyGesture(Key.Up, ModifierKeys.Control | ModifierKeys.Shift));
            DsShapeRotateXRight.InputGestures.Add(new KeyGesture(Key.Down, ModifierKeys.Alt));
            DsShapeRotateXLeft.InputGestures.Add(new KeyGesture(Key.Up, ModifierKeys.Alt));
            DsShapeRotateYRight.InputGestures.Add(new KeyGesture(Key.Right, ModifierKeys.Alt));
            DsShapeRotateYLeft.InputGestures.Add(new KeyGesture(Key.Left, ModifierKeys.Alt));
            DsShapeFieldOfViewIncrease.InputGestures.Add(new KeyGesture(Key.PageUp, ModifierKeys.Alt));
            DsShapeFieldOfViewDecrease.InputGestures.Add(new KeyGesture(Key.PageDown, ModifierKeys.Alt));
            Group.InputGestures.Add(new KeyGesture(Key.G, ModifierKeys.Control));
            Ungroup.InputGestures.Add(new KeyGesture(Key.U, ModifierKeys.Control));

            _usedMemoryRefreshTimer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.ContextIdle, (o, a) => { OnPropertyChanged(nameof(UsedMemory)); }, Dispatcher.CurrentDispatcher);            
            _usedMemoryRefreshTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing) RecentFilesCollectionManager.Dispose();

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public static DesignDsProjectViewModel Instance { get; }

        public static readonly RoutedCommand DsShapeMoveLeft;
        public static readonly RoutedCommand DsShapeMoveUp;
        public static readonly RoutedCommand DsShapeMoveRight;
        public static readonly RoutedCommand DsShapeMoveDown;
        public static readonly RoutedCommand RotateCounterClockwise;
        public static readonly RoutedCommand RotateClockwise;
        public static readonly RoutedCommand FlipHorizontal;
        public static readonly RoutedCommand BringForward;
        public static readonly RoutedCommand BringToFront;
        public static readonly RoutedCommand SendBackward;
        public static readonly RoutedCommand SendToBack;
        public static readonly RoutedCommand DsShapeRotateXRight;
        public static readonly RoutedCommand DsShapeRotateXLeft;
        public static readonly RoutedCommand DsShapeRotateYRight;
        public static readonly RoutedCommand DsShapeRotateYLeft;
        public static readonly RoutedCommand DsShapeFieldOfViewIncrease;
        public static readonly RoutedCommand DsShapeFieldOfViewDecrease;
        public static readonly RoutedCommand Group;
        public static readonly RoutedCommand Ungroup;
        public static readonly RoutedCommand UngroupAndReplaceConstants;
        public static readonly RoutedCommand ConvertToComplexDsShape;
        public static readonly RoutedCommand EditGeometry;
        public static readonly RoutedCommand AlignTop;
        public static readonly RoutedCommand AlignVerticalCenters;
        public static readonly RoutedCommand AlignBottom;
        public static readonly RoutedCommand AlignLeft;
        public static readonly RoutedCommand AlignHorizontalCenters;
        public static readonly RoutedCommand AlignRight;
        public static readonly RoutedCommand EqualizeWidth;
        public static readonly RoutedCommand EqualizeHeight;
        public static readonly RoutedCommand CropUnusedSpace;
        public static readonly RoutedCommand DsShapeLock;
        public static readonly RoutedCommand DsShapeUnlock;
        public static readonly RoutedCommand DsShapeExportProperties;
        public static readonly RoutedCommand DsShapeImportProperties;
        public static readonly RoutedCommand DsShapeDock;
        public static readonly RoutedCommand DsShapeDockLeft;
        public static readonly RoutedCommand DsShapeDockTop;
        public static readonly RoutedCommand DsShapeDockRight;
        public static readonly RoutedCommand DsShapeDockBottom;
        public static readonly RoutedCommand DistributeHorizontal;
        public static readonly RoutedCommand DistributeVertical;


        public static bool SaveDrawings(IEnumerable<DesignDrawingViewModel> drawingViewModels)
        {
            var errorMessages = new List<string>();

            var cancelled = false;
            foreach (DesignDrawingViewModel drawingViewModel in drawingViewModels)
            {
                drawingViewModel.CheckDataChangedFromLastSaveIfNeeded();
                if (!drawingViewModel.Drawing.DataChangedFromLastSave) continue;

                if (drawingViewModel.SaveUnconditionally(errorMessages))
                {
                    cancelled = true;
                    break;
                }
            }

            if (errorMessages.Count > 0) MessageBoxHelper.ShowWarning(string.Join("\n", errorMessages));

            return cancelled;
        }

        public string DiscreteModeComboBoxText
        {
            get => DiscreteModeStep.ToString(CultureInfo.InvariantCulture);
            set
            {
                if (value is null) value = "";
                uint result;
                if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                    if (result > 0 && result < 1000)
                        DiscreteModeStep = result;
            }
        }

        public bool DiscreteMode
        {
            get => _discreteMode;
            set => SetValue(ref _discreteMode, value);
        }

        public uint DiscreteModeStep
        {
            get => _discreteModeStep;
            set => SetValue(ref _discreteModeStep, value);
        }

        public bool ShowDsShapesInfoTooltips
        {
            get => _showDsShapesInfoTooltips;
            set => SetValue(ref _showDsShapesInfoTooltips, value);
        }

        public double DsShapesInfoFontSizeScale
        {
            get => _dsShapesInfoFontSizeScale;
            set => SetValue(ref _dsShapesInfoFontSizeScale, value);
        }

        public double DsShapesInfoOpacity
        {
            get => _dsShapesInfoOpacity;
            set => SetValue(ref _dsShapesInfoOpacity, value);
        }

        public int DsShapesOrdering
        {
            get => (int)_dsShapesOrdering;
            set
            {
                if (SetValue(ref _dsShapesOrdering, (DsShapesOrderingEnum)value)) 
                    FocusedDesignDrawingViewModel = FocusedDesignDrawingViewModel; // Force shapes tree refresh
            }
        }

        public RecentFilesCollectionManager RecentFilesCollectionManager { get; }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetValue(ref _isBusy, value);
        }

        public string BusyHeader
        {
            get => _busyHeader;
            set => SetValue(ref _busyHeader, value);
        }

        public string BusyDescription
        {
            get => _busyDescription;
            set => SetValue(ref _busyDescription, value);
        }

        public double BusyProgressBarPercent
        {
            get => _busyProgressBarPercent;
            set => SetValue(ref _busyProgressBarPercent, value);
        }

        public string Title
        {
            get => _title;
            set => SetValue(ref _title, value);
        }

        public double Zoom
        {
            get { return _zoom; }
            set
            {
                SetValue(ref _zoom, value);
            }
        }

        public long UsedMemory => GC.GetTotalMemory(true) / 1014;

        public DesignDrawingViewModel? FocusedDesignDrawingViewModel
        {
            get => _focusedDesignDrawingViewModel;
            set
            {   
                _focusedDesignDrawingViewModel = value;

                if (_focusedDesignDrawingViewModel is not null)
                {                    
                    SetDesignDrawingViewScale(_focusedDesignDrawingViewModel.ViewScale, null);
                }                

                OnPropertyChangedAuto();
            }
        }


        public int SelectedDesignDrawingIndex
        {
            get
            {
                if (_focusedDesignDrawingViewModel is null) return -1;

                for (var i = 0; i < OpenedDesignDrawingViewModels.Count; i += 1)
                    if (ReferenceEquals(_focusedDesignDrawingViewModel, OpenedDesignDrawingViewModels[i]))
                        return i;

                return -1;
            }
            set
            {
                if (value < 0 || value >= OpenedDesignDrawingViewModels.Count) return;

                FocusedDesignDrawingViewModel = OpenedDesignDrawingViewModels[value];
            }
        }

        public ObservableCollection<DesignDrawingViewModel> OpenedDesignDrawingViewModels { get; } = new();

        public string ScaleComboBoxText
        {
            get => ((int) (DesignDrawingViewScale * 100.0)).ToString(CultureInfo.InvariantCulture) + "%";
            set
            {
                if (value is null) value = "";
                value = value.Replace("%", "");
                int result;
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                    SetDesignDrawingViewScale(result / 100.0, new Point(0.5, 0.5));
            }
        }

        public double DesignDrawingViewScale { get; private set; } = 1.0;

        public bool ForceDisableXamlToDsShapesConversion
        {
            get => _forceDisableXamlToDsShapesConversion;
            set => SetValue(ref _forceDisableXamlToDsShapesConversion, value);
        }

        public bool ForceEnableXamlToDsShapesConversion
        {
            get => _forceEnableXamlToDsShapesConversion;
            set => SetValue(ref _forceEnableXamlToDsShapesConversion, value);
        }        
                
        public event Action<Point>? ShowPoint;

        public void AddCommandBindings(CommandBindingCollection commandBindings)
        {
            commandBindings.Add(DsShapeMoveLeftCommandBinding);
            commandBindings.Add(DsShapeMoveUpCommandBinding);
            commandBindings.Add(DsShapeMoveRightCommandBinding);
            commandBindings.Add(DsShapeMoveDownCommandBinding);
            commandBindings.Add(RotateCounterClockwiseCommandBinding);
            commandBindings.Add(RotateClockwiseCommandBinding);
            commandBindings.Add(FlipHorizontalCommandBinding);
            commandBindings.Add(BringForwardCommandBinding);
            commandBindings.Add(BringToFrontCommandBinding);
            commandBindings.Add(SendBackwardCommandBinding);
            commandBindings.Add(SendToBackCommandBinding);
            commandBindings.Add(DsShapeRotateXRightCommandBinding);
            commandBindings.Add(DsShapeRotateXLeftCommandBinding);
            commandBindings.Add(DsShapeRotateYRightCommandBinding);
            commandBindings.Add(DsShapeRotateYLeftCommandBinding);
            commandBindings.Add(DsShapeFieldOfViewIncreaseCommandBinding);
            commandBindings.Add(DsShapeFieldOfViewDecreaseCommandBinding);
            commandBindings.Add(ConvertToComplexDsShapeCommandBinding);
            commandBindings.Add(GroupCommandBinding);
            commandBindings.Add(UngroupCommandBinding);
            commandBindings.Add(UngroupAndReplaceConstantsCommandBinding);
            commandBindings.Add(EditGeometryCommandBinding);
            commandBindings.Add(AlignTopCommandBinding);
            commandBindings.Add(AlignVerticalCentersCommandBinding);
            commandBindings.Add(AlignBottomCommandBinding);
            commandBindings.Add(AlignLeftCommandBinding);
            commandBindings.Add(AlignHorizontalCentersCommandBinding);
            commandBindings.Add(AlignRightCommandBinding);
            commandBindings.Add(EqualizeWidthCommandBinding);
            commandBindings.Add(EqualizeHeightCommandBinding);
            commandBindings.Add(CropUnusedSpaceCommandBinding);
            commandBindings.Add(DsShapeLockCommandBinding);
            commandBindings.Add(DsShapeUnlockCommandBinding);
            commandBindings.Add(DsShapeExportPropertiesCommandBinding);
            commandBindings.Add(DsShapeImportPropertiesCommandBinding);
            commandBindings.Add(DsShapeDockCommandBinding);
            commandBindings.Add(DsShapeDockLeftCommandBinding);
            commandBindings.Add(DsShapeDockTopCommandBinding);
            commandBindings.Add(DsShapeDockRightCommandBinding);
            commandBindings.Add(DsShapeDockBottomCommandBinding);
            commandBindings.Add(DistributeHorizontalCommandBinding);
            commandBindings.Add(DistributeVerticalCommandBinding);
        }


        public void RemoveCommandBindings(CommandBindingCollection commandBindings)
        {
            commandBindings.Remove(DsShapeMoveLeftCommandBinding);
            commandBindings.Remove(DsShapeMoveUpCommandBinding);
            commandBindings.Remove(DsShapeMoveRightCommandBinding);
            commandBindings.Remove(DsShapeMoveDownCommandBinding);
            commandBindings.Remove(RotateCounterClockwiseCommandBinding);
            commandBindings.Remove(RotateClockwiseCommandBinding);
            commandBindings.Remove(FlipHorizontalCommandBinding);
            commandBindings.Remove(BringForwardCommandBinding);
            commandBindings.Remove(BringToFrontCommandBinding);
            commandBindings.Remove(SendBackwardCommandBinding);
            commandBindings.Remove(SendToBackCommandBinding);
            commandBindings.Remove(DsShapeRotateXRightCommandBinding);
            commandBindings.Remove(DsShapeRotateXLeftCommandBinding);
            commandBindings.Remove(DsShapeRotateYRightCommandBinding);
            commandBindings.Remove(DsShapeRotateYLeftCommandBinding);
            commandBindings.Remove(DsShapeFieldOfViewIncreaseCommandBinding);
            commandBindings.Remove(DsShapeFieldOfViewDecreaseCommandBinding);
            commandBindings.Remove(GroupCommandBinding);
            commandBindings.Remove(UngroupCommandBinding);
            commandBindings.Remove(UngroupAndReplaceConstantsCommandBinding);
            commandBindings.Remove(ConvertToComplexDsShapeCommandBinding);
            commandBindings.Remove(EditGeometryCommandBinding);
            commandBindings.Remove(AlignTopCommandBinding);
            commandBindings.Remove(AlignVerticalCentersCommandBinding);
            commandBindings.Remove(AlignBottomCommandBinding);
            commandBindings.Remove(AlignLeftCommandBinding);
            commandBindings.Remove(AlignHorizontalCentersCommandBinding);
            commandBindings.Remove(AlignRightCommandBinding);
            commandBindings.Remove(EqualizeWidthCommandBinding);
            commandBindings.Remove(EqualizeHeightCommandBinding);
            commandBindings.Remove(CropUnusedSpaceCommandBinding);
            commandBindings.Remove(DsShapeLockCommandBinding);
            commandBindings.Remove(DsShapeUnlockCommandBinding);
            commandBindings.Remove(DsShapeExportPropertiesCommandBinding);
            commandBindings.Remove(DsShapeImportPropertiesCommandBinding);
            commandBindings.Remove(DsShapeDockCommandBinding);
            commandBindings.Remove(DsShapeDockLeftCommandBinding);
            commandBindings.Remove(DsShapeDockTopCommandBinding);
            commandBindings.Remove(DsShapeDockRightCommandBinding);
            commandBindings.Remove(DsShapeDockBottomCommandBinding);
            commandBindings.Remove(DistributeHorizontalCommandBinding);
            commandBindings.Remove(DistributeVerticalCommandBinding);
        }

        public void HelpExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            var fi = new FileInfo(Process.GetCurrentProcess().MainModule?.FileName ?? "");
            var helpFileInfo = new FileInfo(fi.DirectoryName + @"\" + Resources.HelpFileName);

            if (!helpFileInfo.Exists)
                helpFileInfo = new FileInfo(fi.DirectoryName + @"\..\Doc\" + Resources.HelpFileName);
            if (!helpFileInfo.Exists) return;
            Process.Start(helpFileInfo.FullName);
        }

        public void StopExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (_busyCancellationTokenSource is not null) _busyCancellationTokenSource.Cancel();
        }

        public void CutEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.SelectionService.SelectedItems.Any();
        }

        public void CutExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null ||
                FocusedDesignDrawingViewModel.SelectionService.SelectedItems.Length == 0) return;

            CopyExecuted(sender, e);
            DeleteExecuted(sender, e);
        }

        public void CopyEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.SelectionService.SelectedItems.Any();
        }

        public void CopyExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null ||
                FocusedDesignDrawingViewModel.SelectionService.SelectedItems.Length == 0) return;

            string xaml = FocusedDesignDrawingViewModel.GetCurrentSelectionAsXaml();
            Clipboard.Clear();
            if (!string.IsNullOrWhiteSpace(xaml)) Clipboard.SetData(DataFormats.UnicodeText, xaml);
        }

        public void PasteEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = Clipboard.ContainsData(DataFormats.Text);
        }

        public void PasteExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null ||
                FocusedDesignDrawingViewModel.DesignControlsInfo is null ||
                !Clipboard.ContainsData(DataFormats.Text)) return;

            var position = Mouse.GetPosition(FocusedDesignDrawingViewModel.DesignControlsInfo.DesignDrawingCanvas);

            var xaml = Clipboard.GetData(DataFormats.UnicodeText) as string;
            FocusedDesignDrawingViewModel.Paste(xaml, position);
        }

        public void DeleteEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.SelectionService.SelectedItems.Any();
        }

        public void DeleteExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null ||
                FocusedDesignDrawingViewModel.SelectionService.SelectedItems.Length == 0) return;

            FocusedDesignDrawingViewModel.DeleteCurrentSelection();
        }

        public void SelectAllExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;

            var toSelect = FocusedDesignDrawingViewModel.GetRootDsShapeViewModels()
                .Where(svm => !svm.DsShape.IsLocked);
            FocusedDesignDrawingViewModel.SelectionService.ClearSelection();
            foreach (var svm in toSelect) FocusedDesignDrawingViewModel.SelectionService.AddToSelection(svm);
        }

        public void CropUnusedSpaceEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        public void CropUnusedSpaceExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.CropUnusedSpace();
        }

        public void UndoEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.IsUndoEnabled();
        }

        public void UndoExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;

            FocusedDesignDrawingViewModel.UndoExecuted();

            PropertiesWindow.ReloadAll();
        }

        public void RedoEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.IsRedoEnabled();
        }

        public void RedoExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;

            FocusedDesignDrawingViewModel.RedoExecuted();

            PropertiesWindow.ReloadAll();
        }

        public void AlignEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.IsAlignEnabled();
        }

        public void AlignTopExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.AlignTopExecuted();
        }

        public void AlignRightExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.AlignRightExecuted();
        }

        public void AlignHorizontalCentersExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.AlignHorizontalCentersExecuted();
        }

        public void AlignLeftExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.AlignLeftExecuted();
        }

        public void AlignBottomExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.AlignBottomExecuted();
        }

        public void AlignVerticalCentersExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.AlignVerticalCentersExecuted();
        }

        public void EqualizeWidthExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.EqualizeWidthExecuted();
        }

        public void EqualizeHeightExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.EqualizeHeightExecuted();
        }

        public void DsShapeLockEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.IsDsShapeLockEnabled();
        }

        public void DsShapeLockExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeLockExecuted();
        }

        public void DsShapeUnlockEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.IsDsShapeUnlockEnabled();
        }

        public void DsShapeUnlockExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeUnlockExecuted();
        }

        private void DsShapeExportPropertiesEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.IsDsShapeExportPropertiesEnabled();
        }

        private void DsShapeExportPropertiesExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeExportPropertiesExecuted();
        }

        private void DsShapeImportPropertiesEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.IsDsShapeImportPropertiesEnabled();
        }

        private void DsShapeImportPropertiesExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeImportPropertiesExecuted();
        }

        public void DsShapeDockEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.IsDsShapeDockEnabled();
        }

        public void DsShapeDockExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeDockExecuted();
        }

        public void DsShapeDockLeftExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeDockLeftExecuted();
        }

        public void DsShapeDockTopExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeDockTopExecuted();
        }

        public void DsShapeDockRightExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeDockRightExecuted();
        }

        public void DsShapeDockBottomExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeDockBottomExecuted();
        }

        public void DistributeEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.IsDistributeEnabled();
        }

        public void DistributeVerticalExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DistributeVerticalExecuted();
        }

        public void DistributeHorizontalExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DistributeHorizontalExecuted();
        }

        public void DsShapeMoveEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (e is null) return;
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.SelectionService.SelectedItems.Any();
        }

        public void DsShapeMoveLeftExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeMoveLeftExecuted();
        }

        public void DsShapeMoveUpExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeMoveUpExecuted();
        }

        public void DsShapeMoveRightExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeMoveRightExecuted();
        }

        public void DsShapeMoveDownExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeMoveDownExecuted();
        }

        public void GroupEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;

            if (FocusedDesignDrawingViewModel is null) return;

            DsShapeBase[] originalDsShapes =
                FocusedDesignDrawingViewModel.SelectionService.SelectedItems
                    .Where(svm => svm.DsShape.Index >= 0)
                    .Select(svm => svm.DsShape)
                    .OrderBy(sh => sh.Index)
                    .ToArray();

            e.CanExecute = originalDsShapes.Length > 0;
        }

        public void GroupExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;

            DsShapeBase[] originalDsShapes =
                FocusedDesignDrawingViewModel.SelectionService.SelectedItems
                    .Where(svm => svm.DsShape.Index >= 0)
                    .Select(svm => svm.DsShape)
                    .OrderBy(sh => sh.Index)
                    .ToArray();
            if (originalDsShapes.Length == 0) return;

            FocusedDesignDrawingViewModel.Group(originalDsShapes);
        }

        public void UngroupEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;

            if (FocusedDesignDrawingViewModel is null) return;

            e.CanExecute = FocusedDesignDrawingViewModel.SelectionService.SelectedItems
                .Where(svm => svm.DsShape.Index >= 0 && svm.DsShape is ComplexDsShape).Count() > 0;
        }

        public void UngroupExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;

            ComplexDsShape[] complexDsShapes =
                FocusedDesignDrawingViewModel.SelectionService.SelectedItems
                    .Where(svm => svm.DsShape.Index >= 0 && svm.DsShape is ComplexDsShape)
                    .Select(svm => (ComplexDsShape) svm.DsShape)
                    .ToArray();

            FocusedDesignDrawingViewModel.Ungroup(complexDsShapes, false);
        }

        public void UngroupAndReplaceConstantsExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;

            ComplexDsShape[] complexDsShapes =
                FocusedDesignDrawingViewModel.SelectionService.SelectedItems
                    .Where(svm => svm.DsShape.Index >= 0 && svm.DsShape is ComplexDsShape)
                    .Select(svm => (ComplexDsShape) svm.DsShape)
                    .ToArray();

            FocusedDesignDrawingViewModel.Ungroup(complexDsShapes, true);
        }

        public void ConvertToComplexDsShapeEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;

            if (FocusedDesignDrawingViewModel is null) return;

            e.CanExecute = FocusedDesignDrawingViewModel.SelectionService.SelectedItems
                .Where(svm => svm.DsShape.Index >= 0 && svm.DsShape is ContentDsShape).Count() > 0;
        }

        public async void ConvertToComplexDsShapeExecutedAsync(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;

            ContentDsShape[] contentDsShapes =
                FocusedDesignDrawingViewModel.SelectionService.SelectedItems
                    .Where(svm => svm.DsShape.Index >= 0 && svm.DsShape is ContentDsShape)
                    .Select(svm => (ContentDsShape) svm.DsShape)
                    .ToArray();

            if (contentDsShapes.Length == 0) return;

            await FocusedDesignDrawingViewModel.TryConvertContentDsShapeToComplexDsShapeAsync(contentDsShapes);
        }

        public void OrderEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.SelectionService.SelectedItems.Any();
        }

        public void BringForwardExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.BringForwardCurrentSelection();
        }

        public void BringToFrontExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.BringToFrontCurrentSelection();
        }

        public void SendBackwardExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.SendBackwardCurrentSelection();
        }

        public void SendToBackExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.SendToBackCurrentSelection();
        }

        public void EditGeometryExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.EditGeometryCurrentSelection();
        }

        public void EditGeometryEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute =
                FocusedDesignDrawingViewModel.SelectionService.SelectedItems.Any(svm =>
                    svm.DsShape.GetType() == typeof(GeometryDsShape));
        }

        public void DsShapeTransformEnabled(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.SelectionService.SelectedItems.Length == 1;
        }

        public void RotateCounterClockwiseExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.RotateCounterClockwiseCurrentSelection();
        }

        public void RotateClockwiseExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.RotateClockwiseCurrentSelection();
        }

        public void FlipHorizontalExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.FlipHorizontalCurrentSelection();
        }

        public void DsShapeRotateXRightExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.RotateXRightCurrentSelection();
        }

        public void DsShapeRotateXLeftExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.RotateXLeftCurrentSelection();
        }

        public void DsShapeRotateYRightExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.RotateYRightCurrentSelection();
        }

        public void DsShapeRotateYLeftExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.RotateYLeftCurrentSelection();
        }

        public void DsShapeFieldOfViewIncreaseExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeFieldOfViewIncreaseCurrentSelection();
        }

        public void DsShapeFieldOfViewDecreaseExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            FocusedDesignDrawingViewModel.DsShapeFieldOfViewDecreaseCurrentSelection();
        }

        public IEnumerable<DrawingInfo>? GetOnDriveOrOpenedDrawingInfos(DrawingInfo[] onDriveDrawingInfos)
        {
            if (onDriveDrawingInfos is null) return null;
            var onDriveOrOpenedDrawingInfos = new List<DrawingInfo>(onDriveDrawingInfos.Length);
            foreach (DrawingInfo onDriveDrawingInfo in onDriveDrawingInfos)
            {
                var designerDrawingViewModel =
                    OpenedDesignDrawingViewModels.FirstOrDefault(
                        dvm =>
                            FileSystemHelper.Compare(dvm.Drawing.FileFullName,
                                onDriveDrawingInfo.FileInfo.FullName));
                if (designerDrawingViewModel is not null)
                    onDriveOrOpenedDrawingInfos.Add(designerDrawingViewModel.Drawing.GetDrawingInfo());
                else onDriveOrOpenedDrawingInfos.Add(onDriveDrawingInfo);
            }

            return onDriveOrOpenedDrawingInfos;
        }

        public async void NewDsPageDrawingWithFileAndOpenAsync()
        {
            if (!DsProject.Instance.IsInitialized || DsProject.Instance.IsReadOnly) return;

            var dsPagesDirectoryInfo = DsProject.Instance.DsPagesDirectoryInfo;
            if (dsPagesDirectoryInfo is null) return;

            var dlg = new SaveFileDialog
            {
                Title = Resources.NewDsPageDrawingDialogTitle,
                Filter = @"Save file (*" + DsProject.DsPageFileExtension + ")|*" + DsProject.DsPageFileExtension + "|All files (*.*)|*.*",
                InitialDirectory = dsPagesDirectoryInfo.FullName
            };

            if (dlg.ShowDialog() != true) return;

            var dsPageFileInfo = new FileInfo(dlg.FileName);

            if (
                !FileSystemHelper.Compare(dsPagesDirectoryInfo.FullName,
                    dsPageFileInfo.Directory?.FullName))
            {
                MessageBoxHelper.ShowError(Resources.FileMustBeInDsPagesDir);
                return;
            }

            var drawing = NewDsPageDrawingWithFile(dsPageFileInfo);

            if (drawing is null) return;

            DsProject.Instance.OnDsPageDrawingsListChanged();

            await ShowOrOpenDrawingAsync(dsPageFileInfo);
        }

        public async void NewDsShapeDrawingWithFileAndOpenAsync(ComplexDsShape? complexDsShape = null)
        {
            if (!DsProject.Instance.IsInitialized || DsProject.Instance.IsReadOnly) return;

            FileInfo? complexDsShapeFileInfo = null;
            var askFileName = true;

            var dsShapesDirectoryInfo = DsProject.Instance.DsShapesDirectoryInfo;
            if (dsShapesDirectoryInfo is null) return;

            if (complexDsShape is not null && !string.IsNullOrWhiteSpace(complexDsShape.DsShapeDrawingName))
            {
                complexDsShapeFileInfo = new FileInfo(dsShapesDirectoryInfo.FullName + @"\" +
                                                         complexDsShape.DsShapeDrawingName +
                                                         DsProject.DsShapeFileExtension);
                if (!complexDsShapeFileInfo.Exists) askFileName = false;
            }

            if (askFileName)
            {
                var dlg = new SaveFileDialog
                {
                    Title = Resources.NewDsShapeDrawingDialogTitle,
                    Filter = @"Save file (*" + DsProject.DsShapeFileExtension + ")|*" + DsProject.DsShapeFileExtension + "|All files (*.*)|*.*",
                    InitialDirectory = dsShapesDirectoryInfo.FullName
                };

                if (complexDsShapeFileInfo is not null) dlg.FileName = complexDsShapeFileInfo.FullName;

                if (dlg.ShowDialog() != true) return;

                complexDsShapeFileInfo = new FileInfo(dlg.FileName);

                if (!FileSystemHelper.Compare(dsShapesDirectoryInfo.FullName,
                    complexDsShapeFileInfo.Directory?.FullName))
                {
                    MessageBoxHelper.ShowError(Resources.FileMustBeInDsShapesDir);
                    return;
                }
            }

            var drawing = NewDsShapeDrawingWithFile(complexDsShapeFileInfo, complexDsShape);

            if (drawing is null) return;

            DsProject.Instance.OnDsShapeDrawingsListChanged();

            await ShowOrOpenDrawingAsync(complexDsShapeFileInfo);
        }

        public async Task<DesignDrawingViewModel?> ShowOrOpenDrawingAsync(FileInfo? drawingFileInfo)
        {
            if (drawingFileInfo is null) return null;

            var openedDrawingViewModel = FindOpenedDrawingViewModel(drawingFileInfo);
            if (openedDrawingViewModel is not null)
                try
                {
                    var onDriveDrawingInfo =
                        DsProject.ReadDrawingInfo(new FileInfo(openedDrawingViewModel.Drawing.FileFullName),
                            true);

                    if (onDriveDrawingInfo is null ||
                        onDriveDrawingInfo.Guid == openedDrawingViewModel.Drawing.Guid)
                    {
                        FocusedDesignDrawingViewModel = openedDrawingViewModel;
                        return openedDrawingViewModel;
                    }

                    openedDrawingViewModel.CheckDataChangedFromLastSaveIfNeeded();
                    if (openedDrawingViewModel.Drawing.DataChangedFromLastSave)
                    {
                        FocusedDesignDrawingViewModel = openedDrawingViewModel;
                        MessageBoxHelper.ShowWarning(Resources.DrawingOnDiskChangedMessage + " " +
                                                     openedDrawingViewModel.Drawing.FileFullName);
                        return openedDrawingViewModel;
                    }

                    CloseDrawing(openedDrawingViewModel);
                }
                catch (Exception)
                {
                    return null;
                }

            using (GetBusyCloser())
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var drawing =
                    DsProject.ReadDrawing(drawingFileInfo, true, true);

                if (drawing is null)
                {
                    MessageBoxHelper.ShowError(Resources.ReadDrawingErrorMessage + @". " +
                                               Resources.SeeErrorLogForDetails);

                    Mouse.OverrideCursor = null;
                    return null;
                }

                if (drawing.BinDeserializationSkippedBytesCount > 0)
                {
                    DsProject.LoggersSet.Logger.LogError(drawingFileInfo.FullName + @": " +
                                 Resources.NotAllDataWasReadMessage);
                    MessageBoxHelper.ShowWarning(drawingFileInfo.FullName + @": " +
                                                 Resources.NotAllDataWasReadMessage + @" "
                                                 + Resources.SeeErrorLogForDetails);
                    if (!DsProject.Instance.IsReadOnly)
                        try
                        {
                            File.Copy(drawing.FileFullName, drawing.FileFullName + ".backup", true);
                        }
                        catch (Exception)
                        {
                        }
                }

                await RestoreComplexDsShapesAsync(null, new[] {drawing});
                drawing.DeleteUnusedFiles(true);

                var designerDrawingViewModel = new DesignDrawingViewModel(drawing, 800, 600);

                //if (OpenedDesignDrawingViewModels.Count >= OpenedDesignDrawingViewModelsMaxCount)
                //{
                //    CloseDrawing(OpenedDesignDrawingViewModels[0]);
                //}
                OpenedDesignDrawingViewModels.Add(designerDrawingViewModel);
                FocusedDesignDrawingViewModel = designerDrawingViewModel;

                await Task.Run(() =>
                {
                    WaitHandle.WaitAll(new[]
                    {
                        designerDrawingViewModel.IsInitialized, designerDrawingViewModel.IsXamlToDsShapesConverted
                    });
                });

                Mouse.OverrideCursor = null;
                return designerDrawingViewModel;
            }
        }


        public async Task<ToolkitOperationResult> RestoreComplexDsShapesAsync(IProgressInfo? progressInfo,
            DrawingBase[] drawingsToRestoreFrom)
        {
            var newDrawings = new CaseInsensitiveOrderedDictionary<DsShapeDrawing>();
            var result =
                await DsProject.Instance.RestoreComplexDsShapesAsync(progressInfo, drawingsToRestoreFrom,
                    newDrawings);

            var cancellationToken =
                progressInfo is not null ? progressInfo.GetCancellationToken() : new CancellationToken();
            var i = 0;
            var count = newDrawings.Count;
            foreach (DsShapeDrawing newDrawing in newDrawings.Values)
            {
                i += 1;
                if (cancellationToken.IsCancellationRequested) return ToolkitOperationResult.Cancelled;
                if (progressInfo is not null) await progressInfo.RefreshProgressBarAsync(i, count);

                await CreatePreviewImageAsync(newDrawing);
            }

            if (newDrawings.Count > 0) DsProject.Instance.OnDsShapeDrawingsListChanged();
            return result;
        }

        public async void ShowDrawingAndPropertiesAsync(FileInfo drawingFileInfo)
        {
            var drawingViewModel = await ShowOrOpenDrawingAsync(drawingFileInfo);

            if (drawingViewModel is not null) ShowDrawingPropertiesWindow(drawingViewModel);
        }


        public async void ShowDsShapeAndPropertiesAsync(FileInfo drawingFileInfo, DsShapeInfo dsShapeInfo)
        {
            var drawingViewModel = await ShowOrOpenDrawingAsync(drawingFileInfo);

            if (drawingViewModel is not null)
            {
                var dsShapeViewModel = drawingViewModel.GetRootDsShapeViewModel(dsShapeInfo);
                if (dsShapeViewModel is null) return;
                drawingViewModel.SelectionService.SelectOne(dsShapeViewModel);
                ShowFirstSelectedDsShapePropertiesWindow(drawingViewModel);

                SetDesignDrawingViewScale(1.5, new Point(0.5, 0.5));
                var center = dsShapeViewModel.DsShape.GetCenterInitialPositionOnDrawing();

                var showPoint = ShowPoint;
                if (showPoint is not null) showPoint(center);
            }
        }


        public async Task CreatePreviewImageAsync(DsShapeDrawing dsShapeDrawing)
        {
            if (dsShapeDrawing is null) return;

            var designerDrawingViewModel = new DesignDrawingViewModel(dsShapeDrawing, 0, 0);

            OpenedDesignDrawingViewModels.Add(designerDrawingViewModel);
            FocusedDesignDrawingViewModel = designerDrawingViewModel;

            await Task.Run(() => { designerDrawingViewModel.IsInitialized.WaitOne(30000); });

            if (designerDrawingViewModel.DesignControlsInfo is not null)
            {
                await Dispatcher.Yield(DispatcherPriority.Background);

                dsShapeDrawing.CreatePreviewImage(designerDrawingViewModel.DesignControlsInfo
                    .DesignDrawingCanvas);

                DsProject.Instance.SaveUnconditionally(dsShapeDrawing,
                    DsProject.IfFileExistsActions.AskNewFileName, false);
            }

            CloseDrawing(designerDrawingViewModel);
        }

        public async Task AllDsPagesCacheUpdateAsync()
        {
            if (!DsProject.Instance.IsInitialized)
                return;
            using (BusyCloser busyCloser = GetBusyCloser())
            {
                await busyCloser.SetHeaderAsync(Resources.ProgressInfo_AllDsPagesCacheUpdate_Header);

                FileInfo[] dsPageFileInfos = DsProject.Instance.DsPagesDirectoryInfo
                    !.EnumerateFiles(@"*" + DsProject.DsPageFileExtension, SearchOption.TopDirectoryOnly).ToArray();
                var openDsPageDrawingsErrorMessages = new List<string>();
                busyCloser.ProgressBarMaxValue = dsPageFileInfos.Length * 2;
                DrawingInfo[] onDriveDsPageDrawingInfos =
                    await DsProject.Instance.GetDrawingInfosAsync(dsPageFileInfos, busyCloser,
                        openDsPageDrawingsErrorMessages);

                await DsProject.Instance.AllDsPagesCacheUpdateAsync(onDriveDsPageDrawingInfos, busyCloser,
                    openDsPageDrawingsErrorMessages);

                if (openDsPageDrawingsErrorMessages.Count > 0)
                    MessageBoxHelper.ShowWarning(string.Join("\n", openDsPageDrawingsErrorMessages));
            }
        }

        public DsShapeBase? NewDsShape(EntityInfo? entityInfo, Point centerPosition)
        {
            if (entityInfo is null) return null;

            DsShapeBase? newDsShape = null;

            var drawingInfo = entityInfo as DrawingInfo;
            if (drawingInfo is not null)
            {
                var dsShapeDrawing =
                    DsProject.ReadDrawing(drawingInfo.FileInfo, true, true) as DsShapeDrawing;

                if (dsShapeDrawing is not null)
                    newDsShape = dsShapeDrawing.GetComplexDsShape(true);
            }
            else
            {
                newDsShape = DsShapeFactory.NewDsShape(entityInfo.Guid, true, true);
                if (newDsShape is not null) newDsShape.Name = newDsShape.GetDsShapeTypeNameToDisplay();
            }

            if (newDsShape is null) return null;

            newDsShape.CenterInitialPosition = centerPosition;

            return newDsShape;
        }


        public bool PrepareCloseDsProject()
        {
            PropertiesWindow.CloseAll();

            if (CloseAllDrawings()) return true;

            return false;
        }

        public async Task CloseDsProjectAsync()
        {
            if (!DsProject.Instance.IsInitialized || DsProject.Instance.IsReadOnly)
            {
                await Task.Delay(400);

                DsProject.Instance.Close();
                return;
            }

            using (BusyCloser busyCloser = GetBusyCloser())
            {
                await busyCloser.SetHeaderAsync(Resources.CloseDsProjectToolkitOperation);

                busyCloser.ProgressBarMaxValue = DsProject.Instance.AllDsPagesCache.Count;

                var dsPagesDirectoryInfo = DsProject.Instance.DsPagesDirectoryInfo;
                var dsShapesDirectoryInfo = DsProject.Instance.DsShapesDirectoryInfo;

                await DsProject.Instance.AllDsPagesCacheSaveAsync(busyCloser);

                DsProject.Instance.Close();

                await Task.Run(() => DsProject.DeleteUnusedFiles(dsPagesDirectoryInfo, dsShapesDirectoryInfo));
            }
        }


        public bool CloseDrawing(DesignDrawingViewModel? designerDrawingViewModel)
        {
            if (designerDrawingViewModel is null) return false;

            return CloseDrawings(new[] {designerDrawingViewModel});
        }


        public void CloseDrawingUnconditionally(DesignDrawingViewModel? designerDrawingViewModel)
        {
            if (designerDrawingViewModel is null) return;

            PropertiesWindow.CloseAllForFile(designerDrawingViewModel.Drawing.FileFullName);

            if (designerDrawingViewModel.DesignControlsInfo is not null)
            {
                designerDrawingViewModel.DesignControlsInfo.DesignDrawingCanvas.Close();
            }

            var initiallyFocused = FocusedDesignDrawingViewModel;
            var preInitiallySelected = OpenedDesignDrawingViewModels
                .Where(vm => vm != initiallyFocused).OrderBy(vm => vm.SelectedDateTime).LastOrDefault();

            OpenedDesignDrawingViewModels.Remove(designerDrawingViewModel);

            if (designerDrawingViewModel == initiallyFocused) FocusedDesignDrawingViewModel = preInitiallySelected;
            
            designerDrawingViewModel.Dispose();
        }

        public bool CloseOtherDrawings(DesignDrawingViewModel drawingViewModel)
        {
            var fileInfo = new FileInfo(drawingViewModel.Drawing.FileFullName);
            return CloseDrawings(OpenedDesignDrawingViewModels.Where(dvm => !FileSystemHelper.Compare(
                dvm.Drawing.FileFullName,
                fileInfo.FullName)).ToArray());
        }

        public bool CloseAllDrawings(List<DrawingInfo>? closedDrawingInfos = null)
        {
            return CloseDrawings(OpenedDesignDrawingViewModels.ToArray(), closedDrawingInfos);
        }

        public async void OpenDsShapeDrawingFromComplexDsShapeAsync()
        {
            if (FocusedDesignDrawingViewModel is null) return;
            var dsShapeViewModel = FocusedDesignDrawingViewModel.SelectionService.SelectedItems.FirstOrDefault();
            if (dsShapeViewModel is null) return;

            var complexDsShape = dsShapeViewModel.DsShape as ComplexDsShape;
            if (complexDsShape is null) return;

            CaseInsensitiveOrderedDictionary<DsShapeDrawingInfo> drawingInfos =
                DsProject.Instance.GetAllComplexDsShapesDrawingInfos();
            DrawingInfo? drawingInfo = null;
            if (!string.IsNullOrEmpty(complexDsShape.DsShapeDrawingName))
                drawingInfo = drawingInfos.TryGetValue(complexDsShape.DsShapeDrawingName);

            if (drawingInfo is not null && drawingInfo.Guid == complexDsShape.DsShapeDrawingGuid)
            {
                await ShowOrOpenDrawingAsync(drawingInfo.FileInfo);
                return;
            }

            NewDsShapeDrawingWithFileAndOpenAsync(complexDsShape);
        }

        public DesignDrawingViewModel? FindOpenedDrawingViewModel(FileInfo fileInfo)
        {
            return
                OpenedDesignDrawingViewModels.FirstOrDefault(
                    dvm => FileSystemHelper.Compare(dvm.Drawing.FileFullName, fileInfo.FullName));
        }

        public void SetDesignDrawingViewScale(double value, Point? immovableRelativePoint)
        {
            if (value < 0.1) value = 0.1;
            else if (value > 100) value = 100;
            if (Math.Abs(value - DesignDrawingViewScale) < 0.001) return;

            if (_focusedDesignDrawingViewModel is not null)
                _focusedDesignDrawingViewModel.SetViewScale(DesignDrawingViewScale, value, immovableRelativePoint);           
            
            DesignDrawingViewScale = value;
            OnPropertyChanged(nameof(ScaleComboBoxText));
            OnPropertyChanged(nameof(DesignDrawingViewScale));
        }

        public BusyCloser GetBusyCloser()
        {
            return new(this);
        }

        public void UpdateSelection(Rect rubberBand)
        {
            if (FocusedDesignDrawingViewModel is null) return;

            FocusedDesignDrawingViewModel.UpdateSelection(rubberBand);
        }

        public void ShowDrawingPropertiesWindow(DesignDrawingViewModel designerDrawingViewModel)
        {
            designerDrawingViewModel.PropertiesWindowsCountIncrement();

            DrawingBase drawing = designerDrawingViewModel.Drawing;

            if (drawing is DsShapeDrawing || drawing.IsFaceplate)
                drawing.RefreshDsConstantsCollection();
            else
                ConstantsHelper.UpdateDsConstants(drawing.DsConstantsCollection,
                    drawing.DsConstantsCollection.OrderBy(gpi => gpi.Name).ToArray());

            PropertiesWindow.Show(MessageBoxHelper.GetRootWindow(), drawing, drawing.FileFullName,
                w => designerDrawingViewModel.PropertiesWindowsCountDecrement());
        }


        public void ShowFirstSelectedDsShapePropertiesWindow(DesignDrawingViewModel? designerDrawingViewModel)
        {
            if (designerDrawingViewModel is null)
                return;
            DsShapeViewModel[] dsShapeViewModels = designerDrawingViewModel.SelectionService.SelectedItems;
            if (dsShapeViewModels.Length > 0)
            {
                designerDrawingViewModel.PropertiesWindowsCountIncrement();

                DsShapeBase dsShape = dsShapeViewModels[0].DsShape;
                dsShape.PropertyChanged += designerDrawingViewModel.DsShapeOnPropertyChanged;

                var complexDsShape = dsShape as ComplexDsShape;
                if (complexDsShape is not null)
                    ConstantsHelper.UpdateDsConstants(complexDsShape.DsConstantsCollection,
                        complexDsShape.DsConstantsCollection.OrderBy(gpi => gpi.Name).ToArray());

                var parentDrawing = dsShape.GetParentDrawing();
                PropertiesWindow.Show(MessageBoxHelper.GetRootWindow(), dsShape,
                    parentDrawing is not null ? parentDrawing.FileFullName : "",
                    w => { dsShape.PropertyChanged -= designerDrawingViewModel.DsShapeOnPropertyChanged; });
            }
        }

        #endregion

        #region private functions

        private DsPageDrawing? NewDsPageDrawingWithFile(FileInfo dsPageFileInfo)
        {
            if (!DsProject.Instance.IsInitialized) return null;

            try
            {
                DsPageDrawing drawing =
                    DsProject.Instance.NewDsPageDrawingObject(
                        Path.GetFileNameWithoutExtension(dsPageFileInfo.Name), false);
                if (drawing is not null)
                {
                    var openedDrawingViewModel =
                        FindOpenedDrawingViewModel(dsPageFileInfo);
                    if (openedDrawingViewModel is not null)
                    {
                        openedDrawingViewModel.CheckDataChangedFromLastSaveIfNeeded();
                        if (openedDrawingViewModel.Drawing.DataChangedFromLastSave)
                        {
                            MessageBoxHelper.ShowError(Resources.OpenedUnsavedDrawingExistsMessage +
                                                       " " +
                                                       openedDrawingViewModel.Drawing.Name);
                            return null;
                        }

                        CloseDrawing(openedDrawingViewModel);
                    }
                }

                DsProject.Instance.SaveUnconditionally(drawing, DsProject.IfFileExistsActions.CreateBackupAndWarn,
                    false);

                return drawing;
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }

            return null;
        }


        private DsShapeDrawing? NewDsShapeDrawingWithFile(FileInfo? complexDsShapeFileInfo,
            ComplexDsShape? complexDsShape = null)
        {
            if (!DsProject.Instance.IsInitialized) return null;
            if (complexDsShapeFileInfo is null) return null;

            try
            {
                DsShapeDrawing drawing = DsProject.Instance.NewDsShapeDrawingObject(
                    Path.GetFileNameWithoutExtension(complexDsShapeFileInfo.Name), false, complexDsShape);

                if (drawing is not null)
                {
                    var openedDrawingViewModel =
                        FindOpenedDrawingViewModel(complexDsShapeFileInfo);
                    if (openedDrawingViewModel is not null)
                    {
                        openedDrawingViewModel.CheckDataChangedFromLastSaveIfNeeded();
                        if (openedDrawingViewModel.Drawing.DataChangedFromLastSave)
                        {
                            MessageBoxHelper.ShowError(Resources.OpenedUnsavedDrawingExistsMessage +
                                                       " " +
                                                       openedDrawingViewModel.Drawing.Name);
                            return null;
                        }

                        CloseDrawing(openedDrawingViewModel);
                    }
                }

                DsProject.Instance.SaveUnconditionally(drawing, DsProject.IfFileExistsActions.CreateBackupAndWarn,
                    false);

                return drawing;
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }

            return null;
        }


        private bool CloseDrawings(DesignDrawingViewModel[] drawingViewModels,
            List<DrawingInfo>? closedDrawingInfos = null)
        {
            var globalMessageBoxResult = WpfMessageBoxResult.Cancel;

            var dsPageDrawingsListChanged = false;

            foreach (DesignDrawingViewModel dvm in drawingViewModels)
            {
                DrawingInfo? closedDrawingInfo = null;
                if (closedDrawingInfos is not null) closedDrawingInfo = dvm.Drawing.GetDrawingInfo();

                dvm.CheckDataChangedFromLastSaveIfNeeded();
                if (dvm.Drawing.DataChangedFromLastSave)
                {
                    var messageBoxResult = WpfMessageBoxResult.Cancel;

                    if (globalMessageBoxResult == WpfMessageBoxResult.Cancel)
                    {
                        FocusedDesignDrawingViewModel = dvm;

                        if (drawingViewModels.Length == 1)
                            messageBoxResult = WpfMessageBox.Show(MessageBoxHelper.GetRootWindow(),
                                dvm.Drawing.Name + ": " + Resources.MessageSaveFileQuestion,
                                Resources.QuestionMessageBoxCaption,
                                WpfMessageBoxButton.YesNoCancel,
                                MessageBoxImage.Question);
                        else
                            messageBoxResult = WpfMessageBox.Show(MessageBoxHelper.GetRootWindow(),
                                dvm.Drawing.Name + ": " + Resources.MessageSaveFileQuestion,
                                Resources.QuestionMessageBoxCaption,
                                WpfMessageBoxButton.YesNoYesAllNoAllCancel,
                                MessageBoxImage.Question);

                        switch (messageBoxResult)
                        {
                            case WpfMessageBoxResult.YesForAll:
                                globalMessageBoxResult = WpfMessageBoxResult.YesForAll;
                                break;
                            case WpfMessageBoxResult.NoForAll:
                                globalMessageBoxResult = WpfMessageBoxResult.NoForAll;
                                break;
                        }
                    }

                    switch (globalMessageBoxResult)
                    {
                        case WpfMessageBoxResult.YesForAll:
                            messageBoxResult = WpfMessageBoxResult.Yes;
                            break;
                        case WpfMessageBoxResult.NoForAll:
                            messageBoxResult = WpfMessageBoxResult.No;
                            break;
                    }

                    switch (messageBoxResult)
                    {
                        case WpfMessageBoxResult.Yes:
                            if (DsProject.Instance.SaveUnconditionally(dvm.Drawing,
                                DsProject.IfFileExistsActions.AskNewFileName)) return true;
                            break;
                        case WpfMessageBoxResult.No:
                            dsPageDrawingsListChanged =
                                true; // Possible difference with drawing on disk.                            
                            break;
                        default:
                            return true;
                    }
                }

                CloseDrawingUnconditionally(dvm);

                if (closedDrawingInfos is not null && closedDrawingInfo is not null) closedDrawingInfos.Add(closedDrawingInfo);
            }

            if (dsPageDrawingsListChanged) DsProject.Instance.OnDsPageDrawingsListChanged();

            return false;
        }

        #endregion

        #region private fields

        private readonly CommandBinding DsShapeMoveLeftCommandBinding;
        private readonly CommandBinding DsShapeMoveUpCommandBinding;
        private readonly CommandBinding DsShapeMoveRightCommandBinding;
        private readonly CommandBinding DsShapeMoveDownCommandBinding;
        private readonly CommandBinding RotateCounterClockwiseCommandBinding;
        private readonly CommandBinding RotateClockwiseCommandBinding;
        private readonly CommandBinding FlipHorizontalCommandBinding;
        private readonly CommandBinding BringForwardCommandBinding;
        private readonly CommandBinding BringToFrontCommandBinding;
        private readonly CommandBinding SendBackwardCommandBinding;
        private readonly CommandBinding SendToBackCommandBinding;
        private readonly CommandBinding DsShapeRotateXRightCommandBinding;
        private readonly CommandBinding DsShapeRotateXLeftCommandBinding;
        private readonly CommandBinding DsShapeRotateYRightCommandBinding;
        private readonly CommandBinding DsShapeRotateYLeftCommandBinding;
        private readonly CommandBinding DsShapeFieldOfViewIncreaseCommandBinding;
        private readonly CommandBinding DsShapeFieldOfViewDecreaseCommandBinding;
        private readonly CommandBinding GroupCommandBinding;
        private readonly CommandBinding UngroupCommandBinding;
        private readonly CommandBinding UngroupAndReplaceConstantsCommandBinding;
        private readonly CommandBinding ConvertToComplexDsShapeCommandBinding;
        private readonly CommandBinding EditGeometryCommandBinding;
        private readonly CommandBinding AlignTopCommandBinding;
        private readonly CommandBinding AlignVerticalCentersCommandBinding;
        private readonly CommandBinding AlignBottomCommandBinding;
        private readonly CommandBinding AlignLeftCommandBinding;
        private readonly CommandBinding AlignHorizontalCentersCommandBinding;
        private readonly CommandBinding AlignRightCommandBinding;
        private readonly CommandBinding EqualizeWidthCommandBinding;
        private readonly CommandBinding EqualizeHeightCommandBinding;
        private readonly CommandBinding CropUnusedSpaceCommandBinding;
        private readonly CommandBinding DsShapeLockCommandBinding;
        private readonly CommandBinding DsShapeUnlockCommandBinding;
        private readonly CommandBinding DsShapeExportPropertiesCommandBinding;
        private readonly CommandBinding DsShapeImportPropertiesCommandBinding;
        private readonly CommandBinding DsShapeDockCommandBinding;
        private readonly CommandBinding DsShapeDockLeftCommandBinding;
        private readonly CommandBinding DsShapeDockTopCommandBinding;
        private readonly CommandBinding DsShapeDockRightCommandBinding;
        private readonly CommandBinding DsShapeDockBottomCommandBinding;
        private readonly CommandBinding DistributeHorizontalCommandBinding;
        private readonly CommandBinding DistributeVerticalCommandBinding;

        private DesignDrawingViewModel? _focusedDesignDrawingViewModel;

        private bool _isBusy;
        private string _busyHeader = @"";
        private string _busyDescription = @"";
        private double _busyProgressBarPercent;
        private CancellationTokenSource? _busyCancellationTokenSource;
        private string _title = @"";
        private double _zoom = 1.0;
        private bool _discreteMode;
        private uint _discreteModeStep;
        private bool _showDsShapesInfoTooltips;
        private double _dsShapesInfoFontSizeScale;
        private double _dsShapesInfoOpacity;
        private DsShapesOrderingEnum _dsShapesOrdering = DsShapesOrderingEnum.DsShapeZIndex;
        private bool _forceDisableXamlToDsShapesConversion;
        private bool _forceEnableXamlToDsShapesConversion;
        private readonly DispatcherTimer _usedMemoryRefreshTimer;

        #endregion
    }

    public enum DsShapesOrderingEnum : int
    {
        DsShapeZIndex = 0,
        DsShapeType = 1,
        DsShapeName = 2
    }
}