using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core
{
    public static class PlayDsProjectView
    {
        #region public functions

        public static readonly List<IPlayWindow> RootPlayWindows =
            new();

        public static IPlayWindow? LastActiveRootPlayWindow => RootPlayWindows.LastOrDefault();

        public static Rect? TouchScreenRect { get; set; }

        public static TouchScreenMode TouchScreenMode { get; set; }

        public static IVirtualKeyboardWindow? VirtualKeyboardWindow { get; set; }

        public static DsEventSourceModel EventSourceModel { get; } = new();

        public static Buzzer Buzzer { get; } = new();

        public static event Action? BeforeStateLoad;

        public static event EventHandler<BeforeWriteValueEventArgs>? BeforeWriteValue;        

        public static void OnBeforeStateLoad()
        {
            EventSourceModel.Clear();
            BeforeStateLoad?.Invoke();
        }

        public static void OnBeforeWriteValue(object sender, BeforeWriteValueEventArgs args)
        {
            BeforeWriteValue?.Invoke(sender, args);
        }

        public static void Initialize()
        {
            var fi = AddonsHelper.GetAssemblyFileInfo(Buzzer.GetType().Assembly);
            if (fi != null && fi.DirectoryName != null)
            {
                string soundFile = Path.Combine(fi.DirectoryName, "Resources", "Buzzer.wav");
                fi = new FileInfo(soundFile);
                if (fi.Exists)
                {
                    PlayDsProjectView.Buzzer.ClearCustomSoundConfiguration();
                    PlayDsProjectView.Buzzer.SetCustomSoundConfiguration(BuzzerStateEnum.ProcessAlarmMediumPriority, soundFile);
                    PlayDsProjectView.Buzzer.SetCustomSoundConfiguration(BuzzerStateEnum.ProcessAlarmHighPriority, soundFile);
                }
            }
        }

        public static void Shutdown()
        {
            foreach (var playWindow in RootPlayWindows.ToArray()) playWindow.Close();
        }

        public static string? VirtualKeyboardType
        {
            get
            {
                if (VirtualKeyboardWindow is null) return null;
                return VirtualKeyboardWindow.VirtualKeyboardType;
            }
        }

        public static DsPageDrawing? GetDsPageDrawing(IPlayWindow? playWindow, string? dataSourceIdString)
        {
            string[] parts = (dataSourceIdString ?? @"").Split(new[] {','}, StringSplitOptions.None);

            var targetPlayWindow = GetPlayWindow(playWindow, TargetWindow.RootWindow, parts[0]);
            if (targetPlayWindow is null) return null;

            string? frameName = null;
            if (parts.Length > 1) frameName = parts[1];

            FrameDsShapeView? frameDsShapeView;
            if (string.IsNullOrEmpty(frameName))
                frameDsShapeView = TreeHelper.FindChild<FrameDsShapeView>(
                    targetPlayWindow.PlayControlWrapper.PlayControl,
                    fsv => string.IsNullOrEmpty(fsv.FrameName));
            else
                frameDsShapeView = TreeHelper.FindChild<FrameDsShapeView>(
                    targetPlayWindow.PlayControlWrapper.PlayControl,
                    fsv => StringHelper.CompareIgnoreCase(fsv.FrameName, frameName));
            if (frameDsShapeView is null)
                return targetPlayWindow.PlayControlWrapper.PlayControl.DsPageDrawing;
            return frameDsShapeView.DsPageDrawing;
        }

        public static IPlayWindow? GetPlayWindow(IPlayWindow? senderWindow, TargetWindow target,
            string? rootWindowNum = null)
        {
            IPlayWindow? targetWindow = null;

            if (!string.IsNullOrWhiteSpace(rootWindowNum))
            {
                var n = ObsoleteAnyHelper.ConvertTo<int>(rootWindowNum, false);
                if (n > 0)
                    targetWindow = RootPlayWindows.FirstOrDefault(w => w.RootWindowNum == n);
            }

            if (targetWindow is null)
            {
                if (senderWindow is null)
                    senderWindow = LastActiveRootPlayWindow;

                switch (target)
                {
                    case TargetWindow.CurrentWindow:
                        targetWindow = senderWindow;
                        break;
                    case TargetWindow.ParentWindow:
                        if (senderWindow is not null) targetWindow = senderWindow.ParentWindow;
                        break;
                    case TargetWindow.RootWindow:
                        if (senderWindow is not null)
                        {
                            targetWindow = senderWindow;
                            for (; ; )
                            {
                                if (targetWindow.ParentWindow is null) break;
                                targetWindow = targetWindow.ParentWindow;
                            }
                        }

                        break;
                }
            }            

            return targetWindow;
        }

        public static IDsContainer GetGenericContainerCopy(IDsItem? item)
        {
            if (item is null || ReferenceEquals(item, DsProject.Instance)) return DsProject.Instance;

            var genericContainer = new GenericContainer
            {
                ParentItem = DsProject.Instance
            };

            FillGenericContainer(genericContainer, item);

            return genericContainer;
        }

        public static IPlayWindow? GetPlayWindow(DependencyObject? dependencyObject)
        {
            if (dependencyObject is null) return null;
            var playWindow = dependencyObject as IPlayWindow;
            if (playWindow is not null) return playWindow;
            if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.WindowsPlayMode)
            {
                Window window = Window.GetWindow(dependencyObject);
                while (window is not null)
                {
                    playWindow = window as IPlayWindow;
                    if (playWindow is not null) return playWindow;
                    window = window.Owner;
                }
            }

            return null;
        }

        public static string ShowFaceplateForTagUsingPlayInfoAndAllDsPagesCache(IPlayWindow? senderWindow, string tagName,
            int faceplateIndex, bool showErrorMessageBox)
        {
            if (string.IsNullOrEmpty(tagName))
                return @"";

            string fileRelativePath;

            string tagType = DsProject.Instance.DataEngine.GetTagInfo(tagName).TagType;
            if (!string.IsNullOrEmpty(tagType))
            {
                fileRelativePath = ShowFaceplateForTagUsingPlayInfo(senderWindow, tagName, faceplateIndex, tagType);
                if (!string.IsNullOrEmpty(fileRelativePath)) return fileRelativePath;
            }

            CaseInsensitiveDictionary<List<ExtendedDsConstant>> allConstantsValues =
                DsProject.Instance.AllDsPagesCacheGetAllConstantsValues();

            var dsConstants = allConstantsValues.TryGetValue(tagName);
            if (dsConstants is not null)
                foreach (ExtendedDsConstant dsConstant in dsConstants)
                {
                    if ((dsConstant.ComplexDsShape.GetParentDrawing() as DsPageDrawing ??
                         throw new InvalidOperationException()).ExcludeFromTagSearch)
                        continue;

                    tagType = dsConstant.DsConstant.Type;
                    if (!string.IsNullOrEmpty(tagType))
                    {
                        fileRelativePath = ShowFaceplateForTagUsingPlayInfo(senderWindow, tagName, faceplateIndex, tagType);
                        if (!string.IsNullOrEmpty(fileRelativePath)) return fileRelativePath;
                    }
                }

            fileRelativePath = ShowFaceplateForTagUsingPlayInfo(senderWindow, tagName, faceplateIndex, @"");
            if (!string.IsNullOrEmpty(fileRelativePath)) return fileRelativePath;

            if (showErrorMessageBox && senderWindow is not null)
                MessageBoxHelper.ShowError(Resources.CannotFindFaceplateForTag + @": " +
                                           tagName +
                                           "\n" + Resources.SetTagsInfoMessage + @" CsvDb\" +
                                           DsProject.Instance.DataEngine.TagsFileName);

            return @"";
        }

        public static string ShowFaceplateForTagUsingPlayInfo(IPlayWindow? senderWindow, string tag,
            int faceplateIndex, string tagType)
        {
            var tagTypeInfo = DsProject.Instance.DataEngine.GetTagTypeInfo(tagType);
            if (tagTypeInfo is null ||
                string.IsNullOrEmpty(tagTypeInfo.DsPageFileRelativePaths) ||
                string.IsNullOrEmpty(tagTypeInfo.Constant)) return @"";

            string[] fileRelativePaths = (tagTypeInfo.DsPageFileRelativePaths ?? @"").Split(',').ToArray();

            if (faceplateIndex < 0 || faceplateIndex >= fileRelativePaths.Length)
                return @"";

            string fileRelativePath = fileRelativePaths[faceplateIndex];
            if (string.IsNullOrEmpty(fileRelativePath)) return @"";

            if (senderWindow is not null)
            {
                Tuple<IPlayWindow, string> key = Tuple.Create(senderWindow, tag);
                GenericContainer? senderContainer;
                if (!GenericContainers.TryGetValue(key, out senderContainer))
                {
                    senderContainer = new GenericContainer();
                    senderContainer.ParentItem = DsProject.Instance;
                    senderContainer.DsConstantsCollection.Add(new DsConstant
                    {
                        Name = tagTypeInfo.Constant!,
                        Value = tag,
                        Type = tagTypeInfo.TagType ?? ""
                    });
                    GenericContainers[key] = senderContainer;
                }

                CommandsManager.NotifyCommand(senderWindow.MainFrame, CommandsManager.ShowWindowCommand,
                    new ShowWindowDsCommandOptions
                    {
                        ParentItem = senderContainer,
                        FileRelativePath =
                            fileRelativePath
                    });
            }

            return fileRelativePath;
        }

        public static string ShowWindowForTag(IPlayWindow senderWindow, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return @"";

            CaseInsensitiveDictionary<List<ExtendedDsConstant>> allConstantsValues =
                DsProject.Instance.AllDsPagesCacheGetAllConstantsValues();

            var dsConstants = allConstantsValues.TryGetValue(tag);
            if (dsConstants is null) return @"";

            foreach (ExtendedDsConstant dsConstant in dsConstants)
            {
                if (((DsPageDrawing) (dsConstant.ComplexDsShape.GetParentDrawing() ??
                                         throw new InvalidOperationException())).ExcludeFromTagSearch) continue;

                foreach (EmptyDsShape emptyDsShape in dsConstant.ComplexDsShape.FindDsShapes<EmptyDsShape>())
                    if (emptyDsShape.DsCommands is not null)
                    {
                        var dsCommand =
                            emptyDsShape.DsCommands.FirstOrDefault(ci =>
                                ci.Command == CommandsManager.ShowWindowCommand);
                        if (dsCommand is not null)
                        {
                            var dsCommandOptions = (ShowWindowDsCommandOptions?) dsCommand.DsCommandOptions?.Clone();
                            if (dsCommandOptions is null) return @"";
                            dsCommandOptions.ReplaceConstants(emptyDsShape.Container);
                            if (senderWindow is not null)
                            {
                                dsCommandOptions.ParentItem = emptyDsShape;
                                CommandsManager.NotifyCommand(senderWindow.MainFrame,
                                    CommandsManager.ShowWindowCommand, dsCommandOptions);
                            }

                            return dsCommandOptions.FileRelativePath;
                        }
                    }
            }

            return @"";
        }

        public static void SaveCurrentPlayWindowsConfiguration(bool onlyIfIsNotSavedYet)
        {
            if (onlyIfIsNotSavedYet)
            {
                Rect? touchScreen;
                TouchScreenMode touchScreenMode;
                List<WindowProps> existingRegistryRootWindowPropsCollection;
                string virtualKeyboardType;
                AppRegistryOptions.GetScreensInfo(out touchScreen, out touchScreenMode,
                    out existingRegistryRootWindowPropsCollection,
                    out virtualKeyboardType);
                if (existingRegistryRootWindowPropsCollection.Count > 0) return;
            }

            var registryRootWindowPropsCollection = new List<WindowProps>();

            foreach (IPlayWindow rootWindow in RootPlayWindows.OrderBy(w => w.RootWindowNum))
            {
                var rootWindowProps = new WindowProps
                {
                    ContentLeft = rootWindow.Left,
                    ContentTop = rootWindow.Top,
                    ContentWidth = rootWindow.PlayControlWrapper.ActualWidth,
                    ContentHeight = rootWindow.PlayControlWrapper.ActualHeight
                };
                if (rootWindow.WindowState == WindowState.Maximized)
                    rootWindowProps.WindowFullScreen = DefaultFalseTrue.True;
                else
                    rootWindowProps.WindowFullScreen = DefaultFalseTrue.False;
                registryRootWindowPropsCollection.Add(rootWindowProps);
            }

            // Save virtual keyboard type only if keyboard is opened now
            AppRegistryOptions.SaveScreensInfo(registryRootWindowPropsCollection,
                VirtualKeyboardWindow is not null ? VirtualKeyboardWindow.VirtualKeyboardType : string.Empty);
        }

        public static void SaveTouchScreenConfiguration()
        {
            AppRegistryOptions.SaveScreensInfo(TouchScreenRect, TouchScreenMode);
        }

        #endregion

        #region private functions

        private static void FillGenericContainer(GenericContainer genericContainer, IDsItem? item)
        {
            if (item is null || ReferenceEquals(item, DsProject.Instance)) return;

            FillGenericContainer(genericContainer, item.ParentItem);

            var container = item as IDsContainer;
            if (container is null) return;

            foreach (var gpi in container.DsConstantsCollection)
            {
                if (gpi.Value == @"") continue;
                var dsConstant =
                    genericContainer.DsConstantsCollection.FirstOrDefault(g =>
                        StringHelper.CompareIgnoreCase(g.Name, gpi.Name));
                if (dsConstant is not null)
                {
                    dsConstant.Value = gpi.Value;
                    dsConstant.DefaultValue = gpi.DefaultValue;
                    dsConstant.Type = gpi.Type;
                    dsConstant.Desc = gpi.Desc;
                }
                else
                {
                    genericContainer.DsConstantsCollection.Add(new DsConstant(gpi));
                }
            }
        }

        #endregion

        #region private fields

        private static readonly Dictionary<Tuple<IPlayWindow, string>, GenericContainer> GenericContainers
            = new();

        #endregion
    }

    public enum TouchScreenMode
    {
        MouseClick = 0,
        SingleTouch,
        MultiTouch
    }

    public class BeforeWriteValueEventArgs : EventArgs
    {
        public string ElementId { get; set; } = @"";

        public object? NewValue { get; set; }

        public bool Cancel { get; set; }
    }
}