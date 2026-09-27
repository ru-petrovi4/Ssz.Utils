using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Design.Controls;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Utils;

namespace Ssz.Operator.Core.Design.Views;

/// <summary>
///     The commands of the editor and what they do.
///     <para>
///         Ported from the WPF editor's MainWindow. The commands keep their names, their handlers keep
///         their signatures, and the bindings are added in one place as they were - what changed is
///         that a WPF RoutedCommand is the command class of this port (see
///         <see cref="Ssz.Operator.Core.Utils.RoutedCommand" />) and that the dialogs are awaited.
///     </para>
/// </summary>
public partial class DesignMainView
{
    #region public functions

    public static readonly RoutedCommand New = new();
    public static readonly RoutedCommand Open = new();
    public static readonly RoutedCommand Save = new();
    public static readonly RoutedCommand SaveAs = new();
    public static readonly RoutedCommand Print = new();
    public static readonly RoutedCommand Properties = new();
    public static readonly RoutedCommand Find = new();
    public static readonly RoutedCommand Replace = new();
    public static readonly RoutedCommand Undo = new();
    public static readonly RoutedCommand Redo = new();
    public static readonly RoutedCommand Cut = new();
    public static readonly RoutedCommand Copy = new();
    public static readonly RoutedCommand Paste = new();
    public static readonly RoutedCommand Delete = new();
    public static readonly RoutedCommand SelectAll = new();
    public static readonly RoutedCommand Help = new();
    public static readonly RoutedCommand Stop = new();

    public static readonly RoutedCommand ExportToXaml = new();
    public static readonly RoutedCommand ImportFromXaml = new();
    public static readonly RoutedCommand AddDsPagesAndDsShapesFromLibrary = new();
    public static readonly RoutedCommand CreateDsPages = new();
    public static readonly RoutedCommand UpdateDsPages = new();
    public static readonly RoutedCommand Run = new();
    public static readonly RoutedCommand RunCurrent = new();
    public static readonly RoutedCommand RunMultiPlatform = new();
    public static readonly RoutedCommand RunCurrentMultiPlatform = new();
    public static readonly RoutedCommand SaveAll = new();
    public static readonly RoutedCommand NewDsPageDrawing = new();
    public static readonly RoutedCommand NewDsShapeDrawing = new();
    public static readonly RoutedCommand UpdateComplexDsShapesOnAllDsPages = new();
    public static readonly RoutedCommand OpenDsPages = new();
    public static readonly RoutedCommand OpenFilesLocationOfDsPages = new();
    public static readonly RoutedCommand SetAsDsProjectStartDsPage = new();
    public static readonly RoutedCommand RenameDsPage = new();
    public static readonly RoutedCommand DeleteDsPages = new();
    public static readonly RoutedCommand DeleteComplexDsShapes = new();
    public static readonly RoutedCommand UpdateComplexDsShapesOnDsPages = new();
    public static readonly RoutedCommand UpdateComplexDsShapesExtended = new();
    public static readonly RoutedCommand ShowDsPageTypeObjectPropertiesCommand = new();
    public static readonly RoutedCommand ShowDrawingPropertiesCommand = new();
    public static readonly RoutedCommand OpenComplexDsShapes = new();
    public static readonly RoutedCommand OpenFilesLocationOfComplexDsShapes = new();
    public static readonly RoutedCommand RenameComplexDsShape = new();
    public static readonly RoutedCommand UpdateComplexDsShapesSize = new();
    public static readonly RoutedCommand OpenDsShapeDrawingFromComplexDsShape = new();
    public static readonly RoutedCommand DiscreteMode = new();
    public static readonly RoutedCommand ShowHideDsShapesInfoTooltips = new();
    public static readonly RoutedCommand DebugFindDuplicates = new();
    public static readonly RoutedCommand DebugFindIncorrectDsPagesRefs = new();
    public static readonly RoutedCommand DebugFindIncorrectOpcTags = new();
    public static readonly RoutedCommand DebugFindIncorrectExpressions = new();

    public static readonly RoutedCommand SetMark0 = new();
    public static readonly RoutedCommand SetMark1 = new();
    public static readonly RoutedCommand SetMark2 = new();
    public static readonly RoutedCommand SetMark3 = new();
    public static readonly RoutedCommand SetMark4 = new();
    public static readonly RoutedCommand SetMark5 = new();
    public static readonly RoutedCommand SetMark6 = new();

    #endregion

    #region private functions

    private void AddCommandBindings()
    {
        DesignDsProjectViewModel vm = DesignDsProjectViewModel.Instance;

        CommandBindings.Add(new CommandBinding(Help, vm.HelpExecuted));
        CommandBindings.Add(new CommandBinding(Stop, vm.StopExecuted));
        CommandBindings.Add(new CommandBinding(Cut, vm.CutExecuted, vm.CutEnabled));
        CommandBindings.Add(new CommandBinding(Copy, vm.CopyExecuted, vm.CopyEnabled));
        CommandBindings.Add(new CommandBinding(Paste, vm.PasteExecuted, vm.PasteEnabled));
        CommandBindings.Add(new CommandBinding(Delete, vm.DeleteExecuted, vm.DeleteEnabled));
        CommandBindings.Add(new CommandBinding(SelectAll, vm.SelectAllExecuted));
        CommandBindings.Add(new CommandBinding(New, NewExecutedAsync));
        CommandBindings.Add(new CommandBinding(Open, OpenExecutedAsync));
        CommandBindings.Add(new CommandBinding(Save, SaveExecuted, SaveEnabled));
        CommandBindings.Add(new CommandBinding(SaveAs, SaveAsExecuted, SaveAsEnabled));
        CommandBindings.Add(new CommandBinding(Print, PrintExecuted, PrintEnabled));
        CommandBindings.Add(new CommandBinding(Undo, vm.UndoExecuted, vm.UndoEnabled));
        CommandBindings.Add(new CommandBinding(Redo, vm.RedoExecuted, vm.RedoEnabled));
        CommandBindings.Add(new CommandBinding(Properties, PropertiesExecuted, DsProjectLoaded));

        CommandBindings.Add(new CommandBinding(SaveAll, SaveAllExecuted, SaveAllEnabled));
        CommandBindings.Add(new CommandBinding(NewDsPageDrawing, NewDsPageDrawingExecuted, DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(NewDsShapeDrawing, NewDsShapeDrawingExecuted, DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(UpdateComplexDsShapesOnAllDsPages,
            (sender, e) => DoToolkitOperationAsync(UpdateComplexDsShapesOnAllDsPagesToolkitOperation, true),
            DsProjectLoaded));

        CommandBindings.Add(new CommandBinding(OpenDsPages, OpenDsPagesExecutedAsync, DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(OpenFilesLocationOfDsPages, OpenFilesLocationOfDsPagesExecuted,
            DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(SetAsDsProjectStartDsPage, SetAsDsProjectStartDsPageExecuted,
            DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(OpenComplexDsShapes, OpenComplexDsShapesExecutedAsync,
            DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(OpenFilesLocationOfComplexDsShapes,
            OpenFilesLocationOfComplexDsShapesExecuted, DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(ShowDsPageTypeObjectPropertiesCommand,
            ShowDsPageTypeObjectPropertiesExecuted, DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(ShowDrawingPropertiesCommand, ShowDrawingPropertiesExecuted,
            DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(OpenDsShapeDrawingFromComplexDsShape,
            OpenDsShapeDrawingFromComplexDsShapeExecuted, OpenDsShapeDrawingFromComplexDsShapeEnabled));

        CommandBindings.Add(new CommandBinding(Run, RunExecuted, DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(RunCurrent, RunCurrentExecuted, RunCurrentEnabled));

        CommandBindings.Add(new CommandBinding(RenameDsPage, RenameDsPageExecutedAsync, DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(DeleteDsPages, DeleteDsPagesExecutedAsync, DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(DeleteComplexDsShapes, DeleteComplexDsShapesExecutedAsync,
            DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(UpdateComplexDsShapesOnDsPages,
            UpdateComplexDsShapesOnSelectedDsPagesExecutedAsync, DsProjectLoaded));
        CommandBindings.Add(new CommandBinding(UpdateComplexDsShapesSize,
            (sender, e) => DoToolkitOperationAsync(
                UpdateComplexDsShapesSizeToolkitOperation, true,
                DsShapeDrawingInfosSelectionService.SelectedItems.Select(vm => vm.DrawingInfo).ToArray()),
            DsProjectLoaded));

        CommandBindings.Add(new CommandBinding(DiscreteMode, DiscreteModeExecuted));
        CommandBindings.Add(new CommandBinding(ShowHideDsShapesInfoTooltips,
            ShowHideDsShapesInfoTooltipsExecuted));

        CommandBindings.Add(new CommandBinding(SetMark0, SetMark0Executed));
        CommandBindings.Add(new CommandBinding(SetMark1, SetMark1Executed));
        CommandBindings.Add(new CommandBinding(SetMark2, SetMark2Executed));
        CommandBindings.Add(new CommandBinding(SetMark3, SetMark3Executed));
        CommandBindings.Add(new CommandBinding(SetMark4, SetMark4Executed));
        CommandBindings.Add(new CommandBinding(SetMark5, SetMark5Executed));
        CommandBindings.Add(new CommandBinding(SetMark6, SetMark6Executed));

        vm.AddCommandBindings(CommandBindings);
    }

    private DesignDrawingViewModel? FocusedDesignDrawingViewModel =>
        DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;

    private DesignDrawingCanvas? FocusedDesignDrawingCanvas =>
        FocusedDesignDrawingViewModel?.DesignControlsInfo?.DesignDrawingCanvas;

    private ScrollViewer? FocusedScrollViewer =>
        FocusedDesignDrawingViewModel?.DesignControlsInfo?.ScrollViewer;

    private SelectionService<DsPageDrawingInfoViewModel> DsPageDrawingInfosSelectionService =>
        DsPagesListView.ViewModel.DsPageDrawingInfosSelectionService;

    private SelectionService<DrawingInfoViewModel> DsShapeDrawingInfosSelectionService =>
        DsShapesListView.ViewModel.DsShapeDrawingInfosSelectionService;

    private void DsProjectLoaded(object? sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = DsProject.Instance.IsInitialized;
    }

    #region Project

    private async void NewExecutedAsync(object? sender, ExecutedRoutedEventArgs e)
    {
        string? newDsProjectName = await InputBoxHelper.ShowAsync(
            Design.Properties.Resources.NewDsProjectNameInputDialogPrompt,
            Design.Properties.Resources.ProgressInfo_CreatingDsProject_Header,
            @"NewProject");
        if (String.IsNullOrWhiteSpace(newDsProjectName)) return;

        IStorageFolder? storageFolder = await FolderDialogHelper.OpenFolderAsync(
            Design.Properties.Resources.NewDsProjectFolderDialogDescription);
        if (storageFolder is null) return;

        var parentPath = storageFolder.TryGetLocalPath();
        if (String.IsNullOrEmpty(parentPath)) return;

        var newDsProjectDirectoryInfo = new DirectoryInfo(parentPath);
        if (!newDsProjectDirectoryInfo.Exists)
        {
            MessageBoxHelper.ShowError(Design.Properties.Resources.NewDsProjectDirectoryDoesNotExsist);
            return;
        }

        newDsProjectDirectoryInfo = new DirectoryInfo(Path.Combine(newDsProjectDirectoryInfo.FullName,
            Path.GetInvalidPathChars()
                .Aggregate(newDsProjectName, (current, c) => current.Replace(c.ToString(), @"_"))));

        if (!newDsProjectDirectoryInfo.Exists)
            try
            {
                newDsProjectDirectoryInfo.Create();
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex,
                    @"Cannot create dsProject directory: " + newDsProjectDirectoryInfo.FullName);
                MessageBoxHelper.ShowError(Design.Properties.Resources.NewDsProjectCannotCreateDirectory + @" " +
                                           Core.Properties.Resources.SeeErrorLogForDetails);
                return;
            }

        if (await DesignDsProjectViewModel.Instance.PrepareCloseDsProjectAsync()) return;

        await DesignDsProjectViewModel.Instance.CloseDsProjectAsync();

        DsPagesListView.ViewModel.DsPagesTreeViewItemsSource = null;

        var dsProjectFileName = Path.Combine(newDsProjectDirectoryInfo.FullName,
            Path.GetInvalidFileNameChars()
                .Aggregate(newDsProjectName, (current, c) => current.Replace(c.ToString(), @"_")) +
            DsProject.DsProjectFileExtension);

        using (DesignDsProjectViewModel.BusyCloser busyCloser =
               DesignDsProjectViewModel.Instance.GetBusyCloser())
        {
            await busyCloser.SetHeaderAsync(Design.Properties.Resources.ProgressInfo_CreatingDsProject_Header);

            await DsProject.CreateNewAsync(dsProjectFileName, DsProject.DsProjectModeEnum.DesktopDesignMode,
                busyCloser);
        }

        DesignDsProjectViewModel.Instance.RecentFilesCollectionManager.Add(dsProjectFileName);
    }

    private async void OpenExecutedAsync(object? sender, ExecutedRoutedEventArgs e)
    {
        var dsProjectFileName = e.Parameter as string;

        if (String.IsNullOrEmpty(dsProjectFileName))
        {
            IStorageFile? storageFile = await FileDialogHelper.OpenFileAsync(
                Design.Properties.Resources.OpenMenu,
                new FilePickerFileType(@"*" + DsProject.DsProjectFileExtension)
                {
                    Patterns = new[] { @"*" + DsProject.DsProjectFileExtension }
                },
                FileDialogHelper.AllFilesFileType);
            if (storageFile is null) return;

            dsProjectFileName = storageFile.TryGetLocalPath();
            if (String.IsNullOrEmpty(dsProjectFileName)) return;
        }

        if (await DesignDsProjectViewModel.Instance.PrepareCloseDsProjectAsync()) return;

        await DesignDsProjectViewModel.Instance.CloseDsProjectAsync();

        DsPagesListView.ViewModel.DsPagesTreeViewItemsSource = null;

        await ReadDsProjectFromBinFileAsync(dsProjectFileName!);
    }

    /// <summary>
    ///     Opens a project, read-only when its directory cannot be written to.
    /// </summary>
    public async Task ReadDsProjectFromBinFileAsync(string dsProjectFileName)
    {
        var dsProjectDirectoryName = Path.GetDirectoryName(dsProjectFileName);
        var isReadOnly = false;
        if (Directory.Exists(dsProjectDirectoryName) &&
            !FileSystemHelper.IsDirectoryWritable(dsProjectDirectoryName))
        {
            MessageBoxHelper.ShowError(Design.Properties.Resources.DsProjectDirectoryWriteAccessError);
            isReadOnly = true;
        }

        using (DesignDsProjectViewModel.BusyCloser busyCloser =
               DesignDsProjectViewModel.Instance.GetBusyCloser())
        {
            await busyCloser.SetHeaderAsync(Design.Properties.Resources.ProgressInfo_LoadingDsProject_Header);

            await DsProject.ReadDsProjectFromBinFileAsync(dsProjectFileName,
                DsProject.DsProjectModeEnum.DesktopDesignMode,
                isReadOnly, CommandLineOptions.AutoConvert, @"", busyCloser);
        }

        if (!String.IsNullOrEmpty(DsProject.Instance.DsProjectFileFullName))
            DesignDsProjectViewModel.Instance.RecentFilesCollectionManager.Add(
                DsProject.Instance.DsProjectFileFullName!);
    }

    private void PropertiesExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        if (!DsProject.Instance.IsInitialized) return;

        switch (e.Parameter as string)
        {
            case @"Drawing":
            {
                if (FocusedDesignDrawingViewModel is null) return;
                DesignDsProjectViewModel.Instance.ShowDrawingPropertiesWindow(FocusedDesignDrawingViewModel);
            }
                return;
            case @"DsShape":
            {
                if (FocusedDesignDrawingViewModel is null) return;
                DesignDsProjectViewModel.Instance.ShowFirstSelectedDsShapePropertiesWindow(
                    FocusedDesignDrawingViewModel);
            }
                return;
            default:
            {
                Constants.ConstantsHelper.UpdateDsConstants(DsProject.Instance.DsConstantsCollection,
                    DsProject.Instance.DsConstantsCollection.OrderBy(gpi => gpi.Name).ToArray());
                PropertiesWindow.Show(ClipboardHelper.GetTopLevel() as Window, DsProject.Instance,
                    DsProject.Instance.DsProjectFileFullName,
                    propertiesWindow =>
                    {
                        // The project is saved when its properties were changed, which is what the
                        // WPF editor did on closing this window.
                        if (DsProject.Instance.SaveUnconditionally())
                        {
                            DsPagesListView.OnStartDsPageChanged();
                            MessageBoxHelper.ShowInfo(Design.Properties.Resources.DsProjectFileSavedToDiskMessageBox);
                        }
                    });
                return;
            }
        }
    }

    #endregion

    #region Drawings

    private void SaveEnabled(object? sender, CanExecuteRoutedEventArgs e)
    {
        if (FocusedDesignDrawingViewModel is null)
        {
            e.CanExecute = false;
            return;
        }

        e.CanExecute = FocusedDesignDrawingViewModel.Drawing.DataChangedFromLastSave;
    }

    private void SaveExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        if (FocusedDesignDrawingViewModel is null) return;

        DesignDsProjectViewModel.SaveDrawings(new[] { FocusedDesignDrawingViewModel });
    }

    private void SaveAsEnabled(object? sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = FocusedDesignDrawingViewModel is not null;
    }

    private async void SaveAsExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        if (FocusedDesignDrawingViewModel is null) return;

        DrawingBase drawing = FocusedDesignDrawingViewModel.Drawing;

        var xaml = XamlHelper.Save(drawing);
        var copyDrawing = XamlHelper.Load(xaml) as DrawingBase;
        if (copyDrawing is null) return;

        var cancelled = await DsProject.Instance.AskAndSetNewFileNameAsync(copyDrawing,
            Path.GetFileName(drawing.FileFullName));
        if (cancelled) return;

        var errorMessages = new List<string>();

        if (FileSystemHelper.Compare(drawing.FileFullName, copyDrawing.FileFullName))
            cancelled = await DsProject.Instance.SaveUnconditionallyAsync(drawing,
                DsProject.IfFileExistsActions.CreateBackup, true, errorMessages);
        else
            cancelled = await DsProject.Instance.SaveUnconditionallyAsync(copyDrawing,
                DsProject.IfFileExistsActions.CreateBackup, true, errorMessages);

        if (errorMessages.Count > 0) MessageBoxHelper.ShowError(String.Join("\n", errorMessages));

        if (!cancelled)
        {
            if (copyDrawing is DsPageDrawing)
                DsProject.Instance.OnDsPageDrawingsListChanged();
            else
                DsProject.Instance.OnDsShapeDrawingsListChanged();

            await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
                new FileInfo(copyDrawing.FileFullName));
        }
    }

    private void PrintEnabled(object? sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = FocusedDesignDrawingViewModel is not null;
    }

    private void PrintExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        if (FocusedDesignDrawingViewModel is null || FocusedDesignDrawingCanvas is null) return;

        FocusedDesignDrawingCanvas.DesignDrawingViewModel.SelectionService.ClearSelection();

        // Avalonia has no print dialog of its own, so the drawing goes out as a picture the author
        // can print from anywhere. The WPF editor printed the same visual directly.
        _ = PrintHelper.SaveDrawingAsImageAsync(FocusedDesignDrawingCanvas,
            FocusedDesignDrawingViewModel.Drawing.Name);
    }

    private void SaveAllEnabled(object? sender, CanExecuteRoutedEventArgs e)
    {
        if (!DsProject.Instance.IsInitialized)
        {
            e.CanExecute = false;
            return;
        }

        e.CanExecute = false;

        foreach (DesignDrawingViewModel designerDrawingViewModel in
                 DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels)
            if (designerDrawingViewModel.Drawing.DataChangedFromLastSave)
                e.CanExecute = true;
    }

    private void SaveAllExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        DesignDsProjectViewModel.SaveDrawings(DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels);
    }

    private void NewDsPageDrawingExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        DesignDsProjectViewModel.Instance.NewDsPageDrawingWithFileAndOpenAsync();
    }

    private void NewDsShapeDrawingExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        DesignDsProjectViewModel.Instance.NewDsShapeDrawingWithFileAndOpenAsync();
    }

    private async void OpenDsPagesExecutedAsync(object? sender, ExecutedRoutedEventArgs e)
    {
        foreach (DsPageDrawingInfoViewModel dsPageDrawingInfoViewModel in
                 DsPageDrawingInfosSelectionService.SelectedItems.OrderBy(pvm => pvm.Number).Take(30))
            await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
                new FileInfo(dsPageDrawingInfoViewModel.DrawingInfo.FileFullName));
    }

    private async void OpenComplexDsShapesExecutedAsync(object? sender, ExecutedRoutedEventArgs e)
    {
        foreach (DrawingInfoViewModel drawingInfoViewModel in
                 DsShapeDrawingInfosSelectionService.SelectedItems.Take(30))
            await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
                new FileInfo(drawingInfoViewModel.DrawingInfo.FileFullName));
    }

    private void OpenFilesLocationOfDsPagesExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        OpenFilesLocations(DsPageDrawingInfosSelectionService.SelectedItems
            .Select(vm => vm.DrawingInfo).ToArray());
    }

    private void OpenFilesLocationOfComplexDsShapesExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        OpenFilesLocations(DsShapeDrawingInfosSelectionService.SelectedItems
            .Select(vm => vm.DrawingInfo).ToArray());
    }

    private void OpenFilesLocations(DrawingInfo[] drawingInfos)
    {
        if (drawingInfos.Length == 0) return;

        FileLocationHelper.OpenFolderAndSelectFiles(
            Path.GetDirectoryName(drawingInfos[0].FileFullName),
            drawingInfos.Select(di => di.FileFullName).ToArray());
    }

    private void SetAsDsProjectStartDsPageExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        DsPageDrawingInfoViewModel? dsPageDrawingInfoViewModel =
            DsPageDrawingInfosSelectionService.SelectedItems.FirstOrDefault();
        if (dsPageDrawingInfoViewModel is null) return;

        DsProject.Instance.RootWindowProps.FileRelativePath =
            DsProject.Instance.GetFileRelativePath(dsPageDrawingInfoViewModel.DrawingInfo.FileFullName);
        DsProject.Instance.SaveUnconditionally();

        DsPagesListView.OnStartDsPageChanged();
    }

    private void ShowDsPageTypeObjectPropertiesExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        ShowDsPageTypeObjectProperties(DsPageDrawingInfosSelectionService.SelectedItems.FirstOrDefault());
    }

    private async void ShowDrawingPropertiesExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        DrawingInfoViewModel? drawingInfoViewModel =
            DsPageDrawingInfosSelectionService.SelectedItems.FirstOrDefault() as DrawingInfoViewModel ??
            DsShapeDrawingInfosSelectionService.SelectedItems.FirstOrDefault();
        if (drawingInfoViewModel is null) return;

        DesignDrawingViewModel? designDrawingViewModel =
            await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
                new FileInfo(drawingInfoViewModel.DrawingInfo.FileFullName));
        if (designDrawingViewModel is null) return;

        DesignDsProjectViewModel.Instance.ShowDrawingPropertiesWindow(designDrawingViewModel);
    }

    private void OpenDsShapeDrawingFromComplexDsShapeEnabled(object? sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = FocusedDesignDrawingViewModel is not null &&
                       FocusedDesignDrawingViewModel.SelectionService.SelectedItems
                           .Any(svm => svm.DsShape is DsShapes.ComplexDsShape);
    }

    private void OpenDsShapeDrawingFromComplexDsShapeExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        DesignDsProjectViewModel.Instance.OpenDsShapeDrawingFromComplexDsShapeAsync();
    }

    #endregion

    #region Pages and shapes lists

    /// <summary>
    ///     Renames a page: the file is copied under the new name and the old one is deleted, so that
    ///     the folder of files belonging to the page comes along.
    /// </summary>
    private async void RenameDsPageExecutedAsync(object? sender, ExecutedRoutedEventArgs e)
    {
        var oldDrawingInfo = e.Parameter as DrawingInfo;
        if (oldDrawingInfo is null) return;

        string? newName = await InputBoxHelper.ShowAsync(
            Design.Properties.Resources.InputNewDsPageDrawingName,
            Design.Properties.Resources.RenameDsPageText,
            oldDrawingInfo.Name);
        if (String.IsNullOrWhiteSpace(newName) ||
            StringHelper.CompareIgnoreCase(newName, oldDrawingInfo.Name)) return;

        var oldDrawingFileInfo = new FileInfo(oldDrawingInfo.FileFullName);
        var newDrawingFileInfo = new FileInfo(Path.Combine(oldDrawingFileInfo.DirectoryName ?? @"",
            newName + DsProject.DsPageFileExtension));

        if (newDrawingFileInfo.Exists)
        {
            MessageBoxHelper.ShowInfo(Design.Properties.Resources.NameErrorDsPageDrawing);
            return;
        }

        DesignDrawingViewModel? designDrawingViewModel =
            DesignDsProjectViewModel.Instance.FindOpenedDrawingViewModel(oldDrawingFileInfo);
        if (designDrawingViewModel is not null)
            if (await DesignDsProjectViewModel.Instance.CloseDrawingAsync(designDrawingViewModel))
                return;

        try
        {
            await DsProject.Instance.DrawingCopyAsync(oldDrawingFileInfo, newDrawingFileInfo);

            DsProject.Instance.DrawingDelete(oldDrawingFileInfo);

            if (designDrawingViewModel is not null)
                await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(newDrawingFileInfo);
        }
        catch (Exception ex)
        {
            DsProject.LoggersSet.Logger.LogError(ex, @"");
            MessageBoxHelper.ShowError(Design.Properties.Resources.RenameDsPageError + @" " +
                                       Core.Properties.Resources.SeeErrorLogForDetails);
        }

        DsProject.Instance.OnDsPageDrawingsListChanged();
    }

    private async void DeleteDsPagesExecutedAsync(object? sender, ExecutedRoutedEventArgs e)
    {
        DrawingInfo[] drawingInfos = DsPageDrawingInfosSelectionService.SelectedItems
            .Select(vm => vm.DrawingInfo).ToArray();
        if (drawingInfos.Length == 0) return;

        if (await MessageBoxHelper.AskYesNoCancelAsync(
                Core.Properties.Resources.MessageDeleteDsPagesQuestion) != true) return;

        DeleteDrawings(drawingInfos);

        DsProject.Instance.OnDsPageDrawingsListChanged();
    }

    private async void DeleteComplexDsShapesExecutedAsync(object? sender, ExecutedRoutedEventArgs e)
    {
        DrawingInfo[] drawingInfos = DsShapeDrawingInfosSelectionService.SelectedItems
            .Select(vm => vm.DrawingInfo).ToArray();
        if (drawingInfos.Length == 0) return;

        if (await MessageBoxHelper.AskYesNoCancelAsync(
                Core.Properties.Resources.MessageDeleteComplexDsShapesQuestion) != true) return;

        DeleteDrawings(drawingInfos);

        DsProject.Instance.OnDsShapeDrawingsListChanged();
    }

    /// <summary>
    ///     Closes each drawing that is open and deletes its file. The first failure stops the run, as
    ///     in the WPF editor.
    /// </summary>
    private void DeleteDrawings(DrawingInfo[] drawingInfos)
    {
        foreach (DrawingInfo drawingInfo in drawingInfos)
        {
            var drawingFileInfo = new FileInfo(drawingInfo.FileFullName);

            DesignDrawingViewModel? designDrawingViewModel =
                DesignDsProjectViewModel.Instance.FindOpenedDrawingViewModel(drawingFileInfo);
            if (designDrawingViewModel is not null)
                DesignDsProjectViewModel.Instance.CloseDrawingUnconditionally(designDrawingViewModel);

            try
            {
                DsProject.Instance.DrawingDelete(drawingFileInfo);
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
                MessageBoxHelper.ShowError(Design.Properties.Resources.DeleteDrawingError + @" " +
                                           Core.Properties.Resources.SeeErrorLogForDetails);
                break;
            }
        }
    }

    /// <summary>
    ///     Brings the complex shapes used on the selected pages up to date with the shape drawings
    ///     they came from. Only those pages are touched; the pages that were open are reopened after.
    /// </summary>
    private async void UpdateComplexDsShapesOnSelectedDsPagesExecutedAsync(object? sender,
        ExecutedRoutedEventArgs e)
    {
        DrawingInfo[] drawingInfos = DsPageDrawingInfosSelectionService.SelectedItems
            .Select(vm => vm.DrawingInfo).ToArray();
        if (drawingInfos.Length == 0) return;

        ToolkitOperationResult toolkitOperationResult;

        var closedDrawingFileInfos = new List<FileInfo>();

        foreach (DrawingInfo drawingInfo in drawingInfos)
        {
            DesignDrawingViewModel? drawingViewModel =
                DesignDsProjectViewModel.Instance.FindOpenedDrawingViewModel(
                    new FileInfo(drawingInfo.FileFullName));
            if (drawingViewModel is not null)
            {
                closedDrawingFileInfos.Add(new FileInfo(drawingViewModel.Drawing.FileFullName));

                if (await DesignDsProjectViewModel.Instance.CloseDrawingAsync(drawingViewModel)) return;
            }
        }

        try
        {
            using (DesignDsProjectViewModel.BusyCloser busyCloser =
                   DesignDsProjectViewModel.Instance.GetBusyCloser())
            {
                toolkitOperationResult =
                    await DsProject.Instance.UpdateComplexDsShapesAsync(drawingInfos, null, busyCloser);
            }
        }
        catch (Exception ex)
        {
            DsProject.LoggersSet.Logger.LogError(ex, @"");
            MessageBoxHelper.ShowError(Core.Properties.Resources.ToolkitOperationError + @". " +
                                       Core.Properties.Resources.SeeErrorLogForDetails);
            return;
        }

        foreach (FileInfo closedDrawingFileInfo in closedDrawingFileInfos)
            await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(closedDrawingFileInfo);

        switch (toolkitOperationResult)
        {
            case ToolkitOperationResult.Done:
                MessageBoxHelper.ShowInfo(Core.Properties.Resources.Done);
                break;
            case ToolkitOperationResult.DoneWithErrors:
                MessageBoxHelper.ShowWarning(Core.Properties.Resources.DoneWithErrors + @". " +
                                             Core.Properties.Resources.SeeErrorLogForDetails);
                break;
        }
    }

    /// <summary>
    ///     Puts the complex shapes named by the selection back to the size of the shape drawing they
    ///     came from, on the pages the author picks.
    /// </summary>
    private async Task<ToolkitOperationResult> UpdateComplexDsShapesSizeToolkitOperation(
        IProgressInfo progressInfo, object? parameter)
    {
        var dsShapeDrawingInfos = parameter as DrawingInfo[];
        if (dsShapeDrawingInfos is null || dsShapeDrawingInfos.Length == 0)
            return ToolkitOperationResult.Cancelled;

        MessageBoxHelper.ShowInfo(
            Design.Properties.Resources.UpdateComplexDsShapes_GetDsPageDrawingInfosListFromUser);

        List<DrawingInfo>? drawingInfos = await DsProject.Instance.GetDrawingInfosListFromUserAsync();
        if (drawingInfos is null || drawingInfos.Count == 0) return ToolkitOperationResult.Cancelled;

        var toolkitOperationOptions = new DsProjectExtensions.UpdateComplexDsShapesToolkitOperationOptions
        {
            ComplexDsShapeNames = String.Join(@",", dsShapeDrawingInfos.Select(di => di.Name)),
            ResetSizeToOriginal = true
        };

        return await DsProject.Instance.UpdateComplexDsShapesAsync(drawingInfos.ToArray(),
            toolkitOperationOptions, progressInfo);
    }

    #endregion

    #region Marks

    private void SetMark0Executed(object? sender, ExecutedRoutedEventArgs e) => SetMarkAsync(0);

    private void SetMark1Executed(object? sender, ExecutedRoutedEventArgs e) => SetMarkAsync(1);

    private void SetMark2Executed(object? sender, ExecutedRoutedEventArgs e) => SetMarkAsync(2);

    private void SetMark3Executed(object? sender, ExecutedRoutedEventArgs e) => SetMarkAsync(3);

    private void SetMark4Executed(object? sender, ExecutedRoutedEventArgs e) => SetMarkAsync(4);

    private void SetMark5Executed(object? sender, ExecutedRoutedEventArgs e) => SetMarkAsync(5);

    private void SetMark6Executed(object? sender, ExecutedRoutedEventArgs e) => SetMarkAsync(6);

    /// <summary>
    ///     Marks the selected pages with a colour, which is the dot the pages list shows in front of
    ///     them.
    /// </summary>
    private async void SetMarkAsync(int drawingMark)
    {
        DsPageDrawingInfoViewModel[] selectedItems = DsPageDrawingInfosSelectionService.SelectedItems;
        if (selectedItems.Length == 0) return;

        using (DesignDsProjectViewModel.BusyCloser busyCloser =
               DesignDsProjectViewModel.Instance.GetBusyCloser())
        {
            await busyCloser.RefreshProgressBarAsync(0, selectedItems.Length);

            var i = 0;
            foreach (DsPageDrawingInfoViewModel dsPageDrawingInfoViewModel in selectedItems)
            {
                await SetMarkAsync(drawingMark, dsPageDrawingInfoViewModel.DrawingInfo);

                i += 1;
                await busyCloser.RefreshProgressBarAsync(i, selectedItems.Length);
            }
        }

        DsProject.Instance.OnDsPageDrawingsListChanged();
    }

    private async Task SetMarkAsync(int drawingMark, DrawingInfo drawingInfo)
    {
        DesignDrawingViewModel? openedDrawingViewModel =
            DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels.FirstOrDefault(
                dvm => FileSystemHelper.Compare(dvm.Drawing.FileFullName, drawingInfo.FileFullName));
        if (openedDrawingViewModel is not null)
        {
            openedDrawingViewModel.Drawing.Mark = drawingMark;
            return;
        }

        DrawingBase? drawing =
            await DsProject.ReadDrawingAsync(drawingInfo.FileFullName, true, true);
        if (drawing is null) return;

        drawing.Mark = drawingMark;

        await DsProject.Instance.SaveUnconditionallyAsync(drawing,
            DsProject.IfFileExistsActions.CreateBackupAndWarn, false);
    }

    #endregion

    #region View

    private void DiscreteModeExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        DesignDsProjectViewModel.Instance.DiscreteMode = DiscreteModeButton.IsChecked == true;
    }

    private void ShowHideDsShapesInfoTooltipsExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        DesignDsProjectViewModel.Instance.ShowDsShapesInfoTooltips =
            ShowHideDsShapesInfoTooltipsButton.IsChecked == true;
    }

    private void Button100PercentOnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DesignDsProjectViewModel.Instance.SetDesignDrawingViewScale(1, new Point(0.5, 0.5));
    }

    private void ButtonFullDrawingOnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (FocusedDesignDrawingViewModel is null) return;
        ScrollViewer? focusedScrollViewer = FocusedScrollViewer;
        double viewScale;
        if (focusedScrollViewer is not null)
            viewScale = DesignDrawingView.GetFullDrawingViewScale(focusedScrollViewer,
                FocusedDesignDrawingViewModel);
        else viewScale = 1;
        DesignDsProjectViewModel.Instance.SetDesignDrawingViewScale(viewScale, new Point(0.5, 0.5));
        DesignDrawingView.ShowOnViewportCenter(FocusedDesignDrawingViewModel,
            FocusedDesignDrawingViewModel.Width / 2, FocusedDesignDrawingViewModel.Height / 2);
    }

    private void ButtonCenterOnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (FocusedDesignDrawingViewModel is null) return;
        DesignDrawingView.ShowOnViewportCenter(FocusedDesignDrawingViewModel,
            FocusedDesignDrawingViewModel.Width / 2, FocusedDesignDrawingViewModel.Height / 2);
    }

    #endregion

    #region Running the interface being edited

    private void RunExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        RunDsProjectAsync();
    }

    private void RunCurrentEnabled(object? sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = DsProject.Instance.IsInitialized &&
                       FocusedDesignDrawingViewModel?.Drawing is DsPageDrawing;
    }

    private void RunCurrentExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        if (FocusedDesignDrawingViewModel?.Drawing is not DsPageDrawing) return;

        RunDsProjectAsync(new FileInfo(FocusedDesignDrawingViewModel.Drawing.FileFullName));
    }

    #endregion

    #region Toolkit

    private async Task<ToolkitOperationResult> UpdateComplexDsShapesOnAllDsPagesToolkitOperation(
        IProgressInfo progressInfo, object? parameter)
    {
        return await DsProject.Instance.UpdateComplexDsShapesAsync(null, null, progressInfo);
    }

    /// <summary>
    ///     Runs a toolkit operation: closes the drawings it needs closed, shows the busy indicator
    ///     while it works, opens the drawings again and says how it went.
    /// </summary>
    private async void DoToolkitOperationAsync(
        Func<IProgressInfo, object?, Task<ToolkitOperationResult>> toolkitOperationFunc,
        bool closeAllDrawings, object? parameter = null)
    {
        var selectedDrawingViewModelIndex = DesignDsProjectViewModel.Instance.SelectedDesignDrawingIndex;
        var closedDrawingInfos = new List<DrawingInfo>();
        if (closeAllDrawings)
            if (await DesignDsProjectViewModel.Instance.CloseAllDrawingsAsync(closedDrawingInfos))
                return;

        ToolkitOperationResult toolkitOperationResult;

        try
        {
            using (DesignDsProjectViewModel.BusyCloser busyCloser =
                   DesignDsProjectViewModel.Instance.GetBusyCloser())
            {
                toolkitOperationResult = await toolkitOperationFunc(busyCloser, parameter);
            }
        }
        catch (Exception ex)
        {
            DsProject.LoggersSet.Logger.LogError(ex, Core.Properties.Resources.ToolkitOperationError);
            MessageBoxHelper.ShowError(Core.Properties.Resources.ToolkitOperationError + @". " +
                                       Core.Properties.Resources.SeeErrorLogForDetails);
            toolkitOperationResult = ToolkitOperationResult.Cancelled;
        }

        foreach (DrawingInfo closedDrawingInfo in closedDrawingInfos)
            await DesignDsProjectViewModel.Instance.ShowOrOpenDrawingAsync(
                new FileInfo(closedDrawingInfo.FileFullName));
        DesignDsProjectViewModel.Instance.SelectedDesignDrawingIndex = selectedDrawingViewModelIndex;

        switch (toolkitOperationResult)
        {
            case ToolkitOperationResult.Done:
                MessageBoxHelper.ShowInfo(Core.Properties.Resources.Done);
                break;
            case ToolkitOperationResult.DoneWithErrors:
                MessageBoxHelper.ShowWarning(Core.Properties.Resources.DoneWithErrors + @". " +
                                             Core.Properties.Resources.SeeErrorLogForDetails);
                break;
        }
    }

    /// <summary>
    ///     Runs the toolkit operation an addon registered under this name, which is how the editor is
    ///     asked from a command line to do one thing and be done.
    /// </summary>
    private void DoToolkitOperationByName(string toolkitOperationName)
    {
        ToolkitOperation? toolkitOperation = Addons.AddonsManager.GetToolkitOperations()
            .FirstOrDefault(o => StringHelper.CompareIgnoreCase(o.GetType().Name, toolkitOperationName));
        if (toolkitOperation is null) return;

        ToolkitOperation o = toolkitOperation;
        DoToolkitOperationAsync(
            (pi, p) => o.DoWork(pi, p, CommandLineOptions.ToolkitOperationsSilent), o.CloseAllDrawings);
    }

    /// <summary>
    ///     Adds to the Toolkit tab of the ribbon a button for every operation the addons bring.
    /// </summary>
    private void FillInToolkitRibbonTab()
    {
        Avalonia.Collections.AvaloniaList<Controls.Ribbon.RibbonGroupBox> groups =
            ToolkitRibbonTabItem.Groups;

        groups.Clear();
        groups.Add(DsPagesToolkitRibbonGroupBox);

        foreach (ToolkitOperation toolkitOperation in Addons.AddonsManager.GetToolkitOperations())
        {
            Controls.Ribbon.RibbonGroupBox? group = groups.FirstOrDefault(
                rg => StringHelper.CompareIgnoreCase(rg.Header as string, toolkitOperation.RibbonGroup));
            if (group is null)
            {
                group = new Controls.Ribbon.RibbonGroupBox { Header = toolkitOperation.RibbonGroup };
                groups.Add(group);
            }

            ToolkitOperation o = toolkitOperation;
            var button = new Controls.Ribbon.RibbonButton
            {
                Tag = toolkitOperation,
                Padding = new Thickness(5, 2, 5, 2),
                Header = toolkitOperation.ButtonText,
                SizeDefinition = Controls.Ribbon.RibbonControlSize.Small,
                Command = new RelayCommand(
                    obj => DoToolkitOperationAsync(
                        (pi, p) => o.DoWork(pi, p, CommandLineOptions.ToolkitOperationsSilent),
                        o.CloseAllDrawings),
                    obj => DsProject.Instance.IsInitialized, true)
            };
            ToolTip.SetTip(button, toolkitOperation.ButtonToolTip);

            group.Items.Add(button);
        }
    }

    #endregion

    #region Running the interface being edited

    /// <summary>
    ///     Saves what is open and starts the player on this project, beside the executable of the
    ///     editor. Starting it again closes the one started before.
    /// </summary>
    private async void RunDsProjectAsync(FileInfo? startDsPageFileInfo = null)
    {
        var designerFileFullName = Process.GetCurrentProcess().MainModule?.FileName ?? @"";
        var directory = Path.GetDirectoryName(designerFileFullName) ?? @"";
        var playExeFileFullName = Path.Combine(directory, PlayExeFileName);

        if (!File.Exists(playExeFileFullName))
        {
            DsProject.LoggersSet.Logger.LogCritical($"Cannot find '{playExeFileFullName}'");
            MessageBoxHelper.ShowError(Core.Properties.Resources.SeeErrorLogForDetails);
            return;
        }

        DesignDsProjectViewModel.SaveDrawings(DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels);

        await DsProject.Instance.AllDsPagesCacheSaveAsync();

        var arguments = @"-p """ + DsProject.Instance.DsProjectFileFullName + @"""";

        if (startDsPageFileInfo is not null)
            arguments += @" -start """ +
                         DsProject.Instance.GetFileRelativePath(startDsPageFileInfo.FullName) +
                         @"""";

        // Tells the player it was started from the editor.
        arguments += @" -r 1";

        if (_previewSszOperatorProcess is not null && !_previewSszOperatorProcess.HasExited)
            try
            {
                _previewSszOperatorProcess.CloseMainWindow();
            }
            catch (Exception)
            {
            }

        _previewSszOperatorProcess = Process.Start(new ProcessStartInfo(playExeFileFullName, arguments));
    }

    #endregion

    #region private fields

    /// <summary>
    ///     The player that goes with this editor. The WPF editor derived the name from its own by
    ///     replacing the ending; here the editor and the player are one pair of names.
    /// </summary>
    private const string PlayExeFileName = @"Cdt.Operator.Play.Desktop.exe";

    private Process? _previewSszOperatorProcess;

    #endregion

    #endregion
}
