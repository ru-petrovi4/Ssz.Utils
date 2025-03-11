using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsPlay;
//using Ssz.Operator.Core.ControlsPlay.PanoramaPlay;
//using Ssz.Operator.Core.ControlsPlay.PanoramaPlay.Map3D;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsPageTypes;
//using Ssz.Operator.Core.FindReplace;
using Ssz.Operator.Core.Panorama;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.Addons
{
    public class PanoramaAddon : AddonBase
    {
        #region private fields

        private readonly DsPageTypeBase[] _dsPageTypes = {new PanoramaDsPageType()};

        #endregion

        #region construction and destruction

        public PanoramaAddon()
        {
            FrameDsPageDrawingFileName = "";
            AnimationDurationMs = 1000;
            DefaultCameraH = 1.5;
            PanoPointsCollection = new PanoPointsCollection();
            CreateMapToolkitOperationOptions = new CreateMapToolkitOperationOptions();
            TagsConstantsTypes = "";
            AllowDesignModeInPlay = true;
            ShowLines = true;
        }

        #endregion

        #region private functions

        private void CommandsManagerOnGotCommand(Command command)
        {
            switch (command.CommandString)
            {
                case CommandsManager.PanoramaJumpCommand:
                {
                    if (command.CommandOptions is null) 
                            throw new InvalidOperationException();
                    var commandOptionsClone =
                        (PanoramaJumpDsCommandOptions) ((PanoramaJumpDsCommandOptions) command.CommandOptions).Clone();
                    IPlayWindow? senderWindow = null;
                    if (command.SenderFrame is not null) 
                        senderWindow = command.SenderFrame.PlayWindow;
                    var targetWindow = PlayDsProjectView.GetPlayWindow(senderWindow,
                        commandOptionsClone.TargetWindow, commandOptionsClone.RootWindowNum);
                    var senderDsShape = ((PanoramaJumpDsCommandOptions) command.CommandOptions).ParentItem
                        .Find<DsShapeBase>();
                    if (targetWindow is not null && senderDsShape is not null)
                    {
                        var parentDrawing = senderDsShape.GetParentDrawing();
                        if (parentDrawing is not null)
                        {
                            //var panoramaPlayControl =
                            //    targetWindow.PlayControlWrapper.PlayControl as PanoramaPlayControl;
                            //if (panoramaPlayControl is not null)
                            //{
                            //    var dsShapeCenterPositionOnDrawing =
                            //        senderDsShape.GetCenterInitialPositionOnDrawing();
                            //    commandOptionsClone.JumpHorizontalK = dsShapeCenterPositionOnDrawing.X /
                            //                                                   parentDrawing.Width;
                            //    commandOptionsClone.JumpVerticalK = dsShapeCenterPositionOnDrawing.Y /
                            //                                                 parentDrawing.Height;
                            //}
                        }
                    }
                    
                    CommandsManager.NotifyCommand(command.SenderFrame, CommandsManager.JumpCommand,
                        commandOptionsClone);
                }
                    break;
                case CommandsManager.PanoramaShowMapCommand:
                    //Map3DWindow.ShowAsync();
                    break;
                case CommandsManager.PanoramaFindPathCommand:
                {
                    IPlayWindow? senderWindow = null;
                    if (command.SenderFrame is not null) senderWindow = command.SenderFrame.PlayWindow;

                    //FindReplaceDialog.ShowAsPlayPanoramaFindPath(senderWindow as Window);
                }
                    break;
            }
        }

        #endregion

        #region public functions

        public static readonly Guid AddonGuid = new(@"8F44C019-0DE9-4C47-AFEF-EDFA343A4BA8");

        public override Guid Guid => AddonGuid;

        public override string Name => @"Panorama";

        public override string Desc => Resources.PanoramaAddon_Desc;

        public override string Version => "1.0";

        public override string SszOperatorVersion => SszOperatorVersionConst;

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaAddonFrameDsPageDrawingFileName)]
        [LocalizedDescription(ResourceStrings.PanoramaAddonFrameDsPageDrawingFileNameDescription)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        //[PropertyOrder(1)]
        public string FrameDsPageDrawingFileName { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaAddonAnimationDurationMs)]
        //[PropertyOrder(2)]
        public int AnimationDurationMs { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaAddonDefaultCameraH)]
        [LocalizedDescription(ResourceStrings.PanoramaAddonDefaultCameraHDescription)]
        //[PropertyOrder(3)]
        public double DefaultCameraH { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaAddonTagsConstantsTypes)]
        [LocalizedDescription(ResourceStrings.PanoramaAddonTagsConstantsTypesDescription)]
        //[PropertyOrder(4)]
        public string TagsConstantsTypes { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaAddonAllowDesignModeInPlay)]
        [LocalizedDescription(ResourceStrings.PanoramaAddonAllowDesignModeInPlayDescription)]
        //[PropertyOrder(5)]
        public bool AllowDesignModeInPlay { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaAddonShowLines)]
        [LocalizedDescription(ResourceStrings.PanoramaAddonShowLinesDescription)]
        //[PropertyOrder(6)]
        public bool ShowLines { get; set; }


        [Browsable(false)] public PanoPointsCollection PanoPointsCollection { get; }

        [Browsable(false)] public CreateMapToolkitOperationOptions CreateMapToolkitOperationOptions { get; set; }

        public override IEnumerable<DsPageTypeBase> GetDsPageTypes()
        {
            return _dsPageTypes;
        }


        public override void InitializeInPlayMode()
        {
            base.InitializeInPlayMode();

            if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.DesktopPlayMode)
                CommandsManager.AddCommandHandler(CommandsManagerOnGotCommand);
        }


        public override void CloseInPlayMode()
        {
            base.CloseInPlayMode();

            if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.DesktopPlayMode)
                CommandsManager.RemoveCommandHandler(CommandsManagerOnGotCommand);
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(FrameDsPageDrawingFileName);
                writer.Write(AnimationDurationMs);
                writer.Write(PanoPointsCollection, context);
                writer.Write(CreateMapToolkitOperationOptions, context);
                writer.Write(DefaultCameraH);
                writer.Write(TagsConstantsTypes);
                writer.Write(AllowDesignModeInPlay);
                writer.Write(ShowLines);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            FrameDsPageDrawingFileName = reader.ReadString();
                            AnimationDurationMs = reader.ReadInt32();
                            reader.ReadOwnedData(PanoPointsCollection, context);
                            reader.ReadOwnedData(CreateMapToolkitOperationOptions, context);
                            DefaultCameraH = reader.ReadDouble();
                            TagsConstantsTypes = reader.ReadString();
                            AllowDesignModeInPlay = reader.ReadBoolean();
                            ShowLines = reader.ReadBoolean();
                        }
                        catch (BlockEndingException)
                        {
                        }

                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }

            PanoPointsCollection.Initialize();
        }

        #endregion
    }
}