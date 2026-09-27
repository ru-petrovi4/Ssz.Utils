using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Design.Controls;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Utils;

namespace Ssz.Operator.Core.Design.Views;

/// <summary>
///     The editor itself. It is a view and not a window, because in the browser there are no windows:
///     the desktop puts it in <see cref="DesignMainWindow" /> and the browser makes it the single view.
///     <para>
///         Ported from the WPF editor's MainWindow: what it holds and what it does are the same. The
///         commands and their handlers are in the other half of this class.
///     </para>
/// </summary>
public partial class DesignMainView : UserControl
{
    #region construction and destruction

    public DesignMainView()
    {
        InitializeComponent();

        Instance = this;
        DataContext = DesignDsProjectViewModel.Instance;

        AddCommandBindings();
        AddKeyBindings();

        DesignDsProjectViewModel.Instance.PropertyChanged += DesignDsProjectViewModelOnPropertyChanged;

        DesignDsProjectViewModel.Instance.OpenedDesignDrawingViewModels.CollectionChanged +=
            OpenedDrawingViewModelsOnCollectionChanged;
        DesignDsProjectViewModel.Instance.ShowPoint += p => DesignDrawingView.ShowOnViewportCenter(
            DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel, p.X, p.Y);

        DsProject.Instance.OnInitializing += () =>
        {
            DsShapesListView.ViewModel.OpenDsShapeDrawingsErrorMessages = null;
        };

        DsProject.Instance.Initialized += () => Dispatcher.UIThread.Post(() =>
        {
            FillInToolkitRibbonTab();
            RoutedCommand.InvalidateRequerySuggested();
        });

        DsProject.Instance.DsProjectFileInfoChanged += () => Dispatcher.UIThread.Post(RefreshTitle);
        DsProject.Instance.IsReadOnlyChanged += () => Dispatcher.UIThread.Post(() =>
        {
            RefreshTitle();
            if (DsProject.Instance.IsReadOnly)
                MessageBoxHelper.ShowWarning(Design.Properties.Resources.DsProjectIsReadOnlyMessage);
        });

        RefreshTitle();
    }

    #endregion

    #region public functions

    public static DesignMainView? Instance { get; private set; }

    /// <summary>
    ///     What the editor was started with. The shell fills it in before the view is created.
    /// </summary>
    public static DesignOptions CommandLineOptions { get; set; } = new(null);

    /// <summary>
    ///     The bindings that say which handler answers each command. In WPF this collection belonged
    ///     to the window and the routing found it; here it is held and attached in one place.
    /// </summary>
    public CommandBindingCollection CommandBindings { get; } = new();

    /// <summary>
    ///     Brings the lists in step with the drawing being edited: shows the list it belongs to and
    ///     selects it there.
    /// </summary>
    public void SyncWithActiveDrawing()
    {
        DesignDrawingViewModel? focusedDesignDrawingViewModel =
            DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
        if (focusedDesignDrawingViewModel is null) return;

        var drawingFileFullName = focusedDesignDrawingViewModel.Drawing.FileFullName;
        if (focusedDesignDrawingViewModel.Drawing is DsPageDrawing)
        {
            DsPageDrawingInfoViewModel? drawingInfoViewModel =
                DsPagesListView.ViewModel.DsPageDrawingInfosSelectionService.AllItems
                    .FirstOrDefault(i => FileSystemHelper.Compare(i.DrawingInfo.FileFullName, drawingFileFullName));
            if (drawingInfoViewModel is not null)
            {
                ListsTabControl.SelectedIndex = 0;
                DsPagesListView.ViewModel.DsPageDrawingInfosSelectionService.SelectOne(drawingInfoViewModel);
            }
        }
        else
        {
            DrawingInfoViewModel? drawingInfoViewModel =
                DsShapesListView.ViewModel.DsShapeDrawingInfosSelectionService.AllItems
                    .FirstOrDefault(i => FileSystemHelper.Compare(i.DrawingInfo.FileFullName, drawingFileFullName));
            if (drawingInfoViewModel is not null)
            {
                ListsTabControl.SelectedIndex = 1;
                DsShapesListView.ViewModel.DsShapeDrawingInfosSelectionService.SelectOne(drawingInfoViewModel);
            }
        }
    }

    /// <summary>
    ///     Opens the properties of the page type object of a page - what a page of that type is
    ///     parameterised with.
    /// </summary>
    public void ShowDsPageTypeObjectProperties(DsPageDrawingInfoViewModel? dsPageDrawingViewModel)
    {
        if (dsPageDrawingViewModel is null) return;

        DsPageTypeBase? dsPageTypeObject = dsPageDrawingViewModel.DsPageDrawingInfo.DsPageTypeObject;
        if (dsPageTypeObject is null) return;

        PropertiesWindow.Show(ClipboardHelper.GetTopLevel() as Window, dsPageTypeObject,
            dsPageDrawingViewModel.DrawingInfo.FileFullName);
    }

    #endregion

    #region protected functions

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The window is only reachable once the view is in the tree.
        RefreshTitle();

        if (!String.IsNullOrWhiteSpace(CommandLineOptions.ToolkitOperation))
        {
            if (DsProject.Instance.IsInitialized)
                DoToolkitOperationByName(CommandLineOptions.ToolkitOperation);
            else
                DsProject.Instance.Initialized += () =>
                    DoToolkitOperationByName(CommandLineOptions.ToolkitOperation);
        }
        else
        {
            ListsTabControl.SelectedIndex = 0;
        }
    }

    /// <summary>
    ///     The arrow keys move, rotate and reorder the selected shapes, as they did in WPF.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.Left:
                e.Handled = TryExecute(control
                    ? DesignDsProjectViewModel.RotateCounterClockwise
                    : DesignDsProjectViewModel.DsShapeMoveLeft);
                break;
            case Key.Up:
                e.Handled = TryExecute(control
                    ? shift ? DesignDsProjectViewModel.SendToBack : DesignDsProjectViewModel.SendBackward
                    : DesignDsProjectViewModel.DsShapeMoveUp);
                break;
            case Key.Right:
                e.Handled = TryExecute(control
                    ? DesignDsProjectViewModel.RotateClockwise
                    : DesignDsProjectViewModel.DsShapeMoveRight);
                break;
            case Key.Down:
                e.Handled = TryExecute(control
                    ? shift ? DesignDsProjectViewModel.BringToFront : DesignDsProjectViewModel.BringForward
                    : DesignDsProjectViewModel.DsShapeMoveDown);
                break;
        }

        if (!e.Handled) base.OnKeyDown(e);
    }

    #endregion

    #region private functions

    private static bool TryExecute(RoutedCommand command)
    {
        if (!command.CanExecute(null)) return false;

        command.Execute(null);
        return true;
    }

    /// <summary>
    ///     Turns the keys each command carries into key bindings of this view, which is how WPF's
    ///     InputGestures reach the keyboard in Avalonia.
    /// </summary>
    private void AddKeyBindings()
    {
        foreach (CommandBinding commandBinding in CommandBindings)
        foreach (KeyGesture keyGesture in commandBinding.Command.InputGestures)
            KeyBindings.Add(new KeyBinding
            {
                Gesture = keyGesture,
                Command = commandBinding.Command
            });
    }

    private void DesignDsProjectViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DesignDsProjectViewModel.FocusedDesignDrawingViewModel))
            RoutedCommand.InvalidateRequerySuggested();
    }

    private void OpenedDrawingViewModelsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (DesignDrawingViewModel dvm in e.NewItems.OfType<DesignDrawingViewModel>())
                dvm.Drawing.DrawingHeaderChanged += DrawingOnDrawingHeaderChanged;

        if (e.OldItems is not null)
            foreach (DesignDrawingViewModel dvm in e.OldItems.OfType<DesignDrawingViewModel>())
                dvm.Drawing.DrawingHeaderChanged -= DrawingOnDrawingHeaderChanged;

        RoutedCommand.InvalidateRequerySuggested();
    }

    /// <summary>
    ///     A drawing whose name or description changed shows the new one in the lists too.
    /// </summary>
    private void DrawingOnDrawingHeaderChanged(DrawingBase drawing)
    {
        DrawingInfoViewModel? drawingInfoViewModel;
        if (drawing is DsPageDrawing)
            drawingInfoViewModel = DsPagesListView.ViewModel.DsPageDrawingInfosSelectionService.AllItems
                .FirstOrDefault(i => FileSystemHelper.Compare(i.DrawingInfo.FileFullName, drawing.FileFullName));
        else
            drawingInfoViewModel = DsShapesListView.ViewModel.DsShapeDrawingInfosSelectionService.AllItems
                .FirstOrDefault(i => FileSystemHelper.Compare(i.DrawingInfo.FileFullName, drawing.FileFullName));

        if (drawingInfoViewModel is not null)
            drawingInfoViewModel.EntityInfo = drawing.GetDrawingInfo();
    }

    private void DiscreteModeComboBoxOnSelectionChanged(object? sender,
        Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (DiscreteModeComboBox.SelectedItem is ComboBoxItem item)
            DiscreteModeStepTextBox.Text = item.Content as string;
        DiscreteModeComboBox.SelectedItem = null;
    }

    private void ScaleComboBoxOnSelectionChanged(object? sender,
        Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (ScaleComboBox.SelectedItem is ComboBoxItem item)
            ScaleTextBox.Text = item.Content as string;
        ScaleComboBox.SelectedItem = null;
    }

    private void RefreshTitle()
    {
        var title = DsProject.Instance.IsInitialized
            ? DsProject.Instance.DsProjectFileFullName ?? @""
            : @"";

        StatusTextBlock.Text = title;

        if (TopLevel.GetTopLevel(this) is Window window)
            window.Title = title == @"" ? Design.Properties.Resources.WindowTitleNoCurrentFile : title;
    }

    #endregion
}
