using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Markup;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Utils.Wpf;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Utils;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    [TypeConverter(typeof(NameValueCollectionTypeConverter<ShowWindowDsCommandOptions>))]
    //[ValueSerializer(typeof(NameValueCollectionValueSerializer<ShowWindowDsCommandOptions>))]
    public class ShowWindowDsCommandOptions : OwnedDataSerializableAndCloneable, IChildWindowInfoDsCommandOptions,
        IDsItem
    {
        #region construction and destruction

        public ShowWindowDsCommandOptions()
        {
            ShowOnTouchScreen = DefaultFalseTrue.Default;
            WindowStyle = PlayWindowStyle.Default;
            WindowResizeMode = PlayWindowResizeMode.Default;
            WindowStartupLocation = PlayWindowStartupLocation.Default;
            WindowFullScreen = DefaultFalseTrue.Default;
            WindowPosition = null;            
            ContentWidth = null;
            ContentHeight = null;
            TitleInfo = new TextDataBinding(true, true);
            FileRelativePath = "";
            WindowCategory = "";
            FrameName = "";
            WindowShowInTaskbar = DefaultFalseTrue.Default;
            WindowTopmost = DefaultFalseTrue.Default;
            ParentWindow = TargetWindow.RootWindow;
            AutoCloseMs = 0;
        }

        #endregion

        #region public functions

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_FileRelativePath)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        //[PropertyOrder(0)]
        public virtual string FileRelativePath { get; set; }

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_ShowOnTouchScreen)]
        [DefaultValue(DefaultFalseTrue.Default)] // For XAML serialization
        //[PropertyOrder(1)]
        public DefaultFalseTrue ShowOnTouchScreen { get; set; }

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowStyle)]
        [DefaultValue(PlayWindowStyle.Default)] // For XAML serialization
        //[PropertyOrder(2)]
        public PlayWindowStyle WindowStyle { get; set; }

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowResizeMode)]
        [DefaultValue(PlayWindowResizeMode.Default)] // For XAML serialization
        //[PropertyOrder(3)]
        public PlayWindowResizeMode WindowResizeMode { get; set; }

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowStartupLocation)]
        [DefaultValue(PlayWindowStartupLocation.Default)] // For XAML serialization
        //[PropertyOrder(4)]
        public PlayWindowStartupLocation WindowStartupLocation { get; set; }

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowFullScreen)]
        [DefaultValue(DefaultFalseTrue.Default)] // For XAML serialization
        //[PropertyOrder(5)]
        public DefaultFalseTrue WindowFullScreen { get; set; }
        
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_AutoCloseMs)]
        [LocalizedDescription(ResourceStrings.ShowWindowDsCommandOptions_AutoCloseMs_Description)]
        //[PropertyOrder(6)]
        [DefaultValue(0)] // For XAML serialization
        public int AutoCloseMs { get; set; }

        [Browsable(false)]
        [DefaultValue(null)] // For XAML serialization
        public PixelPoint? WindowPosition { get; set; }        

        [Browsable(false)]
        [DefaultValue(null)] // For XAML serialization
        public double? ContentWidth { get; set; }

        [Browsable(false)]
        [DefaultValue(null)] // For XAML serialization
        public double? ContentHeight { get; set; }

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_TitleInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(7)]
        public virtual TextDataBinding TitleInfo { get; set; }

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowCategory)]
        [LocalizedDescription(ResourceStrings.ShowWindowDsCommandOptions_WindowCategory_Description)]
        //[PropertyOrder(8)]
        public virtual string WindowCategory { get; set; }

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_FrameName)]
        [LocalizedDescription(ResourceStrings.ShowWindowDsCommandOptions_FrameName_Description)]
        //[PropertyOrder(9)]
        public virtual string FrameName { get; set; }

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowShowInTaskbar)]
        [DefaultValue(DefaultFalseTrue.Default)] // For XAML serialization
        //[PropertyOrder(10)]
        public DefaultFalseTrue WindowShowInTaskbar { get; set; }

        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowTopmost)]
        [DefaultValue(DefaultFalseTrue.Default)] // For XAML serialization
        //[PropertyOrder(11)]
        public DefaultFalseTrue WindowTopmost { get; set; }

        /// <summary>
        ///     Parent Window for new WIndow
        /// </summary>
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_ParentWindow)]
        //[PropertyOrder(12)]
        public virtual TargetWindow ParentWindow { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string ChildWindowInfo { get; private set; } = @"";

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(9))
            {
                writer.Write((int) ShowOnTouchScreen);
                writer.Write((int) WindowStyle);
                writer.Write((int) WindowResizeMode);
                writer.Write((int) WindowStartupLocation);
                writer.Write((int) WindowFullScreen);
                writer.WriteNullablePixelPoint(WindowPosition);                
                writer.WriteNullable(ContentWidth);
                writer.WriteNullable(ContentHeight);
                writer.Write(TitleInfo, context);
                writer.Write(FileRelativePath);
                writer.Write(WindowCategory);
                writer.Write(FrameName);
                writer.Write((int) WindowShowInTaskbar);
                writer.Write((int) WindowTopmost);
                writer.Write((int) ParentWindow);
                writer.Write(AutoCloseMs);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 8:
                        try
                        {
                            ShowOnTouchScreen = (DefaultFalseTrue)reader.ReadInt32();
                            WindowStyle = (PlayWindowStyle)reader.ReadInt32();
                            WindowResizeMode = (PlayWindowResizeMode)reader.ReadInt32();
                            WindowStartupLocation = (PlayWindowStartupLocation)reader.ReadInt32();
                            WindowFullScreen = (DefaultFalseTrue)reader.ReadInt32();
                            reader.ReadNullableDouble();
                            reader.ReadNullableDouble();
                            ContentWidth = reader.ReadNullableDouble();
                            ContentHeight = reader.ReadNullableDouble();
                            reader.ReadOwnedData(TitleInfo, context);
                            FileRelativePath = reader.ReadString();
                            WindowCategory = reader.ReadString();
                            FrameName = reader.ReadString();
                            WindowShowInTaskbar = (DefaultFalseTrue)reader.ReadInt32();
                            WindowTopmost = (DefaultFalseTrue)reader.ReadInt32();
                            ParentWindow = (TargetWindow)reader.ReadInt32();                            
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    case 9:
                        try
                        {
                            ShowOnTouchScreen = (DefaultFalseTrue)reader.ReadInt32();
                            WindowStyle = (PlayWindowStyle)reader.ReadInt32();
                            WindowResizeMode = (PlayWindowResizeMode)reader.ReadInt32();
                            WindowStartupLocation = (PlayWindowStartupLocation)reader.ReadInt32();
                            WindowFullScreen = (DefaultFalseTrue)reader.ReadInt32();
                            WindowPosition = reader.ReadNullablePixelPoint();                            
                            ContentWidth = reader.ReadNullableDouble();
                            ContentHeight = reader.ReadNullableDouble();
                            reader.ReadOwnedData(TitleInfo, context);
                            FileRelativePath = reader.ReadString();
                            WindowCategory = reader.ReadString();
                            FrameName = reader.ReadString();
                            WindowShowInTaskbar = (DefaultFalseTrue)reader.ReadInt32();
                            WindowTopmost = (DefaultFalseTrue)reader.ReadInt32();
                            ParentWindow = (TargetWindow)reader.ReadInt32();
                            AutoCloseMs = reader.ReadInt32();
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override string ToString()
        {
            return FileRelativePath ?? @"";
        }

        public void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(FileRelativePath,
                constants);
            TitleInfo.FindConstants(constants);

            // TODO
            //var drawing =
            //    DsProject.ReadDrawing(DsProject.Instance.GetExistingDsPageFileFullNameOrNull(FileRelativePath), false,
            //        false) as DsPageDrawing;
            //if (drawing is not null) 
            //    ConstantsHelper.FindConstants(drawing.DsConstantsCollection, constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            FileRelativePath = ConstantsHelper.ComputeValue(container,
                FileRelativePath) ?? @"";
            ItemHelper.ReplaceConstants(TitleInfo, container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
            ItemHelper.RefreshForPropertyGrid(TitleInfo, container);

            var computedValue = ConstantsHelper.ComputeValue(container, FileRelativePath);
            if (computedValue == FileRelativePath) ChildWindowInfo = FileRelativePath;
            else ChildWindowInfo = FileRelativePath + @" [" + computedValue + @"]";
        }

        #endregion
    }

    public enum PlayWindowStyle
    {
        // Summary:
        //     Only the client area is visible - the title bar and border are not shown.
        //     A Avalonia.Navigation.NavigationWindow with a Avalonia.Window.WindowStyle
        //     of Avalonia.WindowStyle.None will still display the navigation user
        //     interface (UI).
        None = 0,

        //
        // Summary:
        //     A window with a single border. This is the default value.
        SingleBorderWindow = 1,

        //
        // Summary:
        //     A window with a 3-D border.
        ThreeDBorderWindow = 2,

        //
        // Summary:
        //     A fixed tool window.
        ToolWindow = 3,
        Default = 4
    }

    public enum PlayWindowResizeMode
    {
        // Summary:
        //     A window cannot be resized. The Minimize and Maximize buttons are not displayed
        //     in the title bar.
        NoResize = 0,

        //
        // Summary:
        //     A window can only be minimized and restored. The Minimize and Maximize buttons
        //     are both shown, but only the Minimize button is enabled.
        CanMinimize = 1,

        //
        // Summary:
        //     A window can be resized. The Minimize and Maximize buttons are both shown
        //     and enabled.
        CanResize = 2,

        //
        // Summary:
        //     A window can be resized. The Minimize and Maximize buttons are both shown
        //     and enabled. A resize grip appears in the bottom-right corner of the window.
        CanResizeWithGrip = 3,
        Default = 4
    }

    public enum PlayWindowStartupLocation
    {
        Default = 0,
        Center = 1,
        LeftCenter = 2,
        UpperLeft = 3,
        UpperCenter = 4,
        UpperRight = 5,
        RightCenter = 6,
        BottomRight = 7,
        BottomCenter = 8,
        BottomLeft = 9,
        Fill = 10,
        LeftDock = 11,
        UpperDock = 12,
        RightDock = 13,
        BottomDock = 14,
        ControlLeft = 15,
        ControlTop = 16,
        ControlRight = 17,
        ControlBottom = 18,        
    }

    public enum DefaultFalseTrue
    {
        Default = 0,
        False = 1,
        True = 2
    }

    public class WindowProps : ShowWindowDsCommandOptions
    {
        #region public functions

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override string FileRelativePath
        {
            get => base.FileRelativePath;
            set => base.FileRelativePath = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override string WindowCategory
        {
            get => base.WindowCategory;
            set => base.WindowCategory = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override string FrameName
        {
            get => base.FrameName;
            set => base.FrameName = value;
        }

        public override string ToString()
        {
            return "...";
        }

        public void CombineWith(WindowProps that)
        {
            if (that.TitleInfo.IsConst && !string.IsNullOrEmpty(that.TitleInfo.ConstValue) || !that.TitleInfo.IsConst)
                TitleInfo = (TextDataBinding) that.TitleInfo.Clone();
            if (that.ShowOnTouchScreen != DefaultFalseTrue.Default) ShowOnTouchScreen = that.ShowOnTouchScreen;
            if (that.WindowStyle != PlayWindowStyle.Default) WindowStyle = that.WindowStyle;
            if (that.WindowResizeMode != PlayWindowResizeMode.Default) WindowResizeMode = that.WindowResizeMode;
            if (that.WindowStartupLocation != PlayWindowStartupLocation.Default)
                WindowStartupLocation = that.WindowStartupLocation;
            if (that.WindowFullScreen != DefaultFalseTrue.Default) WindowFullScreen = that.WindowFullScreen;
            if (that.WindowPosition.HasValue) WindowPosition = that.WindowPosition;            
            if (that.ContentWidth.HasValue) ContentWidth = that.ContentWidth;
            if (that.ContentHeight.HasValue) ContentHeight = that.ContentHeight;
            if (that.WindowShowInTaskbar != DefaultFalseTrue.Default) WindowShowInTaskbar = that.WindowShowInTaskbar;
            if (that.WindowTopmost != DefaultFalseTrue.Default) WindowTopmost = that.WindowTopmost;
            if (that.AutoCloseMs != 0) AutoCloseMs = that.AutoCloseMs;
        }

        #endregion
    }
}