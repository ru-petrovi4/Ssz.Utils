using Ssz.Operator.Core.Utils;
using Ssz.Utils; 
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.VisualEditors;
using Ssz.Operator.Core.VisualEditors.AddDrawingsFromLibrary;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Operator.Design.Core.Controls;
using Ssz.Operator.Core.FindReplace;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using ComboBox = System.Windows.Controls.ComboBox;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using static Ssz.Operator.Core.DsProjectExtensions;
using Ssz.Utils.Wpf.WpfMessageBox;
using Ssz.Operator.Core.Utils.WinApi;
using Ssz.Operator.Core.Constants;
using Ssz.WindowsAPICodePack.Dialogs;
using Ssz.Utils.Wpf;
using System.Globalization;
using Fluent;
using Ssz.Operator.Core.Addons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Ssz.Operator.Design.Core
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Fluent.RibbonWindow
    {
        #region construction and destruction

        public MainWindow(Options commandLineOptions)
        {
            CommandLineOptions = commandLineOptions;

            // Key Gestures
            ApplicationCommands.New.InputGestures.Clear();
            ApplicationCommands.Open.InputGestures.Clear();
            SaveAll.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));

            InitializeComponent();

            SnapsToDevicePixels = true;

            Instance = this;

            DataContext = DesignDsProjectViewModel.Instance;

            DesignDsProjectViewModel.Instance.PropertyChanged += DesignDsProjectViewModel_OnPropertyChanged;                

            RecentFilesItemsControl.ItemsSource =
                DesignDsProjectViewModel.Instance.RecentFilesCollectionManager.RecentFilesCollection;

            DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels.CollectionChanged += OpenedDrawingViewModels_OnCollectionChanged;
            DesignDsProjectViewModel.Instance.ShowPoint += p => DesignDrawingDockControl.ShowOnViewportCenter(
                DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel,
                p.X, p.Y);

            DsProject.Instance.OnInitializing += () =>
            {
                OpenDsShapeDrawingsErrorMessages = null;
            };

            DsProject.Instance.Initialized += FillInToolkitRibbonTab;

            DsProject.Instance.DsProjectFileInfoChanged += () => Dispatcher.BeginInvoke(new Action(RefreshWindowTitle));
            DsProject.Instance.IsReadOnlyChanged += () => Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshWindowTitle();
                if (DsProject.Instance.IsReadOnly)
                {
                    MessageBoxHelper.ShowWarning(Properties.Resources.DsProjectIsReadOnlyMessage);
                }
            }));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Help, DesignDsProjectViewModel.Instance.HelpExecuted));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Stop, DesignDsProjectViewModel.Instance.StopExecuted));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, DesignDsProjectViewModel.Instance.CutExecuted,
                DesignDsProjectViewModel.Instance.CutEnabled));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, DesignDsProjectViewModel.Instance.CopyExecuted,
                DesignDsProjectViewModel.Instance.CopyEnabled));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, DesignDsProjectViewModel.Instance.PasteExecuted,
                DesignDsProjectViewModel.Instance.PasteEnabled));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, DesignDsProjectViewModel.Instance.DeleteExecuted,
                DesignDsProjectViewModel.Instance.DeleteEnabled));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll,
                DesignDsProjectViewModel.Instance.SelectAllExecuted));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, NewExecutedAsync));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenExecutedAync));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, SaveExecuted, SaveEnabled));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, SaveAsExecuted, SaveAsEnabled));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Print, PrintExecuted, PrintEnabled));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, DesignDsProjectViewModel.Instance.UndoExecuted, DesignDsProjectViewModel.Instance.UndoEnabled));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, DesignDsProjectViewModel.Instance.RedoExecuted, DesignDsProjectViewModel.Instance.RedoEnabled));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Find,
                (sender, args) => FindReplaceDialog.ShowAsFind(this), DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace,
                (sender, args) => FindReplaceDialog.ShowAsReplace(this), DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Properties, PropertiesExecuted, DsProjectLoaded));

            CommandBindings.Add(new CommandBinding(ExportToXaml,
                (sender, e) => DoToolkitOperationAsync(ExportToXamlToolkitOperation, true), DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(ImportFromXaml,
                (sender, e) => DoToolkitOperationAsync(ImportFromXamlToolkitOperation, true), DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(AddDsPagesAndDsShapesFromLibrary,
                AddDsPagesAndDsShapesFromLibraryExecutedAsync, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(CreateDsPages,
                (sender, e) => DoToolkitOperationAsync(CreateDsPagesToolkitOperation, false), DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(UpdateDsPages,
                (sender, e) => DoToolkitOperationAsync(UpdateDsPagesToolkitOperation, true), DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(Run, RunExecuted, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(RunCurrent, RunCurrentExecuted, RunCurrentEnabled));
            CommandBindings.Add(new CommandBinding(SaveAll, SaveAllExecuted, SaveAllEnabled));
            CommandBindings.Add(new CommandBinding(NewDsPageDrawing, NewDsPageDrawingExecuted, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(NewDsShapeDrawing, NewDsShapeDrawingExecuted, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(OpenDsPages, OpenDsPagesExecutedAsync, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(OpenFilesLocationOfDsPages, OpenFilesLocationOfDsPagesExecuted, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(SetAsDsProjectStartDsPage, SetAsDsProjectStartDsPageExecuted, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(RenameDsPage, RenameDsPageExecutedAsync, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(DeleteDsPages, DeleteDsPagesExecuted, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(DeleteComplexDsShapes, DeleteComplexDsShapesExecuted, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(UpdateComplexDsShapesOnDsPages, UpdateComplexDsShapesOnSelectedDsPagesExecutedAsync,
                DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(UpdateComplexDsShapesExtended, (sender, e) => DoToolkitOperationAsync(UpdateComplexDsShapesExtendedToolkitOperation, true),
                DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(ShowDsPageTypeObjectPropertiesCommand,
                ShowDsPageTypeObjectPropertiesExecuted,
                DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(ShowDrawingPropertiesCommand, ShowDrawingPropertiesExecuted,
                DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(UpdateComplexDsShapesOnAllDsPages,
                (sender, e) => DoToolkitOperationAsync(UpdateComplexDsShapesOnAllDsPagesToolkitOperation, true), DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(OpenComplexDsShapes, OpenComplexDsShapesExecutedAsync, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(OpenFilesLocationOfComplexDsShapes,
                OpenFilesLocationOfComplexDsShapesExecuted,
                DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(RenameComplexDsShape, RenameComplexDsShapeExecuted, DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(UpdateComplexDsShapesSize,
                (sender, e) => DoToolkitOperationAsync(
                    UpdateComplexDsShapesSizeToolkitOperation, true,
                    DsShapeDrawingInfosSelectionService.SelectedItems.Select(vm => vm.DrawingInfo).ToArray()),
                DsProjectLoaded));

            CommandBindings.Add(new CommandBinding(DiscreteMode, DiscreteModeExecuted));
            CommandBindings.Add(new CommandBinding(ShowHideDsShapesInfoTooltips, ShowHideDsShapesInfoTooltipsExecuted));

            CommandBindings.Add(new CommandBinding(DebugFindDuplicates,
                (sender, args) => FindReplaceDialog.ShowAsDebugFind(FindReplaceViewModel.DuplicatesQueryString, this), DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(DebugFindIncorrectDsPagesRefs,
                (sender, args) => FindReplaceDialog.ShowAsDebugFind(FindReplaceViewModel.IncorrectDsPagesRefsQueryString, this), DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(DebugFindIncorrectOpcTags,
                (sender, args) => FindReplaceDialog.ShowAsDebugFind(FindReplaceViewModel.IncorrectOpcTagsQueryString, this), DsProjectLoaded));
            CommandBindings.Add(new CommandBinding(DebugFindIncorrectExpressions,
                (sender, args) => FindReplaceDialog.ShowAsDebugFind(FindReplaceViewModel.IncorrectExpressionsQueryString, this), DsProjectLoaded));

            CommandBindings.Add(new CommandBinding(OpenDsShapeDrawingFromComplexDsShape,
                OpenDsShapeDrawingFromComplexDsShapeExecuted,
                OpenDsShapeDrawingFromComplexDsShapeEnabled));

            CommandBindings.Add(new CommandBinding(SetMark0, SetMark0Executed));
            CommandBindings.Add(new CommandBinding(SetMark1, SetMark1Executed));
            CommandBindings.Add(new CommandBinding(SetMark2, SetMark2Executed));
            CommandBindings.Add(new CommandBinding(SetMark3, SetMark3Executed));
            CommandBindings.Add(new CommandBinding(SetMark4, SetMark4Executed));
            CommandBindings.Add(new CommandBinding(SetMark5, SetMark5Executed));
            CommandBindings.Add(new CommandBinding(SetMark6, SetMark6Executed));

            DesignDsProjectViewModel.Instance.AddCommandBindings(CommandBindings);

            Loaded += OnLoaded;

            if (!String.IsNullOrEmpty(CommandLineOptions.DsProjectFile))
            {
                ReadDsProjectFromBinFileAsync(CommandLineOptions.DsProjectFile);
            }
        }        

        #endregion

        #region public functions

        public static readonly RoutedCommand ExportToXaml = new RoutedCommand();
        public static readonly RoutedCommand ImportFromXaml = new RoutedCommand();
        public static readonly RoutedCommand AddDsPagesAndDsShapesFromLibrary = new RoutedCommand();
        public static readonly RoutedCommand CreateDsPages = new RoutedCommand();
        public static readonly RoutedCommand UpdateDsPages = new RoutedCommand();
        public static readonly RoutedCommand Run = new RoutedCommand();
        public static readonly RoutedCommand RunCurrent = new RoutedCommand();
        public static readonly RoutedCommand SaveAll = new RoutedCommand();
        public static readonly RoutedCommand NewDsPageDrawing = new RoutedCommand();
        public static readonly RoutedCommand NewDsShapeDrawing = new RoutedCommand();
        public static readonly RoutedCommand UpdateComplexDsShapesOnAllDsPages = new RoutedCommand();
        public static readonly RoutedCommand OpenDsPages = new RoutedCommand();
        public static readonly RoutedCommand OpenFilesLocationOfDsPages = new RoutedCommand();
        public static readonly RoutedCommand SetAsDsProjectStartDsPage = new RoutedCommand();
        public static readonly RoutedCommand RenameDsPage = new RoutedCommand();
        public static readonly RoutedCommand DeleteDsPages = new RoutedCommand();
        public static readonly RoutedCommand DeleteComplexDsShapes = new RoutedCommand();
        public static readonly RoutedCommand UpdateComplexDsShapesOnDsPages = new RoutedCommand();
        public static readonly RoutedCommand UpdateComplexDsShapesExtended = new RoutedCommand();
        public static readonly RoutedCommand ShowDsPageTypeObjectPropertiesCommand = new RoutedCommand();
        public static readonly RoutedCommand ShowDrawingPropertiesCommand = new RoutedCommand();
        public static readonly RoutedCommand OpenComplexDsShapes = new RoutedCommand();
        public static readonly RoutedCommand OpenFilesLocationOfComplexDsShapes = new RoutedCommand();
        public static readonly RoutedCommand RenameComplexDsShape = new RoutedCommand();
        public static readonly RoutedCommand UpdateComplexDsShapesSize = new RoutedCommand();
        public static readonly RoutedCommand OpenDsShapeDrawingFromComplexDsShape = new RoutedCommand();
        public static readonly RoutedCommand DiscreteMode = new RoutedCommand();
        public static readonly RoutedCommand ShowHideDsShapesInfoTooltips = new RoutedCommand();
        public static readonly RoutedCommand DebugFindDuplicates = new RoutedCommand();
        public static readonly RoutedCommand DebugFindIncorrectDsPagesRefs = new RoutedCommand();
        public static readonly RoutedCommand DebugFindIncorrectOpcTags = new RoutedCommand();
        public static readonly RoutedCommand DebugFindIncorrectExpressions = new RoutedCommand();

        public static readonly RoutedCommand SetMark0 = new RoutedCommand();
        public static readonly RoutedCommand SetMark1 = new RoutedCommand();
        public static readonly RoutedCommand SetMark2 = new RoutedCommand();
        public static readonly RoutedCommand SetMark3 = new RoutedCommand();
        public static readonly RoutedCommand SetMark4 = new RoutedCommand();
        public static readonly RoutedCommand SetMark5 = new RoutedCommand();
        public static readonly RoutedCommand SetMark6 = new RoutedCommand();

        public static MainWindow Instance { get; private set; } = null!;

        public Options CommandLineOptions { get; }

        public void ShowDsPageTypeObjectProperties(DsPageDrawingInfoViewModel? dsPageDrawingViewModel)
        {
            if (dsPageDrawingViewModel is null) return;
            var drawingInfo = dsPageDrawingViewModel.DrawingInfo;

            DesignDrawingViewModel? drawingViewModel =
                DesignDsProjectViewModel.Instance.FindOpenedDrawingViewModel(drawingInfo.FileInfo);

            if (drawingViewModel is not null)
            {
                if (drawingViewModel.Drawing is DsPageDrawing)
                {
                    ICloneable? valueResult = CloneableObjectPropertiesDialog.ShowDialog(((DsPageDrawing)drawingViewModel.Drawing!).DsPageTypeObject!);
                    if (valueResult is not null)
                    {
                        ((DsPageDrawing)drawingViewModel.Drawing).DsPageTypeObject =
                            (DsPageTypeBase)valueResult;
                    }
                }
            }
            else
            {
                var dsPageDrawing = DsProject.ReadDrawing(drawingInfo.FileInfo, false, true) as DsPageDrawing;
                if (dsPageDrawing is null) return;
                ICloneable? valueResult = CloneableObjectPropertiesDialog.ShowDialog(dsPageDrawing.DsPageTypeObject!, true);
                if (valueResult is not null)
                {
                    dsPageDrawing.DsPageTypeObject =
                        (DsPageTypeBase)valueResult;

                    DsProject.Instance.SaveUnconditionally(dsPageDrawing, DsProject.IfFileExistsActions.CreateBackup, true);

                    dsPageDrawingViewModel.EntityInfo = dsPageDrawing.GetDrawingInfo();
                }
                dsPageDrawing.Dispose();
            }
        }

        public void OnStartDsPageChanged()
        {
            if (!DsProject.Instance.IsInitialized) return;

            FileInfo? startDsPageFileInfo =
                DsProject.Instance.GetExistingDsPageFileInfoOrNull(DsProject.Instance.RootWindowProps.FileRelativePath);

            //DsProjectInfoViewModel.Instance.IsDsProjectStartupWindowDefined = (startDsPageFileInfo is not null);

            foreach (DsPageDrawingInfoViewModel dsPageDrawingInfoViewModel in DsPageDrawingInfosSelectionService.AllItems)
            {
                if (startDsPageFileInfo is not null &&
                        FileSystemHelper.Compare(dsPageDrawingInfoViewModel.DrawingInfo.FileInfo.FullName,
                            startDsPageFileInfo.FullName))
                {
                    dsPageDrawingInfoViewModel.IsStartDsPage = true;
                }
                else
                {
                    dsPageDrawingInfoViewModel.IsStartDsPage = false;
                }
            }
        }

        public void SyncWithActiveDrawingButtonOnClick()
        {
            var focusedDesignDrawingViewModel = DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
            if (focusedDesignDrawingViewModel is null) return;

            var drawingFileInfo = new FileInfo(focusedDesignDrawingViewModel.Drawing.FileFullName);
            if (focusedDesignDrawingViewModel.Drawing is DsPageDrawing)
            {
                DsPageDrawingInfoViewModel? drawingInfoViewModel = DsPageDrawingInfosSelectionService.AllItems
                    .FirstOrDefault(i => FileSystemHelper.Compare(i.DrawingInfo.FileInfo.FullName, drawingFileInfo.FullName));
                if (drawingInfoViewModel is not null)
                {
                    DockingManagerViewModel.Instance.ActiveContent = DockingManagerViewModel.Instance.DsPagesListDockViewModel;
                    DsPageDrawingInfosSelectionService.SelectOne(drawingInfoViewModel);
                }
            }
            else
            {
                DrawingInfoViewModel? drawingInfoViewModel = DsShapeDrawingInfosSelectionService.AllItems
                    .FirstOrDefault(i => FileSystemHelper.Compare(i.DrawingInfo.FileInfo.FullName, drawingFileInfo.FullName));
                if (drawingInfoViewModel is not null)
                {
                    DockingManagerViewModel.Instance.ActiveContent = DockingManagerViewModel.Instance.DsShapesListDockViewModel;
                    DsShapeDrawingInfosSelectionService.SelectOne(drawingInfoViewModel);
                }
            }
        }

        #endregion

        #region internal functions

        internal DesignDrawingCanvas? FocusedDesignDrawingCanvas
        {
            get
            {
                var selectedDrawingViewModel = DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
                if (selectedDrawingViewModel is null || selectedDrawingViewModel.DesignControlsInfo is null) return null;
                return selectedDrawingViewModel.DesignControlsInfo.DesignDrawingCanvas;
            }
        }

        internal ScrollViewer? FocusedScrollViewer
        {
            get
            {
                var selectedDrawingViewModel = DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
                if (selectedDrawingViewModel is null || selectedDrawingViewModel.DesignControlsInfo is null) return null;
                return selectedDrawingViewModel.DesignControlsInfo.ScrollViewer;
            }
        }

        #endregion        

        #region protected functions

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {                
                case Key.Left:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        if (DesignDsProjectViewModel.RotateCounterClockwise.CanExecute(null, this))
                        {
                            DesignDsProjectViewModel.RotateCounterClockwise.Execute(null, this);
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        if (DesignDsProjectViewModel.DsShapeMoveLeft.CanExecute(null, this))
                        {
                            DesignDsProjectViewModel.DsShapeMoveLeft.Execute(null, this);
                            e.Handled = true;
                        }
                    }                    
                    break;
                case Key.Up:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        {
                            if (DesignDsProjectViewModel.SendToBack.CanExecute(null, this))
                            {
                                DesignDsProjectViewModel.SendToBack.Execute(null, this);
                                e.Handled = true;
                            }
                        }
                        else
                        {
                            if (DesignDsProjectViewModel.SendBackward.CanExecute(null, this))
                            {
                                DesignDsProjectViewModel.SendBackward.Execute(null, this);
                                e.Handled = true;
                            }
                        }
                    }
                    else
                    {
                        if (DesignDsProjectViewModel.DsShapeMoveUp.CanExecute(null, this))
                        {
                            DesignDsProjectViewModel.DsShapeMoveUp.Execute(null, this);
                            e.Handled = true;
                        }
                    }                    
                    break;
                case Key.Right:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        if (DesignDsProjectViewModel.RotateClockwise.CanExecute(null, this))
                        {
                            DesignDsProjectViewModel.RotateClockwise.Execute(null, this);
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        if (DesignDsProjectViewModel.DsShapeMoveRight.CanExecute(null, this))
                        {
                            DesignDsProjectViewModel.DsShapeMoveRight.Execute(null, this);
                            e.Handled = true;
                        }
                    }                    
                    break;
                case Key.Down:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        {
                            if (DesignDsProjectViewModel.BringToFront.CanExecute(null, this))
                            {
                                DesignDsProjectViewModel.BringToFront.Execute(null, this);
                                e.Handled = true;
                            }
                        }
                        else
                        {
                            if (DesignDsProjectViewModel.BringForward.CanExecute(null, this))
                            {
                                DesignDsProjectViewModel.BringForward.Execute(null, this);
                                e.Handled = true;
                            }
                        }
                    }
                    else
                    {
                        if (DesignDsProjectViewModel.DsShapeMoveDown.CanExecute(null, this))
                        {
                            DesignDsProjectViewModel.DsShapeMoveDown.Execute(null, this);
                            e.Handled = true;
                        }
                    }                                        
                    break;                
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!DsProject.Instance.IsInitialized) return;

            e.Cancel = true;

            if (DesignDsProjectViewModel.Instance.PrepareCloseDsProject()) return;
            
            CloseDsProjectAndWindowAsync();
        }

        #endregion

        #region private functions

        private SelectionService<DsPageDrawingInfoViewModel> DsPageDrawingInfosSelectionService
            => DockingManagerViewModel.Instance.DsPagesListDockViewModel.DsPageDrawingInfosSelectionService;

        private SelectionService<DrawingInfoViewModel> DsShapeDrawingInfosSelectionService
            => DockingManagerViewModel.Instance.DsShapesListDockViewModel.DsShapeDrawingInfosSelectionService;

        private List<string>? OpenDsShapeDrawingsErrorMessages
        {
            get => DockingManagerViewModel.Instance.DsShapesListDockViewModel.OpenDsShapeDrawingsErrorMessages;
            set => DockingManagerViewModel.Instance.DsShapesListDockViewModel.OpenDsShapeDrawingsErrorMessages = value;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {            
            RefreshWindowTitle();            

            if (!String.IsNullOrWhiteSpace(CommandLineOptions.ToolkitOperation))
            {
                if (DsProject.Instance.IsInitialized) DoToolkitOperation(CommandLineOptions.ToolkitOperation);
                else
                {
                    DsProject.Instance.Initialized += () => DoToolkitOperation(CommandLineOptions.ToolkitOperation);
                }
            }
            else
            {
                DockingManagerViewModel.Instance.ActiveContent = DockingManagerViewModel.Instance.DsPagesListDockViewModel;
            }
        }

        private void ZoomSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var textFormattingMode = e.NewValue > 1.0 || Math.Abs(e.NewValue - 1.0) < double.Epsilon ? TextFormattingMode.Ideal : TextFormattingMode.Display;
            TextOptions.SetTextFormattingMode(this, textFormattingMode);
        }

        private void DoToolkitOperation(string ToolkitOperation)
        {
            //ToolkitRibbonTab.IsSelected = true;

            //for (int i = 0; i < ToolkitRibbonTab.Items.Count; i += 1)
            //{
            //    for (int j = 0; j < ((RibbonGroup)ToolkitRibbonTab.Items[i]).Items.Count; j += 1)
            //    {
            //        foreach (
            //        var button in
            //            TreeHelper.FindChildsOrSelf<Button>((DependencyObject)((RibbonGroup)ToolkitRibbonTab.Items[i]).Items[j],
            //                b => b.Tag is ToolkitOperation))
            //        {
            //            if (StringHelper.CompareIgnoreCase(ToolkitOperation, button.Tag.GetType().Name))
            //            {
            //                button.Command.Execute(null);
            //                return;
            //            }
            //        }
            //    }
            //}
        }

        private async void CloseDsProjectAndWindowAsync()
        {
            await DesignDsProjectViewModel.Instance.CloseDsProjectAsync();

            DesignDsProjectViewModel.Instance.Dispose();

            await DsDataAccessProvider.StaticDisposeAsync();

            Close();
        }

        private async void ReadDsProjectFromBinFileAsync(string dsProjectFileName)
        {
            var dsProjectDirectoryName = Path.GetDirectoryName(dsProjectFileName);
            //bool isReadOnly = !App.HasMaintenanceLicense; // ULM support
            bool isReadOnly = false; // ULM support
            if (!isReadOnly && Directory.Exists(dsProjectDirectoryName) &&
                !FileSystemHelper.IsDirectoryWritable(dsProjectDirectoryName))
            {
                MessageBoxHelper.ShowError(Properties.Resources.DsProjectDirectoryWriteAccessError);
                isReadOnly = true;
            }

            using (var busyCloser = DesignDsProjectViewModel.Instance.GetBusyCloser())
            {
                await busyCloser.SetHeaderAsync(Properties.Resources.ProgressInfo_LoadingDsProject_Header);

                await DsProject.ReadDsProjectFromBinFileAsync(dsProjectFileName, DsProject.DsProjectModeEnum.VisualDesignMode,
                            isReadOnly, CommandLineOptions.AutoConvert, @"", busyCloser);                
            }

            if (!String.IsNullOrEmpty(DsProject.Instance.DsProjectFileFullName))
                DesignDsProjectViewModel.Instance.RecentFilesCollectionManager.Add(DsProject.Instance.DsProjectFileFullName!);
        }        

        private void DsProjectLoaded(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DsProject.Instance.IsInitialized;
        }               

        private void DrawingOnDrawingHeaderChanged(DrawingBase drawing)
        {
            var drawingFileInfo = new FileInfo(drawing.FileFullName);
            DrawingInfoViewModel? drawingInfoViewModel;
            if (drawing is DsPageDrawing)
                drawingInfoViewModel = DsPageDrawingInfosSelectionService.AllItems
                    .FirstOrDefault(i => FileSystemHelper.Compare(i.DrawingInfo.FileInfo.FullName, drawingFileInfo.FullName));
            else
                drawingInfoViewModel = DsShapeDrawingInfosSelectionService.AllItems
                    .FirstOrDefault(i => FileSystemHelper.Compare(i.DrawingInfo.FileInfo.FullName, drawingFileInfo.FullName));
            if (drawingInfoViewModel is not null)
            {
                drawingInfoViewModel.EntityInfo = drawing.GetDrawingInfo();
            }
        }

        private void OpenFilesLocationOfDsPagesExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFilesLocations(DsPageDrawingInfosSelectionService.SelectedItems
                .Select(vm => vm.DrawingInfo).ToArray());
        }

        private void OpenFilesLocationOfComplexDsShapesExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFilesLocations(DsShapeDrawingInfosSelectionService.SelectedItems
                .Select(vm => vm.DrawingInfo).ToArray());
        }

        /// <summary>
        ///     drawingInfos is not null
        /// </summary>
        private void OpenFilesLocations(DrawingInfo[] drawingInfos)
        {
            if (drawingInfos is null) throw new ArgumentNullException(@"drawingInfos");

            if (drawingInfos.Length == 0) return;

            WindowsFileSystemHelper.OpenFolderInExplorerAndSelectFiles(drawingInfos.FirstOrDefault()?.FileInfo.DirectoryName,
                drawingInfos.Select(di => di.FileInfo.FullName).ToArray());

            /*
            Process.Start("explorer.exe",
                string.Format("/select,\"{0}\"", drawingInfo.FileInfo.FullName));*/
        }

        /*
        private async void UpdateComplexDsShapeExecutedAsync(object sender, ExecutedRoutedEventArgs e)
        {
            var entityInfoViewModel = DsShapesTreeView.SelectedItem as EntityInfoViewModel;
            if (entityInfoViewModel is null) return;

            var closedDrawings = new List<DrawingBase>();
            if (DesignDsProjectViewModel.Instance.CloseAllDrawings(closedDrawings)) return;

            MessageBoxHelper.ShowInfo(Properties.Resources.SelectDrawingsToUpdateMessage);

            List<DrawingInfo> updatingDrawingInfos = DesignDsProjectViewModel.Instance.GetDrawingInfosListFromUser();
            if (updatingDrawingInfos is null) return;

            bool doneWithErrors;
            try
            {
                using (DesignDsProjectViewModel.Instance.GetIsBusyCloser())
                {
                    doneWithErrors =
                        await
                            Task.Run(
                                () =>
                                    DsProject.Instance.UpdateComplexDsShapes(entityInfoViewModel.EntityInfo.Name,
                                        updatingDrawingInfos));
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
                MessageBoxHelper.ShowInfo(ex.Message);
                return;
            }

            foreach (DrawingBase closedDrawing in closedDrawings)
            {
                DesignDsProjectViewModel.Instance.OpenDrawing(closedDrawing.GetDrawingInfo());
            }

            if (doneWithErrors)
            {
                MessageBoxHelper.ShowInfo(Core.Properties.Resources.DoneWithErrors);
            }
            else
            {
                MessageBoxHelper.ShowInfo(Core.Properties.Resources.Done);
            }
        }*/

        private void SetAsDsProjectStartDsPageExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var drawingInfo = e.Parameter as DrawingInfo;
            if (drawingInfo is null) return;
            
            try
            {
                DsProject.Instance.RootWindowProps.FileRelativePath =
                    DsProject.Instance.GetFileRelativePath(drawingInfo.FileInfo.FullName);
                DsProject.Instance.SaveUnconditionally();
                OnStartDsPageChanged();
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }
        }

        private async void RenameDsPageExecutedAsync(object sender, ExecutedRoutedEventArgs e)
        {
            var oldDrawingInfo = e.Parameter as DrawingInfo;
            if (oldDrawingInfo is null) return;

            string newName = Interaction.InputBox(Properties.Resources.InputNewDsPageDrawingName,
                Properties.Resources.RenameDsPageText, oldDrawingInfo.Name);
            if (String.IsNullOrWhiteSpace(newName) || StringHelper.CompareIgnoreCase(newName, oldDrawingInfo.Name)) return;

            var oldDrawingFileInfo = oldDrawingInfo.FileInfo;
            var newDrawingFileInfo = new FileInfo(oldDrawingFileInfo.DirectoryName + @"\" + newName + DsProject.DsPageFileExtension);

            if (newDrawingFileInfo.Exists)
            {
                MessageBoxHelper.ShowInfo(Properties.Resources.NameErrorDsPageDrawing);
                return;
            }

            DesignDrawingViewModel? designerDrawingViewModel = DesignDsProjectViewModel.Instance.FindOpenedDrawingViewModel(oldDrawingInfo.FileInfo);
            if (designerDrawingViewModel is not null)
            {
                if (DesignDsProjectViewModel.Instance.CloseDrawing(designerDrawingViewModel)) return;                
            }

            try
            {
                DsProject.Instance.DrawingCopy(oldDrawingFileInfo, newDrawingFileInfo);

                DsProject.Instance.DrawingDelete(oldDrawingFileInfo);

                if (designerDrawingViewModel is not null)
                {
                    await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(newDrawingFileInfo);
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
                MessageBoxHelper.ShowError(Properties.Resources.RenameDsPageError + @" " + Ssz.Operator.Core.Properties.Resources.SeeErrorLogForDetails);
            }

            DsProject.Instance.OnDsPageDrawingsListChanged();
        }

        private void RenameComplexDsShapeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            /* // Not working because image in memory even after drawing closing
            var entityInfoViewModel = DsPagesTreeView.SelectedItem as EntityInfoViewModel;
            if (entityInfoViewModel is not null)
            {
                var oldDrawingInfo = (DrawingInfo)entityInfoViewModel.EntityInfo;

                string newName = Interaction.InputBox(Properties.Resources.InputNewDsShapeDrawingName,
                    "", oldDrawingInfo.Name);
                if (String.IsNullOrWhiteSpace(newName) || newName == oldDrawingInfo.Name) return;

                var oldFileInfo = oldDrawingInfo.FileInfo;
                var newFileInfo = new FileInfo(oldFileInfo.DirectoryName + @"\" + newName + @".dscontrol");

                if (newFileInfo.Exists)
                {
                    MessageBoxHelper.ShowInfo(Properties.Resources.NameErrorDsShapeDrawing);
                    return;
                }

                DrawingBase drawing = DesignDsProjectViewModel.Instance.FindOpenedDrawing(oldDrawingInfo);

                if (DesignDsProjectViewModel.Instance.CloseDrawing(drawing)) return;

                try
                {
                    File.Move(oldFileInfo.FullName, newFileInfo.FullName);
                    string oldDirectoryName = DrawingBase.GetDrawingFilesDir(oldFileInfo);
                    if (Directory.Exists(oldDirectoryName))
                    {
                        Directory.Move(oldDirectoryName,
                            DrawingBase.GetDrawingFilesDir(newFileInfo));
                    }

                    if (drawing is not null)
                    {
                        var newDrawingInfo = new DrawingInfo(oldDrawingInfo.BinDeserializedVersionDateTimeUtc,
                            newFileInfo, oldDrawingInfo.Guid, oldDrawingInfo.Desc,
                            oldDrawingInfo.DsConstantsCollection, oldDrawingInfo.DsPageType,
                            oldDrawingInfo.StyleObject, oldDrawingInfo.DrawingUserOptions);
                        DesignDsProjectViewModel.Instance.OpenDrawing(newDrawingInfo);
                    }
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                }
            }*/
        }

        private void DeleteDsPagesExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var drawingInfos = DsPageDrawingInfosSelectionService.SelectedItems
                .Select(vm => vm.DrawingInfo).ToArray();
            if (drawingInfos.Length == 0) return;

            WpfMessageBoxResult messageBoxResult = WpfMessageBox.Show(this, Ssz.Operator.Core.Properties.Resources.MessageDeleteDsPagesQuestion,
                            Ssz.Operator.Core.Properties.Resources.QuestionMessageBoxCaption,
                            WpfMessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);            
            if (messageBoxResult != WpfMessageBoxResult.Yes) return;

            foreach (var drawingInfo in drawingInfos)
            {
                DesignDrawingViewModel? designerDrawingViewModel = DesignDsProjectViewModel.Instance.FindOpenedDrawingViewModel(drawingInfo.FileInfo);
                if (designerDrawingViewModel is not null)
                {
                    DesignDsProjectViewModel.Instance.CloseDrawingUnconditionally(designerDrawingViewModel);                    
                }

                try
                {
                    DsProject.Instance.DrawingDelete(drawingInfo.FileInfo);
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                    MessageBoxHelper.ShowError(Properties.Resources.DeleteDrawingError + @" " + Ssz.Operator.Core.Properties.Resources.SeeErrorLogForDetails);
                    break;
                }
            }

            DsProject.Instance.OnDsPageDrawingsListChanged();
        }

        private void DeleteComplexDsShapesExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var drawingInfos = DsShapeDrawingInfosSelectionService.SelectedItems.OfType<DrawingInfoViewModel>()
                .Select(vm => vm.DrawingInfo).ToArray();
            if (drawingInfos.Length == 0) return;

            WpfMessageBoxResult messageBoxResult = WpfMessageBox.Show(this, Ssz.Operator.Core.Properties.Resources.MessageDeleteComplexDsShapesQuestion,
                            Ssz.Operator.Core.Properties.Resources.QuestionMessageBoxCaption,
                            WpfMessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);            
            if (messageBoxResult != WpfMessageBoxResult.Yes) return;

            foreach (var drawingInfo in drawingInfos)
            {
                DesignDrawingViewModel? designerDrawingViewModel =
                    DesignDsProjectViewModel.Instance.FindOpenedDrawingViewModel(drawingInfo.FileInfo);
                if (designerDrawingViewModel is not null)
                {
                    DesignDsProjectViewModel.Instance.CloseDrawingUnconditionally(designerDrawingViewModel);                    
                }

                try
                {
                    DsProject.Instance.DrawingDelete(drawingInfo.FileInfo);
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                    MessageBoxHelper.ShowError(Properties.Resources.DeleteDrawingError + @" " +
                                               Ssz.Operator.Core.Properties.Resources.SeeErrorLogForDetails);
                    break;
                }
            }

            DsProject.Instance.OnDsShapeDrawingsListChanged();
        }

        private async void UpdateComplexDsShapesOnSelectedDsPagesExecutedAsync(object sender, ExecutedRoutedEventArgs e)
        {
            var drawingInfos = DsPageDrawingInfosSelectionService.SelectedItems
                .Select(vm => vm.DrawingInfo).ToArray();
            if (drawingInfos.Length == 0) return;

            var toolkitOperationResult = ToolkitOperationResult.Done;

            var closedDrawingFileInfos = new List<FileInfo>();

            foreach (var drawingInfo in drawingInfos)
            {
                DesignDrawingViewModel? drawingViewModel =
                    DesignDsProjectViewModel.Instance.FindOpenedDrawingViewModel(drawingInfo.FileInfo);
                if (drawingViewModel is not null)
                {
                    closedDrawingFileInfos.Add(new FileInfo(drawingViewModel.Drawing.FileFullName));

                    if (DesignDsProjectViewModel.Instance.CloseDrawing(drawingViewModel)) return;
                }
            }

            try
            {
                using (var busyCloser = DesignDsProjectViewModel.Instance.GetBusyCloser())
                {
                    toolkitOperationResult =
                        await
                            DsProject.Instance.UpdateComplexDsShapesAsync(drawingInfos, null, busyCloser);                    
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
                MessageBoxHelper.ShowError(Ssz.Operator.Core.Properties.Resources.ToolkitOperationError + @". " +
                                                Ssz.Operator.Core.Properties.Resources.SeeErrorLogForDetails);
                return;
            }

            foreach (var closedDrawingFileInfo in closedDrawingFileInfos)
                await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(closedDrawingFileInfo);            

            switch (toolkitOperationResult)
            {
                case ToolkitOperationResult.Done:
                    MessageBoxHelper.ShowInfo(Core.Properties.Resources.Done);
                    break;
                case ToolkitOperationResult.DoneWithErrors:
                    MessageBoxHelper.ShowWarning(Ssz.Operator.Core.Properties.Resources.DoneWithErrors + @". " +
                                                 Ssz.Operator.Core.Properties.Resources.SeeErrorLogForDetails);
                    break;
            }
        }

        private async Task<ToolkitOperationResult> UpdateComplexDsShapesExtendedToolkitOperation(IProgressInfo progressInfo, object? parameter)
        {
            var toolkitOperationOptions = ToolkitOperationOptionsDialog.ShowDialog(new UpdateComplexDsShapesToolkitOperationOptions(),
                Ssz.Operator.Core.Properties.Resources.SpecifyToolkitOperationOptionsMessage) as UpdateComplexDsShapesToolkitOperationOptions;
            if (toolkitOperationOptions is null) return ToolkitOperationResult.Cancelled;

            DrawingInfo[]? updatingDrawingInfos = null;
            if (!toolkitOperationOptions.UpdateOnAllDsPages)
            {
                MessageBoxHelper.ShowInfo(Properties.Resources.UpdateComplexDsShapes_GetDsPageDrawingInfosListFromUser);

                List<DrawingInfo>? drawingInfos = DsProject.Instance.GetDrawingInfosListFromUser();
                if (drawingInfos is null || drawingInfos.Count == 0) return ToolkitOperationResult.Cancelled;
                updatingDrawingInfos = drawingInfos.ToArray();
            }

            return await DsProject.Instance.UpdateComplexDsShapesAsync(updatingDrawingInfos, toolkitOperationOptions, progressInfo);
        }

        private async Task<ToolkitOperationResult> UpdateComplexDsShapesSizeToolkitOperation(IProgressInfo progressInfo, object? parameter)
        {
            var dsShapeDrawingInfos = parameter as DrawingInfo[];
            if (dsShapeDrawingInfos is null || dsShapeDrawingInfos.Length == 0) return ToolkitOperationResult.Cancelled;

            MessageBoxHelper.ShowInfo(Properties.Resources.UpdateComplexDsShapes_GetDsPageDrawingInfosListFromUser);
            
            List<DrawingInfo>? drawingInfos = DsProject.Instance.GetDrawingInfosListFromUser();
            if (drawingInfos is null || drawingInfos.Count == 0) return ToolkitOperationResult.Cancelled;
            DrawingInfo[] updatingDrawingInfos = drawingInfos.ToArray();

            var toolkitOperationOptions = new UpdateComplexDsShapesToolkitOperationOptions();
            toolkitOperationOptions.ComplexDsShapeNames = String.Join(@",", dsShapeDrawingInfos.Select(di => di.Name));
            toolkitOperationOptions.ResetSizeToOriginal = true;

            return await DsProject.Instance.UpdateComplexDsShapesAsync(updatingDrawingInfos, toolkitOperationOptions, progressInfo);
        }

        private void SetMark0Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetMarkAsync(0);
        }

        private void SetMark1Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetMarkAsync(1);
        }

        private void SetMark2Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetMarkAsync(2);
        }

        private void SetMark3Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetMarkAsync(3);
        }

        private void SetMark4Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetMarkAsync(4);
        }

        private void SetMark5Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetMarkAsync(5);
        }

        private void SetMark6Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetMarkAsync(6);
        }

        private async void SetMarkAsync(int drawingMark)
        {            
            using (var progressInfo = DesignDsProjectViewModel.Instance.GetBusyCloser())
            {
                var selectedItems = DsPageDrawingInfosSelectionService.SelectedItems;
                int i = 0;
                foreach (var dsPageDrawingInfoViewModel in selectedItems)
                {
                    i += 1;
                    await progressInfo.RefreshProgressBarAsync(i, selectedItems.Length);

                    SetMark(drawingMark, dsPageDrawingInfoViewModel.DrawingInfo);
                }
            }
        }

        private void SetMark(int drawingMark, DrawingInfo drawingInfo)
        {
            if (drawingInfo is null) return;

            DesignDrawingViewModel? drawingViewModel =
                DesignDsProjectViewModel.Instance.FindOpenedDrawingViewModel(drawingInfo.FileInfo);

            if (drawingViewModel is not null)
            {
                drawingViewModel.Drawing.Mark = drawingMark;
            }
            else
            {
                DrawingBase? drawing = DsProject.ReadDrawing(drawingInfo.FileInfo, false, true);
                if (drawing is null) return;
                if (drawing.Mark != drawingMark)
                {
                    drawing.Mark = drawingMark;
                    DsProject.Instance.SaveUnconditionally(drawing, DsProject.IfFileExistsActions.CreateBackup, true);
                    DrawingOnDrawingHeaderChanged(drawing);
                }
            }
        }

        private void ShowDsPageTypeObjectPropertiesExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var drawingInfo = e.Parameter as DrawingInfo;
            if (drawingInfo is null) return;

            var drawingInfoViewModel = DsPageDrawingInfosSelectionService.AllItems
                    .FirstOrDefault(i => FileSystemHelper.Compare(i.DrawingInfo.FileInfo.FullName, drawingInfo.FileInfo.FullName));

            ShowDsPageTypeObjectProperties(drawingInfoViewModel);
        }        

        private void ShowDrawingPropertiesExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var drawingInfo = e.Parameter as DrawingInfo;
            if (drawingInfo is null) return;

            DesignDrawingViewModel? designerDrawingViewModel =
                DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels.FirstOrDefault(
                    dvm => FileSystemHelper.Compare(dvm.Drawing.FileFullName, drawingInfo.FileInfo.FullName));

            if (designerDrawingViewModel is not null)
            {
                DesignDsProjectViewModel.Instance.ShowDrawingPropertiesWindow(designerDrawingViewModel);
            }
            else
            {
                DrawingBase? drawing = DsProject.ReadDrawing(drawingInfo.FileInfo, false, true);                
                if (drawing is null) return;
                var originalDrawingGroup = drawing.Group;
                Guid originalDrawingTypeGuid = drawing is DsPageDrawing ? ((DsPageDrawing)drawing).DsPageTypeGuid : Guid.Empty;                
                ICloneable? valueResult = CloneableObjectPropertiesDialog.ShowDialog(drawing, true);
                if (valueResult is not null)
                {
                    var resultDrawing = (DrawingBase)valueResult;
                    resultDrawing.FileFullName = drawing.FileFullName;
                    var resultDrawingGroup = resultDrawing.Group;
                    Guid resultDrawingTypeGuid = resultDrawing is DsPageDrawing ? ((DsPageDrawing)resultDrawing).DsPageTypeGuid : Guid.Empty;                    

                    DsProject.Instance.SaveUnconditionally(resultDrawing, DsProject.IfFileExistsActions.CreateBackup, true);

                    if (resultDrawingGroup != originalDrawingGroup ||
                        resultDrawingTypeGuid != originalDrawingTypeGuid)
                    {
                        if (resultDrawing is DsPageDrawing)
                        {
                            DsProject.Instance.OnDsPageDrawingsListChanged();
                        }
                        else if (resultDrawing is DsShapeDrawing)
                        {
                            DsProject.Instance.OnDsShapeDrawingsListChanged();
                        }
                    }
                    else
                    {
                        DrawingOnDrawingHeaderChanged(resultDrawing);
                    }

                    resultDrawing.Dispose();
                }
                drawing.Dispose();
            }
        }

        private async void NewExecutedAsync(object sender, ExecutedRoutedEventArgs e)
        {
            //if (!App.HasMaintenanceLicense)
            //{
            //    MessageBoxHelper.ShowError(Properties.Resources.NoMaintLicenseTitle + "\n\n" +
            //                               Properties.Resources.AMSSupportInfo);
            //    return;
            //}

            string newDsProjectName = Interaction.InputBox(Properties.Resources.NewDsProjectNameInputDialogPrompt,
                Properties.Resources.ProgressInfo_CreatingDsProject_Header,"NewProject");
            if (String.IsNullOrWhiteSpace(newDsProjectName)) return;

            DirectoryInfo newDsProjectDirectoryInfo;
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = Properties.Resources.NewDsProjectFolderDialogDescription;

                CommonFileDialogResult result = dialog.ShowDialog();

                if (result != CommonFileDialogResult.Ok) return;
                
                newDsProjectDirectoryInfo = new DirectoryInfo(dialog.FileName);
            }
            if (!newDsProjectDirectoryInfo.Exists)
            {
                MessageBoxHelper.ShowError(Properties.Resources.NewDsProjectDirectoryDoesNotExsist);
                return;
            }

            newDsProjectDirectoryInfo = new DirectoryInfo(newDsProjectDirectoryInfo.FullName + @"\" +
                Path.GetInvalidPathChars().Aggregate(newDsProjectName, (current, c) => current.Replace(c.ToString(), @"_")));
            
            //MessageBoxHelper.ShowError(Properties.Resources.NewDsProjectDirectoryExsists);                

            if (!newDsProjectDirectoryInfo.Exists)
            {
                try
                {
                    newDsProjectDirectoryInfo.Create();
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"Cannot create dsProject directory: " + newDsProjectDirectoryInfo.FullName);
                    MessageBoxHelper.ShowError(Properties.Resources.NewDsProjectCannotCreateDirectory + " " + Ssz.Operator.Core.Properties.Resources.SeeErrorLogForDetails);
                    return;
                }
            }

            if (DesignDsProjectViewModel.Instance.PrepareCloseDsProject()) return;

            await DesignDsProjectViewModel.Instance.CloseDsProjectAsync();

            DockingManagerViewModel.Instance.DsPagesListDockViewModel.DsPagesTreeViewItemsSource = null;            

            string dsProjectFileName = newDsProjectDirectoryInfo.FullName + @"\" +
                Path.GetInvalidFileNameChars().Aggregate(newDsProjectName, (current, c) => current.Replace(c.ToString(), @"_")) + DsProject.DsProjectFileExtension;

            using (var busyCloser = DesignDsProjectViewModel.Instance.GetBusyCloser())
            {
                await busyCloser.SetHeaderAsync(Properties.Resources.ProgressInfo_CreatingDsProject_Header);

                DsProject.CreateNew(dsProjectFileName, DsProject.DsProjectModeEnum.VisualDesignMode, busyCloser);
            }            

            DesignDsProjectViewModel.Instance.RecentFilesCollectionManager.Add(dsProjectFileName);
        }

        private async void OpenExecutedAync(object sender, ExecutedRoutedEventArgs e)
        {
            var dsProjectFileName = e.Parameter as string;

            if (string.IsNullOrEmpty(dsProjectFileName))
            {
                var dlg = new OpenFileDialog
                {
                    Filter = @"Open file (*" + DsProject.DsProjectFileExtension + ")|*" + DsProject.DsProjectFileExtension + "|All files (*.*)|*.*"
                };
                if (dlg.ShowDialog() != true)
                    return;
                dsProjectFileName = dlg.FileName;
            }

            if (DesignDsProjectViewModel.Instance.PrepareCloseDsProject()) return;

            await DesignDsProjectViewModel.Instance.CloseDsProjectAsync();

            DockingManagerViewModel.Instance.DsPagesListDockViewModel.DsPagesTreeViewItemsSource = null;

            ReadDsProjectFromBinFileAsync(dsProjectFileName!);
        }

        private async Task<ToolkitOperationResult> ExportToXamlToolkitOperation(IProgressInfo progressInfo, object? parameter)
        {
            var dlg = new SaveFileDialog
            {
                Title = Properties.Resources.ExportToXamlSaveAsDialogTitle,
                Filter = @"Save file (*.xaml)|*.xaml|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return ToolkitOperationResult.Cancelled;

            string xamlFileName = dlg.FileName;

            MessageBoxHelper.ShowInfo(Properties.Resources.ExportToXamlGetDsPageDrawingInfosListFromUser);

            List<DrawingInfo>? drawingInfos = DsProject.Instance.GetDrawingInfosListFromUser();
            if (drawingInfos is null || drawingInfos.Count == 0) return ToolkitOperationResult.Cancelled;

            return await DsProject.Instance.ExportToXamlAsync(xamlFileName, drawingInfos, progressInfo);
        }

        private async Task<ToolkitOperationResult> ImportFromXamlToolkitOperation(IProgressInfo progressInfo, object? parameter)
        {
            var dlg = new OpenFileDialog
            {
                Title = Properties.Resources.ImportFromXamlDialogTitle,
                Filter = @"Open file (*.xaml)|*.xaml|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return ToolkitOperationResult.Cancelled;

            string xamlFileName = dlg.FileName;

            return await DsProject.Instance.ImportFromXamlAsync(xamlFileName, progressInfo);
        }

        private async void AddDsPagesAndDsShapesFromLibraryExecutedAsync(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new AddDrawingsFromLibraryDialog
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true) return;

            IEnumerable<DrawingInfo> drawingInfos = dialog.DrawingInfos;

            string? errorString;
            try
            {
                errorString =
                    await
                        Task.Run(
                            () =>
                            {
                                string? filesExists_ = null;
                                string? filesNotCopied_ = null;
                                foreach (DrawingInfo drawingInfo in drawingInfos)
                                {
                                    try
                                    {
                                        DsProject.Instance.CopyDrawingToDsProject(drawingInfo.FileInfo);
                                    }
                                    catch (ArgumentException)
                                    {
                                        filesExists_ += drawingInfo.FileInfo.Name + @"; ";
                                    }
                                    catch (Exception)
                                    {
                                        filesNotCopied_ += drawingInfo.FileInfo.Name + @"; ";
                                    }
                                }
                                string? errorString_ = null;
                                if (filesExists_ is not null)
                                {
                                    errorString_ += Properties.Resources.FilesExists + @" " + filesExists_ + "\n";
                                }
                                if (filesNotCopied_ is not null)
                                {
                                    errorString_ += Properties.Resources.UncknownReason + @" " + filesNotCopied_ + "\n";
                                }
                                return errorString_;
                            });
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
                MessageBoxHelper.ShowError(ex.Message);
                return;
            }

            DsProject.Instance.OnDsShapeDrawingsListChanged();
            DsProject.Instance.OnDsPageDrawingsListChanged();

            if (errorString is not null)
            {
                MessageBoxHelper.ShowWarning(Ssz.Operator.Core.Properties.Resources.DoneWithErrors + @". " +
                                             Properties.Resources.CannotCopyFiles + ".\n" + errorString);
            }
            else
            {
                MessageBoxHelper.ShowInfo(Core.Properties.Resources.Done);
            }
        }

        private async Task<ToolkitOperationResult> CreateDsPagesToolkitOperation(IProgressInfo progressInfo, object? parameter)
        {
            var toolkitOperationOptions = ToolkitOperationOptionsDialog.ShowDialog(new CreateDsPagesToolkitOperationOptions(),
                Ssz.Operator.Core.Properties.Resources.SpecifyToolkitOperationOptionsMessage) as CreateDsPagesToolkitOperationOptions;
            if (toolkitOperationOptions is null) return ToolkitOperationResult.Cancelled;

            MessageBoxHelper.ShowInfo(Properties.Resources.CreateDsPagesSelectImageFilesMessageBox);

            var dialog = new OpenFileDialog
            {
                Filter = @"Open file All files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return ToolkitOperationResult.Cancelled;
            
            return await DsProject.Instance.CreateDsPagesToolkitOperationAsync(dialog.FileNames, toolkitOperationOptions, progressInfo);
        }

        private async Task<ToolkitOperationResult> UpdateDsPagesToolkitOperation(IProgressInfo progressInfo, object? parameter)
        {
            var toolkitOperationOptions = ToolkitOperationOptionsDialog.ShowDialog(new UpdateDsPagesToolkitOperationOptions(),
                Ssz.Operator.Core.Properties.Resources.SpecifyToolkitOperationOptionsMessage) as UpdateDsPagesToolkitOperationOptions;
            if (toolkitOperationOptions is null) return ToolkitOperationResult.Cancelled;

            MessageBoxHelper.ShowInfo(Properties.Resources.UpdateDsPagesSelectImageFilesMessageBox);

            var dialog = new OpenFileDialog
            {
                Filter = @"Open file All files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return ToolkitOperationResult.Cancelled;

            return await DsProject.Instance.UpdateDsPagesToolkitOperationAsync(dialog.FileNames, toolkitOperationOptions, progressInfo);
        }

        private void PropertiesExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!DsProject.Instance.IsInitialized) return;

            switch (e.Parameter as string)
            {
                case "Drawing":
                {
                    if (FocusedDesignDrawingViewModel is null) return;
                    DesignDsProjectViewModel.Instance.ShowDrawingPropertiesWindow(FocusedDesignDrawingViewModel);
                }
                    return;
                case "DsShape":
                {
                    if (FocusedDesignDrawingViewModel is null) return;                    
                    DesignDsProjectViewModel.Instance.ShowFirstSelectedDsShapePropertiesWindow(FocusedDesignDrawingViewModel);
                }
                    return;
                default:
                {
                    Byte[] originalHash;
                    using (var memoryStream = new MemoryStream(1024 * 1024))
                    {
                        using (var writer = new SerializationWriter(memoryStream))
                        {
                            DsProject.Instance.SerializeOwnedData(writer, SerializationContext.ShortBytes);                            
                        }
                        originalHash = memoryStream.ToArray();
                    }                            

                    DsProject.Instance.DesiredAdditionalAddonsInfoChanged += DsProjectOnDesiredAdditionalAddonsInfoChangedAsync;
                    ConstantsHelper.UpdateDsConstants(DsProject.Instance.DsConstantsCollection, DsProject.Instance.DsConstantsCollection.OrderBy(gpi => gpi.Name).ToArray());
                    PropertiesWindow.Show(this, DsProject.Instance, DsProject.Instance.DsProjectFileFullName,
                        (propertiesWindow) =>
                        {
                            DsProject.Instance.DesiredAdditionalAddonsInfoChanged -=
                                DsProjectOnDesiredAdditionalAddonsInfoChangedAsync;

                            Byte[] newHash;
                            using (var memoryStream = new MemoryStream(1024 * 1024))
                            {
                                using (var writer = new SerializationWriter(memoryStream))
                                {
                                    DsProject.Instance.SerializeOwnedData(writer, SerializationContext.ShortBytes);                                    
                                }
                                newHash = memoryStream.ToArray();
                            }                            

                            bool changed = !originalHash.SequenceEqual(newHash);
                            if (changed)
                            {
                                bool succeeded = DsProject.Instance.SaveUnconditionally();
                                OnStartDsPageChanged();
                                if (succeeded) WpfMessageBox.Show(propertiesWindow, Properties.Resources.DsProjectFileSavedToDiskMessageBox, Ssz.Operator.Core.Properties.Resources.InfoMessageBoxCaption,
                                    WpfMessageBoxButton.OK,
                                    MessageBoxImage.Information);                                
                            }
                        });
                    return;
                }
            }
        }

        private async void DsProjectOnDesiredAdditionalAddonsInfoChangedAsync()
        {
            OpenDsShapeDrawingsErrorMessages = null;

            await DesignDsProjectViewModel.Instance.AllDsPagesCacheUpdateAsync();

            PropertiesWindow.ReloadAll();

            DsProject.Instance.OnDsPageDrawingsListChanged();
            DsProject.Instance.OnDsShapeDrawingsListChanged();

            FillInToolkitRibbonTab();
        }

        private void RunExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            RunDsProjectAsync();
        }

        private void RunCurrentEnabled(object sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is not null)
            {
                e.CanExecute = true;
                return;
            }
            else
            {
                var dsPageDrawingInfoViewModel = DsPageDrawingInfosSelectionService.FirstSelectedItem as DsPageDrawingInfoViewModel;
                if (dsPageDrawingInfoViewModel is not null)
                {
                    e.CanExecute = true;
                    return;
                }
            }

            e.CanExecute = false;
        }

        private void RunCurrentExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is not null)
            {
                RunDsProjectAsync(new FileInfo(FocusedDesignDrawingViewModel.Drawing.FileFullName));
            }
            else
            {
                var dsPageDrawingInfoViewModel = DsPageDrawingInfosSelectionService.FirstSelectedItem as DsPageDrawingInfoViewModel;
                if (dsPageDrawingInfoViewModel is not null)
                {
                    RunDsProjectAsync(new FileInfo(dsPageDrawingInfoViewModel.DrawingInfo.FileInfo.FullName));
                }
            }
        }

        private async void RunDsProjectAsync(FileInfo? startDsPageFileInfo = null)
        {
            var designerFileInfo = new FileInfo(Process.GetCurrentProcess().MainModule?.FileName ?? @"");
            var deltaSimOperatorFileInfo = new FileInfo(designerFileInfo.DirectoryName + @"\Ssz.Operator.Play.exe");

            if (!deltaSimOperatorFileInfo.Exists)
            {
                DsProject.LoggersSet.Logger.LogCritical(@"Cannot find 'Ssz.Operator.Play.exe'");
                MessageBoxHelper.ShowError(Ssz.Operator.Core.Properties.Resources.SeeErrorLogForDetails);
                return;
            }

            DesignDsProjectViewModel.SaveDrawings(DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels);

            await DsProject.Instance.AllDsPagesCacheSaveAsync();

            string arguments = @"-p """ + DsProject.Instance.DsProjectFileFullName + @"""";

            if (startDsPageFileInfo is not null)
            {
                arguments += @" -start """ +
                             DsProject.Instance.GetFileRelativePath(startDsPageFileInfo.FullName) +
                             @"""";
            }

            // Indicate we are launching from the Design
            arguments += @" -r 1";

            if (_previewSszOperatorProcess is not null && !_previewSszOperatorProcess.HasExited)
            {
                try
                {
                    ProcessHelper.CloseAllWindows(_previewSszOperatorProcess);
                }
                catch
                {
                }                
            }
            _previewSszOperatorProcess = Process.Start(new ProcessStartInfo(
                    deltaSimOperatorFileInfo.FullName,
                    arguments));
        }

        private void SaveEnabled(object sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = FocusedDesignDrawingViewModel.Drawing.DataChangedFromLastSave;
        }

        private void SaveExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;

            DesignDsProjectViewModel.SaveDrawings(new[] { FocusedDesignDrawingViewModel });
        }

        private void SaveAsEnabled(object sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        private async void SaveAsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;

            DrawingBase drawing = FocusedDesignDrawingViewModel.Drawing;

            var xaml = XamlHelper.Save(drawing);
            var copyDrawing = XamlHelper.Load(xaml) as DrawingBase;
            if (copyDrawing is null)
                return;

            bool cancelled = DsProject.Instance.AskAndSetNewFileName(copyDrawing, Path.GetFileName(drawing.FileFullName));

            if (cancelled) return;

            var errorMessages = new List<string>();

            if (FileSystemHelper.Compare(drawing.FileFullName, copyDrawing.FileFullName))
            {
                cancelled = DsProject.Instance.SaveUnconditionally(drawing, DsProject.IfFileExistsActions.CreateBackup,
                    true,
                    errorMessages);
            }
            else
            {
                cancelled = DsProject.Instance.SaveUnconditionally(copyDrawing, DsProject.IfFileExistsActions.CreateBackup,
                    true,
                    errorMessages);
            }

            if (errorMessages.Count > 0)
            {
                MessageBoxHelper.ShowError(String.Join("\n", errorMessages));
            }

            if (!cancelled)
            {
                if (copyDrawing is DsPageDrawing)
                {
                    DsProject.Instance.OnDsPageDrawingsListChanged();
                }
                else
                {
                    DsProject.Instance.OnDsShapeDrawingsListChanged();
                }

                await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(new FileInfo(copyDrawing.FileFullName));
            }
        }

        private void PrintEnabled(object sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        private void PrintExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null ||
                 FocusedDesignDrawingCanvas is null) return;            

            var printDialog = new System.Windows.Controls.PrintDialog();
            printDialog.CurrentPageEnabled = true;
            printDialog.PageRangeSelection = PageRangeSelection.CurrentPage;
            if (printDialog.ShowDialog() != true) return;
            
            switch (printDialog.PageRangeSelection)
            {
                case PageRangeSelection.CurrentPage:
                    FocusedDesignDrawingCanvas.DesignDrawingViewModel.SelectionService.ClearSelection();
                    printDialog.PrintVisual(FocusedDesignDrawingCanvas, "");
                    break;
                case PageRangeSelection.AllPages:
                    foreach (var dvm in DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels)
                    {
                        dvm.DesignControlsInfo!.DesignDrawingCanvas.DesignDrawingViewModel.SelectionService.ClearSelection();
                        printDialog.PrintVisual(dvm.DesignControlsInfo.DesignDrawingCanvas, "");
                    }                    
                    break;
            }
        }

        private void SaveAllEnabled(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!DsProject.Instance.IsInitialized)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = false;

            foreach (
                DesignDrawingViewModel designerDrawingViewModel in
                    DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels)
            {
                if (designerDrawingViewModel.Drawing.DataChangedFromLastSave) e.CanExecute = true;
            }
        }

        private void SaveAllExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            DesignDsProjectViewModel.SaveDrawings(DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels);
        }

        private void NewDsPageDrawingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            DesignDsProjectViewModel.Instance.NewDsPageDrawingWithFileAndOpenAsync();
        }

        private void NewDsShapeDrawingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            DesignDsProjectViewModel.Instance.NewDsShapeDrawingWithFileAndOpenAsync();
        }

        private async Task<ToolkitOperationResult> UpdateComplexDsShapesOnAllDsPagesToolkitOperation(IProgressInfo progressInfo, object? parameter)
        {
            return await DsProject.Instance.UpdateComplexDsShapesAsync(null, null, progressInfo);
        }

        private void OpenDsShapeDrawingFromComplexDsShapeEnabled(object sender, CanExecuteRoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            var dsShapeViewModel =
                FocusedDesignDrawingViewModel.SelectionService.SelectedItems.FirstOrDefault();

            if (dsShapeViewModel is null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = dsShapeViewModel.DsShape is ComplexDsShape;
        }

        private void OpenDsShapeDrawingFromComplexDsShapeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            DesignDsProjectViewModel.Instance.OpenDsShapeDrawingFromComplexDsShapeAsync();
        }

        private void DiscreteModeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            DesignDsProjectViewModel.Instance.DiscreteMode = DiscreteModeButton.IsChecked == true;
        }

        private void ShowHideDsShapesInfoTooltipsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            DesignDsProjectViewModel.Instance.ShowDsShapesInfoTooltips = ShowHideDsShapesInfoTooltipsButton.IsChecked == true;
        }               

        private async void OpenDsPagesExecutedAsync(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (var dsPageDrawingInfoViewModel in DsPageDrawingInfosSelectionService.SelectedItems.OrderBy(pvm => pvm.Number).Take(30))
            {
                await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
                        dsPageDrawingInfoViewModel.DrawingInfo.FileInfo);
            }
        }                    

        private async void OpenComplexDsShapesExecutedAsync(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (var dsPageDrawingInfoViewModel in DsShapeDrawingInfosSelectionService.SelectedItems.OfType<DrawingInfoViewModel>())
            {
                await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
                        dsPageDrawingInfoViewModel.DrawingInfo.FileInfo);
            }
        }        

        // simple replacement of PathCompactPathEx function
        private String GenerateCompactFileName(string? fileFullName)
        {
            if (fileFullName is null || fileFullName == @"")
                return "...";
            const string separator = @"...\";
            
            if (fileFullName.Length > 80)
            {
                var fileName = Path.GetFileName(fileFullName);
                //fileName = RecentFilesCollectionManager.GetShortNameToDisplay(fi.FullName, 60);
                int fileNameLen = fileName.Length;
                int len = 50 - fileNameLen;
                if (len > 0)
                {
                    int start = 0;
                    int last = 0;
                    while (start < len && start >= 0)
                    {
                        last = start;
                        start = fileFullName.IndexOf('\\', start + 1, len - start);
                    }
                    fileFullName = last == 0 ? "" : fileFullName.Substring(0, last + 1) + separator + fileName;
                }
                else
                    fileFullName = separator + fileName;
            }
            
            return fileFullName;
        }

        private void RefreshWindowTitle()
        {
            string title;
            if (!DsProject.Instance.IsInitialized)
            {
                title = Properties.Resources.WindowTitleNoCurrentFile;
            }
            else
            {
                var dsProjectFileFullName = DsProject.Instance.DsProjectFileFullName;
                title = dsProjectFileFullName is not null && dsProjectFileFullName != @"" ?
                    dsProjectFileFullName : Properties.Resources.WindowTitleNoCurrentFile;
                if (DsProject.Instance.IsReadOnly)
                    title = Properties.Resources.WindowTitleIsReadOnly + @" " + title;
            }
            DesignDsProjectViewModel.Instance.Title = title;
        }        

        private void Button100PercentOnClick(object sender, RoutedEventArgs e)
        {
            DesignDsProjectViewModel.Instance.SetDesignDrawingViewScale(1, new Point(0.5, 0.5));
        }

        private void ButtonFullDrawingOnClick(object sender, RoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            var focusedScrollViewer = FocusedScrollViewer;
            double viewScale;
            if (focusedScrollViewer is not null)
                viewScale =
                    DesignDrawingDockControl.GetFullDrawingViewScale(
                        focusedScrollViewer, FocusedDesignDrawingViewModel);
            else viewScale = 1;
            DesignDsProjectViewModel.Instance.SetDesignDrawingViewScale(viewScale, new Point(0.5, 0.5));
            DesignDrawingDockControl.ShowOnViewportCenter(
                FocusedDesignDrawingViewModel,
                FocusedDesignDrawingViewModel.Width/2,
                FocusedDesignDrawingViewModel.Height/2);
        }

        private void ButtonCenterOnClick(object sender, RoutedEventArgs e)
        {
            if (FocusedDesignDrawingViewModel is null) return;
            DesignDrawingDockControl.ShowOnViewportCenter(
                FocusedDesignDrawingViewModel,
                FocusedDesignDrawingViewModel.Width/2,
                FocusedDesignDrawingViewModel.Height/2);
        }

        private void ScaleComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScaleComboBox.SelectedIndex == -1) return;
            var text = (string)((ComboBoxItem)ScaleComboBox.SelectedValue).Content;
            ScaleComboBox.SelectedIndex = -1;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ScaleComboBox.SetValue(ComboBox.TextProperty, text);
                BindingOperations.GetBindingExpression(ScaleComboBox,
                    ComboBox.TextProperty).UpdateSource();
            }));
        }

        private void ScaleComboBoxOnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BindingOperations.GetBindingExpression(ScaleComboBox, ComboBox.TextProperty).UpdateSource();
            }
        }

        private void ScaleComboBoxOnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            BindingOperations.GetBindingExpression(ScaleComboBox, ComboBox.TextProperty).UpdateSource();
        }

        private void DiscreteModeComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DiscreteModeComboBox.SelectedIndex == -1) return;
            var text = (string)((ComboBoxItem)DiscreteModeComboBox.SelectedValue).Content;
            DiscreteModeComboBox.SelectedIndex = -1;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DiscreteModeComboBox.SetValue(ComboBox.TextProperty, text);
                BindingOperations.GetBindingExpression(DiscreteModeComboBox,
                    ComboBox.TextProperty).UpdateSource();
            }));
        }

        private void DiscreteModeComboBoxOnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BindingOperations.GetBindingExpression(DiscreteModeComboBox, ComboBox.TextProperty).UpdateSource();
            }
        }

        private void DiscreteModeComboBoxOnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            BindingOperations.GetBindingExpression(DiscreteModeComboBox, ComboBox.TextProperty).UpdateSource();
        }

        /*
        private void DesignDsProjectViewModel.InstanceOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e is not null && e.PropertyName == "SelectedDesignDrawingViewModel")
            {
                if (SelectedDesignDrawingViewModel is null) return;
                if (!SelectedDesignDrawingViewModel.Drawing.DrawingUserOptions.FileExists)
                {
                    if (SelectedDesignDrawingViewModel.Drawing is DsShapeDrawing)
                    {
                        SelectedDesignDrawingViewModel.Drawing.RefreshDsConstantsCollection();
                    }

                    PropertiesWindow.Show(this, SelectedDesignDrawingViewModel.Drawing,
                        this.SelectedDesignDrawingViewModel.BeginChangeSetBatch,
                        this.SelectedDesignDrawingViewModel.EndChangeSetBatch,
                        null);

                    SelectedDesignDrawingViewModel.Drawing.DrawingUserOptions.FileExists = true;
                }
            }
        }*/

        private void ClearRecentFilesListButtonOnClick(object sender, RoutedEventArgs e)
        {
            DesignDsProjectViewModel.Instance.RecentFilesCollectionManager.ClearList();
        }

        private void FillInToolkitRibbonTab()
        {
            var groups = ToolkitRibbonTabItem.Groups;

            groups.Clear();
            groups.Add(DsPagesToolkitRibbonGroupBox);
            
            foreach (ToolkitOperation toolkitOperation in AddonsHelper.GetToolkitOperations())
            {
                var group =
                    groups.FirstOrDefault(
                        rg =>
                            StringHelper.CompareIgnoreCase((string)rg.Header, toolkitOperation.RibbonGroup));
                if (group is null)
                {
                    group = new RibbonGroupBox { Header = toolkitOperation.RibbonGroup };
                    groups.Add(group);                    
                }
                var o = toolkitOperation;
                Func<IProgressInfo, object?, Task<ToolkitOperationResult>> toolkitOperationFunc = (pi, p) => o.DoWork(pi, p, CommandLineOptions.ToolkitOperationsSilent);
                var button = new Fluent.Button
                {                    
                    Tag = toolkitOperation,
                    Padding = new Thickness(5, 2, 5, 2),
                    Header = toolkitOperation.ButtonText,                    
                    ToolTip = toolkitOperation.ButtonToolTip,                    
                    Command =
                        new RelayCommand(obj => DoToolkitOperationAsync(toolkitOperationFunc, o.CloseAllDrawings),
                            obj => DsProject.Instance.IsInitialized, true)
                };
                Fluent.RibbonProperties.SetIconSize(button, IconSize.Small);
                group.Items.Add(button);
            }
        }

        private async void DoToolkitOperationAsync(Func<IProgressInfo, object?, Task<ToolkitOperationResult>> toolkitOperationFunc, bool closeAllDrawings, object? parameter = null)
        {
            int selectedDrawingViewModelIndex = DesignDsProjectViewModel.Instance.SelectedDesignDrawingIndex;
            var closedDrawingInfos = new List<DrawingInfo>();
            if (closeAllDrawings)
            {
                if (DesignDsProjectViewModel.Instance.CloseAllDrawings(closedDrawingInfos)) return;
            }

            ToolkitOperationResult toolkitOperationResult;
            
            try
            {
                using (var busyCloser = DesignDsProjectViewModel.Instance.GetBusyCloser())
                {
                    toolkitOperationResult = await toolkitOperationFunc(busyCloser, parameter);
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, Ssz.Operator.Core.Properties.Resources.ToolkitOperationError);
                MessageBoxHelper.ShowError(Ssz.Operator.Core.Properties.Resources.ToolkitOperationError + @". " +
                                        Ssz.Operator.Core.Properties.Resources.SeeErrorLogForDetails);
                toolkitOperationResult = ToolkitOperationResult.Cancelled;
            }

            foreach (DrawingInfo closedDrawingInfo in closedDrawingInfos)
            {
                await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(closedDrawingInfo.FileInfo);
            }
            DesignDsProjectViewModel.Instance.SelectedDesignDrawingIndex = selectedDrawingViewModelIndex;

            switch (toolkitOperationResult)
            {
                case ToolkitOperationResult.Done:
                    MessageBoxHelper.ShowInfo(Ssz.Operator.Core.Properties.Resources.Done);
                    break;
                case ToolkitOperationResult.DoneWithErrors:
                    MessageBoxHelper.ShowWarning(Ssz.Operator.Core.Properties.Resources.DoneWithErrors + @". " +
                                                    Ssz.Operator.Core.Properties.Resources.SeeErrorLogForDetails);
                    break;
            }
        }

        private void OpenedDrawingViewModels_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var addedDesignDrawingViewModels = e.NewItems?.OfType<DesignDrawingViewModel>();
                    if (addedDesignDrawingViewModels is not null)
                        foreach (var addedDesignDrawingViewModel in addedDesignDrawingViewModels)
                        {
                            addedDesignDrawingViewModel.Drawing.DrawingHeaderChanged += DrawingOnDrawingHeaderChanged;
                            DockingManagerViewModel.Instance.DocumentsSource.Add(addedDesignDrawingViewModel);
                        }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var removedDesignDrawingViewModels = e.OldItems?.OfType<DesignDrawingViewModel>();
                    if (removedDesignDrawingViewModels is not null)
                        foreach (var removedDesignDrawingViewModel in removedDesignDrawingViewModels)
                        {
                            removedDesignDrawingViewModel.Drawing.DrawingHeaderChanged -= DrawingOnDrawingHeaderChanged;
                            DockingManagerViewModel.Instance.DocumentsSource.Remove(removedDesignDrawingViewModel);
                        }
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void DesignDsProjectViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DesignDsProjectViewModel.FocusedDesignDrawingViewModel))
            {
                var focusedDesignDrawingViewModel = DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;

                IEnumerable<DsShapeViewModel>? dsShapeViewModels;
                if (focusedDesignDrawingViewModel is not null && !focusedDesignDrawingViewModel.IsDisposed)
                {
                    switch ((DsShapesOrderingEnum)DesignDsProjectViewModel.Instance.DsShapesOrdering)
                    {
                        case DsShapesOrderingEnum.DsShapeZIndex:
                            focusedDesignDrawingViewModel.SelectionService.AllItems =
                                focusedDesignDrawingViewModel.SelectionService.AllItems.OrderBy(i => i.DsShape.Index);
                            break;
                        case DsShapesOrderingEnum.DsShapeType:
                            focusedDesignDrawingViewModel.SelectionService.AllItems =
                                focusedDesignDrawingViewModel.SelectionService.AllItems
                                    .OrderBy(i => i.DsShape.GetDsShapeTypeNameToDisplay())
                                    .ThenBy(i => i.DsShape.Index);
                            break;
                        case DsShapesOrderingEnum.DsShapeName:
                            focusedDesignDrawingViewModel.SelectionService.AllItems =
                                focusedDesignDrawingViewModel.SelectionService.AllItems.OrderBy(i => i.DsShape.Name)
                                    .ThenBy(i => i.DsShape.Index);
                            break;
                    }

                    dsShapeViewModels =
                        focusedDesignDrawingViewModel.SelectionService.AllItems;
                }
                else
                {
                    dsShapeViewModels = null;
                }

                DockingManagerViewModel.Instance.DsDrawingDsShapesDockViewModel.DrawingDsShapesTreeViewItemsSource = dsShapeViewModels;
            }            
        }

        private DesignDrawingViewModel? FocusedDesignDrawingViewModel
        {
            get { return DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel; }
        }

        #endregion

        #region private fields

        private Process? _previewSszOperatorProcess;

        #endregion
    }

    public sealed class Options
    {
        #region construction and destruction

        public Options(IConfiguration configuration)
        {
            DsProjectFile = ConfigurationHelper.GetValue<string>(configuration, "SolutionFile", @"");
            AutoConvert = ConfigurationHelper.GetValue<bool>(configuration, "AutoConvert", false);
            ToolkitOperation = ConfigurationHelper.GetValue<string>(configuration, "ToolkitOperation", @"");
            ToolkitOperationsSilent = ConfigurationHelper.GetValue<bool>(configuration, "ToolkitOperationsSilent", false);
            Options_ = ConfigurationHelper.GetValue<string>(configuration, "Options", @"");
        }

        #endregion

        #region public functions

        public string DsProjectFile { get; set; }

        public bool AutoConvert { get; set; }

        public string ToolkitOperation { get; set; }

        public bool ToolkitOperationsSilent { get; set; }

        public string Options_ { get; set; }

        #endregion
    }
}