using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Ssz.Operator.Core.Utils; 
using Ssz.Utils; 
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.DataAccess;
using Ssz.Utils.DataAccess;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Properties;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Threading;

namespace Ssz.Operator.Play
{
    internal class WindowsManager
    {
        #region public functions

        public static readonly WindowsManager Instance = new WindowsManager();
        
        public void Initialize(string startDsPageFileRelativePathFromArgs, IClassicDesktopStyleApplicationLifetime desktop)
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
                // TODO
                //if (DsProject.Instance.Review) MessageBoxHelper.ShowError(PlayResources.StartDsPageIsNotDefinedMessage);

                //var firstDsPageDrawing = DsProject.Instance.AllDsPagesCache.Values.OrderBy(p => p.IsFaceplate).FirstOrDefault();

                //if (firstDsPageDrawing != null)
                //{
                //    rootWindowProps.FileRelativePath = DsProject.Instance.GetFileRelativePath(firstDsPageDrawing.FileFullName);
                //}
                //else
                //{
                //    MessageBoxHelper.ShowError(PlayResources.NoDsPageToDispalyMessage);
                //}
            }

            //Rect? touchScreenNullable;
            //TouchScreenMode touchScreenMode;
            //List<WindowProps> registryRootWindowPropsCollection;
            //string virtualKeyboardType;
            //AppRegistryOptions.GetScreensInfo(out touchScreenNullable, out touchScreenMode, out registryRootWindowPropsCollection,
            //    out virtualKeyboardType);            
            //if (touchScreenNullable.HasValue)
            //{
            //    var touchScreenRect = touchScreenNullable.Value;
            //    if (ScreenHelper.IsFullyVisible(touchScreenRect))
            //    {
            //        PlayDsProjectView.TouchScreenRect = touchScreenRect;
            //    }
            //    else
            //    {
            //        PlayDsProjectView.TouchScreenRect = ScreenHelper.GetSystemScreenWorkingArea(
            //            new Point(touchScreenRect.Left + touchScreenRect.Width / 2, touchScreenRect.Top + touchScreenRect.Height / 2));
            //    }                
            //}
            //else
            //{
            //    PlayDsProjectView.TouchScreenRect = null;
            //}            
            //PlayDsProjectView.TouchScreenMode = touchScreenMode;

            //if (registryRootWindowPropsCollection.Count == 0)
            //{
                ShowWindow_Desktop(null, rootWindowProps);
            //}
            //else
            //{
            //    foreach (WindowProps registryRootWindowProps in registryRootWindowPropsCollection)
            //    {
            //        var rootWindowPropsClone =
            //            (WindowProps)rootWindowProps.Clone();
            //        rootWindowPropsClone.CombineWith(registryRootWindowProps);

            //        ShowWindow(null, rootWindowPropsClone);
            //    }
            //}

            //ShowVirtualKeyboardWindow(virtualKeyboardType);
        }
        
        public void Initialize(string startDsPageFileRelativePathFromArgs, ISingleViewApplicationLifetime singleViewPlatform)
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
                var firstDsPageDrawing = DsProject.Instance.AllDsPagesCache.Values.OrderBy(p => p.IsFaceplate).FirstOrDefault();

                if (firstDsPageDrawing != null)
                {
                    rootWindowProps.FileRelativePath = DsProject.Instance.GetFileRelativePath(firstDsPageDrawing.FileFullName);
                }
            }

            ShowWindow_Browser(null, rootWindowProps, singleViewPlatform);
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
                // TODO
                startedProcess.Kill(true);
            }
            _startedProcesses.Clear();
        }

        #endregion

        #region private functions

        private event EventHandler AllRootPlayWindowsClosed = delegate { };

        private void DsDataAccessProvider_OnContextStatusChanged(object? sender, ContextStatusChangedEventArgs args)
        {
            if (args.ContextStateCode == ContextStateCodes.STATE_ABORTING)
                (App.Current as App)?.SafeShutdown();
        }        

        private void OnAllRootPlayWindowsClosed(object? sender, EventArgs e)
        {
            (App.Current as App)?.SafeShutdown();
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
            if (sendKeyDsCommandOptions == null) 
                return;
            if (String.IsNullOrWhiteSpace(sendKeyDsCommandOptions.Key)) 
                return;

            //try
            //{
            //    Keys key;
            //    if (Enum.TryParse(sendKeyDsCommandOptions.Key, true, out key))
            //    {
            //        var keyboardKey = new KeyboardKey(key);
            //        keyboardKey.PressAndRelease();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    DsProject.LoggersSet.Logger.LogError(ex, "SendKeys.Send() exception. {0}", sendKeyDsCommandOptions.Key);
            //}
        }

        private async void ShowWindow_Desktop(IPlayWindow? senderPlayWindow, ShowWindowDsCommandOptions? showWindowDsCommandOptions)
        {
#if TEST
            ShowWindow_Browser(senderPlayWindow, showWindowDsCommandOptions, null);
            return;
#endif

            if (showWindowDsCommandOptions == null)
                return;

            string? existingDsPageFileFullName = DsProject.Instance.GetExistingDsPageFileFullNameOrNull(showWindowDsCommandOptions.FileRelativePath);
            if (!String.IsNullOrEmpty(existingDsPageFileFullName))
            {
                switch (Path.GetExtension(existingDsPageFileFullName).ToUpperInvariant())
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
                            //var startInfo =
                            //    new ProcessStartInfo
                            //    {
                            //        FileName = @"rundll32.exe",
                            //        Arguments =
                            //            Environment.ExpandEnvironmentVariables(
                            //                @"%SystemRoot%\System32\shimgvw.dll,ImageView_Fullscreen ") +
                            //            fileInfo.FullName
                            //    };
                            //Process? newProcess = Process.Start(startInfo);
                            //if (newProcess != null)
                            //{
                            //    newProcess.Exited += (s, args) => _startedProcesses.Remove(newProcess);
                            //    _startedProcesses.Add(newProcess);
                            //}
                            return;
                        }
                        catch
                        {
                        }
                        goto default;
                    default:
                        try
                        {
                            //Process.Start(fileInfo.FullName);
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

            var newWindow = new DesktopPlayWindow(parentWindow, rootWindowNum, showWindowDsCommandOptions.AutoCloseMs);                     
            newWindow.PlayControlWrapper.Jump(new JumpDsCommandOptions { FileRelativePath = showWindowDsCommandOptions.FileRelativePath, ParentItem = parentItem });
            await newWindow.PlayControlWrapper.PlayControl.DsPageDrawingIsLoadedSemaphoreSlim.WaitAsync();
            
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
                newWindow.Closing += (s, args) =>
                {
                    if (PlayDsProjectView.RootPlayWindows.Count == 1)
                    {
                        DsProject.Instance.DesignModeInPlay = false;
                        if (DsProject.Instance.DesignModeInPlay) 
                            args.Cancel = true;
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
                    if (PlayDsProjectView.RootPlayWindows.Remove(newWindow))
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

            //if (showWindowDsCommandOptions.WindowStyle != PlayWindowStyle.Default)
            //{
            //    newWindow.WindowStyle = (WindowStyle)showWindowDsCommandOptions.WindowStyle;
            //    switch (newWindow.WindowStyle)
            //    {
            //        case WindowStyle.None:
            //            var windowChrome = new WindowChrome();
            //            windowChrome.CornerRadius = new CornerRadius(0);
            //            windowChrome.CaptionHeight = 0;
            //            windowChrome.GlassFrameThickness = new Thickness(0);
            //            windowChrome.NonClientFrameEdges = NonClientFrameEdges.None;
            //            windowChrome.ResizeBorderThickness = new Thickness(10);
            //            WindowChrome.SetWindowChrome(newWindow, windowChrome);
            //            newWindow.AllowsTransparency = true;
            //            newWindow.Background = Brushes.Transparent;
            //            newWindow.PlayControlWrapper.Margin = new Thickness(10);
            //            newWindow.PlayControlWrapper.Effect = new DropShadowEffect
            //            {
            //                Color = Color.FromRgb(200, 200, 200),
            //                Direction = 270,
            //                BlurRadius = 10,
            //                ShadowDepth = 3
            //            };
            //            break;
            //    }
            //}
            if (showWindowDsCommandOptions.WindowResizeMode != PlayWindowResizeMode.Default)
            {
                newWindow.CanResize = showWindowDsCommandOptions.WindowResizeMode == 
                    PlayWindowResizeMode.CanResize || showWindowDsCommandOptions.WindowResizeMode == PlayWindowResizeMode.CanResizeWithGrip;
            }
            if (showWindowDsCommandOptions.WindowShowInTaskbar != DefaultFalseTrue.Default)
            {
                newWindow.ShowInTaskbar = showWindowDsCommandOptions.WindowShowInTaskbar ==
                                                    DefaultFalseTrue.True;
            }
            if (showWindowDsCommandOptions.WindowTopmost != DefaultFalseTrue.Default)
            {
                newWindow.Topmost = showWindowDsCommandOptions.WindowTopmost == DefaultFalseTrue.True;
            }
            if (showWindowDsCommandOptions.WindowPosition.HasValue &&
                !Double.IsNaN(showWindowDsCommandOptions.WindowPosition.Value.X) && !Double.IsInfinity(showWindowDsCommandOptions.WindowPosition.Value.X) &&
                !Double.IsNaN(showWindowDsCommandOptions.WindowPosition.Value.Y) && !Double.IsInfinity(showWindowDsCommandOptions.WindowPosition.Value.Y))
            {
                newWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                newWindow.Position = showWindowDsCommandOptions.WindowPosition.Value;
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

            newWindow.Show(); // Owner = null; // Windows must be independable 

            newWindow.PlayControlWrapper.Width = Double.NaN;
            newWindow.PlayControlWrapper.Height = Double.NaN;
            newWindow.SizeToContent = SizeToContent.Manual;

            if (showWindowDsCommandOptions.WindowPosition.HasValue)
                newWindow.Position = showWindowDsCommandOptions.WindowPosition.Value;

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

            PixelRect? screenWorkingArea;
            //if (showWindowDsCommandOptions.ShowOnTouchScreen == DefaultFalseTrue.True &&
            //        PlayDsProjectView.TouchScreenRect.HasValue)
            //{
            //    screenWorkingArea = PlayDsProjectView.TouchScreenRect.Value;
            //}
            //else
            {
                if (newWindow.IsRootWindow && !showWindowDsCommandOptions.WindowPosition.HasValue)
                {
                    screenWorkingArea = GetFreeSystemScreenWorkingArea(newWindow);
                }
                else
                {
                    var newWindowPosition = newWindow.Position;
                    var newWindowBounds = newWindow.Bounds;
                    screenWorkingArea = ScreenHelper.GetNearestSystemScreenWorkingArea(
                        new PixelPoint((int)(newWindowPosition.X + newWindowBounds.Width / 2), (int)(newWindowPosition.Y + newWindowBounds.Height / 2)), 
                        newWindow);
                }
            }

            if (screenWorkingArea.HasValue && screenWorkingArea.Value != default)
            {
                double kX = screenWorkingArea.Value.Width / newWindow.Bounds.Width;
                double kY = screenWorkingArea.Value.Height / newWindow.Bounds.Height;
                if (kX < 1.0 || kY < 1.0)
                {
                    double k = Math.Min(kX, kY);
                    newWindow.Width = k * newWindow.Bounds.Width;
                    newWindow.Height = k * newWindow.Bounds.Height;
                }

                ScreenHelper.SetFullyVisible(newWindow, screenWorkingArea.Value);
            }

            if (showWindowDsCommandOptions.WindowFullScreen == DefaultFalseTrue.True)
            {
                newWindow.WindowState = WindowState.Maximized;
            }

            if (newWindow.IsRootWindow)
                PlayDsProjectView.RootPlayWindows.Add(newWindow);
        }

        private void ShowWindow_Browser(IPlayWindow? playWindow, ShowWindowDsCommandOptions? showWindowDsCommandOptions, ISingleViewApplicationLifetime? singleViewPlatform)
        {
            if (showWindowDsCommandOptions == null) 
                return;

            string? existingDsPageFileFullName = DsProject.Instance.GetExistingDsPageFileFullNameOrNull(showWindowDsCommandOptions.FileRelativePath);
            if (!String.IsNullOrEmpty(existingDsPageFileFullName))
            {
                switch (Path.GetExtension(existingDsPageFileFullName).ToUpperInvariant())
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
                            //var startInfo =
                            //    new ProcessStartInfo
                            //    {
                            //        FileName = @"rundll32.exe",
                            //        Arguments =
                            //            Environment.ExpandEnvironmentVariables(
                            //                @"%SystemRoot%\System32\shimgvw.dll,ImageView_Fullscreen ") +
                            //            fileInfo.FullName
                            //    };
                            //Process? newProcess = Process.Start(startInfo);
                            //if (newProcess != null)
                            //{
                            //    newProcess.Exited += (s, args) => _startedProcesses.Remove(newProcess);
                            //    _startedProcesses.Add(newProcess);
                            //}
                            return;
                        }
                        catch
                        {
                        }
                        goto default;
                    default:
                        try
                        {
                            //Process.Start(fileInfo.FullName);
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
                parentWindow = PlayDsProjectView.GetPlayWindow(playWindow, showWindowDsCommandOptions.ParentWindow);
                if (parentWindow != null && parentWindow.PlayControlWrapper.TryActivateExistingChildWindow(parentItem, showWindowDsCommandOptions.FileRelativePath))
                {
                    return;
                }
            }            
            
            var newWindow = new BrowserPlayWindow(parentWindow, rootWindowNum, showWindowDsCommandOptions.AutoCloseMs);

            newWindow.PlayControlWrapper.Jump(new JumpDsCommandOptions { FileRelativePath = showWindowDsCommandOptions.FileRelativePath, ParentItem = parentItem });

            //await Dispatcher.UIThread.InvokeAsync(async () =>
            //    {
            //        await newWindow.PlayControlWrapper.PlayControl.DsPageDrawingIsLoadedSemaphoreSlim.WaitAsync();
            //    }); 
            //var taskCompletionSource = new TaskCompletionSource<int>();
            //var workingThread = new Thread(() =>
            //{
            //    newWindow.PlayControlWrapper.PlayControl.DsPageDrawingIsLoadedEventWaitHandle.WaitOne();
            //    taskCompletionSource.SetResult(0);
            //});
            //workingThread.Start();
            //await taskCompletionSource.Task;
            //await Task.Run(newWindow.PlayControlWrapper.PlayControl.DsPageDrawingIsLoadedEventWaitHandle.WaitOne);

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
                newWindow.Closing += (s, args) =>
                {
                    if (PlayDsProjectView.RootPlayWindows.Count == 1)
                    {
                        DsProject.Instance.DesignModeInPlay = false;
                        if (DsProject.Instance.DesignModeInPlay) 
                            args.Cancel = true;
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
                    if (PlayDsProjectView.RootPlayWindows.Remove(newWindow))
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

                newWindow.Closed += (s, args) =>
                {                    
                    BrowserPlayWindow? browserPlayWindow = parentWindow as BrowserPlayWindow;
                    if (browserPlayWindow is not null)
                        browserPlayWindow.FaceplatesCanvas.Children.Remove(newWindow);
                };
            }

            //if (showWindowDsCommandOptions.WindowStyle != PlayWindowStyle.Default)
            //{
            //    newWindow.WindowStyle = (WindowStyle)showWindowDsCommandOptions.WindowStyle;
            //    switch (newWindow.WindowStyle)
            //    {
            //        case WindowStyle.None:
            //            var windowChrome = new WindowChrome();
            //            windowChrome.CornerRadius = new CornerRadius(0);
            //            windowChrome.CaptionHeight = 0;
            //            windowChrome.GlassFrameThickness = new Thickness(0);
            //            windowChrome.NonClientFrameEdges = NonClientFrameEdges.None;
            //            windowChrome.ResizeBorderThickness = new Thickness(10);
            //            WindowChrome.SetWindowChrome(newWindow, windowChrome);
            //            newWindow.AllowsTransparency = true;
            //            newWindow.Background = Brushes.Transparent;
            //            newWindow.PlayControlWrapper.Margin = new Thickness(10);
            //            newWindow.PlayControlWrapper.Effect = new DropShadowEffect
            //            {
            //                Color = Color.FromRgb(200, 200, 200),
            //                Direction = 270,
            //                BlurRadius = 10,
            //                ShadowDepth = 3
            //            };
            //            break;
            //    }
            //}
            //if (showWindowDsCommandOptions.WindowResizeMode != PlayWindowResizeMode.Default)
            //{
            //    newWindow.ResizeMode = (ResizeMode)showWindowDsCommandOptions.WindowResizeMode;
            //}
            //if (showWindowDsCommandOptions.WindowShowInTaskbar != DefaultFalseTrue.Default)
            //{
            //    newWindow.ShowInTaskbar = (showWindowDsCommandOptions.WindowShowInTaskbar ==
            //                                        DefaultFalseTrue.True);
            //}
            //if (showWindowDsCommandOptions.WindowTopmost != DefaultFalseTrue.Default)
            //{
            //    newWindow.Topmost = (showWindowDsCommandOptions.WindowTopmost == DefaultFalseTrue.True);
            //}            

            if (newWindow.IsRootWindow)
            {
                if (ScreenHelper.IsValidLength(showWindowDsCommandOptions.ContentWidth) &&
                    ScreenHelper.IsValidLength(showWindowDsCommandOptions.ContentHeight))
                {
                    newWindow.Width = showWindowDsCommandOptions.ContentWidth!.Value;
                    newWindow.Height = showWindowDsCommandOptions.ContentHeight!.Value; // + newWindow.WindowHeaderStackPanel.Height;
                }
#if TEST
                Window window = new();
                window.Content = newWindow;
                window.Show();
#else
                singleViewPlatform!.MainView = newWindow;
#endif
            }
            else
            {
                if (showWindowDsCommandOptions.WindowPosition.HasValue &&
                !Double.IsNaN(showWindowDsCommandOptions.WindowPosition.Value.X) && !Double.IsInfinity(showWindowDsCommandOptions.WindowPosition.Value.X) &&
                !Double.IsNaN(showWindowDsCommandOptions.WindowPosition.Value.Y) && !Double.IsInfinity(showWindowDsCommandOptions.WindowPosition.Value.Y))
                {
                    //newWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                    newWindow.Position = showWindowDsCommandOptions.WindowPosition.Value;
                }
                else
                {
                    //newWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                if (ScreenHelper.IsValidLength(showWindowDsCommandOptions.ContentWidth) &&
                    ScreenHelper.IsValidLength(showWindowDsCommandOptions.ContentHeight))
                {
                    newWindow.PlayControlWrapper.Width = showWindowDsCommandOptions.ContentWidth!.Value;
                    newWindow.PlayControlWrapper.Height = showWindowDsCommandOptions.ContentHeight!.Value; // + newWindow.WindowHeaderStackPanel.Height;
                }

                BrowserPlayWindow? browserPlayWindow = parentWindow as BrowserPlayWindow;
                if (browserPlayWindow is not null)
                    browserPlayWindow.FaceplatesCanvas.Children.Add(newWindow);
            }            
            
            //newWindow.PlayControlWrapper.Width = Double.NaN;
            //newWindow.PlayControlWrapper.Height = Double.NaN;
            //newWindow.SizeToContent = SizeToContent.Manual;
            //if (ScreenHelper.IsValidCoordinate(showWindowDsCommandOptions.ContentLeft) &&
            //    ScreenHelper.IsValidCoordinate(showWindowDsCommandOptions.ContentTop))
            //{
            //    var rect = ScreenHelper.GetRect(newWindow.PlayControlWrapper);
            //    var leftDelta = rect.Left - showWindowDsCommandOptions.ContentLeft!.Value;
            //    var topDelta = rect.Top - showWindowDsCommandOptions.ContentTop!.Value;
            //    newWindow.Left = newWindow.Left - leftDelta;
            //    newWindow.Top = newWindow.Top - topDelta;
            //}

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

            //PixelRect? screenWorkingArea;
            //if (showWindowDsCommandOptions.ShowOnTouchScreen == DefaultFalseTrue.True &&
            //        PlayDsProjectView.TouchScreenRect.HasValue)
            //{
            //    screenWorkingArea = PlayDsProjectView.TouchScreenRect.Value;
            //}
            //else
            //{
            //    if (newWindow.IsRootWindow && !showWindowDsCommandOptions.ContentLeft.HasValue && !showWindowDsCommandOptions.ContentTop.HasValue)
            //    {
            //        screenWorkingArea = GetFreeSystemScreenWorkingArea();
            //    }
            //    else
            //    {
            //        screenWorkingArea = ScreenHelper.GetNearestSystemScreenWorkingArea(
            //            new Point(newWindow.Left + newWindow.ActualWidth / 2, newWindow.Top + newWindow.ActualHeight / 2));
            //    }
            //}
            
            //if (screenWorkingArea.HasValue && screenWorkingArea.Value != Rect.Empty)
            //{
            //    double kX = screenWorkingArea.Value.Width / newWindow.ActualWidth;
            //    double kY = screenWorkingArea.Value.Height / newWindow.ActualHeight;
            //    if (kX < 1.0 || kY < 1.0)
            //    {
            //        double k = Math.Min(kX, kY);
            //        newWindow.Width = k * newWindow.ActualWidth;
            //        newWindow.Height = k * newWindow.ActualHeight;
            //    }

            //    ScreenHelper.SetFullyVisible(newWindow, screenWorkingArea.Value);
            //}

            if (showWindowDsCommandOptions.WindowFullScreen == DefaultFalseTrue.True)
            {
                newWindow.WindowState = WindowState.Maximized;
            }

            if (newWindow.IsRootWindow)
                PlayDsProjectView.RootPlayWindows.Add(newWindow);
        }
        
        public static PixelRect GetFreeSystemScreenWorkingArea(TopLevel topLevel)
        {
            var systemScreensWorkingAreas = ScreenHelper.GetSystemScreensWorkingAreas(topLevel);

            var freeSystemScreensWorkingAreas = new List<PixelRect>(systemScreensWorkingAreas);

            if (PlayDsProjectView.TouchScreenRect.HasValue)
                freeSystemScreensWorkingAreas.Remove(PlayDsProjectView.TouchScreenRect.Value);

            foreach (var rootPlayWindow in PlayDsProjectView.RootPlayWindows)
            {
                var newWindowPosition = rootPlayWindow.Position;
                var newWindowBounds = rootPlayWindow.Bounds;
                var point = new PixelPoint((int)(newWindowPosition.X + newWindowBounds.Width / 2), (int)(newWindowPosition.Y + newWindowBounds.Height / 2));
                foreach (PixelRect freeSystemScreensWorkingArea in freeSystemScreensWorkingAreas.ToArray())
                {
                    if (point.X >= freeSystemScreensWorkingArea.X &&
                        point.Y >= freeSystemScreensWorkingArea.Y &&
                        point.X <= freeSystemScreensWorkingArea.Right &&
                        point.Y <= freeSystemScreensWorkingArea.Bottom)
                    {
                        freeSystemScreensWorkingAreas.Remove(freeSystemScreensWorkingArea);
                        break;
                    }
                }
            }

            if (freeSystemScreensWorkingAreas.Count > 0) 
                return freeSystemScreensWorkingAreas.First();
            else 
                return systemScreensWorkingAreas.FirstOrDefault();
        }

        //private void ShowVirtualKeyboardWindow(string virtualKeyboardType)
        //{
        //    if (String.IsNullOrEmpty(virtualKeyboardType)) 
        //        return;            

        //    if (!PlayDsProjectView.TouchScreenRect.HasValue)
        //    {
        //        Avalonia.MessageBox.Show(Resources.TouchScreenIsNotConfiguredMessage);
        //        return;
        //    }

        //    var virtualKeyboardControl = AddonsHelper.NewVirtualKeyboardControl(virtualKeyboardType);
        //    if (virtualKeyboardControl == null)
        //    {
        //        Avalonia.MessageBox.Show(Resources.VirtualKeyboardNotSupportedMessage + @": " + virtualKeyboardType);
        //        return;
        //    }

        //    var virtKeyboardWindow = new VirtualKeyboardWindow(virtualKeyboardControl, virtualKeyboardType);            

        //    virtKeyboardWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        //    virtKeyboardWindow.SizeToContent = SizeToContent.Manual;
        //    virtKeyboardWindow.ShowActivated = false;
        //    virtKeyboardWindow.Show();
        //    var touchScreenRect = PlayDsProjectView.TouchScreenRect.Value;
        //    virtKeyboardWindow.Left = touchScreenRect.X;
        //    virtKeyboardWindow.Top = touchScreenRect.Y;
        //    virtKeyboardWindow.Width = touchScreenRect.Width;
        //    virtKeyboardWindow.Height = touchScreenRect.Height;            

        //    /*
        //    //MaxHeight = touchScreen.Bounds.Height;
        //    //MaxWidth = touchScreen.Bounds.Width;
        //    if (MaxWidth < keyboardView.Width)
        //    {
        //        // Scale keyboard
        //        var rat = keyboardView.Width / MaxWidth;
        //        keyboardView.Width = MaxWidth;
        //        keyboardView.Height = MaxHeight / rat;
        //    }
        //    Width = keyboardView.Width;
        //    Height = keyboardView.Height;
        //    */
        //}

        private void CommandsManager_OnGotCommand(Command command)
        {
            switch (command.CommandString)
            {
                case CommandsManager.ShowWindowCommand:
                    if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.DesktopPlayMode)
                        ShowWindow_Desktop(command.SenderFrame != null ? command.SenderFrame.PlayWindow : null, (ShowWindowDsCommandOptions?)command.CommandOptions);
                    else if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.BrowserPlayMode)
                        ShowWindow_Browser(command.SenderFrame != null ? command.SenderFrame.PlayWindow : null, (ShowWindowDsCommandOptions?)command.CommandOptions, null);
                    break;
                case CommandsManager.ShowFaceplateForTagCommand:                    
                    PlayDsProjectView.ShowFaceplateForTagUsingPlayInfoAndAllDsPagesCache(command.SenderFrame != null ? command.SenderFrame.PlayWindow : null, ((ShowFaceplateForTagDsCommandOptions)command.CommandOptions!).TagName,
                        ObsoleteAnyHelper.ConvertTo<int>(((ShowFaceplateForTagDsCommandOptions)command.CommandOptions).FaceplateIndex, false), true);                    
                    break;
                //case CommandsManager.ShowVirtualKeyboardCommand:
                //    ShowVirtualKeyboardWindow(((GenericDsCommandOptions)command.CommandOptions!).ParamsString);
                //    break;
                case CommandsManager.StartProcessCommand:
                    StartProcess(command.CommandOptions as StartProcessDsCommandOptions);
                    break;
                case CommandsManager.SendKeyCommand:
                    SendKey(command.CommandOptions as SendKeyDsCommandOptions);
                    break;
                //case CommandsManager.FindCommand:
                //    FindReplaceDialog.ShowAsPlayFind(command.SenderFrame != null ? command.SenderFrame.PlayWindow as Window : null);
                //    break;
                case CommandsManager.ShowNewRootWindowCommand:                    
                    ShowWindow_Desktop(null, DsProject.Instance.RootWindowProps);
                    break;
                //case CommandsManager.SetupTouchScreenCommand:
                //    var touchScreenFinderWindow = new TouchScreenSetupWindow();
                //    touchScreenFinderWindow.Show();
                //    break;
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