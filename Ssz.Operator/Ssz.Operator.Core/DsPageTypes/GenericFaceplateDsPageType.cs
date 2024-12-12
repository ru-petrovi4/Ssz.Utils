using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsPageTypes
{
    public class GenericFaceplateDsPageType : DsPageTypeBase, IDsItem
    {
        #region construction and destruction

        public GenericFaceplateDsPageType()
        {
            ShowOnTouchScreen = DefaultFalseTrue.Default;
            WindowStyle = PlayWindowStyle.Default;
            WindowResizeMode = PlayWindowResizeMode.Default;
            WindowStartupLocation = PlayWindowStartupLocation.Default;
            WindowFullScreen = DefaultFalseTrue.Default;
            TitleInfo = new TextDataBinding(false, true) {ConstValue = @""};
            WindowCategory = @"";
            FrameName = @"";
            WindowShowInTaskbar = DefaultFalseTrue.Default;
            WindowTopmost = DefaultFalseTrue.Default;
            AutoCloseMs = 0;
        }

        #endregion

        #region public functions

        public static readonly Guid TypeGuid = new(@"EF3E6C98-99B5-4D47-B021-8BC068EABC42");

        public override Guid Guid => TypeGuid;

        public override string Name => @"Generic Faceplate";

        public override string Desc => Resources.GenericFaceplateDsPageType_Desc;

        public override bool IsFaceplate => true;

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_ShowOnTouchScreen)]
        [PropertyOrder(1)]
        [DefaultValue(DefaultFalseTrue.Default)] // For XAML serialization        
        public DefaultFalseTrue ShowOnTouchScreen { get; set; }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowStyle)]
        [PropertyOrder(2)]
        [DefaultValue(PlayWindowStyle.Default)] // For XAML serialization
        public PlayWindowStyle WindowStyle { get; set; }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowResizeMode)]
        [PropertyOrder(3)]
        [DefaultValue(PlayWindowResizeMode.Default)] // For XAML serialization
        public PlayWindowResizeMode WindowResizeMode { get; set; }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowStartupLocation)]
        [PropertyOrder(4)]
        [DefaultValue(PlayWindowStartupLocation.Default)] // For XAML serialization
        public PlayWindowStartupLocation WindowStartupLocation { get; set; }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowFullScreen)]
        [PropertyOrder(5)]
        [DefaultValue(PlayWindowResizeMode.Default)] // For XAML serialization
        public DefaultFalseTrue WindowFullScreen { get; set; }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_AutoCloseMs)]
        [LocalizedDescription(ResourceStrings.ShowWindowDsCommandOptions_AutoCloseMs_Description)]
        [PropertyOrder(6)]
        [DefaultValue(0)] // For XAML serialization
        public int AutoCloseMs { get; set; }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_TitleInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(7)]
        [DefaultValue(typeof(TextDataBinding), @"")] // For XAML serialization
        public TextDataBinding TitleInfo
        {
            get => _titleInfo;
            set
            {
                if (Equals(value, _titleInfo)) return;
                _titleInfo = value;
            }
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowCategory)]
        [LocalizedDescription(ResourceStrings.ShowWindowDsCommandOptions_WindowCategory_Description)]
        [PropertyOrder(8)]
        [DefaultValue(@"")] // For XAML serialization
        public string WindowCategory { get; set; }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_FrameName)]
        [LocalizedDescription(ResourceStrings.ShowWindowDsCommandOptions_FrameName_Description)]
        [PropertyOrder(9)]
        [DefaultValue(@"")] // For XAML serialization
        public string FrameName { get; set; }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowShowInTaskbar)]
        [PropertyOrder(10)]
        [DefaultValue(DefaultFalseTrue.Default)] // For XAML serialization
        public DefaultFalseTrue WindowShowInTaskbar { get; set; }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ShowWindowDsCommandOptions_WindowTopmost)]
        [PropertyOrder(11)]
        [DefaultValue(DefaultFalseTrue.Default)] // For XAML serialization
        public DefaultFalseTrue WindowTopmost { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // for XAML serialization
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
            using (writer.EnterBlock(4))
            {
                writer.Write(ShowOnTouchScreen != DefaultFalseTrue.Default);
                writer.Write((int) WindowStyle);
                writer.Write((int) WindowResizeMode);
                writer.Write((int) WindowStartupLocation);
                writer.Write((int) WindowFullScreen);
                writer.Write(TitleInfo, context);
                writer.Write(WindowCategory);
                writer.Write(FrameName);
                writer.Write((int) WindowShowInTaskbar);
                writer.Write((int) WindowTopmost);
                writer.Write(AutoCloseMs);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 4:
                        try
                        {
                            var showOnTouchScreen = reader.ReadBoolean();
                            if (showOnTouchScreen) ShowOnTouchScreen = DefaultFalseTrue.True;
                            else ShowOnTouchScreen = DefaultFalseTrue.Default;
                            WindowStyle = (PlayWindowStyle) reader.ReadInt32();
                            WindowResizeMode = (PlayWindowResizeMode) reader.ReadInt32();
                            WindowStartupLocation = (PlayWindowStartupLocation) reader.ReadInt32();
                            WindowFullScreen = (DefaultFalseTrue) reader.ReadInt32();
                            reader.ReadOwnedData(TitleInfo, context);
                            WindowCategory = reader.ReadString();
                            FrameName = reader.ReadString();
                            WindowShowInTaskbar = (DefaultFalseTrue) reader.ReadInt32();
                            WindowTopmost = (DefaultFalseTrue) reader.ReadInt32();
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

        public void ReplaceConstants(IDsContainer? container)
        {
            TitleInfo.ReplaceConstants(container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
            TitleInfo.RefreshForPropertyGrid(container);
        }

        public void FindConstants(HashSet<string> constants)
        {
            TitleInfo.FindConstants(constants);
        }

        #endregion

        #region private fields

        private TextDataBinding _titleInfo = null!;

        #endregion
    }
}