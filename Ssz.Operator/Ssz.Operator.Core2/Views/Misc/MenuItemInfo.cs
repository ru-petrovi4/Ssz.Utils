using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core
{
    public class MenuItemInfo : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region construction and destruction

        public MenuItemInfo()
            : this(true)
        {
        }

        public MenuItemInfo(bool visualDesignMode)
        {            
            HeaderInfo = new TextDataBinding(visualDesignMode, true);
            DsCommand = new DsCommand(visualDesignMode);
            IsVisibleInfo = new BooleanDataBinding(visualDesignMode, true) {ConstValue = true};
        }

        #endregion

        #region public functions

        [DsCategory(ResourceStrings.BasicCategory),
         DsDisplayName(ResourceStrings.MenuItemInfo_Path)]        
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.MenuItemInfoHeaderInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [DefaultValue(typeof(TextDataBinding), @"")] // For XAML serialization
        public virtual TextDataBinding HeaderInfo
        {
            get => _headerInfo;
            set
            {
                if (Equals(value, _headerInfo)) return;
                _headerInfo = value;
            }
        }


        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.MenuItemInfoDsCommand)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public virtual DsCommand DsCommand { get; set; } = null!;

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.MenuItemInfoIsVisibleInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(CheckBoxEditor), typeof(CheckBoxEditor))]
        [DefaultValue(typeof(BooleanDataBinding), "True")] // For XAML serialization
        public BooleanDataBinding IsVisibleInfo
        {
            get => _isVisibleInfo;
            set
            {
                if (Equals(value, _isVisibleInfo)) return;
                _isVisibleInfo = value;
            }
        }

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
            using (writer.EnterBlock(2))
            {
                writer.Write(Path);
                writer.Write(HeaderInfo, context);
                writer.Write(DsCommand, context);
                writer.Write(IsVisibleInfo, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            reader.ReadOwnedData(HeaderInfo, context);
                            reader.ReadOwnedData(DsCommand, context);
                            reader.ReadOwnedData(IsVisibleInfo, context);
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    case 2:
                        try
                        {
                            Path = reader.ReadString();
                            reader.ReadOwnedData(HeaderInfo, context);
                            reader.ReadOwnedData(DsCommand, context);
                            reader.ReadOwnedData(IsVisibleInfo, context);
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

        public virtual void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(Path, constants);
            HeaderInfo.FindConstants(constants);
            DsCommand.FindConstants(constants);
            IsVisibleInfo.FindConstants(constants);
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
            Path = ConstantsHelper.ComputeValue(container, Path)!;
            ItemHelper.ReplaceConstants(HeaderInfo, container);
            ItemHelper.ReplaceConstants(DsCommand, container);
            ItemHelper.ReplaceConstants(IsVisibleInfo, container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public override string ToString()
        {
            return HeaderInfo.ToString();
        }

        #endregion

        #region private fields

        private string _path = @"";
        private TextDataBinding _headerInfo = null!;
        private BooleanDataBinding _isVisibleInfo = null!;

        #endregion
    }

    public class SeparatorMenuItemInfo : MenuItemInfo
    {
        #region construction and destruction

        public SeparatorMenuItemInfo()
            : base(true)
        {
        }

        #endregion

        #region public functions

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override TextDataBinding HeaderInfo
        {
            get => base.HeaderInfo;
            set => base.HeaderInfo = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override DsCommand DsCommand
        {
            get => base.DsCommand;
            set => base.DsCommand = value;
        }

        #endregion
    }

    public class SimpleMenuItemInfo : MenuItemInfo
    {
        #region construction and destruction

        public SimpleMenuItemInfo()
            : base(true)
        {
        }

        #endregion

        #region public functions

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override DsCommand DsCommand
        {
            get => base.DsCommand;
            set => base.DsCommand = value;
        }

        #endregion
    }
}