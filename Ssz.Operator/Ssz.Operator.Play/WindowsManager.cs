using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Threading;
using Ssz.Operator.Core.Utils; 
using Ssz.Utils; 
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.ControlsPlay.BrowserPlay;
using Ssz.Operator.Core.Utils.WinApi;
using Ssz.Operator.Core.ControlsPlay.WpfModel3DPlay;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.DsShapeViews;
using Application = System.Windows.Application;
using Ssz.Operator.Play.Properties;
using System.Threading;
using Ssz.Operator.Core.ControlsPlay.VirtualKeyboards;
using Ssz.Operator.Core.FindReplace;
using Ssz.Operator.Core.DsPageTypes;
using System.Windows.Shell;
using System.Windows.Media.Effects;
using System.Windows.Media;
using Ssz.Utils.Wpf;
using Ssz.Operator.Core.DataAccess;
using Ssz.Utils.DataAccess;
using Microsoft.Extensions.Logging;
using Ssz.Utils.Wpf.WpfMessageBox;

namespace Ssz.Operator.Play
{
    internal class WindowsManager
    {
        #region public functions

        public static readonly WindowsManager Instance = new WindowsManager();

        /// <summary>
        ///     Initialize WindowManager       
        ///     Prepare root windows set for executing FV main tasks
        /// </summary>
        /// <param name="startDsPageFileRelativePathFromArgs">Startup dsPage name (if FV has been runned from Design->Run current dsPage)</param> 
        public void Initialize(string startDsPageFileRelativePathFromArgs)
        {
            CommandsManager.AddCommandHandler(CommandsManager_OnGotCommand);
            DsDataAccessProvider.Instance.ContextStatusChanged += DsDataAccessProvider_OnContextStatusChanged;            
            AllRootPlayWindowsClosed += OnAllRootPlayWindowsClosed;

            var rootWindowProps =
                (WindowProps)DsProject.Instance.RootWindowProps.Clone();
            if (!String.IsNullOrWhiteSpace(startDsPageFileRelativePathFromArgs))
            {
                rootWindowProps.FileRelativePath = startDsPageFileRelativePathFromArgs;
            }

            if (String.IsNullOrWhiteSpace(rootWindowProps.FileRelativePath))
            {
                if (DsProject.Instance.Review) MessageBoxHelper.ShowError(Resources.StartDsPageIsNotDefinedMessage);

                var firstDsPageDrawing = DsProject.Instance.AllDsPagesCache.Values.OrderBy(p => p.IsFaceplate).FirstOrDefault();

                if (firstDsPageDrawing != null)
                {
                    rootWindowProps.FileRelativePath = DsProject.Instance.GetFileRelativePath(firstDsPageDrawing.FileFullName);
                }
                else
                {
                    MessageBoxHelper.ShowError(Resources.NoDsPageToDispalyMessage);
                }
            }

            Rect? touchScreenNullable;
            TouchScreenMode touchScreenMode;
            List<WindowProps> registryRootWindowPropsCollection;
            string virtualKeyboardType;
            AppRegistryOptions.GetScreensInfo(out touchScreenNullable, out touchScreenMode, out registryRootWindowPropsCollection,
                out virtualKeyboardType);            
            if (touchScreenNullable.HasValue)
            {
                var touchScreenRect = touchScreenNullable.Value;
                if (ScreenHelper.IsFullyVisible(touchScreenRect))
                {
                    PlayDsProjectView.TouchScreenRect = touchScreenRect;
                }
                else
                {
                    PlayDsProjectView.TouchScreenRect = ScreenHelper.GetSystemScreenWorkingArea(
                        new Point(touchScreenRect.Left + touchScreenRect.Width / 2, touchScreenRect.Top + touchScreenRect.Height / 2));
                }                
            }
            else
            {
                PlayDsProjectView.TouchScreenRect = null;
            }            
            PlayDsProjectView.TouchScreenMode = touchScreenMode;

            if (registryRootWindowPropsCollection.Count == 0)
            {
                ShowWindow(null, rootWindowProps);
            }
            else
            {
                foreach (WindowProps registryRootWindowProps in registryRootWindowPropsCollection)
                {
                    var rootWindowPropsClone =
                        (WindowProps)rootWindowProps.Clone();
                    rootWindowPropsClone.CombineWith(registryRootWindowProps);

                    ShowWindow(null, rootWindowPropsClone);
                }
            }

            ShowVirtualKeyboardWindow(virtualKeyboardType);
        }        

        public void Close()
        {
            CommandsManager.RemoveCommandHandler(CommandsManager_OnGotCommand);
            DsDataAccessProvider.Instance.ContextStatusChanged -= DsDataAccessProvider_OnContextStatusChanged;            
            AllRootPlayWindowsClosed -= OnAllRootPlayWindowsClosed;

            var virtualKeyboardWindow = PlayDsProjectView.VirtualKeyboardWindow as Window;
            if (virtualKeyboardWindow != null)
            {
                virtualKeyboardWindow.Close();
            }

            foreach (IPlayWindow rootWindow in PlayDsProjectView.RootPlayWindows.ToArray())
            {
                rootWindow.Close();
            }

            foreach (Process startedProcess in _startedProcesses)
            {
                ProcessHelper.CloseAllWindows(startedProcess);
            }
            _startedProcesses.Clear();
        }

        #endregion

        #region private functions

        private event EventHandler AllRootPlayWindowsClosed = delegate { };

        private void DsDataAccessProvider_OnContextStatusChanged(object? sender, ContextStatusChangedEventArgs args)
        {
            if (args.ContextStateCode == ContextStateCodes.STATE_ABORTING)
                ((App)Application.Current).SafeShutdown();
        }        

        private void OnAllRootPlayWindowsClosed(object? sender, EventArgs e)
        {
            ((App)Application.Current).SafeShutdown();
        }

        private void StartProcess(StartProcessDsCommandOptions? startProcessDsCommandOptions)
        {
            if (startProcessDsCommandOptions == null) return;
            if (String.IsNullOrWhiteSpace(startProcessDsCommandOptions.Command)) return;
            
            var pi = new ProcessStartInfo(startProcessDsCommandOptions.Command, startProcessDsCommandOptions.Arguments);
            pi.UseShellExecute = true;
            pi.WorkingDirectory = DsProject.Instance.DsProjectPath;
            try
            {
                Process.Start(pi);
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, "Process start exception. {0} {1}", startProcessDsCommandOptions.Command, startProcessDsCommandOptions.Arguments);
            }
        }

        private void SendKey(SendKeyDsCommandOptions? sendKeyDsCommandOptions)
        {
            if (sendKeyDsCommandOptions == null) return;
            if (String.IsNullOrWhiteSpace(sendKeyDsCommandOptions.Key)) return;

            try
            {
                Keys key;
                if (Enum.TryParse(sendKeyDsCommandOptions.Key, true, out key))
                {
                    var keyboardKey = new KeyboardKey(key);
                    keyboardKey.PressAndRelease();
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, "SendKeys.Send() exception. {0}", sendKeyDsCommandOptions.Key);
            }
        }

        private void ShowWindow(IPlayWindow? senderPlayWindow, ShowWindowDsCommandOptions? showWindowDsCommandOptions)
        {
            if (showWindowDsCommandOptions == null) return;

            FileInfo? fileInfo = DsProject.Instance.GetExistingDsPageFileInfoOrNull(showWindowDsCommandOptions.FileRelativePath);
            if (fileInfo != null)
            {
                switch (fileInfo.Extension.ToUpperInvariant())
                {
                    case DsProject.DsPageFileExtensionUpper:
                    case ".HTM":
                    case ".HTML":
                    case ".OBJ":
                        break;
                    case ".BMP":
                    case ".DIB":
                    case ".JFIF":
                    case ".JPE":
                    case ".JPEG":
                    case ".JPG":
                    case ".PNG":
                    case ".TIF":
                    case ".TIFF":
                    case ".WDP":
                        try
                        {
                            var startInfo =
                                new ProcessStartInfo
                                {
                                    FileName = @"rundll32.exe",
                                    Arguments =
                                        Environment.ExpandEnvironmentVariables(
                                            @"%SystemRoot%\System32\shimgvw.dll,ImageView_Fullscreen ") +
                                        fileInfo.FullName
                                };
                            Process? newProcess = Process.Start(startInfo);
                            if (newProcess != null)
                            {
                                newProcess.Exited += (s, args) => _startedProcesses.Remove(newProcess);
                                _startedProcesses.Add(newProcess);
                            }
                            return;
                        }
                        catch
                        {
                        }
                        goto default;
                    default:
                        try
                        {
                            Process.Start(fileInfo.FullName);
                        }
                        catch (Exception ex)
                        {
                            DsProject.LoggersSet.Logger.LogError(ex, @"");
                        }
                        return;
                }
            }            

            var parentItem = showWindowDsCommandOptions.ParentItem ?? DsProject.Instance;
            IPlayWindow? parentWindow;
            int rootWindowNum = 0;
            if (ReferenceEquals(parentItem, DsProject.Instance)) // New Root window
            {
                parentWindow = null;
                foreach (var rootPlayWindow in PlayDsProjectView.RootPlayWindows)
                {
                    if (rootPlayWindow.RootWindowNum > rootWindowNum)
                        rootWindowNum = rootPlayWindow.RootWindowNum;
                }
                rootWindowNum = rootWindowNum + 1;
            }
            else
            {
                parentWindow = PlayDsProjectView.GetPlayWindow(senderPlayWindow, showWindowDsCommandOptions.ParentWindow);
                if (parentWindow != null && parentWindow.PlayControlWrapper.TryActivateExistingChildWindow(parentItem, showWindowDsCommandOptions.FileRelativePath))
                {
                    return;
                }
            }            
            
            var newWindow = new PlayWindow(parentWindow, rootWindowNum, showWindowDsCommandOptions.AutoCloseMs);            
            newWindow.Owner = null; // Windows must be independable          
            newWindow.PlayControlWrapper.Jump(new JumpDsCommandOptions { FileRelativePath = showWindowDsCommandOptions.FileRelativePath, ParentItem = parentItem });
            showWindowDsCommandOptions = (ShowWindowDsCommandOptions)showWindowDsCommandOptions.Clone();
            showWindowDsCommandOptions.ParentItem = parentItem;
            bool shouldClose = newWindow.PlayControlWrapper.PrepareWindow(newWindow, ref showWindowDsCommandOptions);
            if (shouldClose)
            {
                newWindow.Close();
                return;
            }

            newWindow.WindowCategory = showWindowDsCommandOptions.WindowCategory;            

            if (newWindow.IsRootWindow)
            {
                PlayDsProjectView.RootPlayWindows.Add(newWindow);

                newWindow.Closing += (s, args) =>
                {
                    if (PlayDsProjectView.RootPlayWindows.Count == 1)
                    {
                        DsProject.Instance.DesignModeInPlay = false;
                        if (DsProject.Instance.DesignModeInPlay) args.Cancel = true;
                    }                    
                };

                newWindow.Closed += (s, args) =>
                {
                    if (PlayDsProjectView.RootPlayWindows.Count == 1)
                    {
                        PlayDsProjectView.SaveCurrentPlayWindowsConfiguration(true);                        
                    }
                    PlayDsProjectView.RootPlayWindows.Remove(newWindow);
                    if (PlayDsProjectView.RootPlayWindows.Count == 0)
                        AllRootPlayWindowsClosed(this, EventArgs.Empty);
                };

                newWindow.Activated += (s, args) =>
                {
                    PlayDsProjectView.RootPlayWindows.Remove(newWindow);
                    PlayDsProjectView.RootPlayWindows.Add(newWindow);
                };
            }
            else // !newWindow.IsRootWindow
            {            
                if (parentWindow != null)
                {
                    shouldClose = parentWindow.PlayControlWrapper.PrepareChildWindow(parentItem, newWindow, ref showWindowDsCommandOptions);
                }
                if (shouldClose)
                {
                    newWindow.Close();
                    return;
                }
            }
            
            if (showWindowDsCommandOptions.WindowStyle != PlayWindowStyle.Default)
            {
                newWindow.WindowStyle = (WindowStyle)showWindowDsCommandOptions.WindowStyle;
                switch (newWindow.WindowStyle)
                {
                    case WindowStyle.None:
                        var windowChrome = new WindowChrome();
                        windowChrome.CornerRadius = new CornerRadius(0);
                        windowChrome.CaptionHeight = 0;
                        windowChrome.GlassFrameThickness = new Thickness(0);
                        windowChrome.NonClientFrameEdges = NonClientFrameEdges.None;
                        windowChrome.ResizeBorderThickness = new Thickness(10);
                        WindowChrome.SetWindowChrome(newWindow, windowChrome);
                        newWindow.AllowsTransparency = true;
                        newWindow.Background = Brushes.Transparent;
                        newWindow.PlayControlWrapper.Margin = new Thickness(10);
                        newWindow.PlayControlWrapper.Effect = new DropShadowEffect
                        {
                            Color = Color.FromRgb(200, 200, 200),
                            Direction = 270,
                            BlurRadius = 10,
                            ShadowDepth = 3
                        };
                        break;
                }
            }
            if (showWindowDsCommandOptions.WindowResizeMode != PlayWindowResizeMode.Default)
            {
                newWindow.ResizeMode = (ResizeMode)showWindowDsCommandOptions.WindowResizeMode;
            }
            if (showWindowDsCommandOptions.WindowShowInTaskbar != DefaultFalseTrue.Default)
            {
                newWindow.ShowInTaskbar = (showWindowDsCommandOptions.WindowShowInTaskbar ==
                                                    DefaultFalseTrue.True);
            }
            if (showWindowDsCommandOptions.WindowTopmost != DefaultFalseTrue.Default)
            {
                newWindow.Topmost = (showWindowDsCommandOptions.WindowTopmost == DefaultFalseTrue.True);
            }
            if (ScreenHelper.IsValidCoordinate(showWindowDsCommandOptions.ContentLeft) &&
                ScreenHelper.IsValidCoordinate(showWindowDsCommandOptions.ContentTop))
            {
                newWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                newWindow.Left = showWindowDsCommandOptions.ContentLeft!.Value;
                newWindow.Top = showWindowDsCommandOptions.ContentTop!.Value;
            }
            else
            {
                newWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }            
            
            if (ScreenHelper.IsValidLength(showWindowDsCommandOptions.ContentWidth) &&
                ScreenHelper.IsValidLength(showWindowDsCommandOptions.ContentHeight))
            {
                newWindow.PlayControlWrapper.Width = showWindowDsCommandOptions.ContentWidth!.Value;
                newWindow.PlayControlWrapper.Height = showWindowDsCommandOptions.ContentHeight!.Value;
                newWindow.SizeToContent = SizeToContent.WidthAndHeight;
            }

            newWindow.Show();
            
            newWindow.PlayControlWrapper.Width = Double.NaN;
            newWindow.PlayControlWrapper.Height = Double.NaN;
            newWindow.SizeToContent = SizeToContent.Manual;
            if (ScreenHelper.IsValidCoordinate(showWindowDsCommandOptions.ContentLeft) &&
                ScreenHelper.IsValidCoordinate(showWindowDsCommandOptions.ContentTop))
            {
                var rect = ScreenHelper.GetRect(newWindow.PlayControlWrapper);
                var leftDelta = rect.Left - showWindowDsCommandOptions.ContentLeft!.Value;
                var topDelta = rect.Top - showWindowDsCommandOptions.ContentTop!.Value;
                newWindow.Left = newWindow.Left - leftDelta;
                newWindow.Top = newWindow.Top - topDelta;
            }

            if (!String.IsNullOrEmpty(DsProject.Instance.Desc))
            {
                showWindowDsCommandOptions.TitleInfo.FallbackValue = DsProject.Instance.Desc;
            }
            else
            {
                showWindowDsCommandOptions.TitleInfo.FallbackValue = DsProject.Instance.Name;
            }            
            newWindow.SetBindingOrConst(parentItem.Find<IDsContainer>(), Window.TitleProperty,
                    showWindowDsCommandOptions.TitleInfo, BindingMode.OneWay, UpdateSourceTrigger.Default);

            Rect? screenWorkingArea;
            if (showWindowDsCommandOptions.ShowOnTouchScreen == DefaultFalseTrue.True &&
                    PlayDsProjectView.TouchScreenRect.HasValue)
            {
                screenWorkingArea = PlayDsProjectView.TouchScreenRect.Value;
            }
            else
            {
                if (newWindow.IsRootWindow && !showWindowDsCommandOptions.ContentLeft.HasValue && !showWindowDsCommandOptions.ContentTop.HasValue)
                {
                    screenWorkingArea = GetFreeSystemScreenWorkingArea();
                }
                else
                {
                    screenWorkingArea = ScreenHelper.GetNearestSystemScreenWorkingArea(
                        new Point(newWindow.Left + newWindow.ActualWidth / 2, newWindow.Top + newWindow.ActualHeight / 2));
                }
            }
            
            if (screenWorkingArea.HasValue && screenWorkingArea.Value != Rect.Empty)
            {
                double kX = screenWorkingArea.Value.Width / newWindow.ActualWidth;
                double kY = screenWorkingArea.Value.Height / newWindow.ActualHeight;
                if (kX < 1.0 || kY < 1.0)
                {
                    double k = Math.Min(kX, kY);
                    newWindow.Width = k * newWindow.ActualWidth;
                    newWindow.Height = k * newWindow.ActualHeight;
                }

                ScreenHelper.SetFullyVisible(newWindow, screenWorkingArea.Value);
            }

            if (showWindowDsCommandOptions.WindowFullScreen == DefaultFalseTrue.True)
            {
                newWindow.WindowState = WindowState.Maximized;
            }            
        }
        
        public static Rect GetFreeSystemScreenWorkingArea()
        {
            var systemScreensWorkingAreas = ScreenHelper.GetSystemScreensWorkingAreas();

            var freeSystemScreensWorkingAreas = new List<Rect>(systemScreensWorkingAreas);

            if (PlayDsProjectView.TouchScreenRect.HasValue)
                freeSystemScreensWorkingAreas.Remove(PlayDsProjectView.TouchScreenRect.Value);

            foreach (var rpw in PlayDsProjectView.RootPlayWindows)
            {
                var point = new Point(rpw.Left + rpw.ActualWidth / 2, rpw.Top + rpw.ActualHeight / 2);
                foreach (Rect freeSystemScreensWorkingArea in freeSystemScreensWorkingAreas.ToArray())
                {
                    if (point.X >= freeSystemScreensWorkingArea.Left &&
                        point.Y >= freeSystemScreensWorkingArea.Top &&
                        point.X <= freeSystemScreensWorkingArea.Right &&
                        point.Y <= freeSystemScreensWorkingArea.Bottom)
                    {
                        freeSystemScreensWorkingAreas.Remove(freeSystemScreensWorkingArea);
                        break;
                    }
                }
            }

            if (freeSystemScreensWorkingAreas.Count > 0) return freeSystemScreensWorkingAreas.First();
            else return systemScreensWorkingAreas.FirstOrDefault();
        }

        private void ShowVirtualKeyboardWindow(string virtualKeyboardType)
        {
            if (String.IsNullOrEmpty(virtualKeyboardType)) return;            

            if (!PlayDsProjectView.TouchScreenRect.HasValue)
            {
                System.Windows.MessageBox.Show(Resources.TouchScreenIsNotConfiguredMessage);
                return;
            }

            var virtualKeyboardControl = AddonsHelper.NewVirtualKeyboardControl(virtualKeyboardType);
            if (virtualKeyboardControl == null)
            {
                System.Windows.MessageBox.Show(Resources.VirtualKeyboardNotSupportedMessage + @": " + virtualKeyboardType);
                return;
            }

            var virtKeyboardWindow = new VirtualKeyboardWindow(virtualKeyboardControl, virtualKeyboardType);            

            virtKeyboardWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            virtKeyboardWindow.SizeToContent = SizeToContent.Manual;
            virtKeyboardWindow.ShowActivated = false;
            virtKeyboardWindow.Show();
            var touchScreenRect = PlayDsProjectView.TouchScreenRect.Value;
            virtKeyboardWindow.Left = touchScreenRect.X;
            virtKeyboardWindow.Top = touchScreenRect.Y;
            virtKeyboardWindow.Width = touchScreenRect.Width;
            virtKeyboardWindow.Height = touchScreenRect.Height;            

            /*
            //MaxHeight = touchScreen.Bounds.Height;
            //MaxWidth = touchScreen.Bounds.Width;
            if (MaxWidth < keyboardView.Width)
            {
                // Scale keyboard
                var rat = keyboardView.Width / MaxWidth;
                keyboardView.Width = MaxWidth;
                keyboardView.Height = MaxHeight / rat;
            }
            Width = keyboardView.Width;
            Height = keyboardView.Height;
            */
        }

        private void CommandsManager_OnGotCommand(Command command)
        {
            switch (command.CommandString)
            {
                case CommandsManager.ShowWindowCommand:
                    ShowWindow(command.SenderFrame != null ? command.SenderFrame.PlayWindow : null, (ShowWindowDsCommandOptions?)command.CommandOptions);
                    break;
                case CommandsManager.ShowFaceplateForTagCommand:                    
                    PlayDsProjectView.ShowFaceplateForTagUsingPlayInfoAndAllDsPagesCache(command.SenderFrame != null ? command.SenderFrame.PlayWindow : null, ((ShowFaceplateForTagDsCommandOptions)command.CommandOptions!).TagName,
                        ObsoleteAnyHelper.ConvertTo<int>(((ShowFaceplateForTagDsCommandOptions)command.CommandOptions).FaceplateIndex, false), true);                    
                    break;
                case CommandsManager.ShowVirtualKeyboardCommand:
                    ShowVirtualKeyboardWindow(((GenericDsCommandOptions)command.CommandOptions!).ParamsString);
                    break;
                case CommandsManager.StartProcessCommand:
                    StartProcess(command.CommandOptions as StartProcessDsCommandOptions);
                    break;
                case CommandsManager.SendKeyCommand:
                    SendKey(command.CommandOptions as SendKeyDsCommandOptions);
                    break;
                case CommandsManager.FindCommand:
                    FindReplaceDialog.ShowAsPlayFind(command.SenderFrame != null ? command.SenderFrame.PlayWindow as Window : null);
                    break;
                case CommandsManager.ShowNewRootWindowCommand:                    
                    ShowWindow(null, DsProject.Instance.RootWindowProps);
                    break;
                case CommandsManager.SetupTouchScreenCommand:
                    var touchScreenFinderWindow = new TouchScreenSetupWindow();
                    touchScreenFinderWindow.Show();
                    break;
                case CommandsManager.JumpCommand:
                    OnGotJumpCommand((JumpDsCommandOptions?)command.CommandOptions, command.SenderFrame);
                    break;
                case CommandsManager.JumpBackCommand:
                    OnGotJumpBackCommand((JumpBackDsCommandOptions?)command.CommandOptions, command.SenderFrame);
                    break;
                case CommandsManager.JumpForwardCommand:
                    OnGotJumpForwardCommand((JumpForwardDsCommandOptions?)command.CommandOptions, command.SenderFrame);
                    break;
                case CommandsManager.CloseWindowCommand:
                    OnGotCloseWindowCommand(command.SenderFrame != null ? command.SenderFrame.PlayWindow : null, (CloseWindowDsCommandOptions?)command.CommandOptions);
                    break;
                case CommandsManager.CloseAllFaceplatesCommand:
                    OnGotCloseAllFaceplatesCommand(command.SenderFrame != null ? command.SenderFrame.PlayWindow : null, (CloseAllFaceplatesDsCommandOptions?)command.CommandOptions);
                    break;
                case CommandsManager.ApplyCommand:
                    OnGotApplyCommand(command.SenderFrame != null ? command.SenderFrame.PlayWindow : null, (ApplyDsCommandOptions?)command.CommandOptions);
                    break;                
                case CommandsManager.AckTagCommand:
                    {
                        string paramsString =
                            ((GenericDsCommandOptions)(command.CommandOptions ?? throw new InvalidOperationException()))
                            .ParamsString;
                        if (!string.IsNullOrWhiteSpace(paramsString))
                        {
                            var eventIds = new List<Ssz.Utils.DataAccess.EventId>();

                            DsPageDrawing? dsPageDrawing = DsProject.Instance.AllDsPagesCacheGetDsPageDrawing(paramsString);
                            if (dsPageDrawing != null)                                
                            {   
                                //var eventSourceArea = PlayDsProjectView.EventSourceModel.GetOrCreateEventSourceArea(dsPageDrawing.Name);
                                foreach (var kvp in PlayDsProjectView.EventSourceModel.EventSourceObjects)
                                {
                                    var eventSourceObject = kvp.Value;

                                    if (!eventSourceObject.EventSourceAreas.ContainsKey(dsPageDrawing.Name))
                                        continue;

                                    foreach (var conditionState in eventSourceObject.AlarmConditions.Values)
                                    {
                                        if (conditionState.LastAlarmInfoViewModel is not null &&
                                                conditionState.LastAlarmInfoViewModel.EventId is not null)
                                            eventIds.Add(conditionState.LastAlarmInfoViewModel.EventId);
                                    }
                                    if (eventSourceObject.NormalConditionState.LastAlarmInfoViewModel is not null &&
                                            eventSourceObject.NormalConditionState.LastAlarmInfoViewModel.EventId is not null)
                                        eventIds.Add(eventSourceObject.NormalConditionState.LastAlarmInfoViewModel.EventId);
                                }                                
                            }
                            else
                            {
                                EventSourceObject eventSourceObject =
                                    PlayDsProjectView.EventSourceModel.GetOrCreateEventSourceObject(paramsString);

                                foreach (var conditionState in eventSourceObject.AlarmConditions.Values)
                                {
                                    if (conditionState.LastAlarmInfoViewModel is not null &&
                                            conditionState.LastAlarmInfoViewModel.EventId is not null)
                                        eventIds.Add(conditionState.LastAlarmInfoViewModel.EventId);
                                }
                                if (eventSourceObject.NormalConditionState.LastAlarmInfoViewModel is not null &&
                                        eventSourceObject.NormalConditionState.LastAlarmInfoViewModel.EventId is not null)
                                    eventIds.Add(eventSourceObject.NormalConditionState.LastAlarmInfoViewModel.EventId);
                            }                            

                            if (eventIds.Count > 0)
                                DsDataAccessProvider.Instance.AckAlarms(eventIds.ToArray());
                        }
                    }
                    break;
                case CommandsManager.BuzzerResetCommand:
                    PlayDsProjectView.Buzzer.BuzzerState = BuzzerStateEnum.Silent;
                    break;
                case CommandsManager.BuzzerEnableCommand:
                    if (command.CommandOptions is OnOffToggleDsCommandOptions)
                        switch (((OnOffToggleDsCommandOptions)command.CommandOptions).EnableDisableBuzzer)
                        {
                            case OnOffToggle.Off:
                                PlayDsProjectView.Buzzer.IsEnabled = false;
                                break;
                            case OnOffToggle.On:
                                PlayDsProjectView.Buzzer.IsEnabled = true;
                                break;
                            default:
                                PlayDsProjectView.Buzzer.IsEnabled = !PlayDsProjectView.Buzzer.IsEnabled;
                                break;
                        }

                    break;
            }
        }

        private void OnGotJumpCommand(JumpDsCommandOptions? jumpDsCommandOptions, Frame? senderFrame)
        {
            if (jumpDsCommandOptions == null) 
                return;

            IPlayWindow? targetWindow = PlayDsProjectView.GetPlayWindow(senderFrame != null ? senderFrame.PlayWindow : null, jumpDsCommandOptions.TargetWindow, jumpDsCommandOptions.RootWindowNum);
            if (targetWindow == null) 
                return;

            try
            {                
                targetWindow.PlayControlWrapper.Jump(jumpDsCommandOptions, senderFrame);
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }
        }

        private void OnGotJumpBackCommand(JumpBackDsCommandOptions? jumpBackDsCommandOptions, Frame? senderFrame)
        {
            if (jumpBackDsCommandOptions == null) 
                return;

            IPlayWindow? targetWindow = PlayDsProjectView.GetPlayWindow(senderFrame != null ? senderFrame.PlayWindow : null, jumpBackDsCommandOptions.TargetWindow, jumpBackDsCommandOptions.RootWindowNum);
            if (targetWindow == null) 
                return;

            string frameName;
            if (jumpBackDsCommandOptions.CurrentFrame && senderFrame != null)
            {
                frameName = senderFrame.FrameName;
            }
            else
            {
                frameName = jumpBackDsCommandOptions.FrameName;
            }

            targetWindow.PlayControlWrapper.JumpBack(frameName);
        }

        private void OnGotJumpForwardCommand(JumpForwardDsCommandOptions? jumpForwardDsCommandOptions, Frame? senderFrame)
        {
            if (jumpForwardDsCommandOptions == null) 
                return;

            IPlayWindow? targetWindow = PlayDsProjectView.GetPlayWindow(senderFrame != null ? senderFrame.PlayWindow : null, jumpForwardDsCommandOptions.TargetWindow, jumpForwardDsCommandOptions.RootWindowNum);
            if (targetWindow == null) 
                return;

            string frameName;
            if (jumpForwardDsCommandOptions.CurrentFrame && senderFrame != null)
            {
                frameName = senderFrame.FrameName;
            }
            else
            {
                frameName = jumpForwardDsCommandOptions.FrameName;
            }

            targetWindow.PlayControlWrapper.JumpForward(frameName);
        }

        private void OnGotCloseWindowCommand(IPlayWindow? senderWindow, CloseWindowDsCommandOptions? closeWindowDsCommandOptions)
        {
            if (closeWindowDsCommandOptions == null) 
                return;

            IPlayWindow? targetWindow = PlayDsProjectView.GetPlayWindow(senderWindow, closeWindowDsCommandOptions.TargetWindow);
            if (targetWindow == null) 
                return;

            targetWindow.Close();
        }

        private void OnGotCloseAllFaceplatesCommand(IPlayWindow? senderWindow, CloseAllFaceplatesDsCommandOptions? closeAllFaceplatesDsCommandOptions)
        {
            if (closeAllFaceplatesDsCommandOptions == null) 
                return;

            IPlayWindow? rootWindow = PlayDsProjectView.GetPlayWindow(senderWindow, TargetWindow.RootWindow);
            if (rootWindow == null) 
                return;

            rootWindow.PlayControlWrapper.CloseChildWindows(closeAllFaceplatesDsCommandOptions);
        }

        private void OnGotApplyCommand(IPlayWindow? senderWindow, ApplyDsCommandOptions? applyDsCommandOptions)
        {
            if (applyDsCommandOptions == null) 
                return;

            IPlayWindow? targetWindow = PlayDsProjectView.GetPlayWindow(senderWindow, applyDsCommandOptions.TargetWindow);
            if (targetWindow == null) 
                return;

            foreach (IAppliable appliable in
                        TreeHelper.FindChilds<IAppliable>(targetWindow.PlayControlWrapper))
            {
                appliable.Apply();
            }
        }

        #endregion

        #region private fields

        private readonly List<Process> _startedProcesses = new List<Process>();        

        #endregion
    }
}