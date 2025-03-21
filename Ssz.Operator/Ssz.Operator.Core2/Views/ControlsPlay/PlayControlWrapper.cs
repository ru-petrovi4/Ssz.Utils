using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Ssz.Operator.Core.Commands.DsCommandOptions;
//using Ssz.Operator.Core.ControlsPlay.BrowserPlay;
using Ssz.Operator.Core.ControlsPlay.GenericPlay;
//using Ssz.Operator.Core.ControlsPlay.PanoramaPlay;
//using Ssz.Operator.Core.ControlsPlay.WpfModel3DPlay;
//using Ssz.Operator.Core.ControlsPlay.ZoomboxPlay;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.MonitoredUndo;
//using Ssz.Utils.Wpf;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.DsShapes;
using Microsoft.Extensions.Logging;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.ControlsPlay
{
    public class PlayControlWrapper : Border, ISupportsUndo, IDisposable
    {
        #region construction and destruction

        public PlayControlWrapper(IPlayWindow playWindow)
        {
            PlayWindow = playWindow;
            PlayControl = new EmptyPlayControl(PlayWindow);
            CurrentDsPageTypeGuid = Guid.Empty;
            CurrentDsPageTypeObject = null;

            //SnapsToDevicePixels = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
            {
                UndoService.Current.Clear(GetUndoRoot());

                PlayControl.Dispose();
            }

            // Release unmanaged resources.
            // Set large fields to null.            
            Disposed = true;
        }

        ~PlayControlWrapper()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public IPlayWindow PlayWindow { get; }

        public IEnumerable<IPlayWindow> ChildPlayWindows
        {
            get { return _childPlayWindowInfosCollection.Select(i => i.ChildPlayWindow); }
        }

        public PlayControlBase PlayControl
        {
            get => _playControl; // Because of threading issues
            private set
            {
                _playControl = value;
                Child = value;
            }
        }

        public JumpInfo? CurrentJumpInfo
        {
            get => _currentJumpInfo;
            set
            {
                if (_currentJumpInfo is not null)
                    DefaultChangeFactory.Instance.OnChanging(
                        this, 
                        nameof(CurrentJumpInfo),
                        _currentJumpInfo, 
                        value);
                _currentJumpInfo = value;
                OnCurrentJumpInfoChanged();
            }
        }

        public Guid CurrentDsPageTypeGuid { get; private set; }

        public DsPageTypeBase? CurrentDsPageTypeObject { get; private set; }

        public event Action? CurrentJumpInfoChanged;

        public void Jump(JumpDsCommandOptions jumpDsCommandOptions, Frame? senderFrame = null)
        {
            IDsContainer senderContainerCopy = PlayDsProjectView.GetGenericContainerCopy(jumpDsCommandOptions.ParentItem);

            string frameName;
            if (jumpDsCommandOptions.CurrentFrame && senderFrame != null)
            {
                frameName = senderFrame.FrameName;
            }
            else
            {
                frameName = jumpDsCommandOptions.FrameName;
            }

            FrameDsShapeView? frameDsShapeView = null;
            if (!String.IsNullOrEmpty(frameName))
                frameDsShapeView = GetFrameDsShapeView(frameName);
            if (frameDsShapeView is not null)
            {
                frameDsShapeView.CurrentJumpInfo =
                    new JumpInfo(
                        jumpDsCommandOptions.FileRelativePath,
                        PlayDsProjectView.GetGenericContainerCopy(frameDsShapeView.DsShapeViewModel.DsShape.ParentItem));
            }
            else
            {
                var currentJumpInfo = new JumpInfo(jumpDsCommandOptions.FileRelativePath, senderContainerCopy);
                currentJumpInfo.JumpContext = jumpDsCommandOptions;
                CurrentJumpInfo = currentJumpInfo;
            }
        }

        public void JumpBack(string? frameName = null)
        {
            FrameDsShapeView? frameDsShapeView = null;
            if (!String.IsNullOrEmpty(frameName))
                frameDsShapeView = GetFrameDsShapeView(frameName);
            if (frameDsShapeView is not null)
                frameDsShapeView.JumpBack();
            else
                UndoService.Current[this].Undo();
        }


        public void JumpForward(string? frameName = null)
        {
            FrameDsShapeView? frameDsShapeView = null;
            if (!String.IsNullOrEmpty(frameName))
                frameDsShapeView = GetFrameDsShapeView(frameName);
            if (frameDsShapeView is not null)
                frameDsShapeView.JumpForward();
            else
                UndoService.Current[this].Redo();
        }        
        
        public FrameDsShapeView? GetFrameDsShapeView(string? frameName)
        {
            if (String.IsNullOrEmpty(frameName))
                return TreeHelper.FindChilds<FrameDsShapeView>(
                        PlayControl,
                        fsv => String.IsNullOrEmpty(fsv.FrameName) && fsv.IsVisible && fsv.IsEnabled)
                    .FirstOrDefault();
            else
                return TreeHelper.FindChilds<FrameDsShapeView>(
                        PlayControl,
                        fsv => StringHelper.CompareIgnoreCase(fsv.FrameName, frameName) && fsv.IsVisible && fsv.IsEnabled)
                    .FirstOrDefault();
        }

        public object GetUndoRoot()
        {
            return this;
        }

        public bool PrepareWindow(IPlayWindow newWindow, ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            return PlayControl.PrepareWindow(newWindow, ref showWindowDsCommandOptions);
        }

        public bool TryActivateExistingChildWindow(IDsItem senderItem, string? fileRelativePath)
        {
            fileRelativePath = fileRelativePath ?? @"";

            var existingWindowInfo = _childPlayWindowInfosCollection.FirstOrDefault(i =>
                ReferenceEquals(i.SenderItem, senderItem) &&
                StringHelper.CompareIgnoreCase(i.FileRelativePath, fileRelativePath));
            if (existingWindowInfo is not null)
            {
                existingWindowInfo.ChildPlayWindow.Activate();
                return true;
            }

            return false;
        }

        public bool PrepareChildWindow(IDsItem senderItem, IPlayWindow newChildPlayWindow,
            ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            var shouldClose = PlayControl.PrepareChildWindow(newChildPlayWindow, ref showWindowDsCommandOptions);
            if (shouldClose) return true;

            // Close existing windows

            var childPlayWindowInfo = new ChildPlayWindowInfo(newChildPlayWindow, senderItem,
                showWindowDsCommandOptions.FileRelativePath ?? @"");            
            _childPlayWindowInfosCollection.Add(childPlayWindowInfo);
            newChildPlayWindow.Closed +=
                (o, args) => { _childPlayWindowInfosCollection.Remove(childPlayWindowInfo); };

            if (childPlayWindowInfo.ChildPlayWindowClassOptions.WindowsMaxCount > 0)
            {
                var collection = _childPlayWindowInfosCollection.Where(i =>
                        childPlayWindowInfo.ChildPlayWindowClassOptions.PlayWindowClassInfo.IsForPlayWindow(i.ChildPlayWindow))
                    .ToArray();
                if (collection.Length >= childPlayWindowInfo.ChildPlayWindowClassOptions.WindowsMaxCount)
                {
                    var pw = collection[0].ChildPlayWindow;
                    var values = pw.WindowVariables.TryGetValue(@"IsPinned");
                    if (values == null || !new Any(values[0]).ValueAsBoolean(false))
                    {
                        pw.Close();
                    }
                }
            }

            // Determine position by slot
            if (!string.IsNullOrEmpty(childPlayWindowInfo.FileRelativePath))
            {
                string slotKey;
                var extension = Path.GetExtension(childPlayWindowInfo.FileRelativePath);
                if (!string.IsNullOrEmpty(extension) && !StringHelper.CompareIgnoreCase(extension, DsProject.DsPageFileExtension))
                    slotKey = extension;
                else
                    slotKey = childPlayWindowInfo.FileRelativePath;
                List<WindowSlot>? windowSlotsForKey;
                if (!_windowSlots.TryGetValue(slotKey, out windowSlotsForKey))
                {
                    windowSlotsForKey = new List<WindowSlot>();
                    _windowSlots[slotKey] = windowSlotsForKey;
                }

                var freeWindowSlot = windowSlotsForKey.FirstOrDefault(slot => slot.PlayWindow is null);
                if (freeWindowSlot is null)
                {
                    freeWindowSlot = new WindowSlot
                    {
                        Num = windowSlotsForKey.Count,
                        WindowPosition = null,
                        ContentWidth = double.NaN,
                        ContentHeight = double.NaN,
                        WindowFullScreen = DefaultFalseTrue.Default
                    };
                    windowSlotsForKey.Add(freeWindowSlot);
                }

                freeWindowSlot.PlayWindow = newChildPlayWindow;
                newChildPlayWindow.PlayControlWrapper._slotNum = freeWindowSlot.Num;
                
                if (!showWindowDsCommandOptions.WindowPosition.HasValue)
                    showWindowDsCommandOptions.WindowPosition = freeWindowSlot.WindowPosition;
                if (!showWindowDsCommandOptions.ContentWidth.HasValue && !double.IsNaN(freeWindowSlot.ContentWidth))
                    showWindowDsCommandOptions.ContentWidth = freeWindowSlot.ContentWidth;
                if (!showWindowDsCommandOptions.ContentHeight.HasValue && !double.IsNaN(freeWindowSlot.ContentHeight))
                    showWindowDsCommandOptions.ContentHeight = freeWindowSlot.ContentHeight;
                if (freeWindowSlot.WindowFullScreen != DefaultFalseTrue.Default)
                    showWindowDsCommandOptions.WindowFullScreen = freeWindowSlot.WindowFullScreen;

                newChildPlayWindow.Closed +=
                    (o, args) =>
                    {
                        WindowSlot windowSlot = windowSlotsForKey[newChildPlayWindow.PlayControlWrapper._slotNum];
                        windowSlot.PlayWindow = null;
                        if (newChildPlayWindow.WindowState == WindowState.Maximized)
                            windowSlot.WindowFullScreen = DefaultFalseTrue.True;
                        else
                            windowSlot.WindowFullScreen = DefaultFalseTrue.False;                        
                        windowSlot.WindowPosition = newChildPlayWindow.Position;   
                        windowSlot.ContentWidth = newChildPlayWindow.PlayControlWrapper.Bounds.Width;
                        windowSlot.ContentHeight = newChildPlayWindow.PlayControlWrapper.Bounds.Height;
                    };
            }

            // Determine position by WindowStartupLocation property
            if (ScreenHelper.IsValidLength(showWindowDsCommandOptions.ContentWidth) &&
                ScreenHelper.IsValidLength(showWindowDsCommandOptions.ContentHeight))
            {
                var contentWidth = (int)showWindowDsCommandOptions.ContentWidth!.Value;
                var contentHeight = (int)showWindowDsCommandOptions.ContentHeight!.Value;

                if (showWindowDsCommandOptions.WindowStartupLocation == PlayWindowStartupLocation.ControlLeft ||
                    showWindowDsCommandOptions.WindowStartupLocation == PlayWindowStartupLocation.ControlTop ||
                    showWindowDsCommandOptions.WindowStartupLocation == PlayWindowStartupLocation.ControlRight ||
                    showWindowDsCommandOptions.WindowStartupLocation == PlayWindowStartupLocation.ControlBottom)
                {
                    var controlPixelRect = PlayControl.GetControlPixelRect(senderItem.Find<DsShapeBase>());                    
                    if (controlPixelRect is not null)
                    {
                        switch (showWindowDsCommandOptions.WindowStartupLocation)
                        {
                            case PlayWindowStartupLocation.ControlLeft:
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(controlPixelRect.Value.X - contentWidth,
                                        controlPixelRect.Value.Y);
                                }
                                break;
                            case PlayWindowStartupLocation.ControlTop:
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(controlPixelRect.Value.X,
                                        controlPixelRect.Value.Y - contentHeight);
                                }
                                break;
                            case PlayWindowStartupLocation.ControlRight:
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(controlPixelRect.Value.Right,
                                        controlPixelRect.Value.Y);
                                }
                                break;
                            case PlayWindowStartupLocation.ControlBottom:
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(controlPixelRect.Value.X,
                                        controlPixelRect.Value.Bottom);
                                }
                                break;
                        }
                    }                        
                }
                else
                {
                    var childWindowsArea = PlayControl.GetChildWindowsPixelRect(showWindowDsCommandOptions.FrameName);
                    if (childWindowsArea is not null)
                    {
                        switch (showWindowDsCommandOptions.WindowStartupLocation)
                        {
                            case PlayWindowStartupLocation.Default:
                                break;
                            case PlayWindowStartupLocation.Center:
                                //if (!showWindowDsCommandOptions.ContentLeft.HasValue &&
                                //    !showWindowDsCommandOptions.ContentTop.HasValue)
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X +
                                        childWindowsArea.Value.Width / 2 - contentWidth / 2,
                                        childWindowsArea.Value.Y +
                                        childWindowsArea.Value.Height / 2 - contentHeight / 2);
                                }

                                break;
                            case PlayWindowStartupLocation.LeftCenter:
                                //if (!showWindowDsCommandOptions.ContentLeft.HasValue &&
                                //    !showWindowDsCommandOptions.ContentTop.HasValue)
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X,
                                        childWindowsArea.Value.Y +
                                        childWindowsArea.Value.Height / 2 - contentHeight / 2);
                                }

                                break;
                            case PlayWindowStartupLocation.UpperLeft:
                                //if (!showWindowDsCommandOptions.ContentLeft.HasValue &&
                                //    !showWindowDsCommandOptions.ContentTop.HasValue)
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X,
                                        childWindowsArea.Value.Y);
                                }

                                break;
                            case PlayWindowStartupLocation.UpperCenter:
                                //if (!showWindowDsCommandOptions.ContentLeft.HasValue &&
                                //    !showWindowDsCommandOptions.ContentTop.HasValue)
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X +
                                        childWindowsArea.Value.Width / 2 - contentWidth / 2,
                                        childWindowsArea.Value.Y);
                                }

                                break;
                            case PlayWindowStartupLocation.UpperRight:
                                //if (!showWindowDsCommandOptions.ContentLeft.HasValue &&
                                //    !showWindowDsCommandOptions.ContentTop.HasValue)
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.Right - contentWidth,
                                        childWindowsArea.Value.Y);
                                }

                                break;
                            case PlayWindowStartupLocation.RightCenter:
                                //if (!showWindowDsCommandOptions.ContentLeft.HasValue &&
                                //    !showWindowDsCommandOptions.ContentTop.HasValue)
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.Right - contentWidth,
                                        childWindowsArea.Value.Y +
                                        childWindowsArea.Value.Height / 2 - contentHeight / 2);
                                }

                                break;
                            case PlayWindowStartupLocation.BottomRight:
                                //if (!showWindowDsCommandOptions.ContentLeft.HasValue &&
                                //    !showWindowDsCommandOptions.ContentTop.HasValue)
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.Right - contentWidth,
                                        childWindowsArea.Value.Bottom - contentHeight);
                                }

                                break;
                            case PlayWindowStartupLocation.BottomCenter:
                                //if (!showWindowDsCommandOptions.ContentLeft.HasValue &&
                                //    !showWindowDsCommandOptions.ContentTop.HasValue)
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X +
                                        childWindowsArea.Value.Width / 2 - contentWidth / 2,
                                        childWindowsArea.Value.Bottom - contentHeight);
                                }

                                break;
                            case PlayWindowStartupLocation.BottomLeft:
                                //if (!showWindowDsCommandOptions.ContentLeft.HasValue &&
                                //    !showWindowDsCommandOptions.ContentTop.HasValue)
                                {
                                    showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X,
                                        childWindowsArea.Value.Bottom - contentHeight);
                                }

                                break;
                            case PlayWindowStartupLocation.Fill:
                                showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X,
                                    childWindowsArea.Value.Y);
                                showWindowDsCommandOptions.ContentWidth = childWindowsArea.Value.Width;
                                showWindowDsCommandOptions.ContentHeight = childWindowsArea.Value.Height;
                                break;
                            case PlayWindowStartupLocation.LeftDock:
                                {
                                    var kX = childWindowsArea.Value.Width / contentWidth;
                                    var kY = childWindowsArea.Value.Height / contentHeight;
                                    if (kX > kY)
                                    {
                                        showWindowDsCommandOptions.ContentWidth = contentWidth * kY;
                                        showWindowDsCommandOptions.ContentHeight = childWindowsArea.Value.Height;
                                        showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X,
                                            childWindowsArea.Value.Y);
                                    }
                                    else
                                    {
                                        showWindowDsCommandOptions.ContentWidth = childWindowsArea.Value.Width;
                                        showWindowDsCommandOptions.ContentHeight = contentHeight * kX;
                                        showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X,
                                            childWindowsArea.Value.Y +
                                            childWindowsArea.Value.Height / 2 - contentHeight / 2);
                                    }
                                }
                                break;
                            case PlayWindowStartupLocation.UpperDock:
                                {
                                    var kX = childWindowsArea.Value.Width / contentWidth;
                                    var kY = childWindowsArea.Value.Height / contentHeight;
                                    if (kY > kX)
                                    {
                                        showWindowDsCommandOptions.ContentWidth = childWindowsArea.Value.Width;
                                        showWindowDsCommandOptions.ContentHeight = contentHeight * kX;
                                        showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X,
                                            childWindowsArea.Value.Y);
                                    }
                                    else
                                    {
                                        showWindowDsCommandOptions.ContentWidth = contentWidth * kY;
                                        showWindowDsCommandOptions.ContentHeight = childWindowsArea.Value.Height;
                                        showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X +
                                            childWindowsArea.Value.Width / 2 - contentWidth / 2,
                                            childWindowsArea.Value.Y);
                                    }
                                }
                                break;
                            case PlayWindowStartupLocation.RightDock:
                                {
                                    var kX = childWindowsArea.Value.Width / contentWidth;
                                    var kY = childWindowsArea.Value.Height / contentHeight;
                                    if (kX > kY)
                                    {
                                        showWindowDsCommandOptions.ContentWidth = contentWidth * kY;
                                        showWindowDsCommandOptions.ContentHeight = childWindowsArea.Value.Height;
                                        showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.Right - contentWidth,
                                            childWindowsArea.Value.Y);
                                    }
                                    else
                                    {
                                        showWindowDsCommandOptions.ContentWidth = childWindowsArea.Value.Width;
                                        showWindowDsCommandOptions.ContentHeight = contentHeight * kX;
                                        showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X,
                                            childWindowsArea.Value.Y +
                                            childWindowsArea.Value.Height / 2 - contentHeight / 2);
                                    }
                                }
                                break;
                            case PlayWindowStartupLocation.BottomDock:
                                {
                                    var kX = childWindowsArea.Value.Width / contentWidth;
                                    var kY = childWindowsArea.Value.Height / contentHeight;
                                    if (kY > kX)
                                    {
                                        showWindowDsCommandOptions.ContentWidth = childWindowsArea.Value.Width;
                                        showWindowDsCommandOptions.ContentHeight = contentHeight * kX;
                                        showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X,
                                            childWindowsArea.Value.Bottom - contentHeight);
                                    }
                                    else
                                    {
                                        showWindowDsCommandOptions.ContentWidth = contentWidth * kY;
                                        showWindowDsCommandOptions.ContentHeight = childWindowsArea.Value.Height;
                                        showWindowDsCommandOptions.WindowPosition = new PixelPoint(childWindowsArea.Value.X +
                                            childWindowsArea.Value.Width / 2 - contentWidth / 2,
                                            childWindowsArea.Value.Y);
                                    }
                                }
                                break;
                        }
                    }
                }                
            }

            return false;
        }

        public void CloseChildWindows(CloseAllFaceplatesDsCommandOptions closeAllFaceplatesDsCommandOptions)
        {
            foreach (var childPlayWindow in ChildPlayWindows.ToArray())
            {
                if (closeAllFaceplatesDsCommandOptions.PlayWindowClassInfo.IsForPlayWindow(childPlayWindow))                    
                    childPlayWindow.Close();
                else
                    childPlayWindow.PlayControlWrapper.CloseChildWindows(closeAllFaceplatesDsCommandOptions);
            }
        }

        #endregion

        #region private functions

        private PlayControlBase? NewPlayControl(Guid typeGuid, IPlayWindow window)
        {
            if (typeGuid == GenericGraphicDsPageType.TypeGuid) 
                return new GenericGraphicPlayControl(window);
            if (typeGuid == GenericFaceplateDsPageType.TypeGuid)
                return new GenericFaceplatePlayControl(window);
            //if (typeGuid == PanoramaDsPageType.TypeGuid) return new PanoramaPlayControl(window);
            //if (typeGuid == ZoomboxDsPageType.TypeGuid) return new ZoomboxPlayControl(window);
            if (typeGuid == ToolTipDsPageType.TypeGuid) 
                return new GenericFaceplatePlayControl(window);

            return AddonsManager.NewPlayControl(typeGuid, window) ?? new GenericGraphicPlayControl(window); // TEMPCODE 
        }

        private async void OnCurrentJumpInfoChanged()
        {
            var currentJumpInfoChanged = CurrentJumpInfoChanged;
            if (currentJumpInfoChanged is not null) 
                currentJumpInfoChanged();

            if (_currentJumpInfo is null || string.IsNullOrEmpty(_currentJumpInfo.FileRelativePath)) 
                return;

            var handled = false;

            var currentFileExtensionUpper = Path.GetExtension(_currentJumpInfo.FileRelativePath).ToUpperInvariant();
            if (currentFileExtensionUpper == DsProject.DsPageFileExtensionUpper)
            {
                string fileFullName = Path.Combine(DsProject.Instance.DsPagesDirectoryFullName, _currentJumpInfo.FileRelativePath);
                
                var dsPageDrawingInfo = await DsProject.ReadDrawingInfoAsync(fileFullName, false) as DsPageDrawingInfo;                

                if (dsPageDrawingInfo is null)
                {
                    MessageBoxHelper.ShowError(Properties.Resources.ReadDrawingErrorMessage + @" " +
                                                _currentJumpInfo.FileRelativePath + @". " +
                                                Properties.Resources.SeeErrorLogForDetails);
                    return;
                }

                dsPageDrawingInfo.ParentItem = _currentJumpInfo.SenderContainerCopy;
                /*
                 * 
                 * 
                if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.DesktopPlayMode)
                {
                    DsProject.Instance.CheckDrawingsBinSerializationVersion(new[] {dsPageDrawing.GetDrawingInfo()},
                        new DummyProgressInfo());
                }*/

                if (dsPageDrawingInfo.DsPageTypeInfo.Guid != CurrentDsPageTypeGuid)
                {
                    PlayControlBase? newPlayControl;
                    try
                    {
                        newPlayControl = NewPlayControl(dsPageDrawingInfo.DsPageTypeInfo.Guid, PlayWindow);
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, @"Cannot create play control for dsPage " + dsPageDrawingInfo.Name);
                        MessageBoxHelper.ShowError(Properties.Resources.CannotShowDsPage + ". " +
                                                    Properties.Resources.SeeErrorLogForDetails);
                        return;
                    }

                    if (newPlayControl is null)
                    {
                        MessageBoxHelper.ShowError(Properties.Resources.FileOpenErrorMissingExtension + " " +
                                                    fileFullName);
                        return;
                    }

                    if (PlayControl.GetType() != newPlayControl.GetType())
                    {
                        PlayControl.Dispose();
                        PlayControl = newPlayControl;
                    }
                    CurrentDsPageTypeGuid = dsPageDrawingInfo.DsPageTypeInfo.Guid;
                    CurrentDsPageTypeObject = dsPageDrawingInfo.DsPageTypeObject;
                }

                PlayControl.Jump(_currentJumpInfo, dsPageDrawingInfo);

                //await Task.Run(PlayControl.DsPageDrawingIsLoadedEventWaitHandle.WaitOne); Use Thread

                //                if (!PlayControl.PlayWindow.IsRootWindow)
                //                {
                //                    Width = PlayControl.DsPageDrawing!.Width;
                //                    Height = PlayControl.DsPageDrawing!.Height;
                //#if TEST
                //#else
                //                    if (!OperatingSystem.IsBrowser())
                //                    {
                //                        ((Window)PlayControl.PlayWindow).SizeToContent = SizeToContent.WidthAndHeight;

                //                        await Task.Delay(0);

                //                        Width = Double.NaN;
                //                        Height = Double.NaN;
                //                        ((Window)PlayControl.PlayWindow).SizeToContent = SizeToContent.Manual;
                //                    }
                //#endif
                //                }

                handled = true;                
            }

            if (!handled)
            {
                handled = PlayControl.Jump(_currentJumpInfo);
                if (!handled)
                {
                    switch (currentFileExtensionUpper)
                    {
                        case ".HTM":
                        case ".HTML":
                            // Select preferred browser control 2 use/create  (internal WebBrowser or IE ActiveX control - for HmiWeb)
                            // We need to check additional options in future if both browser controls can be used in one dsProject !!!
                            //if (DsProject.Instance.ShDocVw)
                            //{
                            //    PlayControl.Dispose();
                            //    PlayControl = new ActiveXBrowserPlayControl(PlayWindow);
                            //    CurrentDsPageTypeGuid = Guid.Empty;
                            //? CurrentDsPageTypeObject = null;
                            //}
                            //else
                        {
                            PlayControl.Dispose();
                            //PlayControl = new BrowserPlayControl(PlayWindow);
                            CurrentDsPageTypeGuid = Guid.Empty;
                            CurrentDsPageTypeObject = null;
                        }
                            handled = PlayControl.Jump(_currentJumpInfo);
                            break;
                        case ".OBJ":
                            PlayControl.Dispose();
                            //PlayControl = new WpfModel3DPlayControl(PlayWindow);
                            CurrentDsPageTypeGuid = Guid.Empty;
                            CurrentDsPageTypeObject = null;

                            handled = PlayControl.Jump(_currentJumpInfo);
                            break;
                    }

                    if (!handled)
                        MessageBoxHelper.ShowError(Properties.Resources.FileNotFoundMessage + @" " +
                                                   _currentJumpInfo.FileRelativePath);
                }
            }

            if (handled)
                foreach (ChildPlayWindowInfo playWindowInfo in _childPlayWindowInfosCollection.ToArray())
                {
                    if (playWindowInfo.ChildPlayWindowClassOptions.CloseWindowsOnParentJump)
                    {
                        var values = playWindowInfo.ChildPlayWindow.WindowVariables.TryGetValue(@"IsPinned");
                        if (values == null || !new Any(values[0]).ValueAsBoolean(false))
                        {
                            playWindowInfo.ChildPlayWindow.Close();
                        }                        
                    }
                }
        }

#endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        #region private fields

        private PlayControlBase _playControl = null!;

        private JumpInfo? _currentJumpInfo;

        private readonly List<ChildPlayWindowInfo> _childPlayWindowInfosCollection =
            new();

        private readonly CaseInsensitiveDictionary<List<WindowSlot>> _windowSlots =
            new();

        private int _slotNum;

        #endregion

        private class WindowSlot
        {
            #region public functions

            public int Num { get; set; }
            public IPlayWindow? PlayWindow { get; set; }
            public PixelPoint? WindowPosition { get; set; }
            public double ContentWidth { get; set; }
            public double ContentHeight { get; set; }
            public DefaultFalseTrue WindowFullScreen { get; set; }

            #endregion
        }

        private class ChildPlayWindowInfo
        {
            #region public functions

            public ChildPlayWindowInfo(IPlayWindow childPlayWindow, IDsItem senderItem, string fileRelativePath)
            {
                ChildPlayWindow = childPlayWindow;
                SenderItem = senderItem;
                FileRelativePath = fileRelativePath;

                ChildPlayWindowClassOptions = DsProject.Instance.GetPlayWindowClassOptions(childPlayWindow);
            }

            public IPlayWindow ChildPlayWindow { get; }

            public IDsItem SenderItem { get; }

            public string FileRelativePath { get; }

            public PlayWindowClassOptions ChildPlayWindowClassOptions { get; }

            #endregion
        }
    }

    public class JumpInfo
    {
        public JumpInfo(string fileRelativePath, IDsContainer senderContainerCopy)
        {
            FileRelativePath = fileRelativePath;
            SenderContainerCopy = senderContainerCopy;
        }

        public string FileRelativePath { get; }

        public IDsContainer SenderContainerCopy { get; }

        public object? JumpContext { get; set; }
    }
}