using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Ssz.Operator.Core.Utils; 
using Ssz.Utils; 
using Ssz.Operator.Core;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.ControlsPlay.VirtualKeyboards;
using Ssz.Operator.Core.FindReplace;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.Panorama;
using Ssz.Operator.Core.Addons;
using Microsoft.Win32;
using Ssz.Utils.Wpf.SystemMenu;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.Play
{
    public class PlayWindow : SystemMenuWindow, IPlayWindow
    {
        #region construction and destruction

        /// <summary>
        ///     rootWindowNum - number of root window starting from 1. If 0, then not root window.
        ///     Not changed during window lifetime.
        /// </summary>
        /// <param name="parentWindow"></param>
        /// <param name="rootWindowNum"></param>
        /// <param name="autoCloseMs"></param>
        public PlayWindow(IPlayWindow? parentWindow,
            int rootWindowNum,
            int autoCloseMs)
        {
            ParentWindow = parentWindow;
            RootWindowNum = rootWindowNum;

            PlayControlWrapper = new PlayControlWrapper(this);

            MainFrame = new Frame(this, @"");

            var uri = new Uri("pack://application:,,,/Images/Ssz.Operator.ico",
                UriKind.RelativeOrAbsolute);
            //Icon = BitmapFrame.Create(uri);
            
            if (IsRootWindow) // Is root window
            {
                int menuItemId = 0;

                menuItemId += 1;
                var smi = new SystemMenuItem
                {
                    Id = menuItemId,
                    Header = Properties.Resources.NewRootWindowMenuItemHeader,
                };
                BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                    new Binding { Source = NewRootWindow });
                MenuItems.Add(smi);
                CommandBindings.Add(new CommandBinding(NewRootWindow, NewRootWindowExecuted));

                menuItemId += 1;
                smi = new SystemMenuItem
                {
                    Id = menuItemId,
                    Header = Properties.Resources.SetupTouchScreenMenuItemHeader,
                };
                BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                    new Binding { Source = SetupTouchScreen });
                MenuItems.Add(smi);
                CommandBindings.Add(new CommandBinding(SetupTouchScreen, SetupTouchScreenExecuted));

                // Separator
                smi = new SystemMenuItem
                {
                    IsSeparator = true
                };
                MenuItems.Add(smi);
                
                //// add default menu item for virtual keyboard
                //menuItemId += 1;
                //smi = new SystemMenuItem
                //{
                //    Id = menuItemId,
                //    Header = Properties.Resources.ShowVirtualKeyboardMenuItemHeader,
                //};
                //BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                //    new Binding("ShowVirtualKeyboardCommand"));
                //MenuItems.Add(smi);                

                // add supported virtual keyboards
                var keyboardsInfo = AddonsHelper.GetVirtualKeyboardsInfo();
                if (keyboardsInfo.Length > 0)
                {
                    foreach (VirtualKeyboardInfo virtualKeyboardInfo in keyboardsInfo)
                    {
                        menuItemId += 1;
                        smi = new SystemMenuItem
                        {
                            Id = menuItemId,
                            Header = virtualKeyboardInfo.NameToDisplay,
                            CommandParameter = virtualKeyboardInfo.Type,
                        };
                        BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                            new Binding { Source = ShowVirtualKeyboard });
                        MenuItems.Add(smi);
                    }
                    CommandBindings.Add(new CommandBinding(ShowVirtualKeyboard, ShowVirtualKeyboardExecuted));

                    // Separator
                    smi = new SystemMenuItem
                    {
                        IsSeparator = true
                    };
                    MenuItems.Add(smi);
                }

                menuItemId += 1;
                smi = new SystemMenuItem
                {
                    Id = menuItemId,
                    Header = Properties.Resources.SaveCurrentPlayWindowsConfigurationMenuItemHeader,
                };
                BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                    new Binding { Source = SaveCurrentPlayWindowsConfiguration });
                MenuItems.Add(smi);
                CommandBindings.Add(new CommandBinding(SaveCurrentPlayWindowsConfiguration, SaveCurrentPlayWindowsConfigurationExecuted));

                menuItemId += 1;
                smi = new SystemMenuItem
                {
                    Id = menuItemId,
                    Header = Properties.Resources.OpenDsProjectFolderMenuItemHeader,
                };
                BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                    new Binding { Source = OpenDsProjectFolder });
                MenuItems.Add(smi);
                CommandBindings.Add(new CommandBinding(OpenDsProjectFolder, OpenDsProjectFolderExecuted));

                menuItemId += 1;
                smi = new SystemMenuItem
                {
                    Id = menuItemId,
                    Header = Properties.Resources.OpenLogFilesFolderMenuItemHeader,
                };
                BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                    new Binding { Source = OpenLogFilesFolder });
                MenuItems.Add(smi);
                CommandBindings.Add(new CommandBinding(OpenLogFilesFolder, OpenLogFilesFolderExecuted));

                menuItemId += 1;
                smi = new SystemMenuItem
                {
                    Id = menuItemId,
                    Header = Properties.Resources.ShowDebugWindowMenuItemHeader,
                };
                BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                    new Binding { Source = ShowDebugWindow });
                MenuItems.Add(smi);
                CommandBindings.Add(new CommandBinding(ShowDebugWindow, ShowDebugWindowExecuted));

                if (DsProject.Instance.Review)
                {
                    // Separator
                    smi = new SystemMenuItem
                    {
                        IsSeparator = true
                    };
                    MenuItems.Add(smi);

                    menuItemId += 1;
                    smi = new SystemMenuItem
                    {
                        Id = menuItemId,
                        Header = Properties.Resources.FindMenuItemHeader,
                    };
                    BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                        new Binding { Source = ApplicationCommands.Find });
                    MenuItems.Add(smi);
                    CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, FindExecuted));

                    menuItemId += 1;
                    smi = new SystemMenuItem
                    {
                        Id = menuItemId,
                        Header = Properties.Resources.JumpToDsPageMenuItemHeader,
                    };
                    BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                        new Binding { Source = ApplicationCommands.Open });
                    MenuItems.Add(smi);
                    CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenExecuted));

                    CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, UndoExecuted, UndoRedoEnabled));
                    CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, RedoExecuted, UndoRedoEnabled));
                }

                if (DsProject.Instance.Review ||
                    DsProject.Instance.GetAddon<PanoramaAddon>().AllowDesignModeInPlay)
                {
                    // Separator
                    smi = new SystemMenuItem
                    {
                        IsSeparator = true
                    };
                    MenuItems.Add(smi);

                    menuItemId += 1;
                    smi = new SystemMenuItem
                    {
                        Id = menuItemId,
                        Header = Properties.Resources.DesignModeTurnOnOffMenuItemHeader,
                    };
                    BindingOperations.SetBinding(smi, SystemMenuItem.CommandProperty,
                        new Binding { Source = DesignModeTurnOnOff });
                    MenuItems.Add(smi);
                    CommandBindings.Add(new CommandBinding(DesignModeTurnOnOff, DesignModeTurnOnOffExecuted));

                    CommandBindings.Add(new CommandBinding(DesignModeTurnOn, DesignModeTurnOnExecuted));
                    DesignModeTurnOn.InputGestures.Add(new KeyGesture(Key.D, ModifierKeys.Control));
                    CommandBindings.Add(new CommandBinding(DesignModeTurnOff, DesignModeTurnOffExecuted));                    
                    DesignModeTurnOff.InputGestures.Add(new KeyGesture(Key.R, ModifierKeys.Control));                    
                }

                CommandBindings.Add(new CommandBinding(AckAll, AckAllExecuted));
                AckAll.InputGestures.Add(new KeyGesture(Key.A, ModifierKeys.Control));
            }

            DataContext = new DataValueViewModel(this, false);

            if (autoCloseMs > 0)
            { 
                MouseEnter += (sender, args) =>
                {
                    foreach (var cancellationTokenSource in _autoClose_CancellationTokenSources)
                    {
                        cancellationTokenSource.Cancel();
                    }
                    _autoClose_CancellationTokenSources.Clear();
                };

                MouseLeave += (sender, args) =>
                {
                    CancellationTokenSource cts = new();
                    _autoClose_CancellationTokenSources.Add(cts);
                    var cancellationToken = cts.Token;
                    Task.Run(async () =>
                    {
                        await Task.Delay(autoCloseMs);
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    Close();
                                }
                                catch (Exception)
                                {
                                }
                            });
                        }
                    });
                };
            }
        }

        #endregion

        #region public functions

        public static readonly RoutedCommand NewRootWindow = new RoutedCommand();
        public static readonly RoutedCommand SetupTouchScreen = new RoutedCommand();
        public static readonly RoutedCommand ShowVirtualKeyboard = new RoutedCommand();
        public static readonly RoutedCommand SaveCurrentPlayWindowsConfiguration = new RoutedCommand();
        public static readonly RoutedCommand OpenDsProjectFolder = new RoutedCommand();
        public static readonly RoutedCommand OpenLogFilesFolder = new RoutedCommand();
        public static readonly RoutedCommand ShowDebugWindow = new RoutedCommand();
        public static readonly RoutedCommand DesignModeTurnOnOff = new RoutedCommand();
        public static readonly RoutedCommand DesignModeTurnOn = new RoutedCommand();
        public static readonly RoutedCommand DesignModeTurnOff = new RoutedCommand();
        public static readonly RoutedCommand AckAll = new RoutedCommand();
        
        public IPlayWindow? ParentWindow { get; private set; }

        /// <summary>
        ///     Depends on RootWindowNum property.
        /// </summary>
        public bool IsRootWindow { get { return RootWindowNum != 0; } }

        /// <summary>
        ///     Number of root window starting from 1. If 0, then not root window.
        ///     Not changed during window lifetime.
        /// </summary>
        public int RootWindowNum { get; private set; }
        
        public PlayControlWrapper PlayControlWrapper
        {
            get { return (PlayControlWrapper) Content; }
            private set { Content = value; }
        }

        public Frame MainFrame { get; }

        public string WindowCategory { get; set; } = @"";
        
        public CaseInsensitiveDictionary<List<object?>> WindowVariables { get; } = new();

        #endregion

        #region protected functions

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            PlayControlWrapper.Dispose();

            ((DataValueViewModel) DataContext).Dispose();
        }

        #endregion

        #region private functions        

        private void NewRootWindowExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            CommandsManager.NotifyCommand(MainFrame, CommandsManager.ShowNewRootWindowCommand, dsCommandOptions: null);
        }

        private void SetupTouchScreenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            CommandsManager.NotifyCommand(MainFrame, CommandsManager.SetupTouchScreenCommand, dsCommandOptions: null);
        }

        private void ShowVirtualKeyboardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            CommandsManager.NotifyCommand(MainFrame, CommandsManager.ShowVirtualKeyboardCommand,
                                            dsCommandOptions: new GenericDsCommandOptions { ParamsString = e.Parameter is string ? (string)e.Parameter : string.Empty });
        }

        private void SaveCurrentPlayWindowsConfigurationExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            PlayDsProjectView.SaveCurrentPlayWindowsConfiguration(false);
        }

        private void OpenDsProjectFolderExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            DsProject.Instance.ShowDsProjectDirectoryInExplorer();
        }

        private void OpenLogFilesFolderExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            DsProject.Instance.ShowLogFilesDirectoryInExplorer();
        }

        private void ShowDebugWindowExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            DsProject.Instance.ShowDebugWindow();
        }

        private void DesignModeTurnOnOffExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            DsProject.Instance.DesignModeInPlay = !DsProject.Instance.DesignModeInPlay;
        }

        private void DesignModeTurnOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            DsProject.Instance.DesignModeInPlay = true;
        }

        private void DesignModeTurnOffExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            DsProject.Instance.DesignModeInPlay = false;
        }

        private void AckAllExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            CommandsManager.NotifyCommand(MainFrame, CommandsManager.AckAllCommand,
                                            dsCommandOptions: new GenericDsCommandOptions());
        }

        private void UndoRedoEnabled(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsActive && !DsProject.Instance.DesignModeInPlay;
        }

        private void UndoExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive || DsProject.Instance.DesignModeInPlay)
            {                
                return;
            }

            CommandsManager.NotifyCommand(MainFrame, CommandsManager.JumpBackCommand,
                new JumpBackDsCommandOptions {TargetWindow = TargetWindow.CurrentWindow});
        }

        private void RedoExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive || DsProject.Instance.DesignModeInPlay)
            {                
                return;
            }

            CommandsManager.NotifyCommand(MainFrame, CommandsManager.JumpForwardCommand,
                new JumpForwardDsCommandOptions {TargetWindow = TargetWindow.CurrentWindow});
        }

        private void FindExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            FindReplaceDialog.ShowAsPlayFind(this);
        }

        private void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsActive) return;

            var dlg = new OpenFileDialog
            {
                Filter = @"All files (*.*)|*.*",
                InitialDirectory = DsProject.Instance.DsPagesDirectoryInfo!.FullName
            };

            if (dlg.ShowDialog() != true) return;

            var fileInfo = new FileInfo(dlg.FileName);

            string fileRelativePath = DsProject.Instance.GetFileRelativePath(fileInfo.FullName);

            if (String.IsNullOrWhiteSpace(fileRelativePath))
            {
                MessageBoxHelper.ShowError(Operator.Core.Properties.Resources.FileMustBeInDsProjectDir);
                return;
            }

            CommandsManager.NotifyCommand(MainFrame, CommandsManager.JumpCommand, new JumpDsCommandOptions { FileRelativePath = fileRelativePath });
        }

        #endregion

        #region private fields

        private readonly List<CancellationTokenSource> _autoClose_CancellationTokenSources = new();

        #endregion
    }
}